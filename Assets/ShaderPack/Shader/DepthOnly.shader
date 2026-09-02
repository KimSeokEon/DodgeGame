Shader "GameShaderPack/Utility/DepthOnly"
{
    // =============================================================
    // 화면에는 아무 색도 안 그리고(ColorMask 0), Depth 버퍼에만
    // 정상적인 Opaque 타이밍(Queue=Geometry)으로 깊이값을 기록하는
    // 셰이더.
    //
    // Enemy가 쓰는 Mesh2D-Lit-Default 셰이더는 Queue가 Transparent라서
    // XRayOverlayOnly가 판정하는 시점과 타이밍이 꼬여 "가려짐" 판정이
    // 제대로 안 되는 문제가 있었다. 이 셰이더를 Enemy 렌더러의
    // 추가 머티리얼 슬롯으로 얹어서, 벽처럼 확실한 Opaque 타이밍에
    // depth를 남겨 XRayOverlayOnly가 항상 정확히 감지하도록 한다.
    // =============================================================

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "DepthOnly"

            Tags { "LightMode" = "SRPDefaultUnlit" }

            ColorMask 0

            ZWrite On

            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }

            ENDHLSL
        }
    }
}