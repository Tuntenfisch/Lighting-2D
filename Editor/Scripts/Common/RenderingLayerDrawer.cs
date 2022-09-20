using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.Lighting2D.Common.Editor
{
    [CustomPropertyDrawer(typeof(RenderingLayerAttribute))]
    public class RenderingLayerDrawer : PropertyDrawer
    {
        #region Public Methods
        // Based on https://github.com/Unity-Technologies/UnityCsReference/blob/66ae70d07033d2275ef6d8b42614367747439e58/Editor/Mono/Inspector/RendererEditorBase.cs#L16.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RenderingLayerAttribute renderingLayerAttribute = attribute as RenderingLayerAttribute;
            RenderPipelineAsset renderPipelineAsset = GraphicsSettings.currentRenderPipeline;

            if (renderPipelineAsset == null)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            uint value;

            if (renderingLayerAttribute.AllowMultiple)
            {
                value = (uint)EditorGUI.MaskField(position, property.displayName, property.intValue, renderPipelineAsset.renderingLayerMaskNames);
            }
            else
            {
                int selectedIndex = Mathf.RoundToInt(math.log2(property.intValue));
                selectedIndex = EditorGUI.Popup(position, property.displayName, selectedIndex, renderPipelineAsset.renderingLayerMaskNames);
                value = 1u << selectedIndex;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.uintValue = value;
            }
            EditorGUI.EndProperty();
        }
        #endregion
    }
}