#ifndef TUNTENFISCH_LIGHTING_2D_MATH
#define TUNTENFISCH_LIGHTING_2D_MATH


namespace Math
{
    // Normally HLSL offers "1.#INF" to get a floating point representation of infinity.
    // While this seems to work on my Nvidia GeForce GTX 1080Ti, it doesn't correctly work on my Nvidia GeForce 940MX.
    // So instead, we'll just use some arbitrary large float value for infinity.
    static const float infinity = float(1u << 31);

    float2 GetNormal(float2 vec)
    {
        return normalize(float2(vec.y, -vec.x));
    }
}

#endif