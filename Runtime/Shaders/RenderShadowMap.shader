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

            float FragmentPass(Varyings inputs) : SV_TARGET
            {
                return 1.0f - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, inputs.uv).a;
            }

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

            float SampleShadowCasterDistance(float2 uv)
            {
                float distanceSquared = dot(uv - 0.5f, uv - 0.5f);
                bool shadowCaster = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r == 0.0f;
                return shadowCaster ? distanceSquared : 1.0f;
            }

            float4 FragmentPass(Varyings inputs) : SV_TARGET
            {
                float2 start = 0.5f;
                float2 end = float2(0.0f, inputs.uv.y);
                float2 uv = lerp(start, end, inputs.uv.x);

                float4 color;
                color.r = SampleShadowCasterDistance(uv);           // Map the left quadrant to the red channel.
                color.g = SampleShadowCasterDistance(1.0f - uv);    // Map the right quadrant to the green channel.
                color.b = SampleShadowCasterDistance(uv.yx);        // Map the bottom quadrant to the blue channel.
                color.a = SampleShadowCasterDistance(1.0f - uv.yx); // Map the top quadrant to the alpha channel.
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
