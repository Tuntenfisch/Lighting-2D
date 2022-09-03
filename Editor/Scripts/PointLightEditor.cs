using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointLight))]
    public class PointLightEditor : UnityEditor.Editor
    {
        #region Private Fields
        private PointLight m_light;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            m_light = target as PointLight;
        }

        private void OnSceneGUI()
        {
            Handles.color = Color.white;
            Handles.DrawWireDisc(m_light.transform.position, m_light.transform.forward, m_light.Radius);
        }
        #endregion
    }
}