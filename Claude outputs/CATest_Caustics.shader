// LHS_CATest 전용 "물빛 일렁임(코스틱)" 셰이더 — 심해 위쪽 벽/바닥에 비치는 그물 모양 빛.
//
// ■ 원리
//   물 표면의 물결이 햇빛을 모았다 흩었다 하면서 바닥에 밝은 그물 무늬가 생긴다(코스틱).
//   여기서는 텍스처 없이 "보로노이(세포) 노이즈"로 흉내 낸다:
//     - 평면에 무작위 점(세포 중심)을 뿌리고, 각 픽셀에서 가장 가까운 점과 두 번째로 가까운 점까지 거리의 차이(F2-F1)를 구하면
//       세포 경계에서 0 에 가까워진다 → 1 - smoothstep 으로 경계를 밝은 선으로 만든다.
//     - 세포 중심을 시간에 따라 원을 그리며 움직이면 그물이 살아 있는 것처럼 일렁인다.
//     - 크기가 다른 두 겹을 곱해 더 복잡한 무늬를 만든다.
//   월드 좌표로 계산 → 스프라이트를 아무리 늘려도 무늬 크기가 일정하고, 여러 장을 이어 붙여도 이음새가 없다.
//
// ■ 모양
//   uv.y 위(1)에서 가장 밝고 아래(0)로 갈수록 사라짐 → 수면 가까운 곳에서만 보이는 빛.
//   uv.x 양 끝도 부드럽게 사라짐.
//
// ■ 블렌딩: 가산(One One). SpriteRenderer.color 의 RGB = 빛 색, A = 세기.
Shader "LHS_CATest/Caustics2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (흰색)", 2D) = "white" {}
        _Scale ("무늬 크기(1m당 세포 수)", Range(0.05, 3)) = 0.45
        _Speed ("일렁이는 속도", Range(0, 3)) = 0.6
        _Sharp ("선 굵기(작을수록 가늘고 또렷)", Range(0.02, 0.6)) = 0.18
        _Intensity ("세기", Range(0, 4)) = 1
        _TopFade ("위쪽 페이드", Range(0.01, 1)) = 0.08
        _BottomFade ("아래로 사라지는 범위", Range(0.05, 1)) = 0.85
        _SideFade ("좌우 페이드", Range(0.01, 0.5)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Scale, _Speed, _Sharp, _Intensity, _TopFade, _BottomFade, _SideFade;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; half4 color : COLOR; };

            float2 Hash22(float2 p) {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // 보로노이 F2-F1: 세포 경계에서 0
            float Voronoi(float2 p, float t) {
                float2 cell = floor(p);
                float2 f = frac(p);
                float d1 = 8.0, d2 = 8.0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++) {
                    float2 g = float2(x, y);
                    float2 h = Hash22(cell + g);
                    // 세포 중심이 제자리에서 작은 원을 그리며 움직임
                    float2 o = 0.5 + 0.4 * sin(t + 6.2831 * h);
                    float d = length(g + o - f);
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) { d2 = d; }
                }
                return d2 - d1;
            }

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                float t = _Time.y * _Speed;
                float2 p = i.positionWS.xy * _Scale;
                float a = Voronoi(p, t);
                float b = Voronoi(p * 1.9 + 3.7, t * 1.3 + 1.7);
                float lineA = 1.0 - smoothstep(0.0, _Sharp, a);
                float lineB = 1.0 - smoothstep(0.0, _Sharp * 1.3, b);
                float net = saturate(lineA * 0.75 + lineA * lineB * 1.2);
                // 모양: 위에서 밝고 아래로 사라짐, 좌우 가장자리 페이드
                float top = smoothstep(1.0, 1.0 - _TopFade, i.uv.y);
                float bottom = smoothstep(1.0 - _BottomFade, 1.0, i.uv.y);
                float side = smoothstep(0.0, _SideFade, i.uv.x) * smoothstep(1.0, 1.0 - _SideFade, i.uv.x);
                float k = net * top * bottom * side * _Intensity * i.color.a;
                return half4(i.color.rgb * k, 0);
            }
            ENDHLSL
        }
    }
}
