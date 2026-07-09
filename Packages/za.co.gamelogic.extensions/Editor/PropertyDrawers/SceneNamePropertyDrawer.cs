using UnityEngine;
using UnityEditor;
using EventType = UnityEngine.EventType;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Property drawer for <see cref="SceneNameAttribute"/>.
	/// </summary>
	[Version(4, 5, 0)]
	[CustomPropertyDrawer(typeof(SceneNameAttribute))]
	public class SceneNamePropertyDrawer : PropertyDrawer
	{
		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			HandleDragAndDrop(position, property);
			DrawProperty(position, property, label);
			EditorGUI.EndProperty();
		}

		private void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
		{
			var textFieldRect = position;
			textFieldRect.width -= 20;
			property.stringValue = EditorGUI.TextField(textFieldRect, label, property.stringValue);

			var buttonRect = new Rect(position.x + position.width - 20, position.y, 20, position.height);

			if (GUI.Button(buttonRect, new GUIContent("⊙")))
			{
				EditorGUIUtility.ShowObjectPicker<SceneAsset>(null, false, "", 0);
			}

			if (Event.current.commandName != "ObjectSelectorUpdated")
			{
				return;
			}

			var sceneAsset = EditorGUIUtility.GetObjectPickerObject() as SceneAsset;

			if (sceneAsset == null)
			{
				return;
			}

			property.stringValue = sceneAsset.name;
			property.serializedObject.ApplyModifiedProperties();
		}

		private static void HandleDragAndDrop(Rect position, SerializedProperty property)
		{
			var evt = Event.current;

			bool isDrag =
				!(evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) ||
				!position.Contains(evt.mousePosition);

			if (isDrag)
			{
				return;
			}

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if (evt.type != EventType.DragPerform)
			{
				return;
			}

			DragAndDrop.AcceptDrag();

			if (DragAndDrop.objectReferences.Length > 0)
			{
				GetSceneName(property, evt);
			}
		}

		private static void GetSceneName(SerializedProperty property, Event evt)
		{
			var sceneAsset = DragAndDrop.objectReferences[0] as SceneAsset;
			if (sceneAsset == null)
			{
				return;
			}

			string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
			string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
			property.stringValue = sceneName;
			evt.Use();
		}
	}
}
