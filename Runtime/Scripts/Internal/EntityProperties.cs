using System;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Internal
{
    [Serializable]
    public struct EntityProperties
    {
        #region Public Fields
        [HideInInspector]
        public Bounds Bounds;
        public Material Material;
        [HideInInspector]
        public Action<MaterialPropertyBlock> SetMaterialPropertiesAction;
        #endregion
    }
}