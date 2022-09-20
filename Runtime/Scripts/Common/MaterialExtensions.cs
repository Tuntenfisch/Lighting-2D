using UnityEngine;

namespace Tuntenfisch.Lighting2D.Common
{
    public static class MaterialExtensions
    {
        #region Public Methods
        public static void SetKeyword(this Material material, string keyword, bool enable)
        {
            if (enable)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
        #endregion
    }
}