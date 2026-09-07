Shader "Hidden/PauseBlur"
{
    // =========================================================================
    // PauseBlur.shader
    // -------------------------------------------------------------------------
    // ESC 일시정지 메뉴 배경용 풀스크린 블러 + 검은색 틴트 (가장자리 강조).
    // PauseBlurFeature.cs (URP ScriptableRendererFeature) 가 이 셰이더로 만든
    // 머티리얼을 들고 화면을 블러한다.
    //   Pass 0 : Vogel 디스크 16-tap 블러 + 검은 틴트 + _Weight 로 원본↔결과 lerp.
    //            화면 중앙(vig=0) → 가장자리(vig=1) 로 갈수록 블러/어둡기가 강해짐.
    //            피처가 결과를 카메라 컬러로 스왑한다.
    // 파라미터 (피처가 _mat.SetFloat 으로 주입):
    //   _BlurRadius   : 기본 블러 반경(픽셀)
    //   _EdgeBlurMul  : 가장자리에서 블러 반경 배율 (1 = 균일)
    //   _Darken       : 중앙 어둡기 0~1
    //   _EdgeDarken   : 가장자리 어둡기 0~1
    //   _Weight       : 0~1 페이드 (0 = 원본 그대로)
    // =========================================================================
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurRadius;
        float _EdgeBlurMul;
        float _Darken;
        float _EdgeDarken;
        float _Weight;
        ENDHLSL

        Pass
        {
            Name "PauseBlurComposite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;

                half4 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 화면 중앙 0 → 모서리 1. smoothstep 으로 가운데는 약하게, 밖은 강하게.
                float vig = saturate(distance(uv, float2(0.5, 0.5)) * 1.41421356);
                vig = smoothstep(0.15, 1.0, vig);

                float radius = _BlurRadius * lerp(1.0, _EdgeBlurMul, vig);

                // Vogel 디스크 샘플링 (황금각) — 적은 탭으로 고르게 퍼진 블러
                const int TAPS = 16;
                half4 acc = 0;
                UNITY_UNROLL
                for (int k = 0; k < TAPS; k++)
                {
                    float t = (float(k) + 0.5) / TAPS;
                    float r = sqrt(t) * radius;
                    float a = float(k) * 2.39996323; // golden angle
                    float2 offs = float2(cos(a), sin(a)) * r * texel;
                    acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offs);
                }
                acc /= TAPS;

                // 검은색 틴트 — 중앙 _Darken, 가장자리 _EdgeDarken
                float darkenAmt = saturate(lerp(_Darken, _EdgeDarken, vig));
                half3 tinted = acc.rgb * (1.0h - darkenAmt);

                half3 outc = lerp(sharp.rgb, tinted, saturate(_Weight));
                return half4(outc, 1.0h);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
