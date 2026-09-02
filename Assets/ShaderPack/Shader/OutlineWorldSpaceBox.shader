Shader "GameShaderPack/Outline/Box World Space"
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
        _OutlineWidth("Outline Width", Range(0.0, 0.2)) = 0.01

        [Header(Glow)]
        [Toggle] _OutlineGlow("Outline Glow", Float) = 0
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
        // Box Outline Pass
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _OutlineColor;
                float _OutlineWidth;

                float _OutlineGlow;
                float4 _OutlineGlowColor;
                float _OutlineGlowIntensity;

            CBUFFER_END


            Varyings OutlineVert(Attributes input)
            {
                Varyings output;


                // -------------------------------------------------
                // Pivot -> Vertex
                //
                // Normal�� ������� �ʰ� Object Space�� �߽�
                // (Pivot)�� �������� Vertex ������ ����Ѵ�.
                //
                // ���� Cubeó�� �鸶�� Normal�� �и��Ǿ� �־
                // ���� ��ġ�� Vertex�� �׻� ���� �������� �̵��Ѵ�.
                // -------------------------------------------------

                float3 directionOS =
                    normalize(input.positionOS.xyz);


                // -------------------------------------------------
                // Outline Ȯ��
                // -------------------------------------------------

                float3 expandedPositionOS =
                    input.positionOS.xyz +
                    directionOS *
                    _OutlineWidth;


                output.positionCS =
                    TransformObjectToHClip(
                        expandedPositionOS
                    );


                return output;
            }


            half4 OutlineFrag(Varyings input) : SV_Target
            {
                half3 color =
                    _OutlineColor.rgb;


                // -------------------------------------------------
                // Glow
                //
                // ���� ȭ����� Bloom ȿ����
                // URP Volume�� Bloom�� Ȱ��ȭ�Ǿ� �־�� �Ѵ�.
                // -------------------------------------------------

                color +=
                    _OutlineGlowColor.rgb *
                    _OutlineGlowIntensity *
                    _OutlineGlow;


                return half4(
                    color,
                    _OutlineColor.a
                );
            }

            ENDHLSL
        }
    }
}