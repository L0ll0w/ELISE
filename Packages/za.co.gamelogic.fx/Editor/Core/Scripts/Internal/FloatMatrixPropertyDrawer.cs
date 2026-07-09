using System;
using System.Collections.Generic;
using System.Reflection;
using Gamelogic.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Property drawer for <see cref="FloatMatrix"/> fields.
	/// </summary>
	[CustomPropertyDrawer(typeof(FloatMatrix))]
	internal sealed class FloatMatrixDrawer : PropertyDrawer
	{
		
		public sealed class FloatMatrixPresetPopup : EditorWindow
		{
			private const float ButtonHeight = 22f;

			private SerializedProperty matrixProperty;
			private float contentHeight;
			private Vector2 scroll;
			private KeyValuePair<string, FloatMatrix>[] presets;

			public static void Show(Rect activatorRect, SerializedProperty property, string presetsKey)
			{
				var window = CreateInstance<FloatMatrixPresetPopup>();
				window.matrixProperty = property.Copy();
				
				window.presets = PropertyDrawerData.GetValues<KeyValuePair<string, FloatMatrix>>(presetsKey);

				float width = activatorRect.width;

				// Height: 1 cancel + all presets
				int count = window.presets.Length;
				float total = (ButtonHeight + Spacing) * (count + 1);
				float height = Mathf.Min(total + 6, 300f); // clamp max height

				// Position the popup below the button
				var rect = new Rect(
					GUIUtility.GUIToScreenRect(activatorRect).x,
					GUIUtility.GUIToScreenRect(activatorRect).yMax,
					width,
					height
				);

				window.position = rect;
				window.ShowPopup();
				window.Focus();
			}

			private void OnGUI()
			{
				if (matrixProperty == null)
				{
					Close();
					return;
				}

				float y = 0f;
				
				GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
				if (GUI.Button(new Rect(0, y, position.width, ButtonHeight), "Cancel"))
				{
					Close();
					return;
				}

				GUI.backgroundColor = Color.white;
				y += ButtonHeight + Spacing;
				
				contentHeight = (ButtonHeight + Spacing) * presets.Length;

				var scrollViewRect = new Rect(0, y, position.width, position.height - y);
				var viewRect = new Rect(0, 0, position.width - 20, contentHeight);

				DrawScrollView(scrollViewRect, viewRect);
			}

			private void DrawScrollView(Rect scrollViewRect, Rect viewRect)
			{
				scroll = GUI.BeginScrollView(scrollViewRect, scroll, viewRect);

				float y = 0f;
				
				foreach (var preset in presets)
				{
					var buttonRect = new Rect(0, y, viewRect.width, ButtonHeight);

					if (GUI.Button(buttonRect, preset.Key))
					{
						ApplyPreset(preset.Value);
						Close();
						return;
					}

					y += ButtonHeight + Spacing;
				}

				GUI.EndScrollView();
			}

			private void ApplyPreset(FloatMatrix preset)
			{
				var target = matrixProperty.serializedObject.targetObject;
				var floatMatrix = (FloatMatrix)matrixProperty.GetTargetObjectOfProperty();
				
				Undo.RecordObject(target, "Load Dither Preset");
				floatMatrix.SetFrom(preset);
				EditorUtility.SetDirty(target);
			}
		}
		
		private const float CellSize = 32f;
		private const float CellPadding = 4f;
		private const float ButtonWidth = 18f;
		private const float Spacing = 6f;
		
		private readonly GUIStyle sizeLabel = new GUIStyle(EditorStyles.label)
		{
			alignment = TextAnchor.MiddleCenter
		};
		
		public SerializedProperty GetWidthProperty(SerializedProperty property) 
			=> property.FindPropertyRelative(FloatMatrix.WidthFieldName);
		
		public SerializedProperty GetHeightProperty(SerializedProperty property) 
			=> property.FindPropertyRelative(FloatMatrix.HeightFieldName);
		
		public SerializedProperty GetValuesProperty(SerializedProperty property) 
			=> property.FindPropertyRelative(FloatMatrix.ValuesFieldName);

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
			var buttonRect = new Rect(position.x, position.y, 200, EditorGUIUtility.singleLineHeight);

			var customAttribute = fieldInfo.GetCustomAttribute<PresetsAttribute>();
			bool hasPresets = 
				customAttribute != null 
				&& !string.IsNullOrEmpty(customAttribute.PresetRetrievalKey)
				&& customAttribute.PresetRetrievalKey.Length > 0;
			
			bool oldIsEnabled = GUI.enabled;
			GUI.enabled = hasPresets;
			
			if (GUI.Button(buttonRect, "Presets"))
			{
				Assert.IsNotNull(customAttribute, "PresetsAttribute is null despite hasPresets being true.");
				FloatMatrixPresetPopup.Show(buttonRect, property, customAttribute.PresetRetrievalKey);
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
			var floatMatrix = (FloatMatrix)property.GetTargetObjectOfProperty();

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
			Object target,
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
