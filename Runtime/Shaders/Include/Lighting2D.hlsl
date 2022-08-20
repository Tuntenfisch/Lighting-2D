#ifndef TUNTENFISCH_LIGHTING_2D_LIGHTING_2D
#define TUNTENFISCH_LIGHTING_2D_LIGHTING_2D

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_ShadowMapTexture);
SAMPLER(sampler_ShadowMapTexture);

float SampleShadowMap(float2 uv)
{
    uv = uv - 0.5f;
    int quadrant = 0;

    if (abs(uv.x) < abs(uv.y))
    {
        uv = uv.yx;
        quadrant = 2;
    }
    quadrant += uv.x < 0.0f ? 0 : 1;
    float2 direction = normalize(uv);
    float v = 0.5f - 0.5f * direction.y / direction.x;
    return SAMPLE_TEXTURE2D(_ShadowMapTexture, sampler_ShadowMapTexture, float2(0.5f, v))[quadrant];
}

#endif