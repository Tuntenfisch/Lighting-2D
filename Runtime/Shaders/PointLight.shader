Shader "Tuntenfisch/Lighting2D/PointLight"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE

        #include "Include/Common.hlsl"

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
                float2 positionLS = Lighting2D::GetPositionLightSpace(inputs.uv); //
                float lightingFactor = 1.0f - Lighting2D::GetShadowingFactor(positionLS); //
                float falloffFactor = Lighting2D::GetLinearLightFalloffFactor(positionLS, _LightFalloff);
                return lightingFactor * falloffFactor * _LightColor;
            }

            ENDHLSL
        }
    }
}
