using System;
using System.Collections;
using System.Collections.Generic;
using Tuntenfisch.Lighting2D.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.Lighting2D.Internal
{
    public sealed class LightManager : IDisposable
    {
        #region Public Properties
        internal VisibleLightsEnumerator VisibleLights => new VisibleLightsEnumerator(this);
        #endregion

        #region Private Fields
        private static IndexableLinkedList<ILight> s_lights;

        private NativeList<int> m_lightIDs;
        private NativeList<Bounds> m_lightBounds;
        private NativeList<int> m_visibleLightIDs;
        #endregion

        #region Public Methods
        public void Dispose()
        {
            try
            {
                m_lightIDs.Dispose();
                m_lightBounds.Dispose();
                m_visibleLightIDs.Dispose();
            }
            catch { }
        }
        #endregion

        #region Public Methods
        static LightManager()
        {
            s_lights = new IndexableLinkedList<ILight>();
        }

        public static int Add(ILight light)
        {
            return s_lights.Insert(light);
        }

        public static void Remove(int lightID)
        {
            s_lights.Erase(lightID);
        }
        #endregion

        #region Internal Methods
        internal LightManager()
        {
            m_lightIDs = new NativeList<int>(Allocator.Persistent);
            m_lightBounds = new NativeList<Bounds>(Allocator.Persistent);
            m_visibleLightIDs = new NativeList<int>(Allocator.Persistent);
        }

        internal JobHandle Cull(ref ScriptableCullingParameters cullingParameters, JobHandle dependency = default)
        {
            SetupLightCullingJob();
            return DispatchLightCullingJob(ref cullingParameters, dependency);
        }
        #endregion

        #region Private Methods
        private void SetupLightCullingJob()
        {
            m_lightIDs.Clear();
            m_lightBounds.Clear();

            foreach (var (lightID, light) in s_lights.GetEnumeratorNonAlloc())
            {
                m_lightIDs.Add(lightID);
                m_lightBounds.Add(light.GetLightProperties(update: true).Bounds);
            }
            m_visibleLightIDs.Clear();
            m_visibleLightIDs.EnsureMinimumCapacity(s_lights.Count);
        }

        private JobHandle DispatchLightCullingJob(ref ScriptableCullingParameters cullingParameters, JobHandle dependency = default)
        {
            return new LightCullingJob
            {
                CullingParameters = cullingParameters,
                LightIDs = m_lightIDs,
                LightBounds = m_lightBounds,
                VisibleLightIDs = m_visibleLightIDs.AsParallelWriter()
            }.Schedule(m_lightBounds.Length, 4, dependency);
        }
        #endregion

        #region Public Classes, Enums and Structs
        public struct VisibleLightsEnumerator : IEnumerator<ILight>
        {
            #region Public Properties
            public ILight Current => LightManager.s_lights[m_parent.m_visibleLightIDs[m_index]];
            object IEnumerator.Current => Current;
            #endregion

            #region Private Fields
            private const int c_resetIndex = -1;

            private LightManager m_parent;
            private int m_index;
            #endregion

            #region Public Methods
            public VisibleLightsEnumerator(LightManager parent)
            {
                m_parent = parent;
                m_index = c_resetIndex;
            }

            public void Dispose()
            {
                m_parent = null;
            }

            public bool MoveNext()
            {
                m_index++;
                return m_index < m_parent.m_visibleLightIDs.Length;
            }

            public void Reset()
            {
                m_index = c_resetIndex;
            }

            public VisibleLightsEnumerator GetEnumerator()
            {
                return this;
            }
            #endregion
        }
        #endregion

        #region Private Classes, Enums and Structs
        [BurstCompile]
        private struct LightCullingJob : IJobParallelFor
        {
            #region Public Fields
            public ScriptableCullingParameters CullingParameters;
            [ReadOnly]
            public NativeList<int> LightIDs;
            [ReadOnly]
            public NativeList<Bounds> LightBounds;
            [WriteOnly]
            public NativeList<int>.ParallelWriter VisibleLightIDs;
            #endregion

            #region Public Methods
            public void Execute(int index)
            {
                if (AreBoundsVisible(LightBounds[index]))
                {
                    VisibleLightIDs.AddNoResize(LightIDs[index]);
                }
            }
            #endregion

            #region Private Methods
            // Since I'm way to retarded to figure this out myself I took it from
            // https://forum.unity.com/threads/managed-version-of-geometryutility-testplanesaabb.473575/.
            private bool AreBoundsVisible(Bounds bounds)
            {
                for (int index = 0; index < CullingParameters.cullingPlaneCount; index++)
                {
                    var cullingPlane = CullingParameters.GetCullingPlane(index);
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