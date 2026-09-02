Shader "GameShaderPack/Surface/Transparent"
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
        // Transparency
        // =========================================================

        [Header(Transparency)]

        _Opacity(
            "Opacity",
            Range(0.0, 1.0)
        ) = 0.5


        // =========================================================
        // Alpha Clip
        // =========================================================

        [Header(Alpha Clip)]

        [Toggle]
        _AlphaClipEnabled(
            "Alpha Clip",
            Float
        ) = 0

        _AlphaCutoff(
            "Alpha Cutoff",
            Range(0.0, 1.0)
        ) = 0.5


        // =========================================================
        // Fresnel
        // =========================================================

        [Header(Fresnel)]

        [Toggle]
        _FresnelEnabled(
            "Fresnel",
            Float
        ) = 0

        _FresnelPower(
            "Fresnel Power",
            Range(0.1, 10.0)
        ) = 3.0

        _FresnelStrength(
            "Fresnel Strength",
            Range(0.0, 1.0)
        ) = 0.5


        // =========================================================
        // Render
        // 0 = Off
        // 2 = Back
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
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }


        Pass
        {
            Name "Transparent"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            // =====================================================
            // Render State
            // =====================================================

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            ZTest LEqual

            Cull [_Cull]


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

                float3 positionWS : TEXCOORD1;

                float3 normalWS   : TEXCOORD2;
            };


            // =====================================================
            // Texture
            // =====================================================

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            // =====================================================
            // Material Properties
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _Opacity;

                float _AlphaClipEnabled;
                float _AlphaCutoff;

                float _FresnelEnabled;
                float _FresnelPower;
                float _FresnelStrength;

            CBUFFER_END


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


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

            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                // =================================================
                // Base Texture
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
                // Alpha
                //
                // Texture Alpha
                // × Color Alpha
                // × Opacity
                // =================================================

                half alpha =
                    baseTexture.a *
                    _BaseColor.a *
                    _Opacity;


                // =================================================
                // Alpha Clip
                // =================================================

                if (_AlphaClipEnabled > 0.5)
                {
                    clip(
                        alpha -
                        _AlphaCutoff
                    );
                }


                // =================================================
                // Normal / View
                // =================================================

                float3 normalWS =
                    normalize(
                        input.normalWS
                    );


                float3 viewDirectionWS =
                    normalize(
                        GetWorldSpaceViewDir(
                            input.positionWS
                        )
                    );


                // =================================================
                // Fresnel
                // =================================================

                float fresnel =
                    1.0 -
                    saturate(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );


                fresnel =
                    pow(
                        fresnel,
                        _FresnelPower
                    );


                fresnel *=
                    _FresnelStrength *
                    _FresnelEnabled;


                // 가장자리의 불투명도를 증가
                alpha =
                    saturate(
                        alpha +
                        fresnel
                    );


                // =================================================
                // Simple URP Lighting
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


                half3 ambientLight =
                    SampleSH(
                        normalWS
                    );


                half3 finalColor =
                    baseColor *
                    (
                        directLight +
                        ambientLight
                    );


                // =================================================
                // Final
                // =================================================

                return half4(
                    finalColor,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}