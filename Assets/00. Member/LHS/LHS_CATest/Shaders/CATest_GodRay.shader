// LHS_CATest 전용 "햇살(God Ray)" 셰이더.
//
// ■ 무엇을 그리나
//   흰색 사각 스프라이트(CATest_White) 한 장 위에, 텍스처 없이 수학(절차적)으로 빛줄기를 그린다.
//   - 위(광원 쪽)는 좁고 아래로 갈수록 퍼지는 부채꼴 빛기둥
//   - 기둥 안에 밝기가 다른 여러 가닥(streak) → 천천히 옆으로 흐르며 일렁임
//   - 가닥마다 길이가 달라 끝이 들쭉날쭉하게 사라짐 (균일한 막대처럼 보이지 않게)
//   - 빛 속에서만 보이는 먼지 입자가 반짝이며 떠다님
//   - 전체 밝기가 아주 느리게 숨쉬듯 흔들림(구름이 해를 스치는 느낌)
//
// ■ 좌표 규칙
//   uv.x: 0(왼쪽) ~ 1(오른쪽), uv.y: 0(아래, 빛이 닿는 바닥 쪽) ~ 1(위, 광원 쪽)
//   → 스프라이트를 회전시키면 빛의 각도가 바뀐다(빌더에서 모든 햇살을 같은 각도로 기울여 "해가 한 곳"에 있게 함).
//
// ■ 블렌딩
//   가산(Blend One One): 아래 그림에 빛을 "더한다". 어두운 곳에서는 뚜렷하고 밝은 곳에서는 은은하다.
//   SpriteRenderer.color 의 RGB = 빛 색, A = 전체 세기로 쓴다(정점 색으로 들어옴).
//
// ■ SRP Batcher
//   개별 햇살마다 MaterialPropertyBlock(_Seed 등)을 쓰므로 어차피 SRP Batcher 대상이 아니다.
//   정점 색을 확실히 받기 위해 CBUFFER(UnityPerMaterial) 없이 선언해 조용히 비호환으로 둔다.
//
// ■ URP 2D Renderer
//   LightMode 태그 없는 패스(SRPDefaultUnlit)라 2D Renderer 에서도 그려지고, Light2D 영향은 받지 않는다(스스로 빛나는 요소).
Shader "LHS_CATest/GodRay2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (흰색)", 2D) = "white" {}
        [HDR] _Color ("빛 색", Color) = (1, 0.95, 0.8, 1)
        _Intensity ("세기", Range(0, 4)) = 1
        _TopWidth ("광원 쪽 폭 비율 (작을수록 부채꼴)", Range(0.05, 1)) = 0.35
        _EdgeSoft ("가장자리 부드러움", Range(0.05, 1)) = 0.6
        _RayCount ("가닥 촘촘함", Range(1, 20)) = 7
        _RaySharp ("가닥 대비", Range(0.5, 6)) = 2.2
        _RayMin ("가닥 사이 최소 밝기", Range(0, 1)) = 0.2
        _RaySpeed ("가닥 흐름 속도", Range(0, 0.5)) = 0.06
        _LenVar ("가닥 길이 차이", Range(0, 1)) = 0.55
        _TopFade ("위쪽 페이드", Range(0.01, 0.5)) = 0.12
        _BottomFade ("아래쪽 페이드", Range(0.01, 1)) = 0.45
        _Core ("중심 코어 밝기", Range(0, 2)) = 0.45
        _DustAmount ("먼지 양", Range(0, 2)) = 0.7
        _DustScale ("먼지 칸 크기(1m당 칸 수)", Range(0.3, 6)) = 1.4
        _DustSpeed ("먼지 속도", Range(0, 2)) = 0.35
        _Flicker ("일렁임", Range(0, 0.6)) = 0.15
        _Seed ("시드(햇살마다 다르게)", Float) = 0
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
            half4 _Color;
            float _Intensity, _TopWidth, _EdgeSoft, _RayCount, _RaySharp, _RayMin, _RaySpeed, _LenVar;
            float _TopFade, _BottomFade, _Core, _DustAmount, _DustScale, _DustSpeed, _Flicker, _Seed;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1; // 먼지를 월드 좌표로 찍어야 스프라이트를 늘려도 동그랗게 보인다
                half4 color : COLOR;
            };

            // ── 해시/노이즈 (텍스처 없이 의사난수) ──
            float Hash11(float p) { p = frac(p * 0.1031); p *= p + 33.33; p *= p + p; return frac(p); }
            float Hash21(float2 p) { float3 p3 = frac(p.xyx * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }
            // 1D 값 노이즈: 정수 격자점마다 난수, 사이를 부드럽게(smoothstep 곡선) 보간 → 매끈한 가닥 모양
            float VNoise(float x) { float i = floor(x); float f = frac(x); float u = f * f * (3.0 - 2.0 * f); return lerp(Hash11(i), Hash11(i + 1.0), u); }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float y = i.uv.y;

                // 1) 부채꼴: 높이에 따라 폭을 줄여 x 를 정규화. 위(y=1)에선 _TopWidth 만큼만, 아래(y=0)에선 전체 폭.
                //    이렇게 나눈 x 로 가닥을 만들면 가닥들이 위쪽 한 점에서 "방사형으로" 퍼져 나온다.
                float spread = lerp(1.0, _TopWidth, y);
                float x = (i.uv.x - 0.5) / spread;                    // 기둥 안: -0.5 ~ 0.5
                float edge = 1.0 - smoothstep(0.5 - _EdgeSoft * 0.5, 0.5, abs(x)); // 양옆을 부드럽게

                // 2) 가닥: 서로 다른 주파수/속도의 노이즈 두 개를 섞고, 대비를 올려 선명한 줄로 만든다.
                float r = x * _RayCount + _Seed * 7.13;
                float n = VNoise(r + t * _RaySpeed) * 0.65 + VNoise(r * 2.3 - t * _RaySpeed * 1.7 + 13.1) * 0.35;
                float rays = lerp(_RayMin, 1.0, pow(saturate(n), _RaySharp));

                // 3) 가닥 길이: 가닥마다(느린 노이즈) 시작 높이가 달라 끝이 들쭉날쭉.
                float len = lerp(1.0 - _LenVar, 1.0, VNoise(r * 0.8 + 5.7 + _Seed));
                float lenMask = smoothstep(1.0 - len - 0.18, 1.0 - len + 0.22, y);

                // 4) 세로 페이드: 광원 쪽 끝(위)은 살짝, 바닥 쪽(아래)은 길게 사라짐. 위쪽이 조금 더 밝게.
                float along = smoothstep(1.0, 1.0 - _TopFade, y) * smoothstep(0.0, _BottomFade, y) * (0.55 + 0.45 * y);

                // 5) 중심 코어: 광원 가까운 중앙이 조금 더 뜨겁게 → 블룸이 여기서 살짝 번진다
                float core = exp(-x * x * 18.0) * y * y * _Core;

                // 6) 먼지: 월드 좌표 격자 한 칸에 하나씩(20% 확률) 점을 찍고, 칸 안에서 흔들리고 깜빡이게.
                float2 dp = i.positionWS.xy * _DustScale + float2(t * _DustSpeed * 0.4, -t * _DustSpeed) + _Seed * 3.1;
                float2 cell = floor(dp);
                float2 f = frac(dp);
                float h = Hash21(cell);
                float2 pos = float2(Hash21(cell + 17.1), Hash21(cell + 41.7)) * 0.6 + 0.2;
                pos += 0.12 * float2(sin(t * 0.7 + h * 20.0), cos(t * 0.9 + h * 31.0));
                float size = lerp(0.035, 0.09, Hash21(cell + 3.3));
                float dust = smoothstep(size, 0.0, length(f - pos)) * step(0.8, h) * (0.55 + 0.45 * sin(t * (1.5 + h * 3.0) + h * 50.0));

                // 7) 일렁임: 서로 다른 두 사인파 → 반복이 잘 안 느껴지는 느린 밝기 변화
                float flick = 1.0 + (sin(t * 0.9 + _Seed * 2.0) * 0.6 + sin(t * 2.3 + _Seed * 5.0) * 0.4) * _Flicker;

                float beam = edge * along * (rays * lenMask + core);
                float motes = dust * _DustAmount * edge * smoothstep(0.0, 0.35, y) * smoothstep(1.0, 0.8, y);
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a; // 스프라이트 알파(흰 사각형이면 1)
                float3 col = _Color.rgb * i.color.rgb * (beam + motes) * _Intensity * flick * i.color.a * a;
                return half4(col, 0);
            }
            ENDHLSL
        }
    }
}
