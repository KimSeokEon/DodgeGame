Shader "GameShaderPack/Surface/Surface Wave"
{
    Properties
    {
        // =========================================================
        // Base
        // =========================================================

        [Header(Base)]
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)


        // =========================================================
        // Wave 1
        // =========================================================

        [Header(Wave 1)]

        _WaveAmplitude1(
            "Amplitude",
            Range(0.0, 2.0)
        ) = 0.1

        _WaveFrequency1(
            "Frequency",
            Range(0.0, 20.0)
        ) = 2.0

        _WaveSpeed1(
            "Speed",
            Range(-10.0, 10.0)
        ) = 1.0

        _WaveDirection1(
            "Direction (XZ)",
            Vector
        ) = (1, 0, 0, 0)


        // =========================================================
        // Wave 2
        // =========================================================

        [Header(Wave 2)]

        [Toggle]
        _Wave2Enabled(
            "Enable Wave 2",
            Float
        ) = 1

        _WaveAmplitude2(
            "Amplitude",
            Range(0.0, 2.0)
        ) = 0.05

        _WaveFrequency2(
            "Frequency",
            Range(0.0, 20.0)
        ) = 3.0

        _WaveSpeed2(
            "Speed",
            Range(-10.0, 10.0)
        ) = 1.5

        _WaveDirection2(
            "Direction (XZ)",
            Vector
        ) = (0, 1, 0, 0)
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
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // =====================================================
            // Vertex Input
            // =====================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };


            // =====================================================
            // Vertex -> Fragment
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
            // Material Properties
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;


                float _WaveAmplitude1;
                float _WaveFrequency1;
                float _WaveSpeed1;
                float4 _WaveDirection1;


                float _Wave2Enabled;

                float _WaveAmplitude2;
                float _WaveFrequency2;
                float _WaveSpeed2;
                float4 _WaveDirection2;

            CBUFFER_END


            // =====================================================
            // Safe Normalize
            // =====================================================

            float2 SafeNormalize(float2 value)
            {
                float lengthSq =
                    dot(value, value);

                if (lengthSq < 0.000001)
                {
                    return float2(
                        1.0,
                        0.0
                    );
                }

                return value *
                    rsqrt(lengthSq);
            }


            // =====================================================
            // Wave Calculation
            // =====================================================

            float CalculateWave(
                float2 position,
                float2 direction,
                float amplitude,
                float frequency,
                float speed
            )
            {
                direction =
                    SafeNormalize(direction);

                float wavePosition =
                    dot(
                        position,
                        direction
                    );

                float phase =
                    wavePosition *
                    frequency +
                    _Time.y *
                    speed;

                return
                    sin(phase) *
                    amplitude;
            }


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;


                // -------------------------------------------------
                // Original Position
                // -------------------------------------------------

                float3 positionOS =
                    input.positionOS.xyz;


                // -------------------------------------------------
                // Wave 1
                // -------------------------------------------------

                float wave1 =
                    CalculateWave(
                        positionOS.xz,
                        _WaveDirection1.xy,
                        _WaveAmplitude1,
                        _WaveFrequency1,
                        _WaveSpeed1
                    );


                // -------------------------------------------------
                // Wave 2
                // -------------------------------------------------

                float wave2 =
                    CalculateWave(
                        positionOS.xz,
                        _WaveDirection2.xy,
                        _WaveAmplitude2,
                        _WaveFrequency2,
                        _WaveSpeed2
                    );

                wave2 *=
                    _Wave2Enabled;


                // -------------------------------------------------
                // Combine
                // -------------------------------------------------

                float wave =
                    wave1 +
                    wave2;


                // -------------------------------------------------
                // Vertex Displacement
                //
                // Object Space Y축으로 표면을 변형한다.
                // -------------------------------------------------

                positionOS.y +=
                    wave;


                // -------------------------------------------------
                // Transform
                // -------------------------------------------------

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        positionOS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                // 현재 버전에서는 원래 Mesh Normal 사용
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
                // Texture
                // -------------------------------------------------

                half4 texColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 baseColor =
                    texColor.rgb *
                    _BaseColor.rgb;


                // -------------------------------------------------
                // Normal
                // -------------------------------------------------

                half3 normalWS =
                    normalize(
                        input.normalWS
                    );


                // -------------------------------------------------
                // Main Light
                // -------------------------------------------------

                Light mainLight =
                    GetMainLight();


                half NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                half3 directLighting =
                    mainLight.color *
                    NdotL;


                // -------------------------------------------------
                // Ambient
                // -------------------------------------------------

                half3 ambientLighting =
                    SampleSH(
                        normalWS
                    );


                // -------------------------------------------------
                // Final
                // -------------------------------------------------

                half3 finalColor =
                    baseColor *
                    (
                        directLighting +
                        ambientLighting
                    );


                return half4(
                    finalColor,
                    texColor.a *
                    _BaseColor.a
                );
            }


            ENDHLSL
        }
    }
}