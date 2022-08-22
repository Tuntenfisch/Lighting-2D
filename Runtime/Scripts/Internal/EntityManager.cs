using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class EntityManager
    {
        #region Public Properties
        public static List<IEntity> VisibileEntities => m_visibleEntities;
        public static HashSet<IEntity> Entities => m_entities;
        #endregion

        #region Private Fields
        private static List<IEntity> m_visibleEntities = new List<IEntity>();
        private static HashSet<IEntity> m_entities = new HashSet<IEntity>();
        #endregion

        #region Public Methods
        public static void Add(IEntity entity)
        {
            m_entities.Add(entity);
        }

        public static void Remove(IEntity entity)
        {
            m_entities.Remove(entity);
        }
        #endregion

        #region Internal Methods
        internal static void Cull(ref ScriptableCullingParameters cullingParameters, Camera camera)
        {
            m_visibleEntities.Clear();

            foreach (var entity in m_entities)
            {
                var entityProperties = entity.GetProperties();
                var entityBounds = entityProperties.Bounds;

                if (AreBoundsVisible(ref cullingParameters, entityBounds))
                {
                    m_visibleEntities.Add(entity);
                }
            }
        }
        #endregion

        #region Private Methods
        // Since I'm way to retarded to figure this out myself I took it from
        // https://forum.unity.com/threads/managed-version-of-geometryutility-testplanesaabb.473575/.
        private static bool AreBoundsVisible(ref ScriptableCullingParameters cullingParameters, Bounds bounds)
        {
            for (int index = 0; index < cullingParameters.cullingPlaneCount; index++)
            {
                var plane = cullingParameters.GetCullingPlane(index);
                var sign = math.sign(plane.normal);
                var point = (float3)bounds.center + sign * bounds.extents;

                if (math.dot(point, plane.normal) + plane.distance < 0.0f)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}