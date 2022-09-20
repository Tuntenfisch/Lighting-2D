#ifndef TUNTENFISCH_LIGHTING_2D_COMMON
#define TUNTENFISCH_LIGHTING_2D_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Math.hlsl"

TEXTURE2D(_ShadowMapTexture);
SAMPLER(sampler_ShadowMapTexture);
float4 _LightIlluminationSize;

namespace Lighting2D
{
    // -----------------------------------------------------------------------------------------------------------------------------
    // API for basic operations.
    // -----------------------------------------------------------------------------------------------------------------------------
    float2 GetPositionLightSpace(float2 uv)
    {
        // Transform UV coordinates to light space coordinates.
        return (uv - 0.5f) * _LightIlluminationSize.zw;
    }

    float GetDistanceToLight(float2 positionLS)
    {
        return length(positionLS);
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Basic sampling of shadow map.
    // -----------------------------------------------------------------------------------------------------------------------------
    float SampleShadowDistance(float2 positionLS)
    {
        // For sampling the shadow distance from the shadow map we need the uv coordinates in -0.5 to +0.5 range.
        float2 centeredUV = positionLS * _LightIlluminationSize.xy;
        int quadrant = 0;

        if (abs(centeredUV.x) < abs(centeredUV.y))
        {
            centeredUV = centeredUV.yx;
            quadrant = 2;
        }
        quadrant += centeredUV.x < 0.0f ? 0 : 1;
        float2 direction = normalize(centeredUV);
        float v = 0.5f * (1.0f - direction.y / direction.x);
        return SAMPLE_TEXTURE2D(_ShadowMapTexture, sampler_ShadowMapTexture, float2(0.5f, v))[quadrant];
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Implementation of basic hard shadows.
    // -----------------------------------------------------------------------------------------------------------------------------
    float GetHardShadowingFactor(float2 positionLS)
    {
        float distance = GetDistanceToLight(positionLS);
        float shadowDistance = SampleShadowDistance(positionLS);
        return shadowDistance - distance <= 0.0f;
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Implementation of realistic soft shadows based on PCSS:
    //
    //     https://developer.nvidia.com/gpugems/gpugems2/part-ii-shading-lighting-and-shadows/chapter-17-efficient-soft-edged-shadows-using
    //     https://http.download.nvidia.com/developer/presentations/2005/SIGGRAPH/Percentage_Closer_Soft_Shadows.pdf
    //     https://andrew-pham.blog/2019/08/03/percentage-closer-soft-shadows/
    // -----------------------------------------------------------------------------------------------------------------------------
    float EstimateShadowCasterDistance(float2 positionLS, float lightSize, int numberOfSamples)
    {
        float kernelWidth = lightSize * GetDistanceToLight(positionLS);
        float spacing = kernelWidth / numberOfSamples;
        float2 normalLS = Math::GetNormal(positionLS); //
        positionLS -= 0.5f * kernelWidth * normalLS;
        float distanceReceiver = GetDistanceToLight(positionLS);
        float shadowCasterDistance = 0.0f;
        int numberOfShadowCasters = 0;

        for (int sample = 0; sample < numberOfSamples; sample++)
        {
            float distance = SampleShadowDistance(positionLS);

            if (distance < distanceReceiver)
            {
                shadowCasterDistance += distance;
                numberOfShadowCasters++;
            }
            positionLS += spacing * normalLS;
        }
        return shadowCasterDistance / numberOfShadowCasters;
    }

    float EstimatePenumbraSize(float2 positionLS, float lightSize)
    {
        float receiverDistance = GetDistanceToLight(positionLS);
        float shadowCasterDistance = EstimateShadowCasterDistance(positionLS, lightSize, 33);
        return lightSize * (receiverDistance - shadowCasterDistance) / shadowCasterDistance;
    }

    float PercentageCloserFiltering(float2 positionLS, float kernelWidth, int numberOfSamples)
    {
        float2 normalLS = Math::GetNormal(positionLS);
        float spacing = kernelWidth / numberOfSamples;
        positionLS -= 0.5f * kernelWidth * normalLS;
        float shadowFactor = 0.0f;

        for (float sample = 0; sample < numberOfSamples; sample++)
        {
            shadowFactor += GetHardShadowingFactor(positionLS);
            positionLS += spacing * normalLS;
        }
        return shadowFactor / numberOfSamples;
    }

    float PercentageCloserSoftShadows(float2 positionLS)
    {
        float penumbraSize = EstimatePenumbraSize(positionLS, 0.25f);
        return PercentageCloserFiltering(positionLS, penumbraSize, 33);
    }

    float GetSoftShadowingFactor(float2 positionLS)
    {
        return PercentageCloserSoftShadows(positionLS);
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    //
    // -----------------------------------------------------------------------------------------------------------------------------
    float GetShadowingFactor(float2 positionLS)
    {
        #if LIGHTING_2D_SOFT_SHADOWS_ENABLED
            return GetSoftShadowingFactor(positionLS);
        #else
            return GetHardShadowingFactor(positionLS);
        #endif
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Implementation of smooth light falloff.
    // -----------------------------------------------------------------------------------------------------------------------------
    float GetLinearLightFalloffFactor(float2 positionLS, float lightFalloff)
    {
        float distance = GetDistanceToLight(positionLS);
        float interpolant = distance * 2.0f * max(_LightIlluminationSize.x, _LightIlluminationSize.y);
        return 1.0f - smoothstep(1.0f - lightFalloff, 1.0f, interpolant);
    }
};

#endif