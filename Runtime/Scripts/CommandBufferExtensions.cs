using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tuntenfisch.Lighting2D
{
    public static class CommandBufferExtensions
    {
        private static readonly Matrix4x4 m_viewMatrix;
        private static readonly Matrix4x4 m_projectionMatrix;

        #region Public Methods
        static CommandBufferExtensions()
        {
            m_viewMatrix = Matrix4x4.TRS(new float3(0.0f, 0.0f, -1.0f), Quaternion.identity, new float3(1.0f, 1.0f, -1.0f));
            m_projectionMatrix = Matrix4x4.Ortho(-0.5f, 0.5f, -0.5f, 0.5f, 0.3f, 1000.0f);
        }

        public static void Blit(this CommandBuffer commandBuffer, RTHandle source, RTHandle destination, Mesh mesh, Material material, int pass, CameraData cameraData)
        {
            material.mainTexture = source;
            commandBuffer.SetRenderTarget(destination);
            commandBuffer.SetViewProjectionMatrices(m_viewMatrix, m_projectionMatrix);
            commandBuffer.DrawMesh(mesh, Matrix4x4.identity, material, 0, pass);
            commandBuffer.SetViewProjectionMatrices(cameraData.GetViewMatrix(), cameraData.GetProjectionMatrix());
        }
        #endregion
    }
}