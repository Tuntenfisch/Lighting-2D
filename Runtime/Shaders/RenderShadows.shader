Shader "Tuntenfisch/Lighting2D/RenderShadows"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE

        #include "Include/Lighting2D.hlsl"

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

        ENDHLSL

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            FragmentPassInputs VertexPass(VertexPassInputs inputs)
            {
                FragmentPassInputs outputs;
                outputs.positionCS = GetVertexPositionInputs(inputs.positionOS.xyz).positionCS;
                outputs.uv = inputs.uv;
                return outputs;
            }

            float4 FragmentPass(FragmentPassInputs inputs) : SV_Target
            {
                float distance = length(inputs.uv - 0.5f);
                float shadowDistance = SampleShadowMap(inputs.uv);
                float3 shadowColor = float3(0.0f, 0.0f, 0.0f);
                float alpha = distance > shadowDistance ? 0.1f : 0.0f;
                return float4(shadowColor, alpha);
            }

            ENDHLSL
        }
    }
}
