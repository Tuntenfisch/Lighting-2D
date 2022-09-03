using System;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Internal
{
    [Serializable]
    public struct LightProperties
    {
        #region Public Properties
        public bool AreValid => Material != null && MaterialPropertyBlock != null;
        public float3 Position { get => Bounds.center; set => Bounds.center = value; }
        public float2 Extents { get => 0.5f * new float2(Bounds.size.x, Bounds.size.y); set => Bounds.size = new float3(2.0f * value, float.PositiveInfinity); }
        #endregion

        #region Public Fields
        public Material Material;
        public MaterialPropertyBlock MaterialPropertyBlock;
        #endregion

        #region Internal Fields
        internal Bounds Bounds;
        #endregion
    }
}