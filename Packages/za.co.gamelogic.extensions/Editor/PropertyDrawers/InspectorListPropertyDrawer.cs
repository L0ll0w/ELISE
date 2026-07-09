// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;
using Gamelogic.Extensions.Internal;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// A property drawer for type InspectorList.
	/// </summary>
	[Version(2, 5, 0)]
	[CustomPropertyDrawer(typeof (InspectorList), true)]
	[Obsolete("Unity's default list functionality in inspectors is now superior.")]
	public class InspectorListPropertyDrawer : PropertyDrawer
	{
		private ReorderableList reorderableList;
		private float lastHeight = 0;

		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var list = property.FindPropertyRelative("values");

			if (list == null)
			{
				return 0;
			}

			InitList(list, property);

			if (reorderableList != null)
			{
				return reorderableList.GetHeight();
			}

			return lastHeight;
			
			//return EditorGUIUtility.singleLineHeight;
		}

		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var list = property.FindPropertyRelative("values");

			if (list == null)
			{
				return;
			}

			int indentLevel = EditorGUI.indentLevel;

			InitList(list, property);

			if (list.arraySize > 0)
				reorderableList.elementHeight = EditorGUI.GetPropertyHeight(list.GetArrayElementAtIndex(0));

			if(position.height <= 0)
			{
				return;
			}

			lastHeight = reorderableList.GetHeight();

			reorderableList.DoList(position);
			
			EditorGUI.indentLevel = indentLevel;
		}

		/// <summary>Initializes the reorderable list if it has not been initialized yet.</summary>
		/// <param name="list">The underlying array serialized property.</param>
		/// <param name="property">The parent serialized property.</param>
		public void InitList(SerializedProperty list, SerializedProperty property)
		{
			if (reorderableList != null)
			{
				return;
			}

			reorderableList = new ReorderableList(property.serializedObject, list, true, true, true, true)
			{
				drawElementCallback = DrawElement,
				drawHeaderCallback = DrawHeader,
#if UNITY_5
					elementHeightCallback =
 index => EditorGUI.GetPropertyHeight(list.GetArrayElementAtIndex(index), null, true)
#endif

			};
			
			return;

			void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				var element = list.GetArrayElementAtIndex(index);
				var labelProperty = element;
				var potentialProperty = (SerializedProperty)null;
				int maxCheck = 0;

				while (labelProperty.Next(true) && maxCheck++ < 3)
				{
					if (labelProperty.propertyType == SerializedPropertyType.String)
					{
						// @omar this is always true

						// ReSharper disable once ConditionIsAlwaysTrueOrFalse
						// This class is obsolete anyways so no need to fix this bug
						if (labelProperty.name == "name" || potentialProperty == null)
						{
							potentialProperty = labelProperty;
							break;
						}
					}
				}

				var itemLabel = potentialProperty == null
					? new GUIContent("Element: " + index)
					: new GUIContent(labelProperty.stringValue);

				EditorGUI.PropertyField(rect, list.GetArrayElementAtIndex(index), itemLabel, true);
			}
			
			void DrawHeader(Rect rect)
			{
				EditorGUI.indentLevel++;
				EditorGUI.LabelField(rect, property.displayName);
			}
		}
	}
}
