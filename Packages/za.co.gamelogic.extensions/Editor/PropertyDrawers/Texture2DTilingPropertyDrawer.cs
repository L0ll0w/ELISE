using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Property drawer for <see cref="Texture2DTiling"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(Texture2DTiling))]
	public sealed class Texture2DTilingPropertyDrawer : PropertyDrawer
	{
		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			return line + (line + spacing) * 4 + 4; // texture + tiling + offset
		}

		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

			var textureProperty = property.FindPropertyRelative("texture");
			var scaleProperty = property.FindPropertyRelative("tilingScale");
			var offsetProperty = property.FindPropertyRelative("tilingOffset");
			var relativeToScreenProperty = property.FindPropertyRelative("useScreenAspect");

			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			var r = new Rect(position.x, position.y, position.width, line);

			EditorGUI.PropertyField(r, textureProperty, new GUIContent(label.text));

			EditorGUI.indentLevel++;
			r.y += line + spacing;
			EditorGUI.PropertyField(r, scaleProperty, new GUIContent("Tiling"));

			r.y += line + spacing;
			EditorGUI.PropertyField(r, offsetProperty, new GUIContent("Offset"));
			
			r.y += line + spacing;
			EditorGUI.PropertyField(r, relativeToScreenProperty, new GUIContent("Relative To Screen"));
			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}
	}
}
