Shader "GameShaderPack/Effect/Hologram"
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
        _InnerColor("Inner Color", Color) = (0.05, 0.4, 0.6, 1)

        _InnerAlpha(
            "Inner Alpha",
            Range(0.0, 1.0)
        ) = 0.35


        // =========================================================
        // Electric Field / Scan Wave
        // =========================================================

        [Header(Electric Field)]

        [HDR]
        _WaveColor(
            "Wave Color",
            Color
        ) = (0.0, 1.0, 1.0, 1.0)

        _WaveAlpha(
            "Wave Alpha",
            Range(0.0, 1.0)
        ) = 0.8

        _WaveFrequency(
            "Wave Frequency",
            Range(1.0, 100.0)
        ) = 20.0

        _WaveWidth(
            "Wave Width",
            Range(0.01, 0.95)
        ) = 0.15

        _WaveSpeed(
            "Wave Speed",
            Range(0.0, 10.0)
        ) = 1.0

        [Enum(Up,0,Down,1,Right,2,Left,3)]
        _WaveDirection(
            "Wave Direction",
            Float
        ) = 0

        _WaveIntensity(
            "Wave Intensity",
            Range(0.0, 10.0)
        ) = 2.0


        // =========================================================
        // Edge
        // =========================================================

        [Header(Fresnel Edge)]

        [HDR]
        _EdgeColor(
            "Edge Color",
            Color
        ) = (0.0, 1.0, 1.0, 1.0)

        _EdgePower(
            "Edge Power",
            Range(0.1, 10.0)
        ) = 3.0

        _EdgeIntensity(
            "Edge Intensity",
            Range(0.0, 10.0)
        ) = 1.5

        _EdgeAlpha(
            "Edge Alpha",
            Range(0.0, 1.0)
        ) = 0.5
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
            Name "Hologram"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            // =====================================================
            // Transparent
            // =====================================================

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            ZTest LEqual

            // 홀로그램이므로 양면 렌더링
            Cull Off


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


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
            // Properties
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;

                float4 _InnerColor;
                float _InnerAlpha;


                float4 _WaveColor;
                float _WaveAlpha;

                float _WaveFrequency;
                float _WaveWidth;
                float _WaveSpeed;
                float _WaveDirection;
                float _WaveIntensity;


                float4 _EdgeColor;
                float _EdgePower;
                float _EdgeIntensity;
                float _EdgeAlpha;

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
            // Wave Coordinate
            // =====================================================

            float GetWaveCoordinate(
                float2 uv)
            {
                // Up
                if (_WaveDirection < 0.5)
                {
                    return uv.y;
                }

                // Down
                if (_WaveDirection < 1.5)
                {
                    return -uv.y;
                }

                // Right
                if (_WaveDirection < 2.5)
                {
                    return uv.x;
                }

                // Left
                return -uv.x;
            }


            // =====================================================
            // Wave
            // =====================================================

            float CalculateWave(float2 uv)
            {
                float coordinate =
                    GetWaveCoordinate(uv);

                // 시간에 따라 파장 이동
                float phase =
                    coordinate * _WaveFrequency
                    -
                    _Time.y * _WaveSpeed;

                // 0 ~ 1 반복
                float wave =
                    frac(phase);

                float width =
                    max(
                        _WaveWidth,
                        0.001
                    );

                // line -> scanLine
                float scanLine =
                    1.0 -
                    smoothstep(
                        0.0,
                        width,
                        wave
                    );

                return scanLine;
            }


            // =====================================================
            // Fragment
            // =====================================================

            half4 Frag(Varyings input) : SV_Target
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


                // =================================================
                // Inner Hologram
                // =================================================

                half3 innerColor =
                    baseTexture.rgb *
                    _InnerColor.rgb;


                half innerAlpha =
                    baseTexture.a *
                    _InnerAlpha;


                // =================================================
                // Electric Wave
                // =================================================

                float wave =
                    CalculateWave(
                        input.uv
                    );


                half3 waveColor =
                    _WaveColor.rgb *
                    wave *
                    _WaveIntensity;


                half waveAlpha =
                    wave *
                    _WaveAlpha;


                // =================================================
                // Fresnel Edge
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
                        _EdgePower
                    );


                half3 edgeColor =
                    _EdgeColor.rgb *
                    fresnel *
                    _EdgeIntensity;


                half edgeAlpha =
                    fresnel *
                    _EdgeAlpha;


                // =================================================
                // Final Color
                // =================================================

                half3 finalColor =
                    innerColor +
                    waveColor +
                    edgeColor;


                // =================================================
                // Final Alpha
                // =================================================

                half finalAlpha =
                    saturate(
                        innerAlpha +
                        waveAlpha +
                        edgeAlpha
                    );


                return half4(
                    finalColor,
                    finalAlpha
                );
            }


            ENDHLSL
        }
    }
}