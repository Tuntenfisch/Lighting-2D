using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class LightManager
    {
        #region Public Properties
        public static IEnumerable<ILight> VisibleLights
        {
            get
            {
                foreach (var visibleLightIndex in s_visibleLightIndices)
                {
                    yield return s_lights[visibleLightIndex];
                }
            }
        }
        #endregion

        #region Private Fields
        private static List<ILight> s_lights = new List<ILight>();
        private static NativeList<Plane> s_cullingPlanes = new NativeList<Plane>(6, Allocator.Persistent);
        private static NativeList<Bounds> s_lightBounds = new NativeList<Bounds>(Allocator.Persistent);
        private static NativeList<int> s_visibleLightIndices = new NativeList<int>(100, Allocator.Persistent);
        #endregion

        #region Public Methods
        public static void Add(ILight light)
        {
            if (s_lights.Contains(light))
            {
                return;
            }
            s_lights.Add(light);
        }

        public static void Remove(ILight light)
        {
            if (!s_lights.Contains(light))
            {
                return;
            }
            s_lights.Remove(light);
        }
        #endregion

        #region Internal Methods
        internal static void Cull(ref ScriptableCullingParameters cullingParameters)
        {
            Profiler.BeginSample($"{nameof(LightManager)}.{nameof(Cull)}");
            UpdateCullingPlanes(ref cullingParameters);
            UpdateLightBounds();
            UpdateVisibleLightIndices();
            Profiler.EndSample();
        }

        internal static void Dispose()
        {
            s_cullingPlanes.Dispose();
            s_lightBounds.Dispose();
            s_visibleLightIndices.Dispose();
        }
        #endregion

        #region Private Methods
        private static void UpdateCullingPlanes(ref ScriptableCullingParameters cullingParameters)
        {
            s_cullingPlanes.Clear();

            for (int index = 0; index < cullingParameters.cullingPlaneCount; index++)
            {
                s_cullingPlanes.Add(cullingParameters.GetCullingPlane(index));
            }
        }

        private static void UpdateLightBounds()
        {
            s_lightBounds.Clear();

            foreach (var light in s_lights)
            {
                s_lightBounds.Add(light.GetLightProperties(update: true).Bounds);
            }
        }

        private static void UpdateVisibleLightIndices()
        {
            s_visibleLightIndices.Clear();

            if (s_lights.Count > s_visibleLightIndices.Capacity)
            {
                s_visibleLightIndices.SetCapacity(2 * s_lightBounds.Length);
            }

            new LightCullJob
            {
                CullingPlanes = s_cullingPlanes,
                LightBounds = s_lightBounds,
                VisibleLightIndices = s_visibleLightIndices.AsParallelWriter()
            }.Run(s_lightBounds.Length);
        }
        #endregion

        #region Private Classes, Enums and Structs
        private struct LightCullJob : IJobParallelFor
        {
            #region Public Fields
            [ReadOnly]
            public NativeList<Plane> CullingPlanes;
            [ReadOnly]
            public NativeList<Bounds> LightBounds;
            [WriteOnly]
            public NativeList<int>.ParallelWriter VisibleLightIndices;
            #endregion

            #region Public Methods
            public void Execute(int index)
            {
                if (AreBoundsVisible(LightBounds[index]))
                {
                    VisibleLightIndices.AddNoResize(index);
                }
            }
            #endregion

            #region Private Methods
            // Since I'm way to retarded to figure this out myself I took it from
            // https://forum.unity.com/threads/managed-version-of-geometryutility-testplanesaabb.473575/.
            private bool AreBoundsVisible(Bounds bounds)
            {
                foreach (var cullingPlane in CullingPlanes)
                {
                    var sign = math.sign(cullingPlane.normal);
                    var point = (float3)bounds.center + sign * bounds.extents;

                    if (math.dot(point, cullingPlane.normal) + cullingPlane.distance < 0.0f)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion
        }
        #endregion
    }
}