using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Common.Editor
{
    public class ShowIfDecorator : MaterialPropertyDrawer
    {
        #region Private Fields
        private readonly string m_keyword;
        private readonly bool m_enabled;
        #endregion

        #region Public Methods
        public override void OnGUI(Rect position, MaterialProperty property, string label, MaterialEditor editor)
        {
            // This approach could break at any time if Unity decides to change their internal implementation of MaterialPropertyHandler:
            // https://github.com/Unity-Technologies/UnityCsReference/blob/a048de916b23331bf6dfe92c4a6c205989b83b4f/Editor/Mono/Inspector/MaterialPropertyDrawer.cs
            Type type = typeof(MaterialPropertyDrawer).Assembly.GetType("UnityEditor.MaterialPropertyHandler");
            FieldInfo fieldInfo = type.GetField("s_PropertyHandlers", BindingFlags.Static | BindingFlags.NonPublic);
            IDictionary dictionary = fieldInfo.GetValue(null) as IDictionary;

            if (ShouldShow(editor))
            {
                dictionary.Remove((editor.target as Material).shader.GetInstanceID() + "_" + property.name);
            }
            else
            {
                object propertyHandle = dictionary[(editor.target as Material).shader.GetInstanceID() + "_" + property.name];
                FieldInfo propertyDrawer = propertyHandle.GetType().GetField("m_PropertyDrawer", BindingFlags.Instance | BindingFlags.NonPublic);
                propertyDrawer.SetValue(propertyHandle, new EmptyDrawer());
            }
        }

        public override float GetPropertyHeight(MaterialProperty property, string label, MaterialEditor editor)
        {
            return 0.0f;
        }

        public ShowIfDecorator(string keyword, string enabled)
        {
            m_keyword = keyword;

            if (!bool.TryParse(enabled, out m_enabled))
            {
                string message = $"Invalid \"{nameof(enabled)}\" value supplied to {nameof(ShowIfDecorator)}. Value should be parsable as a bool.";
                Debug.LogWarning(message);
                throw new ArgumentException(message, nameof(enabled));
            }
        }
        #endregion

        #region Private Methods
        private bool ShouldShow(MaterialEditor editor)
        {
            foreach (Material material in from target in editor.targets where target != null select target as Material)
            {
                if (material.IsKeywordEnabled(m_keyword) == m_enabled)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Private Classes, Enums and Structs
        private class EmptyDrawer : MaterialPropertyDrawer
        {
            #region Public Methods
            public override void OnGUI(Rect position, MaterialProperty property, GUIContent label, MaterialEditor editor)
            {

            }

            public override float GetPropertyHeight(MaterialProperty property, string label, MaterialEditor editor)
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
            #endregion
        }
        #endregion
    }
}