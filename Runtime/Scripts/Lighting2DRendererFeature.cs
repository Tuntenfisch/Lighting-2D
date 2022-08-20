using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D
{
    [DisallowMultipleRendererFeature("Lighting 2D")]
    public class Lighting2DRendererFeature : ScriptableRendererFeature
    {
        #region Public Properties
        public int ShadowResolution => m_shadowResolution;
        public LayerMask ShadowCasterLayer => m_shadowCasterLayer;
        public Shader RenderShadowCastersShader => m_renderShadowCastersShader;
        public Shader RenderShadowMapShader => m_renderShadowMapShader;
        public Shader RenderShadowsShader => m_renderShadowsShader;
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
        private Shader m_renderShadowCastersShader;
        [HideInInspector]
        [SerializeField]
        private Shader m_renderShadowMapShader;
        [HideInInspector]
        [SerializeField]
        private Shader m_renderShadowsShader;
        #endregion

        #region Private Fields
        private Lighting2DRenderPass m_pass;
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
            m_renderShadowCastersShader = Shader.Find("Tuntenfisch/Lighting2D/RenderShadowCasters");
            m_renderShadowMapShader = Shader.Find("Tuntenfisch/Lighting2D/RenderShadowMap");
            m_renderShadowsShader = Shader.Find("Tuntenfisch/Lighting2D/RenderShadows");
            m_pass = new Lighting2DRenderPass(this);
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