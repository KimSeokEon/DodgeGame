Shader "Hidden/PauseBlur"
{
    // =========================================================================
    // PauseBlur.shader
    // -------------------------------------------------------------------------
    // ESC 일시정지 메뉴 배경용 풀스크린 블러 + 어둡게 틴트.
    // PauseBlurFeature.cs (URP ScriptableRendererFeature) 가 이 셰이더로 만든
    // 머티리얼을 들고 화면을 블러한다.
    //   Pass 0 : Vogel 디스크 16-tap 블러 + (_Darken 만큼 어둡게) + _Weight 로
    //            원본↔블러 를 lerp. 피처가 결과를 카메라 컬러로 스왑한다.
    // 파라미터는 피처가 _mat.SetFloat 으로 넣어준다:
    //   _BlurRadius : 블러 반경(픽셀), _Darken : 0~1 어둡게, _Weight : 0~1 페이드
    // =========================================================================
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurRadius;
        float _Darken;
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

                // Vogel 디스크 샘플링 (황금각) — 적은 탭으로 고르게 퍼진 블러
                const int TAPS = 16;
                half4 acc = 0;
                UNITY_UNROLL
                for (int k = 0; k < TAPS; k++)
                {
                    float t = (float(k) + 0.5) / TAPS;
                    float r = sqrt(t) * _BlurRadius;
                    float a = float(k) * 2.39996323; // golden angle
                    float2 offs = float2(cos(a), sin(a)) * r * texel;
                    acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offs);
                }
                acc /= TAPS;

                half3 tinted = acc.rgb * (1.0h - saturate(_Darken));
                half3 outc = lerp(sharp.rgb, tinted, saturate(_Weight));
                return half4(outc, 1.0h);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
