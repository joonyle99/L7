Shader "Custom/SpriteOutlineDouble"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Inner Outline)]
        _OutlineColor ("Inner Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Inner Outline Thickness", Range(0, 10)) = 1

        [Header(Outer Outline)]
        _OuterOutlineColor ("Outer Outline Color", Color) = (0,0,0,1)
        _OuterOutlineThickness ("Outer Outline Thickness", Range(0, 10)) = 2

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

            // 주변 8방향 샘플링으로 알파 최대값 검출
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

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainTex *= input.color;

                if (_OutlineEnabled > 0.5)
                {
                    // 1) 내곽 아웃라인 알파 (스프라이트 바로 바깥)
                    float innerAlpha = SampleOutlineAlpha(input.uv, _OutlineThickness);

                    // 2) 외곽 아웃라인 알파 (내곽 + 외곽 두께만큼 더 바깥)
                    float totalThickness = _OutlineThickness + _OuterOutlineThickness;
                    float outerAlpha = SampleOutlineAlpha(input.uv, totalThickness);

                    // 외곽 마스크: 외곽 영역에만 (내곽 영역 제외)
                    float outerMask = outerAlpha * (1.0 - innerAlpha) * (1.0 - mainTex.a);
                    // 내곽 마스크: 내곽 영역에만 (원본 제외)
                    float innerMask = innerAlpha * (1.0 - mainTex.a);

                    half4 outer = half4(_OuterOutlineColor.rgb * _OuterOutlineColor.a, _OuterOutlineColor.a) * outerMask;
                    half4 inner = half4(_OutlineColor.rgb * _OutlineColor.a, _OutlineColor.a) * innerMask;

                    // premultiply alpha
                    mainTex.rgb *= mainTex.a;

                    // 레이어링: 외곽 → 내곽 → 원본 순서로 합성
                    half4 result;
                    // 외곽 베이스
                    result = outer;
                    // 내곽 위에 덮기
                    result.rgb = inner.rgb + result.rgb * (1.0 - inner.a);
                    result.a = inner.a + result.a * (1.0 - inner.a);
                    // 원본 위에 덮기
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
