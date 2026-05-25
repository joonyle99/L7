Shader "Custom/SpriteOutlineDoubleNoise"
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

        [Header(Noise Flicker)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise UV Scale", Range(0.1, 10)) = 2
        _NoiseScrollSpeed ("Noise Scroll Speed", Vector) = (0.5, 0.3, 0, 0)
        _FlickerSpeed ("Flicker Pulse Speed", Range(0.5, 10)) = 3
        _FlickerAlphaMin ("Flicker Alpha Min", Range(0, 1)) = 0.3
        _FlickerDistortion ("Thickness Distortion", Range(0, 5)) = 1.5
        _FlickerBrightness ("Brightness Boost", Range(0, 3)) = 1.0

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

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineThickness;
                float4 _OuterOutlineColor;
                float _OuterOutlineThickness;
                float _NoiseScale;
                float4 _NoiseScrollSpeed;
                float _FlickerSpeed;
                float _FlickerAlphaMin;
                float _FlickerDistortion;
                float _FlickerBrightness;
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

            // 노이즈 텍스처 샘플 (UV 스크롤 적용)
            float SampleNoise(float2 uv, float time)
            {
                float2 noiseUV = uv * _NoiseScale + _NoiseScrollSpeed.xy * time;
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
            }

            // 고정 두께 8방향 샘플링
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

            // 노이즈 기반 두께 변동 8방향 샘플링
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
                    float2 dir = offsets[i];

                    // 방향별로 노이즈 텍스처 샘플해서 두께 변동
                    float2 noiseUV = (uv + dir * 0.1) * _NoiseScale + _NoiseScrollSpeed.xy * time;
                    float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                    // 노이즈 값으로 두께 흔들기 (0~1 -> -0.5~0.5 -> 스케일 적용)
                    float thicknessVar = baseThickness + (n - 0.5) * _FlickerDistortion;
                    thicknessVar = max(thicknessVar, 0.0);

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

                    // 노이즈 텍스처에서 현재 픽셀의 이글거림 값
                    float noiseVal = SampleNoise(input.uv, time);

                    // 1) 내곽 아웃라인 (고정, 안정적)
                    float innerAlpha = SampleOutlineAlpha(input.uv, _OutlineThickness);

                    // 2) 외곽 아웃라인 (노이즈 기반 두께 변동)
                    float totalThickness = _OutlineThickness + _OuterOutlineThickness;
                    float outerAlpha = SampleFlickerOutlineAlpha(input.uv, totalThickness, time);

                    // 마스크
                    float outerMask = outerAlpha * (1.0 - innerAlpha) * (1.0 - mainTex.a);
                    float innerMask = innerAlpha * (1.0 - mainTex.a);

                    // 이글거림 알파: sin 펄스 + 노이즈 텍스처 조합
                    float pulse = sin(time * _FlickerSpeed) * 0.5 + 0.5;
                    float flickerAlpha = lerp(_FlickerAlphaMin, 1.0, pulse * 0.4 + noiseVal * 0.6);

                    // 밝기 변동: 노이즈 기반
                    float brightness = 1.0 + (noiseVal - 0.5) * _FlickerBrightness;
                    half3 outerColor = _OuterOutlineColor.rgb * brightness;

                    // 합성
                    half4 outer = half4(outerColor * _OuterOutlineColor.a * flickerAlpha,
                                        _OuterOutlineColor.a * flickerAlpha) * outerMask;
                    half4 inner = half4(_OutlineColor.rgb * _OutlineColor.a,
                                        _OutlineColor.a) * innerMask;

                    mainTex.rgb *= mainTex.a;

                    // 레이어링: 외곽 → 내곽 → 원본
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
