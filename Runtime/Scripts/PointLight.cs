using System;
using Tuntenfisch.Lighting2D.Internal;
using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    [ExecuteAlways]
    public class PointLight : MonoBehaviour, ILight
    {
        #region Public Fields
        public float Radius { get => m_radius; set => m_radius = value; }
        public float Falloff { get => m_falloff; set => m_falloff = value; }
        public Color Color { get => m_color; set => m_color = value; }
        #endregion

        #region Inspector Fields
        [Header("Light")]
        [Min(0.0f)]
        [SerializeField]
        private float m_radius = 3.0f;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_falloff = 0.5f;
        [SerializeField]
        private Color m_color = new Color(1.0f, 1.0f, 1.0f, 0.2f);

        [Header("Shadows")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_depthBias;
        [SerializeField]
        private ShadowType m_type;

        [HideInInspector]
        [SerializeField]
        private Shader m_shader;
        [HideInInspector]
        [SerializeField]
        private LightProperties m_properties;
        #endregion

        #region Unity Events
        private void OnValidate()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            LightManager.Add(this);
        }

        private void OnDisable()
        {
            LightManager.Remove(this);
        }
        #endregion

        #region Public Methods
        public LightProperties GetLightProperties(bool update = false)
        {
            if (update)
            {
                m_properties.Position = transform.position;
                m_properties.Extents = m_radius;
            }
            return m_properties;
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            if (!m_properties.AreValid)
            {
                m_shader = Shader.Find(ShaderInfo.PointLightShaderName);
                m_properties.Material = new Material(m_shader);
                m_properties.MaterialPropertyBlock = new MaterialPropertyBlock();
            }
            m_properties.Material.SetKeyword(ShaderInfo.DepthBiasEnabledKeyword, m_depthBias > 0.0f);
            m_properties.Material.SetKeyword(ShaderInfo.SoftShadowsEnabledKeyword, m_type == ShadowType.Soft);
            m_properties.MaterialPropertyBlock.SetFloat("_DepthBias", m_depthBias);
            m_properties.MaterialPropertyBlock.SetFloat("_LightFalloff", m_falloff);
            m_properties.MaterialPropertyBlock.SetColor("_LightColor", m_color);
        }
        #endregion
    }
}