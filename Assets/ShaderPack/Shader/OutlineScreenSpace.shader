Shader "GameShaderPack/OutlineScreenSpace"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Surface)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        // ���� Object Space ������ �ƴ϶� Pixel ����
        _OutlineWidth("Outline Width (Pixels)", Range(0.0, 20.0)) = 2.0

        [Header(Glow)]
        [Toggle(_OUTLINE_GLOW)] _OutlineGlow("Outline Glow", Float) = 0
        [HDR] _OutlineGlowColor("Glow Color", Color) = (1, 1, 1, 1)
        _OutlineGlowIntensity("Glow Intensity", Range(0.0, 10.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // =========================================================
        // Base Pass
        // =========================================================

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VertLit
            #pragma fragment FragLit

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct LitAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct LitVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS    : TEXCOORD2;
                half fogFactor    : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _OcclusionStrength;
            CBUFFER_END

            LitVaryings VertLit(LitAttributes input)
            {
                LitVaryings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = NormalizeNormalPerVertex(nrm.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 FragLit(LitVaryings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, normalWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = tex.rgb * _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.5h, 0.5h, 0.5h);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = 0;
                surfaceData.occlusion = _OcclusionStrength;
                surfaceData.alpha = tex.a * _BaseColor.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }


        // =========================================================
        // Outline Pass
        // =========================================================

        Pass
        {
            Name "Outline"

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            #pragma shader_feature_local _OUTLINE_GLOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)

                float4 _OutlineColor;
                float _OutlineWidth;

                float4 _OutlineGlowColor;
                float _OutlineGlowIntensity;

            CBUFFER_END


            Varyings OutlineVert(Attributes input)
            {
                Varyings output;


                // =================================================
                // 1. Vertex�� ���� ��ġ
                // =================================================

                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );

                float4 positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                // =================================================
                // 2. Normal�� World Space�� ��ȯ
                // =================================================

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );

                normalWS =
                    normalize(normalWS);


                // =================================================
                // 3. Normal ������ ���� Point ����
                //
                // Vertex
                //
                //     �ܦ������������� Normal Point
                //
                // �� ���� ȭ�鿡 �����ؼ� ���� ȭ��� Normal��
                // ������ ����Ѵ�.
                // =================================================

                float3 normalPointWS =
                    positionWS +
                    normalWS;


                float4 normalPointCS =
                    TransformWorldToHClip(
                        normalPointWS
                    );


                // =================================================
                // 4. Perspective Divide
                //
                // Clip Space -> NDC
                // =================================================

                float2 positionNDC =
                    positionCS.xy /
                    positionCS.w;

                float2 normalPointNDC =
                    normalPointCS.xy /
                    normalPointCS.w;


                // =================================================
                // 5. NDC -> Pixel ��ǥ�� ����
                //
                // NDC������ X/Y�� �� �� -1 ~ 1������
                // ���� ȭ���� ����/���� �ػ󵵰� �ٸ���.
                //
                // ���� Pixel ��ǥ��� �ٲ� �� normalize�ϸ�
                // ����/���� ��� ���⿡���� ���� Pixel �β���
                // ���� �� �ִ�.
                // =================================================

                float2 directionNDC =
                    normalPointNDC -
                    positionNDC;


                float2 directionPixel =
                    directionNDC *
                    _ScreenParams.xy;


                float directionLength =
                    length(directionPixel);


                if (directionLength > 0.0001)
                {
                    directionPixel /=
                        directionLength;
                }


                // =================================================
                // 6. ���ϴ� Pixel��ŭ �̵�
                // =================================================

                float2 offsetPixel =
                    directionPixel *
                    _OutlineWidth;


                // =================================================
                // 7. Pixel -> NDC
                //
                // NDC ��ü ũ�Ⱑ 2�̹Ƿ� * 2
                // =================================================

                float2 offsetNDC =
                    offsetPixel *
                    (2.0 / _ScreenParams.xy);


                // =================================================
                // 8. NDC -> Clip Space
                //
                // NDC = Clip / W
                //
                // ���� �ٽ� W�� ���Ѵ�.
                // =================================================

                positionCS.xy +=
                    offsetNDC *
                    positionCS.w;


                output.positionCS =
                    positionCS;

                return output;
            }


            half4 OutlineFrag(Varyings input) : SV_Target
            {
                half3 color =
                    _OutlineColor.rgb;


                #ifdef _OUTLINE_GLOW

                    color +=
                        _OutlineGlowColor.rgb *
                        _OutlineGlowIntensity;

                #endif


                return half4(
                    color,
                    _OutlineColor.a
                );
            }

            ENDHLSL
        }
    }
}