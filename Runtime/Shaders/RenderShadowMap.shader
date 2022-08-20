// Based on the following links:
//     http://www.catalinzima.com/2010/07/my-technique-for-the-shader-based-dynamic-2d-shadows/
//     https://gamedev.stackexchange.com/questions/27019/how-can-i-generate-signed-distance-fields-2d-in-real-time-fast
Shader "Tuntenfisch/Lighting2D/RenderShadowMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }

    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

        #pragma vertex FullscreenVert
        #pragma fragment FragmentPass

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        ENDHLSL

        Pass
        {
            HLSLPROGRAM

            float FragmentPass(Varyings inputs) : SV_Target
            {
                bool shadowCaster = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, inputs.uv).r == 0.0f;
                return shadowCaster ? length(inputs.uv - 0.5f) : 1.0f;
            }

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

            float4 FragmentPass(Varyings inputs) : SV_TARGET
            {
                float2 start = 0.5f;
                float2 end = float2(0.0f, inputs.uv.y);
                float2 uv = lerp(start, end, inputs.uv.x);

                float4 color;
                color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;            // Map the left quadrant to the red channel.
                color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, 1.0f - uv).r;     // Map the right quadrant to the green channel.
                color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv.yx).r;         // Map the bottom quadrant to the blue channel.
                color.a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, 1.0f - uv.yx).r;  // Map the top quadrant to the alpha channel.
                return color;
            }

            ENDHLSL
        }

        Pass
        {
            Blend One One
            BlendOp Min

            HLSLPROGRAM

            float4 FragmentPass(Varyings inputs) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, inputs.uv);
            }

            ENDHLSL
        }
    }
}
