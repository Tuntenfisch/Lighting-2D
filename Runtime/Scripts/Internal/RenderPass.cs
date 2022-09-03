using System;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    public class RenderPass : ScriptableRenderPass, IDisposable
    {
        #region Private Fields
        private readonly ShaderTagId[] c_shaderTags = new ShaderTagId[] { new ShaderTagId("UniversalForward") };
        private readonly Color c_shadowMapClearColor = new Color(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        private readonly Matrix4x4 c_orthographicViewMatrix = Matrix4x4.TRS(new float3(0.0f, 0.0f, -1.0f), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
        private readonly Matrix4x4 c_orthographicProjectionMatrix = Matrix4x4.Ortho(-1.0f, 1.0f, -1.0f, 1.0f, 0.3f, 1000.0f);

        private RendererFeature m_rendererFeature;

        // -----------------------------------------------------------------------------------------------------------------------------
        // Fields related to shadow map generation.
        // -----------------------------------------------------------------------------------------------------------------------------
        private Material m_shadowCasterMaterial;
        private Material m_shadowMapMaterial;
        private MaterialPropertyBlock m_shadowMapMaterialProperties;
        private Mesh m_shadowMapMesh;

        // -----------------------------------------------------------------------------------------------------------------------------
        // Render textures needed.
        // -----------------------------------------------------------------------------------------------------------------------------
        private RTHandle m_shadowCastersTextureHandle;
        private RTHandle m_shadowMapTextureHandle;
        #endregion

        #region Public Methods
        public RenderPass(RendererFeature renderFeature)
        {
            m_rendererFeature = renderFeature;
            m_shadowCasterMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.ShadowCasterShader);
            m_shadowMapMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.ShadowMapShader);
            m_shadowMapMaterialProperties = new MaterialPropertyBlock();
            // Initialize the remaining fields.
            OnValidate();
        }

        public void OnValidate()
        {
            m_shadowMapMaterial.SetFloat(ShaderInfo.OneMinusShadowCasterAlphaThresholdID, 1.0f - m_rendererFeature.ShadowCasterAlphaThreshold);

            if (m_shadowMapMesh != null)
            {
                CoreUtils.Destroy(m_shadowMapMesh);
            }
            m_shadowMapMesh = MeshUtility.GenerateShadowMapMesh(m_rendererFeature.ShadowMapResolution / 2);

            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowCastersTextureHandle,
                new RenderTextureDescriptor(m_rendererFeature.ShadowMapResolution, m_rendererFeature.ShadowMapResolution, RenderTextureFormat.R8, 0, 0),
                m_rendererFeature.FilterMode,
                TextureWrapMode.Clamp,
                name: ShaderInfo.ShadowCastersTextureName
            );

            var textureFormat = m_rendererFeature.FloatPrecision switch
            {
                FloatPrecision.Float32 => RenderTextureFormat.ARGBFloat,
                FloatPrecision.Float16 => RenderTextureFormat.ARGBHalf,
                _ => throw new ArgumentException()
            };
            RenderingUtils.ReAllocateIfNeeded
            (
                ref m_shadowMapTextureHandle,
                new RenderTextureDescriptor(1, m_rendererFeature.ShadowMapResolution, textureFormat, 0, 0),
                m_rendererFeature.FilterMode,
                TextureWrapMode.Clamp,
                name: ShaderInfo.ShadowMapTextureName
            );
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_shadowCasterMaterial);
            CoreUtils.Destroy(m_shadowMapMaterial);
            CoreUtils.Destroy(m_shadowMapMesh);

            m_shadowCastersTextureHandle?.Release();
            m_shadowMapTextureHandle?.Release();
        }

        public override void OnCameraSetup(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            commandBuffer.SetGlobalTexture(m_shadowCastersTextureHandle.name, m_shadowCastersTextureHandle);
#endif
            commandBuffer.SetGlobalTexture(m_shadowMapTextureHandle.name, m_shadowMapTextureHandle);
            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get();

            using (new ProfilingScope(commandBuffer, profilingSampler))
            {
                RenderLights(ref context, ref renderingData, commandBuffer);
            }
            CommandBufferPool.Release(commandBuffer);
        }
        #endregion

        #region Private Methods
        private void RenderLights(ref ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            var camera = renderingData.cameraData.camera;
            Debug.Assert(camera.TryGetCullingParameters(out var cullingParameters));
            LightManager.Cull(ref cullingParameters);

            foreach (var light in LightManager.VisibleLights)
            {
                var lightProperties = light.GetLightProperties();
                var lightPosition = (float3)lightProperties.Bounds.center;
                var lightExtents = (float3)lightProperties.Bounds.extents;
                var viewMatrix = Matrix4x4.TRS(new float3(-lightPosition.xy, camera.transform.position.z), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
                var projectionMatrix = Matrix4x4.Ortho(-lightExtents.x, lightExtents.x, -lightExtents.y, lightExtents.y, camera.nearClipPlane, camera.farClipPlane);
                var localToWorldMatrix = Matrix4x4.TRS(lightPosition, Quaternion.identity, new float3(lightExtents.x, lightExtents.y, 1.0f));

                // First, render the relevant shadow casters.
                RenderShadowCasters(ref lightProperties, ref viewMatrix, ref projectionMatrix, ref context, ref renderingData, commandBuffer);
                // Next, render the shadow map for the current light.
                RenderShadowMap(ref lightProperties, ref renderingData, commandBuffer);
                // After the shadow map has been rendered, render the light itself using a quad.
                RenderLight(ref lightProperties, ref localToWorldMatrix, ref renderingData, commandBuffer);

                context.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Clear();
            }
        }

        private void RenderShadowCasters(ref LightProperties lightProperties, ref Matrix4x4 viewMatrix, ref Matrix4x4 projectionMatrix, ref ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            commandBuffer.SetRenderTarget(m_shadowCastersTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, Color.white, 1.0f, 0);

            ShadowCasterManager.Cull(ref lightProperties);

            foreach (var shadowCaster in ShadowCasterManager.VisibleShadowCasters)
            {
                commandBuffer.DrawRenderer(shadowCaster.Renderer, m_shadowCasterMaterial, 0, 0);
            }
        }

        private unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            return default;
        }

        private void RenderShadowMap(ref LightProperties lightProperties, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            // The shader used to render the shadow map does a couple things at once:
            //
            //     First, we use the previously rendered shadow caster texture to replace each pixel containing a shadow caster with the distance to the light (texture center).
            //
            //     Additionally, within the same pass, we warp the distances in such a way, that the shadow casters appear to be seen from the light source itself.
            //     Now each light ray the light is sending out will be mapped to one row along our texture. A visually representation can be found here:
            //
            //     http://www.catalinzima.com/2010/07/my-technique-for-the-shader-based-dynamic-2d-shadows/
            //
            //     Finally, to obtain the actual shadow map, we want to find the minimum distance to a shadow caster along each light ray and save that to the corresponding
            //     row of the shadow map texture. It effectively boils down to performing a row-wise min-reduction of the warped shadow caster distances texture.
            //
            //     To achieve this, we use a special mesh that consists of an appropriate amount of overlayed fullscreen quads, whereas each
            //     quad's uv coordinates map to one column of the warped shadow caster distance texture. The overlayed quads are then rendered using
            //
            //         Blend One One
            //         BlendOp Min
            //
            //     which effectively performs the aforementioned row-wise min-reduction. It's basically a blit operation that is using our special
            //     custom mesh instead of a fullscreen quad.
            //
            m_shadowMapMaterialProperties.SetTexture("_MainTex", m_shadowCastersTextureHandle);
            m_shadowMapMaterialProperties.SetVector(ShaderInfo.LightIlluminationSizeID, new float4(2.0f * lightProperties.Extents, 0.0f, 0.0f));
            commandBuffer.SetRenderTarget(m_shadowMapTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, c_shadowMapClearColor, 1.0f, 0);
            commandBuffer.SetViewProjectionMatrices(c_orthographicViewMatrix, c_orthographicProjectionMatrix);
            commandBuffer.DrawMesh(m_shadowMapMesh, Matrix4x4.identity, m_shadowMapMaterial, 0, 0, m_shadowMapMaterialProperties);
        }

        private void RenderLight(ref LightProperties lightProperties, ref Matrix4x4 localToWorldMatrix, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            lightProperties.MaterialPropertyBlock.SetVector(ShaderInfo.LightIlluminationSizeID, new float4(2.0f * lightProperties.Extents, 0.0f, 0.0f));
            commandBuffer.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
            commandBuffer.SetRenderTarget(colorAttachmentHandle, depthAttachmentHandle);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, localToWorldMatrix, lightProperties.Material, 0, 0, lightProperties.MaterialPropertyBlock);
        }
        #endregion
    }
}