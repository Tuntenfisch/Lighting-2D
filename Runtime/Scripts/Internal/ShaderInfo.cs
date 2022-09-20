using UnityEngine;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class ShaderInfo
    {
        #region Public Fields
        // -----------------------------------------------------------------------------------------------------------------------------
        // Shader Keywords
        // -----------------------------------------------------------------------------------------------------------------------------
        public const string SoftShadowsEnabledKeyword = "LIGHTING_2D_SOFT_SHADOWS_ENABLED";

        // -----------------------------------------------------------------------------------------------------------------------------
        // Shader Names
        // -----------------------------------------------------------------------------------------------------------------------------#
        public const string ShadowCasterShaderName = "Tuntenfisch/Lighting2D/ShadowCaster";
        public const string ShadowMapShaderName = "Tuntenfisch/Lighting2D/ShadowMap";

        // -----------------------------------------------------------------------------------------------------------------------------
        // Shader Properties
        // -----------------------------------------------------------------------------------------------------------------------------
        public static readonly int MainTextureID = Shader.PropertyToID("_MainTex");
        public static readonly int ShadowDepthBiasID = Shader.PropertyToID("_ShadowDepthBias");
        public static readonly int OneMinusShadowCasterAlphaThresholdID = Shader.PropertyToID("_OneMinusShadowCasterAlphaThreshold");
        public static readonly int LightIlluminationSizeID = Shader.PropertyToID("_LightIlluminationSize");

        // -----------------------------------------------------------------------------------------------------------------------------
        // Textures Names
        // -----------------------------------------------------------------------------------------------------------------------------
        public const string ShadowCastersTextureName = "_ShadowCastersTexture";
        public const string ShadowMapTextureName = "_ShadowMapTexture";
        #endregion
    }
}