Shader "Custom/SpriteOutlineDoubleFlicker"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Inner Outline)]
        _OutlineColor ("Inner Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Inner Outline Thickness", Range(0, 10)) = 1

        [Header(Outer Outline)]
        _OuterOutlineColor ("Outer Outline Color", Color) = (1,0.5,0,1)
        _OuterOutlineThickness ("Outer Outline Thickness", Range(0, 10)) = 2

        [Header(Flicker Effect)]
        _FlickerSpeed ("Flicker Speed", Range(0.5, 10)) = 3
        _FlickerIntensity ("Flicker Intensity", Range(0, 5)) = 1.5
        _FlickerAlphaMin ("Flicker Alpha Min", Range(0, 1)) = 0.4
        _FlickerDistortion ("Flicker Distortion", Range(0, 3)) = 0.8

        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineThickness;
                float4 _OuterOutlineColor;
                float _OuterOutlineThickness;
                float _FlickerSpeed;
                float _FlickerIntensity;
                float _FlickerAlphaMin;
                float _FlickerDistortion;
                float _OutlineEnabled;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            // 간단한 노이즈 함수 (UV + 시간 기반)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 8방향 샘플링
            float SampleOutlineAlpha(float2 uv, float thickness)
            {
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0),
                    float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(1, -1),
                    float2(-1, 1), float2(1, 1)
                };

                float maxAlpha = 0;
                float2 texelSize = _MainTex_TexelSize.xy * thickness;

                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * texelSize;
                    half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                    maxAlpha = max(maxAlpha, s.a);
                }
                return maxAlpha;
            }

            // 이글거림이 적용된 외곽 알파 샘플링
            float SampleFlickerOutlineAlpha(float2 uv, float baseThickness, float time)
            {
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0),
                    float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(1, -1),
                    float2(-1, 1), float2(1, 1)
                };

                float maxAlpha = 0;

                for (int i = 0; i < 8; i++)
                {
                    // 방향별로 노이즈 기반 두께 변동
                    float2 dir = offsets[i];
                    float angle = atan2(dir.y, dir.x);

                    // 각도 + 시간 기반 노이즈로 두께 흔들기
                    float n = noise(float2(angle * 2.0 + time * _FlickerSpeed, time * _FlickerSpeed * 0.7));
                    float thicknessVar = baseThickness + n * _FlickerDistortion;

                    float2 texelSize = _MainTex_TexelSize.xy * thicknessVar;
                    float2 sampleUV = uv + dir * texelSize;
                    half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                    maxAlpha = max(maxAlpha, s.a);
                }
                return maxAlpha;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainTex *= input.color;

                if (_OutlineEnabled > 0.5)
                {
                    float time = _Time.y;

                    // 1) 내곽 아웃라인 (고정)
                    float innerAlpha = SampleOutlineAlpha(input.uv, _OutlineThickness);

                    // 2) 외곽 아웃라인 (이글거림 적용)
                    float totalThickness = _OutlineThickness + _OuterOutlineThickness;
                    float outerAlpha = SampleFlickerOutlineAlpha(input.uv, totalThickness, time);

                    // 마스크
                    float outerMask = outerAlpha * (1.0 - innerAlpha) * (1.0 - mainTex.a);
                    float innerMask = innerAlpha * (1.0 - mainTex.a);

                    // 외곽 이글거림: 알파 펄스 + 밝기 변동
                    float flickerPulse = sin(time * _FlickerSpeed * 2.0) * 0.5 + 0.5;
                    float flickerNoise = noise(input.uv * 8.0 + time * _FlickerSpeed);
                    float flickerAlpha = lerp(_FlickerAlphaMin, 1.0, flickerPulse * 0.5 + flickerNoise * 0.5);

                    // 외곽 색상에 밝기 변동 추가
                    float brightnessBoost = 1.0 + flickerNoise * _FlickerIntensity * 0.3;
                    half3 outerColor = _OuterOutlineColor.rgb * brightnessBoost;

                    half4 outer = half4(outerColor * _OuterOutlineColor.a * flickerAlpha,
                                        _OuterOutlineColor.a * flickerAlpha) * outerMask;
                    half4 inner = half4(_OutlineColor.rgb * _OutlineColor.a,
                                        _OutlineColor.a) * innerMask;

                    // premultiply
                    mainTex.rgb *= mainTex.a;

                    // 합성: 외곽 → 내곽 → 원본
                    half4 result = outer;
                    result.rgb = inner.rgb + result.rgb * (1.0 - inner.a);
                    result.a = inner.a + result.a * (1.0 - inner.a);
                    result.rgb = mainTex.rgb + result.rgb * (1.0 - mainTex.a);
                    result.a = mainTex.a + result.a * (1.0 - mainTex.a);

                    return result;
                }

                mainTex.rgb *= mainTex.a;
                return mainTex;
            }
            ENDHLSL
        }
    }
}
