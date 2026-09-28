// LHS_CATest 마지막 챕터(황혼의 성역) 하늘 셰이더 — "일식이 걸린 황혼 하늘".
//
// ■ 무엇을 그리나 (흰 사각 스프라이트 한 장 위에 텍스처 없이 수학으로)
//   1) 세로 그라데이션: 위(짙은 남색) → 가운데(보라) → 지평선(탁한 장밋빛)
//   2) 일식: 검은 원판 + 가장자리 한쪽이 특히 뜨겁게 타오르는 붉은/금빛 고리 + 바깥으로 번지는 후광
//   3) 플레어: 고리의 밝은 쪽을 비스듬히 가로지르는 가느다란 빛줄기(렌즈 플레어 느낌)
//   4) 구름: 노이즈(fbm)를 가로로 늘인 띠 구름. 일식 가까이는 구름 가장자리가 붉게 물듦
//   5) 별: 위쪽에만 희미하게 반짝임
//
// ■ 좌표
//   uv (0~1). _Aspect = 사각형의 가로/세로 비율 → 원이 찌그러지지 않도록 x 에 곱해 계산한다.
//   일식 위치 _EclipsePos 는 uv 기준(0~1).
//
// ■ 사용법
//   CATestSkyDome 이 이 사각형을 카메라를 따라 아주 멀리(z 800) 붙여 두므로, 일식은 "무한히 먼" 천체처럼 제자리에 보인다.
//   Light2D 영향 없음(스스로 빛나는 하늘). 불투명.
Shader "LHS_CATest/EclipseSky"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (흰색)", 2D) = "white" {}
        _Aspect ("가로/세로 비율", Float) = 1.9
        [Header(Gradient)]
        _TopColor ("위", Color) = (0.035, 0.035, 0.09, 1)
        _MidColor ("가운데", Color) = (0.14, 0.11, 0.24, 1)
        _HorizonColor ("지평선", Color) = (0.42, 0.26, 0.36, 1)
        _HorizonY ("지평선 높이(uv)", Range(0, 1)) = 0.3
        _MidY ("가운데 높이(uv)", Range(0, 1)) = 0.62
        [Header(Eclipse)]
        _EclipsePos ("일식 위치(uv)", Vector) = (0.62, 0.72, 0, 0)
        _Radius ("반지름(uv 세로 기준)", Range(0.02, 0.4)) = 0.12
        _DiskColor ("원판 색", Color) = (0.03, 0.02, 0.05, 1)
        [HDR] _RingColor ("고리 색(붉은 쪽)", Color) = (1.6, 0.28, 0.22, 1)
        [HDR] _RingHot ("고리 색(뜨거운 쪽)", Color) = (3.2, 2.0, 1.1, 1)
        _RingWidth ("고리 두께", Range(0.001, 0.05)) = 0.008
        _HotDir ("뜨거운 쪽 방향(xy)", Vector) = (0.55, -0.83, 0, 0)
        _HotPower ("뜨거운 쪽 집중도", Range(0.5, 8)) = 3
        [HDR] _GlowColor ("후광 색", Color) = (0.9, 0.25, 0.3, 1)
        _GlowSize ("후광 크기", Range(0.01, 1)) = 0.22
        _FlareStrength ("플레어 세기", Range(0, 3)) = 0.9
        _FlareAngle ("플레어 각도(도)", Range(-90, 90)) = -58
        [Header(Clouds)]
        _CloudDark ("구름 어두운 색", Color) = (0.08, 0.07, 0.14, 1)
        _CloudLit ("구름 물든 색", Color) = (0.62, 0.3, 0.36, 1)
        _CloudAmount ("구름 양", Range(0, 1.5)) = 0.8
        _CloudSpeed ("구름 흐름", Range(0, 0.05)) = 0.004
        [Header(Stars)]
        _StarAmount ("별 양", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Aspect, _HorizonY, _MidY, _Radius, _RingWidth, _HotPower, _GlowSize, _FlareStrength, _FlareAngle;
            float _CloudAmount, _CloudSpeed, _StarAmount;
            float4 _TopColor, _MidColor, _HorizonColor, _DiskColor, _RingColor, _RingHot, _GlowColor, _CloudDark, _CloudLit;
            float4 _EclipsePos, _HotDir;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            float Hash21(float2 p) { float3 p3 = frac(p.xyx * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }
            // 2D 값 노이즈 + fbm(여러 크기를 겹쳐 구름 결을 만든다)
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1, 0)), c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            float Fbm(float2 p)
            {
                float s = 0, a = 0.5;
                for (int k = 0; k < 5; k++) { s += VNoise(p) * a; p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float2 p = float2(uv.x * _Aspect, uv.y);                 // 가로세로 비율 보정 좌표
                float2 c = float2(_EclipsePos.x * _Aspect, _EclipsePos.y);
                float2 dv = p - c;
                float d = length(dv);
                float2 dir = dv / max(d, 1e-4);

                // 1) 그라데이션: 지평선 → 가운데 → 위
                float3 col = lerp(_HorizonColor.rgb, _MidColor.rgb, smoothstep(_HorizonY - 0.05, _MidY, uv.y));
                col = lerp(col, _TopColor.rgb, smoothstep(_MidY, 1.0, uv.y));
                col *= lerp(0.55, 1.0, smoothstep(0.0, _HorizonY, uv.y));  // 지평선 아래는 어둡게(먼 대지의 그늘)

                // 2) 일식 후광: 원판 밖으로 지수적으로 줄어드는 붉은 빛. 뜨거운 쪽(_HotDir)이 더 넓게 번진다.
                float hot = pow(saturate(dot(dir, normalize(_HotDir.xy)) * 0.5 + 0.5), _HotPower);
                float outside = max(d - _Radius, 0.0);
                float glow = exp(-outside / (_GlowSize * (0.45 + hot))) * step(_Radius, d);
                col += _GlowColor.rgb * glow * (0.35 + 0.65 * hot);

                // 4) 구름: 가로로 늘인 fbm. 높이 띠(지평선 위, 중간)에서만 나타남. 일식 가까이의 구름은 붉게 물든다.
                float2 cp = float2(p.x * 1.4 + t * _CloudSpeed, uv.y * 5.0);
                float n = Fbm(cp * 2.2);
                float band = smoothstep(_HorizonY - 0.08, _HorizonY + 0.12, uv.y) * smoothstep(0.9, 0.55, uv.y);
                float cloud = smoothstep(0.52, 0.78, n) * band * _CloudAmount;
                float lit = saturate(glow * 1.6 + hot * exp(-outside * 6.0));
                float3 cloudCol = lerp(_CloudDark.rgb, _CloudLit.rgb, lit);
                col = lerp(col, cloudCol, saturate(cloud));

                // 5) 별: 위쪽에만. 격자 칸마다 3% 확률로 점, 천천히 깜빡.
                float2 sp = uv * float2(_Aspect, 1.0) * 180.0;
                float2 cell = floor(sp);
                float h = Hash21(cell);
                float star = step(0.97, h) * smoothstep(0.18, 0.0, length(frac(sp) - 0.5)) * (0.6 + 0.4 * sin(t * (0.8 + h * 3.0) + h * 40.0));
                col += star * _StarAmount * smoothstep(0.55, 0.95, uv.y) * (1.0 - glow) * (1.0 - cloud);

                // 2') 일식 원판: 반지름 안쪽은 어두운 원판(가장자리만 살짝 부드럽게). 구름보다 앞(천체가 구름을 가리지 않게 순서 조정 가능)
                float disk = smoothstep(_Radius, _Radius - 0.003, d);
                col = lerp(col, _DiskColor.rgb, disk);

                // 3) 고리: 반지름 근처에서 가우스 모양으로 밝음. 뜨거운 쪽일수록 두껍고 금빛.
                float w = _RingWidth * (0.6 + 1.8 * hot);
                float ring = exp(-pow((d - _Radius) / w, 2.0));
                float3 ringCol = lerp(_RingColor.rgb, _RingHot.rgb, hot);
                col += ringCol * ring * (0.35 + 0.65 * hot);

                // 3') 플레어: 뜨거운 쪽 고리 지점에서 비스듬히 뻗는 가는 선 + 넓게 번지는 빛
                float2 hp = c + normalize(_HotDir.xy) * _Radius;           // 가장 뜨거운 고리 지점
                float ang = radians(_FlareAngle);
                float2 fd = float2(cos(ang), sin(ang));
                float2 q = p - hp;
                float along = dot(q, fd);
                float across = abs(dot(q, float2(-fd.y, fd.x)));
                float flare = exp(-across * 260.0) * exp(-abs(along) * 3.2) + exp(-length(q) * 18.0) * 0.6;
                col += _RingHot.rgb * flare * _FlareStrength * 0.35;

                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                return half4(col * i.color.rgb, a * i.color.a);
            }
            ENDHLSL
        }
    }
}
