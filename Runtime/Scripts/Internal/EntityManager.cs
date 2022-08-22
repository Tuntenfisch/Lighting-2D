using System.Collections.Generic;

namespace Tuntenfisch.Lighting2D.Internal
{
    public static class EntityManager
    {
        #region Public Properties
        public static HashSet<IEntity> Entities => m_entities;
        #endregion

        #region Private Fields
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
    }
}