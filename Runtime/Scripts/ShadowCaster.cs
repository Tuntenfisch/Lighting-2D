using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public sealed class ShadowCaster : MonoBehaviour
    {
        #region Private Fields
        private Renderer m_renderer;
        private uint m_renderingLayer;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            if (m_renderer == null)
            {
                m_renderer = GetComponent<Renderer>();
            }
            m_renderingLayer = Internal.Lighting2D.ShadowCasterRenderingLayer;
            m_renderer.renderingLayerMask |= m_renderingLayer;
            Internal.Lighting2D.OnShadowCasterRenderingLayerChanged += UpdateRenderingLayer;
        }

        private void OnDisable()
        {
            m_renderer.renderingLayerMask &= ~m_renderingLayer;
            Internal.Lighting2D.OnShadowCasterRenderingLayerChanged -= UpdateRenderingLayer;
        }
        #endregion

        #region Private Methods
        private void UpdateRenderingLayer()
        {
            m_renderer.renderingLayerMask &= ~m_renderingLayer;
            m_renderingLayer = Internal.Lighting2D.ShadowCasterRenderingLayer;
            m_renderer.renderingLayerMask |= m_renderingLayer;
        }
        #endregion
    }
}