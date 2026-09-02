Shader "GameShaderPack/Effect/OccludedOnly"
{
    Properties
    {
        // =========================================================
        // Occluded
        // =========================================================

        [Header(Occluded)]

        [HDR]
        _OccludedColor(
            "Occluded Color",
            Color
        ) = (0.0, 1.0, 1.0, 1.0)

        _Opacity(
            "Opacity",
            Range(0.0, 1.0)
        ) = 0.7


        // =========================================================
        // Glow
        // =========================================================

        [Header(Glow)]

        [Toggle]
        _GlowEnabled(
            "Glow",
            Float
        ) = 1

        [HDR]
        _GlowColor(
            "Glow Color",
            Color
        ) = (0.0, 1.0, 1.0, 1.0)

        _GlowIntensity(
            "Glow Intensity",
            Range(0.0, 10.0)
        ) = 2.0


        // =========================================================
        // Edge
        // =========================================================

        [Header(Edge)]

        [Toggle]
        _EdgeEnabled(
            "Edge Highlight",
            Float
        ) = 1

        _EdgePower(
            "Edge Power",
            Range(0.1, 10.0)
        ) = 3.0

        _EdgeIntensity(
            "Edge Intensity",
            Range(0.0, 5.0)
        ) = 1.0


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
            "RenderPipeline" = "UniversalPipeline"

            // 일반 Opaque 오브젝트들이 먼저 Depth를 기록해야 한다.
            "Queue" = "Transparent"

            "RenderType" = "Transparent"
        }


        Pass
        {
            Name "OccludedOnly"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }


            // =====================================================
            // 핵심
            //
            // 이미 Depth Buffer에 기록된 표면보다
            // 뒤에 있는 Fragment만 통과한다.
            //
            // 가려짐     → Render
            // 안 가려짐  → Reject
            // =====================================================

            ZTest Greater

            // 자신의 Depth는 기록하지 않는다.
            ZWrite Off

            Cull [_Cull]


            // =====================================================
            // Transparency
            // =====================================================

            Blend SrcAlpha OneMinusSrcAlpha


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
            };


            // =====================================================
            // Varyings
            // =====================================================

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };


            // =====================================================
            // Material
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _OccludedColor;
                float _Opacity;

                float _GlowEnabled;
                float4 _GlowColor;
                float _GlowIntensity;

                float _EdgeEnabled;
                float _EdgePower;
                float _EdgeIntensity;

            CBUFFER_END


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(
                Attributes input
            )
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
                // Normal
                // =================================================

                float3 normalWS =
                    normalize(
                        input.normalWS
                    );


                // =================================================
                // View Direction
                // =================================================

                float3 viewDirectionWS =
                    normalize(
                        GetWorldSpaceViewDir(
                            input.positionWS
                        )
                    );


                // =================================================
                // Fresnel Edge
                // =================================================

                float NdotV =
                    saturate(
                        abs(
                            dot(
                                normalWS,
                                viewDirectionWS
                            )
                        )
                    );


                float edge =
                    pow(
                        1.0 - NdotV,
                        _EdgePower
                    );


                edge *=
                    _EdgeIntensity *
                    _EdgeEnabled;


                // =================================================
                // Base Occluded Color
                // =================================================

                half3 finalColor =
                    _OccludedColor.rgb;


                // =================================================
                // Edge Highlight
                // =================================================

                finalColor +=
                    _OccludedColor.rgb *
                    edge;


                // =================================================
                // Glow
                //
                // HDR 밝기를 출력한다.
                // 실제 번짐은 URP Bloom 사용.
                // =================================================

                half glowAmount =
                    _GlowIntensity *
                    _GlowEnabled;


                finalColor +=
                    _GlowColor.rgb *
                    glowAmount;


                // 가장자리에 Glow를 추가로 강화
                finalColor +=
                    _GlowColor.rgb *
                    edge *
                    glowAmount;


                // =================================================
                // Alpha
                // =================================================

                half alpha =
                    _OccludedColor.a *
                    _Opacity;


                // Edge는 조금 더 선명하게
                alpha =
                    saturate(
                        alpha +
                        edge * 0.25
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