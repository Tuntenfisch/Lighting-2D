// ToDo: Implement some sort of soft shadow support:
//
//     https://developer.nvidia.com/gpugems/gpugems2/part-ii-shading-lighting-and-shadows/chapter-17-efficient-soft-edged-shadows-using
//     https://http.download.nvidia.com/developer/presentations/2005/SIGGRAPH/Percentage_Closer_Soft_Shadows.pdf
//     https://andrew-pham.blog/2019/08/03/percentage-closer-soft-shadows/
//
Shader "Tuntenfisch/Lighting2D/PointLight"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE

        #include "Include/Common.hlsl"

        #pragma shader_feature LIGHTING_2D_DEPTH_BIAS_ENABLED
        #pragma shader_feature LIGHTING_2D_SOFT_SHADOWS_ENABLED

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

        float _DepthBias;
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
                float lightingFactor = 1.0f - Lighting2D::GetShadowingFactor(inputs.uv, _DepthBias); //
                float falloffFactor = Lighting2D::GetLinearLightFalloffFactor(inputs.uv, _LightFalloff);
                return lightingFactor * falloffFactor * _LightColor;
            }

            ENDHLSL
        }
    }
}
