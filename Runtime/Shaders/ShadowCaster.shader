Shader "Tuntenfisch/Lighting2D/ShadowCaster"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
    }

    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #pragma vertex VertexPass
        #pragma fragment FragmentPass

        struct VertexPassInputs
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct FragmentPassInputs
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        ENDHLSL

        Pass
        {
            Blend OneMinusSrcColor SrcColor

            HLSLPROGRAM

            FragmentPassInputs VertexPass(VertexPassInputs inputs)
            {
                FragmentPassInputs outputs;
                outputs.positionCS = GetVertexPositionInputs(inputs.positionOS.xyz).positionCS;
                outputs.uv = inputs.uv;
                return outputs;
            }

            float FragmentPass(FragmentPassInputs inputs) : SV_TARGET
            {
                return 1.0f - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, inputs.uv).a;
            }

            ENDHLSL
        }
    }
}
