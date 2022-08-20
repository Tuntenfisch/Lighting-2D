Shader "Tuntenfisch/Lighting2D/RenderShadowCasters"
{
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
    }
}
