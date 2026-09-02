Shader "GameShaderPack/Surface/Glass"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================

        [Header(Base)]

        [MainTexture]
        _BaseMap("Base Texture", 2D) = "white" {}

        [MainColor]
        _GlassColor("Glass Color", Color) = (0.8, 0.95, 1.0, 1.0)

        _Opacity(
            "Opacity",
            Range(0.0, 1.0)
        ) = 0.15


        // =========================================================
        // Surface
        // =========================================================

        [Header(Surface)]

        _Smoothness(
            "Smoothness",
            Range(0.0, 1.0)
        ) = 0.95

        _SpecularIntensity(
            "Specular Intensity",
            Range(0.0, 5.0)
        ) = 1.0


        // =========================================================
        // Fresnel
        // =========================================================

        [Header(Fresnel)]

        [Toggle]
        _FresnelEnabled(
            "Fresnel",
            Float
        ) = 1

        [HDR]
        _FresnelColor(
            "Fresnel Color",
            Color
        ) = (0.5, 0.9, 1.0, 1.0)

        _FresnelPower(
            "Fresnel Power",
            Range(0.1, 10.0)
        ) = 4.0

        _FresnelIntensity(
            "Fresnel Intensity",
            Range(0.0, 5.0)
        ) = 0.5

        _FresnelOpacity(
            "Fresnel Opacity",
            Range(0.0, 1.0)
        ) = 0.35


        // =========================================================
        // Reflection
        // =========================================================

        [Header(Reflection)]

        [Toggle]
        _ReflectionEnabled(
            "Reflection",
            Float
        ) = 1

        _ReflectionIntensity(
            "Reflection Intensity",
            Range(0.0, 2.0)
        ) = 0.5


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
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }


        Pass
        {
            Name "Glass"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            ZTest LEqual

            Cull [_Cull]


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

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
            // Material
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;

                float4 _GlassColor;
                float _Opacity;

                float _Smoothness;
                float _SpecularIntensity;

                float _FresnelEnabled;
                float4 _FresnelColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _FresnelOpacity;

                float _ReflectionEnabled;
                float _ReflectionIntensity;

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

            half4 Frag(Varyings input) : SV_Target
            {
                // -------------------------------------------------
                // Base
                // -------------------------------------------------

                half4 baseTexture =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 glassColor =
                    baseTexture.rgb *
                    _GlassColor.rgb;


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
                //
                // 카메라를 정면으로 바라보는 표면 = 0
                // 옆으로 꺾이는 표면 = 1
                // =================================================

                float NdotV =
                    saturate(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );


                float fresnel =
                    pow(
                        1.0 - NdotV,
                        _FresnelPower
                    );


                fresnel *=
                    _FresnelEnabled;


                // =================================================
                // Main Light Specular
                // =================================================

                Light mainLight =
                    GetMainLight();


                float3 lightDirectionWS =
                    normalize(
                        mainLight.direction
                    );


                float3 halfDirectionWS =
                    normalize(
                        lightDirectionWS +
                        viewDirectionWS
                    );


                float NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfDirectionWS
                        )
                    );


                // Smoothness를 Specular Power로 변환
                float specularPower =
                    lerp(
                        8.0,
                        256.0,
                        _Smoothness
                    );


                float specular =
                    pow(
                        NdotH,
                        specularPower
                    );


                specular *=
                    _SpecularIntensity;


                half3 specularColor =
                    mainLight.color *
                    specular;


                // =================================================
                // Environment Reflection
                // =================================================

                float3 reflectionDirection =
                    reflect(
                        -viewDirectionWS,
                        normalWS
                    );


                // Smoothness가 낮을수록
                // Reflection Probe의 높은 Mip 사용
                float perceptualRoughness =
                    1.0 -
                    _Smoothness;


                half3 reflectionColor =
                    GlossyEnvironmentReflection(
                        reflectionDirection,
                        input.positionWS,
                        perceptualRoughness,
                        1.0
                    );


                reflectionColor *=
                    _ReflectionIntensity *
                    _ReflectionEnabled;


                // Fresnel이 강한 영역에서
                // Reflection이 더 잘 보이도록 한다.
                reflectionColor *=
                    lerp(
                        0.25,
                        1.0,
                        fresnel
                    );


                // =================================================
                // Fresnel Color
                // =================================================

                half3 fresnelColor =
                    _FresnelColor.rgb *
                    fresnel *
                    _FresnelIntensity;


                // =================================================
                // Final Color
                // =================================================

                half3 finalColor =
                    glassColor;


                finalColor +=
                    specularColor;


                finalColor +=
                    reflectionColor;


                finalColor +=
                    fresnelColor;


                // =================================================
                // Alpha
                // =================================================

                half alpha =
                    baseTexture.a *
                    _GlassColor.a *
                    _Opacity;


                // 가장자리로 갈수록 유리가 더 진하게 보임
                alpha +=
                    fresnel *
                    _FresnelOpacity;


                alpha =
                    saturate(
                        alpha
                    );


                return half4(
                    finalColor,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}