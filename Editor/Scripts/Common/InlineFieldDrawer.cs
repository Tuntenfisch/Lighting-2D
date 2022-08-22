using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Lighting2D.Common.Editor
{
    [CustomPropertyDrawer(typeof(InlineFieldAttribute))]
    public class InlineFieldDrawer : PropertyDrawer
    {
        #region Unity Callbacks
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren)
            {
                int parentDepth = property.depth;
                property.NextVisible(true);

                do
                {
                    position.height = EditorGUI.GetPropertyHeight(property);
                    EditorGUI.PropertyField(position, property, true);
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                }
                while (property.NextVisible(false) && property.depth != parentDepth);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren)
            {
                float propertyHeight = 0.0f;
                int parentDepth = property.depth;
                property.NextVisible(true);

                do
                {
                    propertyHeight += EditorGUI.GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
                while (property.NextVisible(false) && property.depth != parentDepth);

                return propertyHeight - EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property, true);
            }
        }
        #endregion
    }
}