Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0, 10)) = 1
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

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainTex *= input.color;

                if (_OutlineEnabled > 0.5 && _OutlineThickness > 0)
                {
                    // 8방향 샘플링으로 아웃라인 검출
                    float2 offsets[8] = {
                        float2(-1, 0), float2(1, 0),
                        float2(0, -1), float2(0, 1),
                        float2(-1, -1), float2(1, -1),
                        float2(-1, 1), float2(1, 1)
                    };

                    float outlineAlpha = 0;
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;

                    for (int i = 0; i < 8; i++)
                    {
                        float2 sampleUV = input.uv + offsets[i] * texelSize;
                        half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                        outlineAlpha = max(outlineAlpha, sample.a);
                    }

                    // 원본 알파가 없는 곳에만 아웃라인 표시
                    float outlineMask = outlineAlpha * (1.0 - mainTex.a);
                    half4 outline = half4(_OutlineColor.rgb * _OutlineColor.a, _OutlineColor.a) * outlineMask;

                    // 원본 premultiply alpha
                    mainTex.rgb *= mainTex.a;

                    // 아웃라인 위에 원본을 덮어씌움
                    half4 result;
                    result.rgb = mainTex.rgb + outline.rgb * (1.0 - mainTex.a);
                    result.a = mainTex.a + outline.a * (1.0 - mainTex.a);
                    return result;
                }

                mainTex.rgb *= mainTex.a;
                return mainTex;
            }
            ENDHLSL
        }
    }
}
