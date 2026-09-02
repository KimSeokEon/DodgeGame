Shader "GameShaderPack/Surface/Plane Wave"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Wave 1)]
        _WaveAmplitude1("Amplitude", Range(0.0, 2.0)) = 0.15
        _WaveFrequency1("Frequency", Range(0.0, 20.0)) = 2.0
        _WaveSpeed1("Speed", Range(-10.0, 10.0)) = 1.0
        _WaveDirection1("Direction XZ", Vector) = (1, 0, 0, 0)

        [Header(Wave 2)]
        [Toggle] _Wave2Enabled("Enable Wave 2", Float) = 1
        _WaveAmplitude2("Amplitude", Range(0.0, 2.0)) = 0.08
        _WaveFrequency2("Frequency", Range(0.0, 20.0)) = 3.0
        _WaveSpeed2("Speed", Range(-10.0, 10.0)) = 1.5
        _WaveDirection2("Direction XZ", Vector) = (0, 1, 0, 0)
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


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


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
            // Direction Normalize
            // =====================================================

            float2 NormalizeDirection(float2 direction)
            {
                float lengthSq =
                    dot(direction, direction);

                if (lengthSq < 0.000001)
                    return float2(1.0, 0.0);

                return direction * rsqrt(lengthSq);
            }


            // =====================================================
            // Wave Height
            // =====================================================

            float CalculateWave(
                float2 position,
                float2 direction,
                float amplitude,
                float frequency,
                float speed)
            {
                direction =
                    NormalizeDirection(direction);

                float phase =
                    dot(position, direction) *
                    frequency
                    +
                    _Time.y * speed;

                return
                    sin(phase) *
                    amplitude;
            }


            // =====================================================
            // Wave Derivative
            //
            // 파도의 기울기를 계산한다.
            // Normal 재계산에 사용.
            // =====================================================

            float2 CalculateWaveDerivative(
                float2 position,
                float2 direction,
                float amplitude,
                float frequency,
                float speed)
            {
                direction =
                    NormalizeDirection(direction);

                float phase =
                    dot(position, direction) *
                    frequency
                    +
                    _Time.y * speed;


                // d/dx sin(x) = cos(x)

                float derivative =
                    cos(phase) *
                    amplitude *
                    frequency;


                return
                    direction *
                    derivative;
            }


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;


                float3 positionOS =
                    input.positionOS.xyz;

                float2 planePosition =
                    positionOS.xz;


                // -------------------------------------------------
                // Wave 1
                // -------------------------------------------------

                float wave1 =
                    CalculateWave(
                        planePosition,
                        _WaveDirection1.xy,
                        _WaveAmplitude1,
                        _WaveFrequency1,
                        _WaveSpeed1
                    );


                float2 derivative1 =
                    CalculateWaveDerivative(
                        planePosition,
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
                        planePosition,
                        _WaveDirection2.xy,
                        _WaveAmplitude2,
                        _WaveFrequency2,
                        _WaveSpeed2
                    );


                float2 derivative2 =
                    CalculateWaveDerivative(
                        planePosition,
                        _WaveDirection2.xy,
                        _WaveAmplitude2,
                        _WaveFrequency2,
                        _WaveSpeed2
                    );


                wave2 *=
                    _Wave2Enabled;

                derivative2 *=
                    _Wave2Enabled;


                // -------------------------------------------------
                // Combine Waves
                // -------------------------------------------------

                float wave =
                    wave1 +
                    wave2;


                float2 derivative =
                    derivative1 +
                    derivative2;


                // -------------------------------------------------
                // Vertex Displacement
                // -------------------------------------------------

                positionOS.y +=
                    wave;


                // -------------------------------------------------
                // Recalculate Normal
                //
                // Height Field:
                //
                // y = f(x,z)
                //
                // Normal:
                //
                // (-df/dx, 1, -df/dz)
                // -------------------------------------------------

                float3 normalOS =
                    normalize(
                        float3(
                            -derivative.x,
                            1.0,
                            -derivative.y
                        )
                    );


                // -------------------------------------------------
                // Transform
                // -------------------------------------------------

                output.positionCS =
                    TransformObjectToHClip(
                        positionOS
                    );


                output.normalWS =
                    TransformObjectToWorldNormal(
                        normalOS
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
                half4 texColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 baseColor =
                    texColor.rgb *
                    _BaseColor.rgb;


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