using Gamelogic.Extensions.Algorithms;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Property drawer for <see cref="FixedKeyDictionary{TKey,TValue}"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(FixedKeyDictionary<,>), useForChildren:true)]
	public class FixedKeyDictionaryPropertyDrawer : PropertyDrawer
	{
#if UNITY_2020_1_OR_NEWER
		private Vector2 scrollPosition;
#endif

		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
#if UNITY_2020_1_OR_NEWER
			var pairsProperty = property.FindPropertyRelative("pairs");
			float spaceBetweenLines = 2f;
			float headerHeight = EditorGUIUtility.singleLineHeight;

			var headerRect = new Rect(position.x, position.y, position.width, headerHeight);
			EditorGUI.DrawRect(headerRect, new Color(66/256f, 92/256f, 199/256f));

			var style = new GUIStyle(EditorStyles.boldLabel)
			{
				border = new RectOffset(2, 2, 2, 2),
			};

			EditorGUI.LabelField(headerRect, label, style);

			// Scroll area
			float scrollY = position.y + headerHeight;
			float scrollHeight = position.height - headerHeight;

			var scrollRect = new Rect(position.x, scrollY, position.width, scrollHeight);

			// Full content height
			float contentHeight = 0f;
			for (int i = 0; i < pairsProperty.arraySize; i++)
			{
				var p = pairsProperty.GetArrayElementAtIndex(i);
				contentHeight += EditorGUI.GetPropertyHeight(p, true) + spaceBetweenLines;
			}

			var contentRect = new Rect(0, 0, position.width - 20f, contentHeight);

			scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, contentRect);

			float yOffset = 0f;

			for (int i = 0; i < pairsProperty.arraySize; i++)
			{
				var pairProperty = pairsProperty.GetArrayElementAtIndex(i);
				float pairHeight = EditorGUI.GetPropertyHeight(pairProperty, true);

				var pairRect = new Rect(0, yOffset, contentRect.width, pairHeight);

				EditorGUI.PropertyField(pairRect, pairProperty, GUIContent.none, true);

				yOffset += pairHeight + spaceBetweenLines;
			}

			GUI.EndScrollView();
#else
			var warningRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2f);
			EditorGUI.HelpBox(
				warningRect,
				"FixedKeyDictionary serialization is not supported before Unity 2020.1.\n" +
				"Do not use this type for serializations in earlier versions.",
				MessageType.Error
			);
#endif
		}

		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			#if UNITY_2020_1_OR_NEWER
			
			// Make the property drawer a fixed height so Unity shows the scroll area
			return 200f; 
			#else
			// Height for the HelpBox shown in older Unity versions
			return EditorGUIUtility.singleLineHeight * 2.5f;
			#endif
		}
	}
}
