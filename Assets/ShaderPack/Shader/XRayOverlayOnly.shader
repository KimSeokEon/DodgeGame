Shader "GameShaderPack/Effect/XRayOverlayOnly"
{
    // =============================================================
    // 원본 캐릭터 머티리얼(Player.mat)은 그대로 두고,
    // 렌더러의 두 번째 머티리얼 슬롯에 이 셰이더만 추가로 얹어서
    // "다른 물체에 가려졌을 때만" 살짝 비쳐 보이게 하는 오버레이 전용 셰이더.
    //
    // NormalSurface 패스가 없기 때문에 원본 텍스처/라이팅에는
    // 전혀 영향을 주지 않는다.
    //
    // Queue를 Transparent+1로 설정한 이유:
    // 이 프로젝트의 Player/Enemy는 URP 2D Lit 셰이더를 쓰는데
    // 이 셰이더는 ZWrite On이지만 Queue가 Transparent(3000)라서
    // 일반 Opaque 렌더(2000번대)보다 "늦게" 그려진다.
    // 이 오버레이가 Opaque 대역(Geometry+1 등)에 있으면
    // Enemy가 아직 안 그려진 시점이라 depth가 없어서
    // 가려짐 판정이 되지 않는다. 그래서 모든 Player/Enemy가
    // 다 그려진 뒤(Transparent+1)에 그려지도록 한다.
    // =============================================================

    Properties
    {
        [Header(XRay)]

        [HDR]
        _XRayColor("XRay Color", Color) = (0.0, 1.0, 1.0, 1.0)

        _XRayAlpha(
            "XRay Alpha",
            Range(0.0, 1.0)
        ) = 0.6


        [Header(Glow)]

        [Toggle]
        _GlowEnabled("Glow", Float) = 1

        [HDR]
        _GlowColor("Glow Color", Color) = (0.0, 1.0, 1.0, 1.0)

        _GlowIntensity(
            "Glow Intensity",
            Range(0.0, 10.0)
        ) = 2.0


        [Header(XRay Edge)]

        _FresnelPower(
            "Edge Power",
            Range(0.1, 10.0)
        ) = 3.0

        _FresnelIntensity(
            "Edge Intensity",
            Range(0.0, 5.0)
        ) = 1.0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
        }


        // =========================================================
        //
        // OCCLUDED X-RAY (단일 패스)
        //
        // 씬의 Opaque 오브젝트(벽 등)와, Transparent 큐를 쓰는
        // Player/Enemy 본체 메쉬가 전부 그려져서 depth를 다 써놓은
        // 뒤(Transparent+1)에 그려진다. 그래서 다른 물체(벽이든
        // 적이든)에 실제로 가려진 부분만 ZTest Greater로 걸러져서
        // 보인다.
        //
        // =========================================================

        Pass
        {
            Name "XRayOccluded"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }


            ZTest Greater

            ZWrite Off

            Cull Back


            Blend SrcAlpha OneMinusSrcAlpha


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragXRay


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };


            CBUFFER_START(UnityPerMaterial)

                float4 _XRayColor;
                float _XRayAlpha;

                float _GlowEnabled;
                float4 _GlowColor;
                float _GlowIntensity;

                float _FresnelPower;
                float _FresnelIntensity;

            CBUFFER_END


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


                return output;
            }


            half4 FragXRay(
                Varyings input
            ) : SV_Target
            {
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
                    _FresnelIntensity;


                half3 xrayColor =
                    _XRayColor.rgb;


                half3 glowColor =
                    _GlowColor.rgb *
                    fresnel *
                    _GlowIntensity *
                    _GlowEnabled;


                half3 finalColor =
                    xrayColor +
                    glowColor;


                half alpha =
                    _XRayAlpha;


                alpha =
                    saturate(
                        alpha +
                        fresnel *
                        0.25
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