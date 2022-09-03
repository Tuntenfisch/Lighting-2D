using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    public static class ShaderInfo
    {
        #region Public Fields
        // -----------------------------------------------------------------------------------------------------------------------------
        // Keywords
        // -----------------------------------------------------------------------------------------------------------------------------
        public const string DepthBiasEnabledKeyword = "LIGHTING_2D_DEPTH_BIAS_ENABLED";
        public const string SoftShadowsEnabledKeyword = "LIGHTING_2D_SOFT_SHADOWS_ENABLED";

        // -----------------------------------------------------------------------------------------------------------------------------
        // Shaders
        // -----------------------------------------------------------------------------------------------------------------------------#
        public const string ShadowCasterShaderName = "Tuntenfisch/Lighting2D/ShadowCaster";
        public const string ShadowMapShaderName = "Tuntenfisch/Lighting2D/ShadowMap";
        public const string PointLightShaderName = "Tuntenfisch/Lighting2D/PointLight";

        // -----------------------------------------------------------------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------------------------------------------------------------
        public static readonly int LightIlluminationSizeID = Shader.PropertyToID("_LightIlluminationSize");
        public static readonly int OneMinusShadowCasterAlphaThresholdID = Shader.PropertyToID("_OneMinusShadowCasterAlphaThreshold");

        // -----------------------------------------------------------------------------------------------------------------------------
        // Textures
        // -----------------------------------------------------------------------------------------------------------------------------
        public const string ShadowCastersTextureName = "_ShadowCastersTexture";
        public const string ShadowMapTextureName = "_ShadowMapTexture";
        #endregion
    }
}