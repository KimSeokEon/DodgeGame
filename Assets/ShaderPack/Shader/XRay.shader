Shader "GameShaderPack/Effect/XRay"
{
    Properties
    {
        [Header(Base)]
        _BaseMap("Base Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Surface)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(XRay)]
        _XRayColor("XRay Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _XRayAlpha("XRay Alpha", Range(0.0, 1.0)) = 0.6

        [Header(Glow)]
        [Toggle]
        _GlowEnabled("Glow", Float) = 1.0
        _GlowColor("Glow Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _GlowIntensity("Glow Intensity", Range(0.0, 10.0)) = 2.0

        [Header(XRay Edge)]
        _FresnelPower("Edge Power", Range(0.1, 10.0)) = 3.0
        _FresnelIntensity("Edge Intensity", Range(0.0, 5.0)) = 1.0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }


        // =========================================================
        // PASS 1 : X-RAY
        //
        // 다른 오브젝트 뒤에 가려진 부분만 렌더링
        // =========================================================

        Pass
        {
            Name "XRayOccluded"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Back

            ZWrite Off

            // 이미 Depth에 기록된 다른 물체보다
            // 뒤에 있는 픽셀만 통과
            ZTest Greater

            Blend SrcAlpha OneMinusSrcAlpha


            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex VertXRay
            #pragma fragment FragXRay


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


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
            // Vertex Output
            // =====================================================

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;

                float2 uv : TEXCOORD2;
            };


            // =====================================================
            // Material
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _XRayColor;
                float _XRayAlpha;

                float _GlowEnabled;
                float4 _GlowColor;
                float _GlowIntensity;

                float _FresnelPower;
                float _FresnelIntensity;

            CBUFFER_END


            // =====================================================
            // Vertex
            // =====================================================

            Varyings VertXRay(Attributes input)
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
                    input.uv;

                return output;
            }


            // =====================================================
            // Fragment
            // =====================================================

            half4 FragXRay(Varyings input) : SV_Target
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


                // -------------------------------------------------
                // Fresnel
                // -------------------------------------------------

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


                // -------------------------------------------------
                // X-Ray Color
                // -------------------------------------------------

                half3 xrayColor =
                    _XRayColor.rgb;


                // -------------------------------------------------
                // Edge Glow
                // -------------------------------------------------

                half3 glowColor =
                    _GlowColor.rgb
                    *
                    fresnel
                    *
                    _GlowIntensity
                    *
                    _GlowEnabled;


                half3 finalColor =
                    xrayColor
                    +
                    glowColor;


                // -------------------------------------------------
                // Alpha
                // -------------------------------------------------

                half alpha =
                    _XRayAlpha;

                alpha +=
                    fresnel * 0.25h;

                alpha =
                    saturate(alpha);


                return half4(
                    finalColor,
                    alpha
                );
            }

            ENDHLSL
        }


        // =========================================================
        // PASS 2 : NORMAL LIT
        //
        // 가려지지 않은 부분은 일반적인 URP PBR 조명
        // =========================================================

        Pass
        {
            Name "NormalSurface"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back

            ZWrite On

            ZTest LEqual


            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex VertNormalLit
            #pragma fragment FragNormalLit


            // -----------------------------------------------------
            // Main Light
            // -----------------------------------------------------

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN


            // -----------------------------------------------------
            // Additional Lights
            // -----------------------------------------------------

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHTS


            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT


            // -----------------------------------------------------
            // Fog
            // -----------------------------------------------------

            #pragma multi_compile_fog


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // =====================================================
            // Attributes
            // =====================================================

            struct NormalLitAttributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float2 uv : TEXCOORD0;
            };


            // =====================================================
            // Varyings
            // =====================================================

            struct NormalLitVaryings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                float3 positionWS : TEXCOORD1;

                half3 normalWS : TEXCOORD2;

                half fogFactor : TEXCOORD3;
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

                half4 _BaseColor;

                half _Metallic;

                half _Smoothness;

                half _OcclusionStrength;

            CBUFFER_END


            // =====================================================
            // Vertex
            // =====================================================

            NormalLitVaryings VertNormalLit(
                NormalLitAttributes input
            )
            {
                NormalLitVaryings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


                output.normalWS =
                    NormalizeNormalPerVertex(
                        normalInputs.normalWS
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                output.fogFactor =
                    ComputeFogFactor(
                        positionInputs.positionCS.z
                    );


                return output;
            }


            // =====================================================
            // Fragment
            // =====================================================

            half4 FragNormalLit(
                NormalLitVaryings input
            ) : SV_Target
            {
                // -------------------------------------------------
                // Base Texture
                // -------------------------------------------------

                half4 baseTexture =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                // -------------------------------------------------
                // Normal
                // -------------------------------------------------

                half3 normalWS =
                    NormalizeNormalPerPixel(
                        input.normalWS
                    );


                // -------------------------------------------------
                // InputData
                // -------------------------------------------------

                InputData inputData =
                    (InputData)0;


                inputData.positionWS =
                    input.positionWS;


                inputData.normalWS =
                    normalWS;


                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        input.positionWS
                    );


                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );


                inputData.fogCoord =
                    input.fogFactor;


                inputData.vertexLighting =
                    VertexLighting(
                        input.positionWS,
                        normalWS
                    );


                inputData.bakedGI =
                    SampleSH(
                        normalWS
                    );


                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );


                inputData.shadowMask =
                    half4(
                        1.0h,
                        1.0h,
                        1.0h,
                        1.0h
                    );


                // -------------------------------------------------
                // SurfaceData
                // -------------------------------------------------

                SurfaceData surfaceData =
                    (SurfaceData)0;


                surfaceData.albedo =
                    baseTexture.rgb
                    *
                    _BaseColor.rgb;


                surfaceData.metallic =
                    _Metallic;


                surfaceData.specular =
                    half3(
                        0.5h,
                        0.5h,
                        0.5h
                    );


                surfaceData.smoothness =
                    _Smoothness;


                surfaceData.normalTS =
                    half3(
                        0.0h,
                        0.0h,
                        1.0h
                    );


                surfaceData.occlusion =
                    _OcclusionStrength;


                surfaceData.emission =
                    half3(
                        0.0h,
                        0.0h,
                        0.0h
                    );


                surfaceData.alpha =
                    baseTexture.a
                    *
                    _BaseColor.a;


                // -------------------------------------------------
                // URP PBR
                // -------------------------------------------------

                half4 finalColor =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );


                // -------------------------------------------------
                // Fog
                // -------------------------------------------------

                finalColor.rgb =
                    MixFog(
                        finalColor.rgb,
                        inputData.fogCoord
                    );


                return finalColor;
            }

            ENDHLSL
        }

        // =========================================================
        // PASS 3 : SHADOW CASTER
        //
        // 이 오브젝트가 메인/추가 광원의 Shadow Map에 기록되어
        // 다른 오브젝트 위에 그림자를 투영할 수 있도록 한다.
        // =========================================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex VertShadowCaster
            #pragma fragment FragShadowCaster

            // Point / Spot Light shadow 지원
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };


            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };


            float3 _LightDirection;
            float3 _LightPosition;


            float4 GetShadowPositionHClip(
                ShadowAttributes input
            )
            {
                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );

                float3 lightDirectionWS =
                    _LightDirection;

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDirectionWS =
                        normalize(
                            _LightPosition - positionWS
                        );
                #endif

                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionWS,
                            normalWS,
                            lightDirectionWS
                        )
                    );

                // Shadow pancaking
                #if UNITY_REVERSED_Z
                    positionCS.z =
                        min(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );
                #else
                    positionCS.z =
                        max(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );
                #endif

                return positionCS;
            }


            ShadowVaryings VertShadowCaster(
                ShadowAttributes input
            )
            {
                ShadowVaryings output;

                output.positionCS =
                    GetShadowPositionHClip(input);

                return output;
            }


            half4 FragShadowCaster(
                ShadowVaryings input
            ) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

    }
}