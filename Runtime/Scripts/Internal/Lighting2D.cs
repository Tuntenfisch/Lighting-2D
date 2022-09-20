using System;
using Tuntenfisch.Lighting2D.Common;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    [DisallowMultipleRendererFeature("Lighting 2D")]
    internal sealed class Lighting2D : ScriptableRendererFeature, ISerializationCallbackReceiver
    {
        #region Internal Properties
        internal static event Action OnShadowCasterRenderingLayerChanged;
        internal static uint ShadowCasterRenderingLayer { get; private set; }

        internal FilterMode FilterMode => m_filterMode;
        internal FloatPrecision FloatPrecision => m_floatPrecision;
        internal int ShadowMapResolution => (int)m_resolution;
        internal float ShadowCasterAlphaThreshold => m_alphaThreshold;
        internal float ShadowDepthBias => m_depthBias;
        internal Shader ShadowCasterShader => m_shadowCasterShader;
        internal Shader ShadowMapShader => m_shadowMapShader;
        internal LightManager LightManager => m_lightManager;
        internal CullLightsPass CullLightsPass => m_cullLightsPass;
        internal RenderLightsPass RenderLightsPass => m_renderLightsPass;
        #endregion

        #region Inspector Fields
        [Header("General")]
        [SerializeField]
        private FilterMode m_filterMode = FilterMode.Bilinear;
        [SerializeField]
        private FloatPrecision m_floatPrecision = FloatPrecision.Float16;

        [Header("Shadows")]
        [RenderingLayer]
        [SerializeField]
        private uint m_renderingLayer = 8u;
        [SerializeField]
        private UnityEngine.Rendering.Universal.ShadowResolution m_resolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_alphaThreshold = 0.25f;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_depthBias;

        [HideInInspector]
        [SerializeField]
        private Shader m_shadowCasterShader;
        [HideInInspector]
        [SerializeField]
        private Shader m_shadowMapShader;
        #endregion

        #region Private Fields
        private LightManager m_lightManager;
        private CullLightsPass m_cullLightsPass;
        private RenderLightsPass m_renderLightsPass;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            Dispose();
        }

        private void OnValidate()
        {
            if (m_renderLightsPass != null)
            {
                m_renderLightsPass.OnValidate();
            }
            UpdateShadowCasterRenderingLayer(m_renderingLayer);
        }
        #endregion

        #region Public Methods
        public override void Create()
        {
            m_shadowCasterShader = Shader.Find(ShaderInfo.ShadowCasterShaderName);
            m_shadowMapShader = Shader.Find(ShaderInfo.ShadowMapShaderName);
            m_lightManager = new LightManager();
            m_cullLightsPass = new CullLightsPass(this);
            m_cullLightsPass.renderPassEvent = RenderPassEvent.BeforeRendering;
            m_renderLightsPass = new RenderLightsPass(this);
            m_renderLightsPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            // Initialize the static ShadowCasterRenderingLayer field.
            UpdateShadowCasterRenderingLayer(m_renderingLayer);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_cullLightsPass);
            renderer.EnqueuePass(m_renderLightsPass);
        }

        public void OnBeforeSerialize()
        {
            m_renderingLayer = ShadowCasterRenderingLayer;
        }

        public void OnAfterDeserialize() { }
        #endregion

        #region Protected Methods
        protected override void Dispose(bool disposing)
        {
            m_lightManager?.Dispose();
            m_lightManager = null;
            m_cullLightsPass?.Dispose();
            m_cullLightsPass = null;
            m_renderLightsPass?.Dispose();
            m_renderLightsPass = null;
        }
        #endregion

        #region Private Methods
        private void UpdateShadowCasterRenderingLayer(uint value)
        {
            bool valueChanged = ShadowCasterRenderingLayer != value;
            ShadowCasterRenderingLayer = value;

            if (valueChanged)
            {
                OnShadowCasterRenderingLayerChanged?.Invoke();
            }
        }
        #endregion
    }
}