Shader "Tuntenfisch/Lighting2D/RenderLight"
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

        float _LightFalloff;
        float4 _LightColor;

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
                float2 uv = inputs.uv - 0.5f;
                float distanceSquared = dot(uv, uv);
                float shadowDistanceSquared = SampleShadowDistanceSquaredFromShadowMap(inputs.uv);
                clip(shadowDistanceSquared - distanceSquared);
                return (1.0f - smoothstep(_LightFalloff, 1.0f, 2.0f * sqrt(distanceSquared))) * _LightColor;
            }

            ENDHLSL
        }
    }
}
