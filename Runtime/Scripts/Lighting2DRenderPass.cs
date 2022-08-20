using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D
{
    public class Lighting2DRenderPass : ScriptableRenderPass, IDisposable
    {
        #region Private Fields
        private readonly ShaderTagId[] c_shaderTags = new ShaderTagId[] { new ShaderTagId("UniversalForward") };

        private Lighting2DRendererFeature m_rendererFeature;
        private Material m_renderShadowCastersMaterial;
        private Material m_renderShadowMapMaterial;
        private Mesh m_shadowMapMesh;

        private RTHandle m_shadowCasterTextureHandle;
        private RTHandle m_shadowCasterDistanceTextureHandle;
        private RTHandle m_warpedShadowCasterDistanceTextureHandle;
        private RTHandle m_shadowMapTextureHandle;
        #endregion

        #region Public Methods
        public Lighting2DRenderPass(Lighting2DRendererFeature renderFeature)
        {
            m_rendererFeature = renderFeature;
            m_renderShadowCastersMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.RenderShadowCastersShader);
            m_renderShadowMapMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.RenderShadowMapShader);
            m_shadowMapMesh = MeshUtility.GenerateShadowMapMesh(m_rendererFeature.ShadowResolution / 2);
        }

        public void OnValidate()
        {
            if (m_shadowMapMesh != null)
            {
                CoreUtils.Destroy(m_shadowMapMesh);
            }
            m_shadowMapMesh = MeshUtility.GenerateShadowMapMesh(m_rendererFeature.ShadowResolution / 2);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_renderShadowCastersMaterial);
            CoreUtils.Destroy(m_renderShadowMapMaterial);
            CoreUtils.Destroy(m_shadowMapMesh);

            m_shadowCasterTextureHandle.Release();
            m_shadowCasterDistanceTextureHandle.Release();
            m_warpedShadowCasterDistanceTextureHandle.Release();
            m_shadowMapTextureHandle.Release();
        }

        public override void OnCameraSetup(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowCasterTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowResolution, m_rendererFeature.ShadowResolution, RenderTextureFormat.R8),
                name: "_ShadowCastersTexture"
            );
            commandBuffer.SetGlobalTexture(m_shadowCasterTextureHandle.name, m_shadowCasterTextureHandle);

            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowCasterDistanceTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowResolution, m_rendererFeature.ShadowResolution, RenderTextureFormat.RFloat),
                name: "_ShadowCasterDistancesTexture"
            );
            commandBuffer.SetGlobalTexture(m_shadowCasterDistanceTextureHandle.name, m_shadowCasterDistanceTextureHandle);

            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_warpedShadowCasterDistanceTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowResolution / 2, m_rendererFeature.ShadowResolution, RenderTextureFormat.ARGBFloat),
                name: "_WarpedShadowCasterDistancesTexture"
            );
            commandBuffer.SetGlobalTexture(m_warpedShadowCasterDistanceTextureHandle.name, m_warpedShadowCasterDistanceTextureHandle);

            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowMapTextureHandle,
                new RenderTextureDescriptor(1, m_rendererFeature.ShadowResolution, RenderTextureFormat.ARGBFloat),
                name: "_ShadowMapTexture"
            );
            commandBuffer.SetGlobalTexture(m_shadowMapTextureHandle.name, m_shadowMapTextureHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
            {
                return;
            }
            var commandBuffer = CommandBufferPool.Get();

            using (new ProfilingScope(commandBuffer, profilingSampler))
            {
                RenderShadowCasters(context, ref renderingData, commandBuffer);
                RenderShadowMap(context, ref renderingData, commandBuffer);
            }
            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }
        #endregion

        #region Private Methods
        private void RenderShadowCasters(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            commandBuffer.Blit(Texture2D.whiteTexture, m_shadowCasterTextureHandle);

            var rendererListDescriptor = new RendererListDesc(c_shaderTags, renderingData.cullResults, renderingData.cameraData.camera)
            {
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                overrideMaterial = m_renderShadowCastersMaterial,
                overrideMaterialPassIndex = 0,
                layerMask = m_rendererFeature.ShadowCasterLayer,
            };
            commandBuffer.DrawRendererList(context.CreateRendererList(rendererListDescriptor));
        }

        private void RenderShadowMap(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            commandBuffer.Blit(m_shadowCasterTextureHandle, m_shadowCasterDistanceTextureHandle, m_renderShadowMapMaterial, 0);
            commandBuffer.Blit(m_shadowCasterDistanceTextureHandle, m_warpedShadowCasterDistanceTextureHandle, m_renderShadowMapMaterial, 1);
            commandBuffer.Blit(Texture2D.whiteTexture, m_shadowMapTextureHandle);
            commandBuffer.Blit(m_warpedShadowCasterDistanceTextureHandle, m_shadowMapTextureHandle, m_shadowMapMesh, m_renderShadowMapMaterial, 2, renderingData.cameraData);
        }
        #endregion
    }
}