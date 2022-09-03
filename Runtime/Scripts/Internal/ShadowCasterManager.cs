using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class ShadowCasterManager
    {
        #region Public Properties
        public static List<ShadowCaster> VisibleShadowCasters => s_visibleShadowCasters;
        public static HashSet<ShadowCaster> ShadowCasters => s_shadowCasters;
        #endregion

        #region Private Fields
        private static List<ShadowCaster> s_visibleShadowCasters = new List<ShadowCaster>();
        private static HashSet<ShadowCaster> s_shadowCasters = new HashSet<ShadowCaster>();
        private static object s_lock = new object();
        #endregion

        #region Public Methods
        public static void Add(ShadowCaster shadowCaster)
        {
            if (s_shadowCasters.Contains(shadowCaster))
            {
                return;
            }
            s_shadowCasters.Add(shadowCaster);
        }

        public static void Remove(ShadowCaster shadowCaster)
        {
            if (!s_shadowCasters.Contains(shadowCaster))
            {
                return;
            }
            s_shadowCasters.Remove(shadowCaster);
        }
        #endregion

        #region Internal Methods
        internal static void Cull(ref LightProperties lightProperties)
        {
            Profiler.BeginSample($"{nameof(ShadowCasterManager)}.{nameof(Cull)}");

            s_visibleShadowCasters.Clear();

            foreach (var shadowCaster in s_shadowCasters)
            {
                var shadowCasterBounds = shadowCaster.Renderer.bounds;

                if (AreBoundsVisible(ref lightProperties.Bounds, ref shadowCasterBounds))
                {
                    s_visibleShadowCasters.Add(shadowCaster);
                }
            }
            Profiler.EndSample();
        }
        #endregion

        #region Private Methods
        // Since I'm way to retarded to figure this out myself I took it from
        // https://forum.unity.com/threads/managed-version-of-geometryutility-testplanesaabb.473575/.
        private static bool AreBoundsVisible(ref Bounds lightBounds, ref Bounds shadowCasterBounds)
        {
            return lightBounds.Intersects(shadowCasterBounds);
        }
        #endregion
    }
}