using Gamelogic.Fx.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Property drawer for <see cref="ColorShaderPropertyList"/> fields.
	/// </summary>
	[CustomPropertyDrawer(typeof(ColorShaderPropertyList))]
	public class ColorShaderPropertyListDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var colorsProp = property.FindPropertyRelative("colors");
			return EditorGUI.GetPropertyHeight(colorsProp, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var colorsProp = property.FindPropertyRelative("colors");
			EditorGUI.PropertyField(position, colorsProp, label, true);
		}
	}
}
