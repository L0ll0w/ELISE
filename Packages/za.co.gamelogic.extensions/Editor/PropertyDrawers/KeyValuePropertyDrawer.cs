using Gamelogic.Extensions.Algorithms;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Property drawer for <see cref="KeyValue{TKey,TValue}"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(KeyValue<,>))]
	public class KeyValuePropertyDrawer : PropertyDrawer
	{
		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var idProperty = property.FindPropertyRelative("key");
			var gameObjectProperty = property.FindPropertyRelative("value");
			var newLabel = new GUIContent
			{
				text = idProperty.stringValue,
				tooltip = "ID"
			};
			EditorGUI.PropertyField(position, gameObjectProperty, newLabel); 
		}

		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var gameObjectProperty = property.FindPropertyRelative("value");
			return EditorGUI.GetPropertyHeight(gameObjectProperty, label, true);
		}
	}
}