using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Editor
{
    [CustomEditor(typeof(Light2D))]
    public class Light2DEditor : UnityEditor.Editor
    {
        #region Private Fields
        private Light2D m_light;
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            m_light = target as Light2D;
        }

        private void OnSceneGUI()
        {
            Handles.color = Color.white;
            Handles.DrawWireDisc(m_light.transform.position, m_light.transform.forward, m_light.Radius);
        }
        #endregion
    }
}