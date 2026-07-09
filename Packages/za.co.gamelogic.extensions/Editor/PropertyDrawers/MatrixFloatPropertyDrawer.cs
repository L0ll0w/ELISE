using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityObject = UnityEngine.Object;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Property drawer for <see cref="MatrixFloat"/> fields.
	/// </summary>
	[CustomPropertyDrawer(typeof(MatrixFloat))]
	public sealed class MatrixFloatPropertyDrawer : PropertyDrawer
	{
		private const float CellSize = 32f;
		private const float CellPadding = 4f;
		private const float ButtonWidth = 18f;
		private const float Spacing = 6f;
		
		private readonly GUIStyle sizeLabel = new GUIStyle(EditorStyles.label)
		{
			alignment = TextAnchor.MiddleCenter
		};
		
		/// <summary>Returns the serialized property for the width field of a <see cref="MatrixFloat"/>.</summary>
		/// <param name="property">The parent serialized property.</param>
		public SerializedProperty GetWidthProperty(SerializedProperty property)
			=> property.FindPropertyRelative(MatrixFloat.WidthFieldName);

		/// <summary>Returns the serialized property for the height field of a <see cref="MatrixFloat"/>.</summary>
		/// <param name="property">The parent serialized property.</param>
		public SerializedProperty GetHeightProperty(SerializedProperty property)
			=> property.FindPropertyRelative(MatrixFloat.HeightFieldName);

		/// <summary>Returns the serialized property for the values array of a <see cref="MatrixFloat"/>.</summary>
		/// <param name="property">The parent serialized property.</param>
		public SerializedProperty GetValuesProperty(SerializedProperty property)
			=> property.FindPropertyRelative(MatrixFloat.ValuesFieldName);

		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var heightProp = GetHeightProperty(property);
			int height = Mathf.Max(1, heightProp.intValue);

			float titleHeight = EditorGUIUtility.singleLineHeight;
			float presetsButtonHeight = EditorGUIUtility.singleLineHeight + Spacing;
			float matrixHeight = height * (CellSize + CellPadding) + Spacing;
			float sizeControlHeight = EditorGUIUtility.singleLineHeight + Spacing;

			return titleHeight + presetsButtonHeight + matrixHeight + sizeControlHeight + 10f;
		}

		/// <inheritdoc />
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var widthProp = GetWidthProperty(property);
			var heightProp = GetHeightProperty(property);
			var valuesProp = GetValuesProperty(property);

			int width = widthProp.intValue;
			int height = heightProp.intValue;

			DrawTitle(ref position, label);
			DrawPresetsButton(ref position, property);
			DrawGrid(ref position, height, width, valuesProp);
			DrawSizeControls(ref position, property);

			EditorGUI.EndProperty();
		}

		private void DrawPresetsButton(ref Rect position, SerializedProperty property)
		{
			var buttonRect = new Rect(position.x, position.y, 100, EditorGUIUtility.singleLineHeight);

			var presetsAttribute = fieldInfo.GetCustomAttribute<PresetsAttribute>();
			bool hasPresets = 
				presetsAttribute != null 
				&& !string.IsNullOrEmpty(presetsAttribute.PresetRetrievalKey)
				&& presetsAttribute.PresetRetrievalKey.Length > 0;
			
			bool oldIsEnabled = GUI.enabled;
			GUI.enabled = hasPresets;
			
			if (GUI.Button(buttonRect, "Presets"))
			{
				Assert.IsNotNull(presetsAttribute, "PresetsAttribute is null despite hasPresets being true.");
				var presets = PropertyDrawerData.GetValues<KeyValuePair<string, MatrixFloat>>(presetsAttribute.PresetRetrievalKey);
				string[] labels = presets.Select(p => p.Key).ToArray();
				ListSelectionPopup.Show(presetsAttribute.Title, labels, SelectPreset);
				
				void SelectPreset(int selectedIndex)
				{
					var selectedPreset = presets[selectedIndex].Value;
					var target = property.serializedObject.targetObject;
					var floatMatrix = (MatrixFloat) fieldInfo.GetValue(target);
	
					Undo.RecordObject(target, "Load Matrix Preset");
					floatMatrix.SetFrom(selectedPreset);
					EditorUtility.SetDirty(target);
				}
			}
			GUI.enabled = oldIsEnabled;

			position.y += EditorGUIUtility.singleLineHeight + Spacing;
		}

		private void DrawSizeControls(ref Rect position, SerializedProperty property)
		{
			var rect = position;
			var sizeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			float xPos = sizeRect.x;

			var target = property.serializedObject.targetObject;
			var floatMatrix = (MatrixFloat) fieldInfo.GetValue(target);

			int width = floatMatrix.Width;
			int height = floatMatrix.Height;

			SizeButton("-", GetSizeButtonRect(), target, WidthAtLeastOne, RemoveColumn, "Remove Column");
			xPos += ButtonWidth + 2;

			float sizeWidth = 20f;
			
			EditorGUI.LabelField(new Rect(xPos, position.y, sizeWidth, sizeRect.height), width.ToString(), sizeLabel);
			xPos += sizeWidth + 2;

			SizeButton("+", GetSizeButtonRect(), target, Always, AddColumn, "Add Column");
			xPos += ButtonWidth + 10;
			
			EditorGUI.LabelField(new Rect(xPos, position.y, 20f, sizeRect.height), "×");
			xPos += 20;
			
			SizeButton("-", GetSizeButtonRect(), target, HeightAtLEastOne, RemoveRow, "Remove Row");
			xPos += ButtonWidth + 2;

			EditorGUI.LabelField(new Rect(xPos, position.y, sizeWidth, sizeRect.height), height.ToString(), sizeLabel);
			xPos += sizeWidth + 2;

			SizeButton("+", GetSizeButtonRect(), target, Always, AddRow, "Add Row");
	
			property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			position.y += sizeRect.height + Spacing;

			return;

			Rect GetSizeButtonRect() => new Rect(xPos, rect.y, ButtonWidth, sizeRect.height);
			void AddColumn() => floatMatrix.AddColumn();
			void AddRow() => floatMatrix.AddRow();
			void RemoveColumn() => floatMatrix.RemoveColumn();
			void RemoveRow() => floatMatrix.RemoveRow();
			bool Always() => true;
			bool WidthAtLeastOne() => width > 1;
			bool HeightAtLEastOne() => height > 1;
		}

		private static void SizeButton(
			string label,
			Rect rect,
			UnityObject target,
			Func<bool> canCall, 
			Action call,
			string undoName)
		{
			if (!GUI.Button(rect, label))
			{
				return;
			}

			if (!canCall())
			{
				return;
			}
			
			Undo.RecordObject(target, undoName);
			call();
			EditorUtility.SetDirty(target);
		}
		

		private static void DrawGrid(ref Rect position, int height, int width, SerializedProperty valuesProp)
		{
			for (int y = 0; y < height; y++)
			{
				var rowRect = new Rect(position.x, position.y + y * (CellSize + CellPadding), position.width, CellSize);

				for (int x = 0; x < width; x++)
				{
					int index = y * width + x;
					var cellProp = valuesProp.GetArrayElementAtIndex(index);

					var cellRect = new Rect(
						rowRect.x + x * (CellSize + CellPadding),
						rowRect.y,
						CellSize,
						CellSize
					);

					cellProp.floatValue = EditorGUI.FloatField(cellRect, cellProp.floatValue);
				}
			}
			
			position.y += height * (CellSize + CellPadding) + Spacing;
		}

		private static void DrawTitle(ref Rect position, GUIContent label)
		{
			var titleRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(titleRect, label, EditorStyles.boldLabel);
			
			position.y += EditorGUIUtility.singleLineHeight + Spacing;
		}
	}
}
