using Tuntenfisch.Lighting2D.Common;
using Tuntenfisch.Lighting2D.Internal;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    [ExecuteAlways]
    public class Light2D : MonoBehaviour, IEntity
    {
        #region Public Fields
        public float Radius { get => m_radius; set => m_radius = value; }
        public float Falloff { get => m_falloff; set => m_falloff = value; }
        public Color Color { get => m_color; set => m_color = value; }
        #endregion

        #region Inspector Fields
        [Min(0.0f)]
        [SerializeField]
        private float m_radius = 3.0f;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_falloff = 0.5f;
        [SerializeField]
        private Color m_color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        [InlineField]
        [SerializeField]
        private EntityProperties m_properties;
        #endregion

        #region Private Fields
        private MaterialPropertyBlock m_materialPropertyBlock;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            EntityManager.Add(this);
        }

        private void OnDisable()
        {
            EntityManager.Remove(this);
        }

        public EntityProperties GetProperties()
        {
            m_properties.Bounds = new Bounds(transform.position, new float3(2.0f * m_radius, 2.0f * m_radius, 0.0f));
            return m_properties;
        }

        public void SetMaterialProperties(MaterialPropertyBlock properties)
        {
            properties.SetFloat("_LightFalloff", m_falloff);
            properties.SetColor("_LightColor", m_color);
        }
        #endregion
    }
}