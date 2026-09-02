Shader "GameShaderPack/Mask/Stencil Cutout"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================

        [Header(Base)]

        [MainTexture]
        _BaseMap(
            "Base Texture",
            2D
        ) = "white" {}

        [MainColor]
        _BaseColor(
            "Base Color",
            Color
        ) = (1, 1, 1, 1)


        // =========================================================
        // Stencil
        // =========================================================

        [Header(Stencil)]

        _StencilRef(
            "Stencil Reference",
            Range(1, 255)
        ) = 1


        // =========================================================
        // Render
        // =========================================================

        [Header(Render)]

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull(
            "Cull Mode",
            Float
        ) = 2
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }


        Pass
        {
            Name "StencilCutout"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            Cull [_Cull]

            ZWrite On
            ZTest LEqual


            // =====================================================
            // Stencil
            //
            // Mask가 없는 곳에서만 렌더링한다.
            //
            // Stencil != _StencilRef
            //      → Render
            //
            // Stencil == _StencilRef
            //      → Reject
            // =====================================================

            Stencil
            {
                Ref [_StencilRef]

                Comp NotEqual

                Pass Keep
            }


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // =====================================================
            // Attributes
            // =====================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };


            // =====================================================
            // Varyings
            // =====================================================

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv         : TEXCOORD0;

                float3 normalWS   : TEXCOORD1;
            };


            // =====================================================
            // Texture
            // =====================================================

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            // =====================================================
            // Material
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

            CBUFFER_END


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                return output;
            }


            // =====================================================
            // Fragment
            // =====================================================

            half4 Frag(Varyings input) : SV_Target
            {
                // =================================================
                // Texture
                // =================================================

                half4 baseTexture =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 baseColor =
                    baseTexture.rgb *
                    _BaseColor.rgb;


                // =================================================
                // Normal
                // =================================================

                half3 normalWS =
                    normalize(
                        input.normalWS
                    );


                // =================================================
                // Main Light
                // =================================================

                Light mainLight =
                    GetMainLight();


                half NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                half3 directLight =
                    mainLight.color *
                    NdotL;


                // =================================================
                // Ambient
                // =================================================

                half3 ambientLight =
                    SampleSH(
                        normalWS
                    );


                // =================================================
                // Final
                // =================================================

                half3 finalColor =
                    baseColor *
                    (
                        directLight +
                        ambientLight
                    );


                return half4(
                    finalColor,
                    baseTexture.a *
                    _BaseColor.a
                );
            }


            ENDHLSL
        }
    }
}