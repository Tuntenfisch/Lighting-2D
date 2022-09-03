using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    [DisallowMultipleRendererFeature("Lighting 2D")]
    public class RendererFeature : ScriptableRendererFeature
    {
        #region Public Properties
        public FilterMode FilterMode => m_filterMode;
        public FloatPrecision FloatPrecision => m_floatPrecision;
        public float ShadowCasterAlphaThreshold => m_shadowCasterAlphaThreshold;
        public int ShadowMapResolution => m_shadowMapResolution;
        public Shader ShadowCasterShader => m_shadowCasterShader;
        public Shader ShadowMapShader => m_shadowMapShader;
        #endregion

        #region Inspector Fields
        [Header("General")]
        [SerializeField]
        private FilterMode m_filterMode = FilterMode.Bilinear;
        [SerializeField]
        private FloatPrecision m_floatPrecision = FloatPrecision.Float16;

        [Header("Shadows")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_shadowCasterAlphaThreshold = 0.25f;
        [Range(256, 8192)]
        [SerializeField]
        private int m_shadowMapResolution = 2048;

        [HideInInspector]
        [SerializeField]
        private Shader m_shadowCasterShader;
        [HideInInspector]
        [SerializeField]
        private Shader m_shadowMapShader;
        #endregion

        #region Private Fields
        private RenderPass m_renderPass;
        #endregion

        #region Unity Events
        private void OnValidate()
        {
            m_shadowMapResolution = Mathf.ClosestPowerOfTwo(m_shadowMapResolution);

            if (m_renderPass != null)
            {
                m_renderPass.OnValidate();
            }
        }
        #endregion

        #region Public Methods
        public override void Create()
        {
            m_shadowCasterShader = Shader.Find(ShaderInfo.ShadowCasterShaderName);
            m_shadowMapShader = Shader.Find(ShaderInfo.ShadowMapShaderName);
            m_renderPass = new RenderPass(this);
            m_renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_renderPass.Dispose();
        }
        #endregion
    }
}