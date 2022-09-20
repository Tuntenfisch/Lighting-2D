using System;
using Tuntenfisch.Lighting2D.Common;
using Tuntenfisch.Lighting2D.Internal;
using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    [ExecuteAlways]
    public sealed class PointLight : MonoBehaviour, ILight
    {
        #region Public Fields
        public float Radius
        {
            get => m_radius;
            set
            {
                m_radius = value;
                m_properties.Extents = m_radius;
            }
        }
        public float Falloff
        {
            get => m_falloff;
            set
            {
                m_falloff = value;
                m_properties.MaterialPropertyBlock.SetFloat(c_lightFalloffID, m_falloff);
            }
        }
        public Color Color
        {
            get => m_color;
            set
            {
                m_color = value;
                m_properties.MaterialPropertyBlock.SetColor(c_lightColorID, m_color);
            }
        }
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
        [SerializeField]
        private Internal.LightType m_type;

        [HideInInspector]
        [SerializeField]
        private Shader m_shader;
        #endregion

        #region Private Fields
        private const string c_shaderName = "Tuntenfisch/Lighting2D/PointLight";
        private static readonly int c_lightFalloffID = Shader.PropertyToID("_LightFalloff");
        private static readonly int c_lightColorID = Shader.PropertyToID("_LightColor");

        private LightProperties m_properties;
        private int m_lightID;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            OnValidate();
            m_lightID = LightManager.Add(this);
        }

        private void OnDisable()
        {
            LightManager.Remove(m_lightID);
        }

        private void OnValidate()
        {
            if (!m_properties.AreValid)
            {
                m_shader = Shader.Find(c_shaderName);
                m_properties.Material = new Material(m_shader);
                m_properties.MaterialPropertyBlock = new MaterialPropertyBlock();
            }
            m_properties.Extents = m_radius;
            m_properties.Material.SetKeyword(ShaderInfo.SoftShadowsEnabledKeyword, m_type == Internal.LightType.Soft);
            m_properties.MaterialPropertyBlock.SetFloat(c_lightFalloffID, m_falloff);
            m_properties.MaterialPropertyBlock.SetColor(c_lightColorID, m_color);
        }
        #endregion

        #region Public Methods
        public LightProperties GetLightProperties(bool update)
        {
            if (update)
            {
                m_properties.Position = transform.position;
            }
            return m_properties;
        }
        #endregion
    }
}