using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class MeshUtility
    {
        #region Public Methods
        public static Mesh GenerateShadowMapMesh(int numberOfOverlayedQuads)
        {
            var vertices = new Vector3[2 * numberOfOverlayedQuads + 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int3[2 * numberOfOverlayedQuads];

            for (int layerIndex = 0, vertexIndex = 0, triangleIndex = 0; layerIndex <= numberOfOverlayedQuads; layerIndex++, vertexIndex += 2, triangleIndex += 2)
            {
                var even = layerIndex % 2 == 0;

                vertices[vertexIndex + 0] = new float3(even ? -1.0f : 1.0f, -1.0f, 0.0f);
                vertices[vertexIndex + 1] = new float3(even ? -1.0f : 1.0f, 1.0f, 0.0f);

                uvs[vertexIndex + 0] = new float2(layerIndex / (float)numberOfOverlayedQuads, 0.0f);
                uvs[vertexIndex + 1] = new float2(layerIndex / (float)numberOfOverlayedQuads, 1.0f);

                if (layerIndex < numberOfOverlayedQuads)
                {
                    int3 triangleA = vertexIndex + new int3(0, 1, 2);
                    int3 triangleB = vertexIndex + new int3(2, 1, 3);

                    triangles[triangleIndex + 0] = even ? triangleA : triangleA.xzy;
                    triangles[triangleIndex + 1] = even ? triangleB : triangleA.xzy;
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetIndexBufferParams(3 * triangles.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, 3 * triangles.Length));
            return mesh;
        }
        #endregion
    }
}