using System;
using Unity.Jobs;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    internal sealed class CullLightsPass : ScriptableRenderPass, IDisposable
    {
        #region Private Fields
        private Lighting2D m_rendererFeature;
        private JobHandle m_jobHandle;
        #endregion

        #region Public Methods


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.TryGetCullingParameters(out var cullingParameters))
            {
                m_jobHandle = m_rendererFeature.LightManager.Cull(ref cullingParameters);
            }
        }

        public void Dispose()
        {

        }
        #endregion

        #region Internal Methods
        internal CullLightsPass(Lighting2D rendererFeature)
        {
            m_rendererFeature = rendererFeature;
        }

        internal void EnsureCompletion()
        {
            m_jobHandle.Complete();
        }
        #endregion
    }
}