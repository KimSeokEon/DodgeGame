Shader "GameShaderPack/Mask/Stencil Mask"
{
    Properties
    {
        [Header(Stencil)]

        _StencilRef(
            "Stencil Reference",
            Range(1, 255)
        ) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry-10"
        }


        Pass
        {
            Name "StencilMask"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }


            // =====================================================
            // 화면에는 아무것도 출력하지 않는다.
            // =====================================================

            ColorMask 0


            // =====================================================
            // Mask가 다른 오브젝트의 Depth를 가리지 않도록 한다.
            // =====================================================

            ZWrite Off
            ZTest LEqual


            // =====================================================
            // 양면 사용
            // =====================================================

            Cull Off


            // =====================================================
            // Stencil
            //
            // Mask가 렌더링되는 위치에
            // _StencilRef 값을 기록한다.
            // =====================================================

            Stencil
            {
                Ref [_StencilRef]

                Comp Always

                Pass Replace

                Fail Keep
                ZFail Keep
            }


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
            };


            // =====================================================
            // Varyings
            // =====================================================

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };


            // =====================================================
            // Vertex
            // =====================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                return output;
            }


            // =====================================================
            // Fragment
            //
            // ColorMask 0이므로 실제 색상은 출력되지 않는다.
            // =====================================================

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(
                    0.0,
                    0.0,
                    0.0,
                    0.0
                );
            }


            ENDHLSL
        }
    }
}