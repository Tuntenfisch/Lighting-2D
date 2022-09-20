using UnityEngine;

namespace Tuntenfisch.Lighting2D.Common
{
    public sealed class RenderingLayerAttribute : PropertyAttribute
    {
        #region Public Properties
        public bool AllowMultiple { get; init; }
        #endregion

        #region Public Methods
        public RenderingLayerAttribute(bool allowMultiple = false)
        {
            AllowMultiple = allowMultiple;
        }
        #endregion
    }
}