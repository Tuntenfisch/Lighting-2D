using System;
using Tuntenfisch.Lighting2D.Common;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D.Internal
{
    internal sealed class RenderLightsPass : ScriptableRenderPass, IDisposable
    {
        #region Private Fields
        private readonly ShaderTagId[] c_shaderTags = new ShaderTagId[] { new ShaderTagId("UniversalForward") };
        private readonly Plane[] c_cullingPlanes = new Plane[6];
        private readonly Color c_shadowMapClearColor = new Color(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        private readonly Matrix4x4 c_orthographicViewMatrix = Matrix4x4.TRS(new float3(0.0f, 0.0f, -1.0f), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
        private readonly Matrix4x4 c_orthographicProjectionMatrix = Matrix4x4.Ortho(-1.0f, 1.0f, -1.0f, 1.0f, 0.3f, 1000.0f);

        private Lighting2D m_rendererFeature;

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

        public void Dispose()
        {
            CoreUtils.Destroy(m_shadowCasterMaterial);
            m_shadowCasterMaterial = null;
            CoreUtils.Destroy(m_shadowMapMaterial);
            m_shadowMapMaterial = null;
            CoreUtils.Destroy(m_shadowMapMesh);
            m_shadowMapMesh = null;

            m_shadowCastersTextureHandle?.Release();
            m_shadowCastersTextureHandle = null;
            m_shadowMapTextureHandle?.Release();
            m_shadowMapTextureHandle = null;
        }
        #endregion

        #region Internal Methods
        internal RenderLightsPass(Lighting2D renderFeature)
        {
            m_rendererFeature = renderFeature;
            m_shadowCasterMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.ShadowCasterShader);
            m_shadowMapMaterial = CoreUtils.CreateEngineMaterial(m_rendererFeature.ShadowMapShader);
            m_shadowMapMaterialProperties = new MaterialPropertyBlock();

            // Initialize the remaining fields.
            OnValidate();
        }

        internal void OnValidate()
        {
            m_shadowMapMaterial.SetFloat(ShaderInfo.ShadowDepthBiasID, m_rendererFeature.ShadowDepthBias);
            m_shadowMapMaterial.SetFloat(ShaderInfo.OneMinusShadowCasterAlphaThresholdID, 1.0f - m_rendererFeature.ShadowCasterAlphaThreshold);

            if (m_shadowMapMesh != null)
            {
                CoreUtils.Destroy(m_shadowMapMesh);
            }
            m_shadowMapMesh = ShadowMapMesh.Generate(m_rendererFeature.ShadowMapResolution / 2);

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
        #endregion

        #region Private Methods
        private void RenderLights(ref ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            var camera = renderingData.cameraData.camera;
            m_rendererFeature.CullLightsPass.EnsureCompletion();

            foreach (var light in m_rendererFeature.LightManager.VisibleLights)
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

            // Based on https://forum.unity.com/threads/can-i-use-scriptablerendercontext-cull-to-get-another-culling-results-in-scriptablerenderpass.1075930/.
            if (renderingData.cameraData.camera.TryGetCullingParameters(out var cullingParameters))
            {
                cullingParameters.cullingMatrix = projectionMatrix * viewMatrix;
                GeometryUtility.CalculateFrustumPlanes(cullingParameters.cullingMatrix, c_cullingPlanes);

                for (int index = 0; index < c_cullingPlanes.Length; index++)
                {
                    cullingParameters.SetCullingPlane(index, c_cullingPlanes[index]);
                }
                var cullResults = context.Cull(ref cullingParameters);
                var shadowCasters = context.CreateRendererList(new RendererListDesc(c_shaderTags, cullResults, renderingData.cameraData.camera)
                {
                    renderQueueRange = RenderQueueRange.all,
                    sortingCriteria = SortingCriteria.None,
                    overrideMaterial = m_shadowCasterMaterial,
                    overrideMaterialPassIndex = 0,
                    renderingLayerMask = Lighting2D.ShadowCasterRenderingLayer
                });
                commandBuffer.DrawRendererList(shadowCasters);
            }
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
            //         http://www.catalinzima.com/2010/07/my-technique-for-the-shader-based-dynamic-2d-shadows/
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
            m_shadowMapMaterialProperties.SetTexture(ShaderInfo.MainTextureID, m_shadowCastersTextureHandle);
            m_shadowMapMaterialProperties.SetVector(ShaderInfo.LightIlluminationSizeID, new float4(1.0f / (2.0f * lightProperties.Extents), 2.0f * lightProperties.Extents));
            commandBuffer.SetRenderTarget(m_shadowMapTextureHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.ClearRenderTarget(RTClearFlags.Color, c_shadowMapClearColor, 1.0f, 0);
            commandBuffer.SetViewProjectionMatrices(c_orthographicViewMatrix, c_orthographicProjectionMatrix);
            commandBuffer.DrawMesh(m_shadowMapMesh, Matrix4x4.identity, m_shadowMapMaterial, 0, 0, m_shadowMapMaterialProperties);
        }

        private void RenderLight(ref LightProperties lightProperties, ref Matrix4x4 localToWorldMatrix, ref RenderingData renderingData, CommandBuffer commandBuffer)
        {
            lightProperties.MaterialPropertyBlock.SetVector(ShaderInfo.LightIlluminationSizeID, new float4(1.0f / (2.0f * lightProperties.Extents), 2.0f * lightProperties.Extents));
            commandBuffer.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
            commandBuffer.SetRenderTarget(colorAttachmentHandle, depthAttachmentHandle);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, localToWorldMatrix, lightProperties.Material, 0, 0, lightProperties.MaterialPropertyBlock);
        }
        #endregion
    }
}