Shader "GameShaderPack/Effect/SurfaceSpikeSphere"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Spike)]
        _SpikeCount("Spike Count", Range(1, 16)) = 6
        _MinHeight("Min Height", Range(0.0, 1.0)) = 0.1
        _MaxHeight("Max Height", Range(0.0, 2.0)) = 0.5
        _SpikeRadius("Spike Radius", Range(0.05, 1.5)) = 0.4
        _Sharpness("Sharpness", Range(0.5, 10.0)) = 2.5

        [Header(Animation)]
        _Interval("Interval", Range(0.1, 10.0)) = 2.0
        _RiseSpeed("Rise Speed", Range(0.1, 10.0)) = 3.0
        _FallSpeed("Fall Speed", Range(0.1, 10.0)) = 2.0
        _Randomness("Randomness", Range(0.0, 1.0)) = 1.0

        [Header(Impact)]
        _DipStrength("Dip Strength", Range(0.0, 1.0)) = 0.05
        _DipRadius("Dip Radius", Range(1.0, 4.0)) = 1.8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MAX_SPIKES 16

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _SpikeCount;
                float _MinHeight;
                float _MaxHeight;
                float _SpikeRadius;
                float _Sharpness;
                float _Interval;
                float _RiseSpeed;
                float _FallSpeed;
                float _Randomness;
                float _DipStrength;
                float _DipRadius;
            CBUFFER_END

            float Hash11(float value)
            {
                value = frac(value * 0.1031);
                value *= value + 33.33;
                value *= value + value;
                return frac(value);
            }

            // 구 표면 상에 균일하게 분포하는 3D 방향 벡터 생성
            float3 Hash31(float value)
            {
                float u = Hash11(value + 17.17) * 2.0 - 1.0; // -1 ~ 1
                float theta = Hash11(value + 91.73) * TWO_PI;
                float r = sqrt(saturate(1.0 - u * u));
                return float3(r * cos(theta), u, r * sin(theta));
            }

            float GetImpactPhase(float index)
            {
                float randomOffset = Hash11(index * 31.71);
                float interval = max(_Interval, 0.001);
                return frac(_Time.y / interval + randomOffset * _Randomness);
            }

            float CalculateSurfaceHeight(float3 positionOS, float3 normalOS)
            {
                float finalHeight = 0.0;
                int spikeCount = clamp((int)_SpikeCount, 1, MAX_SPIKES);

                // 구의 중심 좌표 기준으로 정점의 방향 구함 (단위 구체 형태 기준)
                float3 dirOS = normalize(positionOS);

                [loop]
                for (int i = 0; i < MAX_SPIKES; i++)
                {
                    if (i >= spikeCount) break;

                    float index = (float)i + 1.0;

                    // 구 표면의 3D 랜덤 위치 (방향 벡터)
                    float3 spikeCenterDir = Hash31(index * 14.37);

                    // 2D 평면 거리 대신, 3D 벡터 각도/호의 거리를 이용
                    float distanceToCenter = length(dirOS - spikeCenterDir);

                    float phase = GetImpactPhase(index);
                    float randomHeight = lerp(_MinHeight, _MaxHeight, Hash11(index * 73.91));
                    float radius = max(_SpikeRadius, 0.001);

                    // Jet 형태
                    float jetDistance = distanceToCenter / radius;
                    float jetShape = saturate(1.0 - jetDistance);
                    jetShape = pow(jetShape, _Sharpness);

                    // 애니메이션
                    float riseDuration = clamp(0.2 / max(_RiseSpeed, 0.001), 0.01, 0.4);
                    float fallDuration = clamp(0.5 / max(_FallSpeed, 0.001), 0.01, 0.4);

                    float jetAnimation = 0.0;
                    if (phase < riseDuration)
                    {
                        jetAnimation = smoothstep(0.0, 1.0, phase / riseDuration);
                    }
                    else if (phase < (riseDuration + fallDuration))
                    {
                        float t = (phase - riseDuration) / fallDuration;
                        jetAnimation = 1.0 - smoothstep(0.0, 1.0, t);
                    }

                    float jetHeight = jetShape * randomHeight * jetAnimation;

                    // 웅덩이(Dip) 연출
                    float depressionRadius = radius * _DipRadius;
                    float depressionDistance = distanceToCenter / max(depressionRadius, 0.001);
                    float depressionShape = saturate(1.0 - depressionDistance);
                    depressionShape *= depressionShape;

                    float centerMask = smoothstep(0.0, radius * 0.5, distanceToCenter);
                    depressionShape *= centerMask;

                    float depressionAnimation = sin(saturate(phase * 4.0) * PI);
                    float depression = depressionShape * _DipStrength * max(depressionAnimation, 0.0);

                    float currentHeight = jetHeight - depression;

                    if (abs(currentHeight) > abs(finalHeight))
                    {
                        finalHeight = currentHeight;
                    }
                }

                return finalHeight;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = normalize(input.normalOS);

                // 1. 구 표면 기준 돌출 높이 계산
                float surfaceHeight = CalculateSurfaceHeight(positionOS, normalOS);

                // 2. Y축이 아닌 정점의 법선(Normal) 방향으로 밀어냄!
                positionOS += normalOS * surfaceHeight;

                // 3. Normal 재계산 (구체용 변위 오프셋 시뮬레이션)
                float delta = 0.01;
                // 법선과 수직인 두 축(Tangent, Bitangent)으로 주변 정점 높이 샘플링
                float3 tangentOS = cross(normalOS, float3(0, 1, 0));
                if (length(tangentOS) < 0.001) tangentOS = cross(normalOS, float3(1, 0, 0));
                tangentOS = normalize(tangentOS);
                float3 bitangentOS = cross(normalOS, tangentOS);

                float h1 = CalculateSurfaceHeight(input.positionOS.xyz + tangentOS * delta, normalOS);
                float h2 = CalculateSurfaceHeight(input.positionOS.xyz + bitangentOS * delta, normalOS);

                float3 p1 = (input.positionOS.xyz + tangentOS * delta) + normalOS * h1;
                float3 p2 = (input.positionOS.xyz + bitangentOS * delta) + normalOS * h2;

                float3 modifiedNormalOS = normalize(cross(p2 - positionOS, p1 - positionOS));

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(modifiedNormalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = baseTexture.rgb * _BaseColor.rgb;
                half3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directLight = mainLight.color * NdotL;
                half3 ambientLight = SampleSH(normalWS);

                half3 finalColor = baseColor * (directLight + ambientLight);

                return half4(finalColor, baseTexture.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}