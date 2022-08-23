using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    public class RenderPass : ScriptableRenderPass, IDisposable
    {
        #region Private Fields
        private readonly ShaderTagId[] c_shaderTags = new ShaderTagId[] { new ShaderTagId("UniversalForward") };
        private readonly Matrix4x4 c_orthographicViewMatrix = Matrix4x4.TRS(new float3(0.0f, 0.0f, -1.0f), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
        private readonly Matrix4x4 c_orthographicProjectionMatrix = Matrix4x4.Ortho(-0.5f, 0.5f, -0.5f, 0.5f, 0.3f, 1000.0f);

        private RendererFeature m_rendererFeature;
        private Material m_renderShadowMapMaterial;
        private MaterialPropertyBlock m_renderShadowMapMaterialProperties;
        private MaterialPropertyBlock m_renderEntityMaterialProperties;
        private Mesh m_shadowMapMesh;

        private RTHandle m_shadowCastersTextureHandle;
        private RTHandle m_warpedShadowCasterDistancesTextureHandle;
        private RTHandle m_shadowMapTextureHandle;
        #endregion

        #region Public Methods
        public RenderPass(RendererFeature renderFeature)
        {
            m_rendererFeature = renderFeature;
            m_renderShadowMapMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.RenderShadowMapShader);
            m_renderShadowMapMaterialProperties = new MaterialPropertyBlock();
            m_renderEntityMaterialProperties = new MaterialPropertyBlock();
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
            CoreUtils.Destroy(m_renderShadowMapMaterial);
            CoreUtils.Destroy(m_shadowMapMesh);

            m_shadowCastersTextureHandle?.Release();
            m_warpedShadowCasterDistancesTextureHandle?.Release();
            m_shadowMapTextureHandle?.Release();
        }

        public override void OnCameraSetup(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowCastersTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowResolution, m_rendererFeature.ShadowResolution, RenderTextureFormat.R8),
                name: "_ShadowCastersTexture"
            );
            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_warpedShadowCasterDistancesTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowResolution / 2, m_rendererFeature.ShadowResolution, RenderTextureFormat.ARGBFloat),
                name: "_WarpedShadowCasterDistancesTexture"
            );
            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowMapTextureHandle,
                new RenderTextureDescriptor(1, m_rendererFeature.ShadowResolution, RenderTextureFormat.ARGBFloat),
                name: "_ShadowMapTexture"
            );

#if UNITY_EDITOR
            commandBuffer.SetGlobalTexture(m_shadowCastersTextureHandle.name, m_shadowCastersTextureHandle);
            commandBuffer.SetGlobalTexture(m_warpedShadowCasterDistancesTextureHandle.name, m_warpedShadowCasterDistancesTextureHandle);
#endif
            commandBuffer.SetGlobalTexture(m_shadowMapTextureHandle.name, m_shadowMapTextureHandle);

            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get();

            using (new ProfilingScope(commandBuffer, profilingSampler))
            {
                RenderLights(context, ref renderingData, commandBuffer);
            }
            CommandBufferPool.Release(commandBuffer);
        }
        #endregion

        #region Private Methods
        private void RenderLights(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            var camera = renderingData.cameraData.camera;
            Debug.Assert(camera.TryGetCullingParameters(out var cullingParameters));
            EntityManager.Cull(ref cullingParameters, camera);

            foreach (var entity in EntityManager.VisibileEntities)
            {
                var entityProperties = entity.GetProperties();
                var entityPosition = (float3)entityProperties.Bounds.center;
                var entityExtents = (float3)entityProperties.Bounds.extents;
                var viewMatrix = Matrix4x4.TRS(new float3(-entityPosition.xy, camera.transform.position.z), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
                var projectionMatrix = Matrix4x4.Ortho(-entityExtents.x, entityExtents.x, -entityExtents.y, entityExtents.y, camera.nearClipPlane, camera.farClipPlane);
                var localToWorldMatrix = Matrix4x4.TRS(entityPosition, Quaternion.identity, new float3(entityExtents.x, entityExtents.y, 1.0f));

                // First we render the shadow map for the current light,
                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                RenderShadowCasters(context, ref renderingData, commandBuffer);
                RenderShadowMap(context, ref renderingData, commandBuffer);

                // After the shadow map has been rendered, we render the light itself using a quad.
                commandBuffer.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
                commandBuffer.SetRenderTarget(colorAttachmentHandle, depthAttachmentHandle);
                m_renderEntityMaterialProperties.Clear();
                entityProperties.SetMaterialPropertiesAction(m_renderEntityMaterialProperties);
                commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, localToWorldMatrix, entityProperties.Material, 0, 0, m_renderEntityMaterialProperties);
                context.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Clear();
            }
        }

        private void RenderShadowCasters(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            commandBuffer.SetRenderTarget(m_shadowCastersTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, Color.white, 1.0f, 0);

            var rendererListDescriptor = new RendererListDesc(c_shaderTags, renderingData.cullResults, renderingData.cameraData.camera)
            {
                renderQueueRange = RenderQueueRange.transparent,
                sortingCriteria = SortingCriteria.BackToFront,
                overrideMaterial = m_renderShadowMapMaterial,
                overrideMaterialPassIndex = 0,
                layerMask = m_rendererFeature.ShadowCasterLayer
            };
            commandBuffer.DrawRendererList(context.CreateRendererList(rendererListDescriptor));
        }

        private void RenderShadowMap(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            // First, we use the previously rendered shadow caster texture to replace each pixel containing a shadow caster with the distance to the center of the texture.
            // Additionally, within the same pass, we warp that distance in such a way, that the shadow casters appear to be seen from the light source itself.
            commandBuffer.SetRenderTarget(m_warpedShadowCasterDistancesTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.Blit(m_shadowCastersTextureHandle, BuiltinRenderTextureType.CurrentActive, m_renderShadowMapMaterial, 1);

            // Finally, we render the shadow map itself using the warped shadow caster distances texture.
            // In this last step we want to find the minimum value along each row and save that to the corresponding row of the shadow map texture.
            // It effectively boils down to performing a row-wise min-reduction of the warped shadow caster distances texture.
            //
            // To achieve this, we use a special mesh that consists of an appropriate amount of overlayed fullscreen quads, whereas each
            // quad's uv coordinates map to one column of the warped shadow caster distance texture. The overlayed quads are then rendered using
            //
            //     Blend One One
            //     BlendOp Min
            //
            // which effectively performs the aforementioned row-wise min-reduction. It's basically a blit operation that is using our special
            // custom mesh instead of a fullscreen quad.
            commandBuffer.SetRenderTarget(m_shadowMapTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, Color.white, 1.0f, 0);
            m_renderShadowMapMaterialProperties.SetTexture("_MainTex", m_warpedShadowCasterDistancesTextureHandle);
            commandBuffer.SetViewProjectionMatrices(c_orthographicViewMatrix, c_orthographicProjectionMatrix);
            commandBuffer.DrawMesh(m_shadowMapMesh, Matrix4x4.identity, m_renderShadowMapMaterial, 0, 2, m_renderShadowMapMaterialProperties);
        }
        #endregion
    }
}