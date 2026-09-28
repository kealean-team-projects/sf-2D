// LHS_CATest 마지막 챕터 "거울 같은 얕은 물" 셰이더.
//
// ■ 무엇을 그리나
//   사진 속 보르미르처럼 모래 평원 사이에 고인 얕은 물이 하늘을 비추는 띠.
//   1) 위(물 뒤쪽 가장자리)는 하늘 지평선 색, 아래(가까운 쪽)로 갈수록 더 어둡게 → 수면 반사 그라데이션
//   2) 가로로 긴 잔물결 줄(노이즈를 x 로 길게 늘임)이 천천히 흔들림
//   3) 일식 반사: 화면에서 일식이 떠 있는 가로 위치(전역 _TW_EclipseScreenX, 0~1 화면 좌표 — CATestSkyDome 이 매 프레임 넣음)에 세로로 길게 번지는 붉은/금빛 반사줄.
//      하늘은 카메라를 따라다니므로(무한히 먼 천체), 반사도 "화면 좌표"로 계산해야 카메라가 움직여도 일식 아래에 머문다.
//   4) 가장자리(위/아래)는 부드럽게 사라짐
//
// ■ 블렌딩: 일반 알파. Light2D 영향 없음(하늘을 비추는 면이라 스스로 빛나는 것처럼 보임).
Shader "LHS_CATest/MirrorWater"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (흰색)", 2D) = "white" {}
        _SkyColor ("비친 하늘색(먼 쪽)", Color) = (0.36, 0.25, 0.36, 1)
        _DeepColor ("가까운 쪽 색", Color) = (0.07, 0.06, 0.12, 1)
        [HDR] _StreakColor ("일식 반사 색", Color) = (2.2, 0.8, 0.55, 1)
        _StreakWidth ("반사 폭(화면 비율)", Range(0.002, 0.2)) = 0.03
        _RippleScale ("잔물결 촘촘함", Range(1, 60)) = 18
        _RippleSpeed ("잔물결 속도", Range(0, 2)) = 0.25
        _EdgeFade ("가장자리 페이드", Range(0.01, 0.5)) = 0.12
        _Alpha ("불투명도", Range(0, 1)) = 0.92
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _SkyColor, _DeepColor, _StreakColor;
            float _TW_EclipseScreenX; // 전역값: CATestSkyDome 이 매 프레임 일식의 화면 x(0~1)를 넣어 준다
            float _StreakWidth, _RippleScale, _RippleSpeed, _EdgeFade, _Alpha;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; float3 positionWS : TEXCOORD2; half4 color : COLOR; };

            float Hash11(float p) { p = frac(p * 0.1031); p *= p + 33.33; p *= p + p; return frac(p); }
            float VNoise(float x) { float i = floor(x); float f = frac(x); float u = f * f * (3.0 - 2.0 * f); return lerp(Hash11(i), Hash11(i + 1.0), u); }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float y = i.uv.y;
                // 1) 반사 그라데이션: 먼 쪽(위) 하늘색 → 가까운 쪽(아래) 어두운 색
                float3 col = lerp(_DeepColor.rgb, _SkyColor.rgb, smoothstep(0.0, 1.0, y));

                // 2) 잔물결: 줄 번호(세로 방향)마다 다른 가로 노이즈 → 끊겼다 이어지는 가로 빛줄
                float row = floor(y * _RippleScale);
                float wob = VNoise(i.positionWS.x * 0.35 + row * 13.7 + t * _RippleSpeed * (0.5 + Hash11(row)));
                float lineMask = smoothstep(0.35, 0.0, abs(frac(y * _RippleScale) - 0.5)) * smoothstep(0.55, 0.9, wob);
                col += _SkyColor.rgb * lineMask * 0.35;

                // 3) 일식 반사줄: 화면 x 가 일식 x 에 가까울수록 밝음. 잔물결로 끊기며 흔들린다.
                float sx = i.screenPos.x / i.screenPos.w;
                float jitter = (VNoise(y * 40.0 + t * 1.3) - 0.5) * _StreakWidth * 0.8;
                float streak = exp(-pow((sx - _TW_EclipseScreenX + jitter) / _StreakWidth, 2.0));
                streak *= 0.55 + 0.45 * smoothstep(0.3, 0.8, VNoise(y * 55.0 - t * 0.8 + 3.0));
                col += _StreakColor.rgb * streak * (0.4 + 0.6 * y);

                // 4) 위/아래 가장자리 페이드
                float edge = smoothstep(0.0, _EdgeFade, y) * smoothstep(1.0, 1.0 - _EdgeFade, y);
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return half4(col * i.color.rgb, edge * _Alpha * a * i.color.a);
            }
            ENDHLSL
        }
    }
}
