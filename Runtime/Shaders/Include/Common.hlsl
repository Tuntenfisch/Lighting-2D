#ifndef TUNTENFISCH_LIGHTING_2D_COMMON
#define TUNTENFISCH_LIGHTING_2D_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_ShadowMapTexture);
float4 _ShadowMapTexture_TexelSize;
SAMPLER(sampler_ShadowMapTexture);
float2 _LightIlluminationSize;

namespace Lighting2D
{
    // -----------------------------------------------------------------------------------------------------------------------------
    // API mostly for internal usage.
    // -----------------------------------------------------------------------------------------------------------------------------
    float2 GetPositionLightSpace(float2 uv)
    {
        // Transform UV coordinates to light space coordinates.
        return (uv - 0.5f) * _LightIlluminationSize;
    }

    float GetDistanceSquaredToLight(float2 uv)
    {
        float2 positionLS = GetPositionLightSpace(uv);
        return dot(positionLS, positionLS);
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // API for sampling the shadow map.
    // -----------------------------------------------------------------------------------------------------------------------------
    float SampleShadowDistanceSquared(float2 uv)
    {
        uv -= 0.5f;
        int quadrant = 0;

        if (abs(uv.x) < abs(uv.y))
        {
            uv = uv.yx;
            quadrant = 2;
        }
        quadrant += uv.x < 0.0f ? 0 : 1;
        float2 direction = normalize(uv);
        float v = 0.5f - 0.5f * direction.y / direction.x;
        float normalizedDistanceSquared = SAMPLE_TEXTURE2D(_ShadowMapTexture, sampler_ShadowMapTexture, float2(0.5f, v))[quadrant];
        return normalizedDistanceSquared;
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // API for getting the shadowing factor with deph bias support.
    // -----------------------------------------------------------------------------------------------------------------------------
    float GetHardShadowingFactor(float2 uv, float depthBias = 0.0f)
    {
        float distanceSquared = GetDistanceSquaredToLight(uv);
        float shadowDistanceSquared = SampleShadowDistanceSquared(uv);

        #if LIGHTING_2D_DEPTH_BIAS_ENABLED
            return sqrt(shadowDistanceSquared) - sqrt(distanceSquared) + depthBias <= 0.0f;
        #else
            return shadowDistanceSquared - distanceSquared <= 0.0f;
        #endif
    }

    float GetSoftShadowingFactor(float2 uv, float depthBias = 0.0f)
    {
        return 0.0f;
    }

    float GetShadowingFactor(float2 uv, float depthBias = 0.0f)
    {
        #if LIGHTING_2D_SOFT_SHADOWS_ENABLED
            return GetSoftShadowingFactor(uv, depthBias);
        #else
            return GetHardShadowingFactor(uv, depthBias);
        #endif
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // API for smooth light falloff.
    // -----------------------------------------------------------------------------------------------------------------------------
    float GetLinearLightFalloffFactor(float2 uv, float lightFalloff)
    {
        float distance = sqrt(GetDistanceSquaredToLight(uv));
        float interpolant = distance / (0.5f * min(_LightIlluminationSize.x, _LightIlluminationSize.y));
        return (1.0f - smoothstep(1.0f - lightFalloff, 1.0f, interpolant));
    }
};

#endif