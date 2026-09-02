Shader "GameShaderPack/Effect/RandomSpike"
{
    Properties
    {
        [Header(Base)]

        [MainTexture]
        _BaseMap("Base Texture", 2D) = "white" {}

        [MainColor]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)


        [Header(Spike)]

        _SpikeCount(
            "Spike Count",
            Range(1, 32)
        ) = 8

        _MinHeight(
            "Min Height",
            Range(0.0, 1.0)
        ) = 0.05

        _MaxHeight(
            "Max Height",
            Range(0.0, 2.0)
        ) = 0.3

        _SpikeWidth(
            "Spike Width",
            Range(0.01, 1.0)
        ) = 0.15


        [Header(Animation)]

        _Cycle(
            "Cycle",
            Range(0.1, 10.0)
        ) = 2.0

        _RiseSpeed(
            "Rise Speed",
            Range(0.1, 10.0)
        ) = 2.0

        _FallSpeed(
            "Fall Speed",
            Range(0.1, 10.0)
        ) = 2.0

        _Randomness(
            "Randomness",
            Range(0.0, 1.0)
        ) = 1.0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }


        Pass
        {
            Name "Forward"

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
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            #define MAX_SPIKES 32


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _SpikeCount;
                float _MinHeight;
                float _MaxHeight;
                float _SpikeWidth;

                float _Cycle;
                float _RiseSpeed;
                float _FallSpeed;
                float _Randomness;

            CBUFFER_END


            float Hash11(float value)
            {
                value = frac(value * 0.1031);
                value *= value + 33.33;
                value *= value + value;

                return frac(value);
            }


            float3 Hash31(float value)
            {
                float3 result;

                result.x =
                    Hash11(value + 17.0);

                result.y =
                    Hash11(value + 47.0);

                result.z =
                    Hash11(value + 83.0);

                return result;
            }


            float3 RandomDirection(float index)
            {
                float3 randomValue =
                    Hash31(index * 13.17);


                float z =
                    randomValue.x * 2.0 - 1.0;


                float angle =
                    randomValue.y *
                    TWO_PI;


                float radius =
                    sqrt(
                        max(
                            0.0,
                            1.0 - z * z
                        )
                    );


                return float3(
                    radius * cos(angle),
                    radius * sin(angle),
                    z
                );
            }


            float SpikeAnimation(
                float index
            )
            {
                float randomOffset =
                    Hash11(
                        index * 19.31
                    );


                float cycle =
                    max(
                        _Cycle,
                        0.001
                    );


                float timeValue =
                    _Time.y / cycle;


                float phase =
                    frac(
                        timeValue +
                        randomOffset *
                        _Randomness
                    );


                float riseDuration =
                    1.0 /
                    max(
                        _RiseSpeed,
                        0.001
                    );


                float fallDuration =
                    1.0 /
                    max(
                        _FallSpeed,
                        0.001
                    );


                float rise =
                    saturate(
                        phase /
                        max(
                            riseDuration,
                            0.001
                        )
                    );


                float fall =
                    saturate(
                        (1.0 - phase) /
                        max(
                            fallDuration,
                            0.001
                        )
                    );


                float animation =
                    min(
                        rise,
                        fall
                    );


                animation =
                    smoothstep(
                        0.0,
                        1.0,
                        animation
                    );


                return animation;
            }


            float CalculateSpikes(
                float3 positionOS,
                float3 normalOS
            )
            {
                float displacement = 0.0;


                float3 positionDirection =
                    normalize(
                        positionOS +
                        float3(
                            0.0001,
                            0.0001,
                            0.0001
                        )
                    );


                int spikeCount =
                    clamp(
                        (int)_SpikeCount,
                        1,
                        MAX_SPIKES
                    );


                [loop]
                for (
                    int i = 0;
                    i < MAX_SPIKES;
                    i++
                )
                {
                    if (i >= spikeCount)
                    {
                        break;
                    }


                    float index =
                        (float)i + 1.0;


                    float3 spikeDirection =
                        RandomDirection(
                            index
                        );


                    float alignment =
                        dot(
                            positionDirection,
                            spikeDirection
                        );


                    alignment =
                        saturate(
                            (
                                alignment -
                                (1.0 - _SpikeWidth)
                            ) /
                            max(
                                _SpikeWidth,
                                0.0001
                            )
                        );


                    float spikeShape =
                        alignment *
                        alignment;


                    float randomHeight =
                        lerp(
                            _MinHeight,
                            _MaxHeight,
                            Hash11(
                                index * 41.73
                            )
                        );


                    float animation =
                        SpikeAnimation(
                            index
                        );


                    float spike =
                        spikeShape *
                        randomHeight *
                        animation;


                    displacement =
                        max(
                            displacement,
                            spike
                        );
                }


                return displacement;
            }


            Varyings Vert(
                Attributes input
            )
            {
                Varyings output;


                float3 positionOS =
                    input.positionOS.xyz;


                float3 normalOS =
                    normalize(
                        input.normalOS
                    );


                float displacement =
                    CalculateSpikes(
                        positionOS,
                        normalOS
                    );


                positionOS +=
                    normalOS *
                    displacement;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        positionOS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


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


            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                half4 baseTexture =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 baseColor =
                    baseTexture.rgb *
                    _BaseColor.rgb;


                half3 normalWS =
                    normalize(
                        input.normalWS
                    );


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