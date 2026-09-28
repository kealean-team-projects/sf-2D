// LHS_CATest 전용 아주 단순한 스프라이트/파티클/메쉬 셰이더.
// - 빛줄기, 발광, 안개, 파티클처럼 "조명(Light2D)의 영향을 받지 않고 스스로 보이는" 요소에 쓴다.
// - _SrcBlend/_DstBlend 로 알파 블렌드(SrcAlpha, OneMinusSrcAlpha)와 가산(SrcAlpha, One)을 같은 셰이더로 전환한다.
// - 정점 색(SpriteRenderer.color, 파티클 색, 보스 빛줄기 메쉬의 정점 알파)을 그대로 곱해 준다.
// - LightMode 태그가 없는 패스(SRPDefaultUnlit)는 URP 2D Renderer에서도 그려진다.
Shader "LHS_CATest/SpriteBlend"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
        Blend [_SrcBlend] [_DstBlend]
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
            // 이 셰이더는 일부러 SRP Batcher 를 쓰지 않는다: SRP Batcher 경로에서는 SpriteRenderer 색이 정점색이 아닌
            // unity_SpriteColor 로 전달되어 알파가 무시되기 때문.
            // (예전엔 _MainTex_ST 를 CBUFFER 에 넣어 비호환으로 만들었는데, 그러면 매 빌드마다
            //  "_TexelSize/_ST ... not supported by 2D SRP Batcher" 경고가 떴다. 이제는 _Color 를
            //  UnityPerMaterial CBUFFER 밖에 선언해 경고 없이 조용히 비호환으로 만든다.)
            half4 _Color;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                return c;
            }
            ENDHLSL
        }
    }
}
