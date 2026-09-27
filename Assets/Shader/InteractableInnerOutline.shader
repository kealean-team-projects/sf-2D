Shader "Interaction/Sprite Inner Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (Texture Pixels)", Range(0,4)) = 1
        [HideInInspector] _SpriteUVRect ("Sprite UV Bounds", Vector) = (0,0,1,1)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float4 _SpriteUVRect;
                float _OutlineWidth;
            CBUFFER_END

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half AlphaAt(float2 uv)
            {
                float2 inside = step(_SpriteUVRect.xy, uv) * step(uv, _SpriteUVRect.zw);
                float2 halfTexel = abs(_MainTex_TexelSize.xy) * 0.5;
                float2 safeUV = clamp(uv, _SpriteUVRect.xy + halfTexel, _SpriteUVRect.zw - halfTexel);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, safeUV).a * inside.x * inside.y;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half neighbourAlpha = source.a;
                float2 texel = abs(_MainTex_TexelSize.xy);
                // Check every step so thick outlines do not skip narrow transparent gaps.
                [unroll] for (int i = 1; i <= 4; i++)
                {
                    if (i > _OutlineWidth) break;
                    float2 d = texel * i;
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv + float2(d.x, 0)));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv - float2(d.x, 0)));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv + float2(0, d.y)));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv - float2(0, d.y)));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv + d));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv - d));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv + float2(d.x, -d.y)));
                    neighbourAlpha = min(neighbourAlpha, AlphaAt(input.uv + float2(-d.x, d.y)));
                }
                half edge = saturate((source.a - neighbourAlpha) / max(source.a, 0.0001h));
                half3 rgb = lerp(source.rgb * input.color.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return half4(rgb, source.a * input.color.a);
            }
            ENDHLSL
        }
    }
}