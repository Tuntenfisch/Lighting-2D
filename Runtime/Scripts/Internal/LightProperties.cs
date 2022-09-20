using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Internal
{
    public struct LightProperties
    {
        #region Public Properties
        public bool AreValid => Material != null && MaterialPropertyBlock != null;
        public float3 Position { get => m_bounds.center; set => m_bounds.center = value; }
        public float2 Extents { get => 0.5f * new float2(m_bounds.size.x, m_bounds.size.y); set => m_bounds.size = new float3(2.0f * value, 1.0f); }
        #endregion

        #region Internal Properties
        internal Bounds Bounds => m_bounds;
        #endregion

        #region Public Fields
        public Material Material;
        public MaterialPropertyBlock MaterialPropertyBlock;
        #endregion

        #region Private Fields
        private Bounds m_bounds;
        #endregion
    }
}