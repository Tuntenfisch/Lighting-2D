using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    [DisallowMultipleRendererFeature("Lighting 2D")]
    public class RendererFeature : ScriptableRendererFeature
    {
        #region Public Properties
        public int ShadowResolution => m_shadowResolution;
        public LayerMask ShadowCasterLayer => m_shadowCasterLayer;
        public Shader RenderShadowMapShader => m_renderShadowMapShader;
        #endregion

        #region Inspector Fields
        [Header("Shadows")]
        [Range(256, 4096)]
        [SerializeField]
        private int m_shadowResolution = 512;
        [SerializeField]
        private LayerMask m_shadowCasterLayer;

        [HideInInspector]
        [SerializeField]
        private Shader m_renderShadowMapShader;
        #endregion

        #region Private Fields
        private RenderPass m_pass;
        #endregion

        #region Unity Events
        private void OnValidate()
        {
            m_shadowResolution = Mathf.ClosestPowerOfTwo(m_shadowResolution);

            if (m_pass != null)
            {
                m_pass.OnValidate();
            }
        }
        #endregion

        #region Public Methods
        public override void Create()
        {
            m_renderShadowMapShader = Shader.Find("Tuntenfisch/Lighting2D/RenderShadowMap");
            m_pass = new RenderPass(this);
            m_pass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_pass.Dispose();
        }
        #endregion
    }
}