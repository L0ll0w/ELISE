using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// The custom property drawer for the <see cref="Keyword"/> class.
	/// </summary>
	[CustomPropertyDrawer(typeof(Keyword))]
	internal sealed class KeywordDrawer : PropertyDrawer
	{
		/// <inheritdoc/>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Calculate rects
			Rect nameRect = new Rect(position.x, position.y, position.width - 55, position.height);
			Rect enabledRect = new Rect(position.x + position.width - 50, position.y, 50, position.height);

			// Draw fields
			SerializedProperty enabledProp = property.FindPropertyRelative("enabled");
			SerializedProperty nameProp = property.FindPropertyRelative("name");

			EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
			EditorGUI.PropertyField(enabledRect, enabledProp, GUIContent.none);

			EditorGUI.EndProperty();
		}

		/// <inheritdoc/>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
