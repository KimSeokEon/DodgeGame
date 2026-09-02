Shader "GameShaderPack/Effect/XRayOutline"
{
    Properties
    {
        [Header(Base)]

        [MainTexture]
        _BaseMap(
            "Base Texture",
            2D
        ) = "white" {}

        [MainColor]
        _BaseColor(
            "Base Color",
            Color
        ) = (1, 1, 1, 1)


        
        [Header(Surface)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(XRay Outline)]

        [HDR]
        _OutlineColor(
            "Outline Color",
            Color
        ) = (0, 1, 1, 1)

        _OutlineWidth(
            "Outline Width",
            Range(0.01, 1.0)
        ) = 0.15


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
        ) = (0, 1, 1, 1)

        _GlowIntensity(
            "Glow Intensity",
            Range(0.0, 10.0)
        ) = 2.0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }


        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);


        CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;
            float4 _BaseColor;
            half _Metallic;
            half _Smoothness;
            half _OcclusionStrength;

            float4 _OutlineColor;
            float _OutlineWidth;

            float _GlowEnabled;
            float4 _GlowColor;
            float _GlowIntensity;

        CBUFFER_END


        ENDHLSL


        Pass
        {
            Name "NormalSurface"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VertNormalLit
            #pragma fragment FragNormalLit
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct NormalLitAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct NormalLitVaryings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normalWS:TEXCOORD2; half fogFactor:TEXCOORD3; };
            NormalLitVaryings VertNormalLit(NormalLitAttributes input) {
                NormalLitVaryings o; VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz); VertexNormalInputs n=GetVertexNormalInputs(input.normalOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=NormalizeNormalPerVertex(n.normalWS); o.uv=TRANSFORM_TEX(input.uv,_BaseMap); o.fogFactor=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 FragNormalLit(NormalLitVaryings input):SV_Target {
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv); half3 n=NormalizeNormalPerPixel(input.normalWS);
                InputData d=(InputData)0; d.positionWS=input.positionWS; d.normalWS=n; d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS); d.shadowCoord=TransformWorldToShadowCoord(input.positionWS); d.fogCoord=input.fogFactor; d.vertexLighting=VertexLighting(input.positionWS,n); d.bakedGI=SampleSH(n); d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS); d.shadowMask=half4(1,1,1,1);
                SurfaceData sd=(SurfaceData)0; sd.albedo=tex.rgb*_BaseColor.rgb; sd.metallic=_Metallic; sd.specular=half3(.5h,.5h,.5h); sd.smoothness=_Smoothness; sd.normalTS=half3(0,0,1); sd.occlusion=_OcclusionStrength; sd.alpha=tex.a*_BaseColor.a;
                half4 c=UniversalFragmentPBR(d,sd); c.rgb=MixFog(c.rgb,d.fogCoord); return c;
            }
            ENDHLSL
        }


        Pass
        {
            Name "XRayOutline"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }


            Cull Back

            ZWrite Off

            ZTest Greater

            Blend SrcAlpha OneMinusSrcAlpha


            HLSLPROGRAM

            #pragma vertex VertOutline
            #pragma fragment FragOutline


            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };


            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;
            };


            OutlineVaryings VertOutline(
                OutlineAttributes input
            )
            {
                OutlineVaryings output;


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


            half4 FragOutline(
                OutlineVaryings input
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
                    abs(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );


                float silhouette =
                    1.0 -
                    saturate(
                        NdotV
                    );


                // _OutlineWidth가 클수록(1에 가까울수록) 감쇠 지수가 낮아져서
                // 실루엣 전체를 부드럽게 채우는 큰 글로우가 되고,
                // 작을수록 지수가 높아져서 가장자리만 얇게 빛나는 라인이 됩니다.
                float glowPower =
                    lerp(
                        16.0,
                        0.6,
                        saturate(_OutlineWidth)
                    );


                float glowMask =
                    pow(
                        saturate(silhouette),
                        glowPower
                    );


                clip(
                    glowMask -
                    0.001
                );


                half3 finalColor =
                    _OutlineColor.rgb;


                float glowEnabled =
                    step(
                        0.5,
                        _GlowEnabled
                    );


                finalColor +=
                    _GlowColor.rgb *
                    _GlowIntensity *
                    glowEnabled *
                    glowMask;


                half alpha =
                    _OutlineColor.a *
                    glowMask;


                return half4(
                    finalColor,
                    alpha
                );
            }


            ENDHLSL
        }
    }
}