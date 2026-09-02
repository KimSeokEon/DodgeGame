Shader "GameShaderPack/Effect/Pulse"
{
    Properties
    {
        [Header(Background)]
        _BackgroundColor(
            "Background Color",
            Color
        ) = (0.01, 0.03, 0.03, 1)


        [Header(Pulse Line)]

        [HDR]
        _LineColor(
            "Line Color",
            Color
        ) = (0.0, 1.0, 0.3, 1.0)

        _LineWidth(
            "Line Width",
            Range(0.001, 0.05)
        ) = 0.008


        [Header(Pulse)]

        _HeartRate(
            "Heart Rate",
            Range(0.1, 5.0)
        ) = 1.0

        _PulseHeight(
            "Pulse Height",
            Range(0.0, 0.3)
        ) = 0.10

        _PulseSharpness(
            "Pulse Sharpness",
            Range(15.0, 100.0)
        ) = 50.0

        _PulseFrequency(
            "Pulse Frequency",
            Range(1.0, 10.0)
        ) = 3.0

        _PulseSpeed(
            "Scroll Speed",
            Range(-5.0, 5.0)
        ) = 0.5


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
        ) = (0.0, 1.0, 0.3, 1.0)

        _GlowWidth(
            "Glow Width",
            Range(0.001, 0.1)
        ) = 0.025

        _GlowIntensity(
            "Glow Intensity",
            Range(0.0, 10.0)
        ) = 1.5
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }


        Pass
        {
            Name "Pulse"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // =====================================================
            // Settings
            // =====================================================

            #define PULSE_SEGMENT_COUNT 20


            // =====================================================
            // Attributes
            // =====================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };


            // =====================================================
            // Varyings
            // =====================================================

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };


            // =====================================================
            // Material
            // =====================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BackgroundColor;

                float4 _LineColor;
                float _LineWidth;

                float _HeartRate;
                float _PulseHeight;
                float _PulseSharpness;
                float _PulseFrequency;
                float _PulseSpeed;

                float _GlowEnabled;
                float4 _GlowColor;
                float _GlowWidth;
                float _GlowIntensity;

            CBUFFER_END


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

                output.uv =
                    input.uv;

                return output;
            }


            // =====================================================
            // Gaussian
            // =====================================================

            float GaussianPulse(
                float x,
                float center,
                float sharpness
            )
            {
                float delta =
                    (x - center) *
                    sharpness;

                return exp(
                    -(delta * delta)
                );
            }


            // =====================================================
            // ECG Shape
            //
            //                         R
            //                        /\
            //                       /  \
            //          P           /    \             T
            //         / \         /      \           / \
            // -------/   \-------/        \---------/   \-------
            //                  Q             S
            //
            // =====================================================

            float CalculatePulseShape(float x)
            {
                float result =
                    0.0;


                // P Wave
                float pWave =
                    GaussianPulse(
                        x,
                        0.20,
                        18.0
                    );

                result +=
                    pWave * 0.10;


                // Q Wave
                float qWave =
                    GaussianPulse(
                        x,
                        0.36,
                        40.0
                    );

                result -=
                    qWave * 0.10;


                // R Wave
                float rWave =
                    GaussianPulse(
                        x,
                        0.42,
                        _PulseSharpness
                    );

                result +=
                    rWave;


                // S Wave
                float sWave =
                    GaussianPulse(
                        x,
                        0.48,
                        40.0
                    );

                result -=
                    sWave * 0.15;


                // T Wave
                float tWave =
                    GaussianPulse(
                        x,
                        0.68,
                        14.0
                    );

                result +=
                    tWave * 0.15;


                return result;
            }


            // =====================================================
            // Get Pulse Y
            // =====================================================

            float GetPulseY(
                float uvX,
                float animationOffset
            )
            {
                float phase =
                    uvX *
                    _PulseFrequency
                    -
                    animationOffset *
                    _HeartRate;


                float localPhase =
                    frac(phase);


                float pulseValue =
                    CalculatePulseShape(
                        localPhase
                    );


                return
                    0.5 +
                    pulseValue *
                    _PulseHeight;
            }


            // =====================================================
            // Distance To Segment
            //
            // segmentStart ●────────────● segmentEnd
            //
            //                    × targetPos
            //
            // =====================================================

            float DistanceToSegment(
                float2 targetPos,
                float2 segmentStart,
                float2 segmentEnd
            )
            {
                float2 segmentVector =
                    segmentEnd -
                    segmentStart;


                float2 targetVector =
                    targetPos -
                    segmentStart;


                float segmentLengthSq =
                    dot(
                        segmentVector,
                        segmentVector
                    );


                if (segmentLengthSq < 0.0000001)
                {
                    return length(
                        targetVector
                    );
                }


                float projection =
                    dot(
                        targetVector,
                        segmentVector
                    )
                    /
                    segmentLengthSq;


                projection =
                    saturate(
                        projection
                    );


                float2 closestPos =
                    segmentStart +
                    segmentVector *
                    projection;


                return length(
                    targetPos -
                    closestPos
                );
            }


            // =====================================================
            // Distance To Pulse Curve
            //
            // Pulse를 여러 개의 연속된 Segment로 근사한다.
            //
            // ●──●──●──●──●──●
            //
            // 각 Fragment에서 가장 가까운 Segment와의
            // 거리를 사용한다.
            // =====================================================

            float CalculatePulseDistance(
                float2 uv,
                float animationOffset
            )
            {
                float minDistance =
                    1000.0;


                // =================================================
                // Search Range
                //
                // 현재 Fragment 주변만 검사한다.
                // =================================================

                float maxEffectWidth =
                    max(
                        _LineWidth,
                        _GlowWidth
                    );


                float searchRange =
                    max(
                        maxEffectWidth *
                        4.0,
                        0.04
                    );


                // =================================================
                // Aspect Correction
                //
                // UV 공간에서 X/Y 단위가 화면상 같은 길이가
                // 아니므로 화면 비율을 반영한다.
                // =================================================

                float aspectRatio =
                    _ScreenParams.x /
                    max(
                        _ScreenParams.y,
                        1.0
                    );


                float2 targetPos =
                    float2(
                        uv.x * aspectRatio,
                        uv.y
                    );


                // =================================================
                // First Sample
                // =================================================

                float rangeStart =
                    uv.x -
                    searchRange;


                float rangeEnd =
                    uv.x +
                    searchRange;


                float previousX =
                    rangeStart;


                float previousY =
                    GetPulseY(
                        previousX,
                        animationOffset
                    );


                float2 previousSample =
                    float2(
                        previousX * aspectRatio,
                        previousY
                    );


                // =================================================
                // Segments
                // =================================================

                [loop]
                for (
                    int sampleIndex = 1;
                    sampleIndex <= PULSE_SEGMENT_COUNT;
                    ++sampleIndex
                )
                {
                    float sampleRatio =
                        (float)sampleIndex /
                        (float)PULSE_SEGMENT_COUNT;


                    float currentX =
                        lerp(
                            rangeStart,
                            rangeEnd,
                            sampleRatio
                        );


                    float currentY =
                        GetPulseY(
                            currentX,
                            animationOffset
                        );


                    float2 currentSample =
                        float2(
                            currentX * aspectRatio,
                            currentY
                        );


                    float currentDistance =
                        DistanceToSegment(
                            targetPos,
                            previousSample,
                            currentSample
                        );


                    minDistance =
                        min(
                            minDistance,
                            currentDistance
                        );


                    previousSample =
                        currentSample;
                }


                return minDistance;
            }


            // =====================================================
            // Fragment
            // =====================================================

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv =
                    input.uv;


                // =================================================
                // Animation
                // =================================================

                float animationOffset =
                    _Time.y *
                    _PulseSpeed;


                // =================================================
                // Distance
                // =================================================

                float pulseDistance =
                    CalculatePulseDistance(
                        uv,
                        animationOffset
                    );


                // =================================================
                // Anti Aliasing
                // =================================================

                float edgeSoftness =
                    max(
                        fwidth(
                            pulseDistance
                        ),
                        0.00001
                    );


                // =================================================
                // Main Line
                // =================================================

                float lineMask =
                    1.0 -
                    smoothstep(
                        _LineWidth,
                        _LineWidth +
                        edgeSoftness,
                        pulseDistance
                    );


                // =================================================
                // Glow
                // =================================================

                float safeGlowWidth =
                    max(
                        _GlowWidth,
                        _LineWidth +
                        0.0001
                    );


                float glowMask =
                    1.0 -
                    smoothstep(
                        _LineWidth,
                        safeGlowWidth,
                        pulseDistance
                    );


                glowMask *=
                    _GlowEnabled;


                // Glow가 선 내부를 덮어버리지 않도록
                // 외부 영역 비중을 높인다.

                float outerGlowMask =
                    glowMask *
                    (1.0 - lineMask);


                // =================================================
                // Background
                // =================================================

                half3 finalColor =
                    _BackgroundColor.rgb;


                // =================================================
                // Outer Glow
                // =================================================

                finalColor +=
                    _GlowColor.rgb *
                    outerGlowMask *
                    _GlowIntensity;


                // =================================================
                // Main Line
                // =================================================

                half3 pulseLineColor =
                    _LineColor.rgb;


                // 선 자체에도 약간의 Emission
                pulseLineColor +=
                    _GlowColor.rgb *
                    _GlowIntensity *
                    0.25 *
                    _GlowEnabled;


                finalColor =
                    lerp(
                        finalColor,
                        pulseLineColor,
                        lineMask
                    );


                // =================================================
                // Final
                // =================================================

                return half4(
                    finalColor,
                    _BackgroundColor.a
                );
            }

            ENDHLSL
        }
    }
}