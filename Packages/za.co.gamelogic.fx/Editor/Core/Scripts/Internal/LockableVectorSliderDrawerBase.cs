using System.Reflection;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Base property drawer for lockable vector fields, rendering each component as a slider with a lock toggle.
	/// </summary>
	internal abstract class LockableVectorSliderDrawerBase : PropertyDrawer
	{
		/// <summary>Gets the number of vector components drawn by this drawer.</summary>
		protected abstract int ComponentCount { get; }

		private const float LineHeight = 18f;
		private const float Spacing = 2f;
		private const float Indent = 15f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight + Spacing + ComponentCount * (LineHeight + Spacing);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var rangeAttribute = fieldInfo.GetCustomAttribute<LockableVectorRangeAttribute>();
			float min = rangeAttribute?.min ?? 0f;
			float max = rangeAttribute?.max ?? 1f;

			var valueProp = property.FindPropertyRelative("vector");
			var lockProp = property.FindPropertyRelative("locked");

			Vector3 vector = ComponentCount == 3 ? valueProp.vector3Value : (Vector3)valueProp.vector2Value;
			float y = position.y;

			float labelTextWidth = GUI.skin.label.CalcSize(label).x;
			var labelRect = new Rect(position.x, y, labelTextWidth, EditorGUIUtility.singleLineHeight);
			var lockRect = new Rect(labelRect.xMax + 6f, y,
				position.width - labelTextWidth - 6f,
				EditorGUIUtility.singleLineHeight);

			EditorGUI.LabelField(labelRect, label);

			bool wasLocked = lockProp.boolValue;
			bool locked = EditorGUI.ToggleLeft(lockRect, "Lock", wasLocked);

			if (locked != wasLocked)
			{
				lockProp.boolValue = locked;
				if (locked)
				{
					if (ComponentCount >= 2) vector.y = vector.x;
					if (ComponentCount == 3) vector.z = vector.x;
					
					if(ComponentCount == 2)
					{
						valueProp.vector2Value = vector.To2DXY();
					}
					else if(ComponentCount == 3)
					{
						valueProp.vector3Value = vector;
					}
				}
			}

			y += EditorGUIUtility.singleLineHeight + Spacing;

			float nameWidth = 15f;
			float sliderWidth = position.width - Indent - nameWidth - 6f;

			float nameX = position.x + Indent;
			float sliderX = nameX + nameWidth + 2f;

			EditorGUI.BeginChangeCheck();

			float nx = DrawRow("X", vector.x, ref y, nameX, sliderX, sliderWidth, min, max, true);
			float ny = vector.y;
			float nz = vector.z;

			if (ComponentCount >= 2)
			{
				ny = DrawRow("Y", vector.y, ref y, nameX, sliderX, sliderWidth, min, max, !locked);
			}

			if (ComponentCount == 3)
			{
				nz = DrawRow("Z", vector.z, ref y, nameX, sliderX, sliderWidth, min, max, !locked);
			}

			if (locked)
			{
				if (ComponentCount >= 2) ny = nx;
				if (ComponentCount == 3) nz = nx;
			}

			if (EditorGUI.EndChangeCheck())
			{
				if (ComponentCount == 3)
				{
					valueProp.vector3Value = new Vector3(nx, ny, nz);
				}
				
				if(ComponentCount == 2)
				{
					valueProp.vector2Value = new Vector2(nx, ny);
				}
			}

			EditorGUI.EndProperty();
		}

		private float DrawRow(
			string label,
			float current,
			ref float y,
			float nameX,
			float sliderX,
			float sliderWidth,
			float min,
			float max,
			bool enabled)
		{
			var labelRect = new Rect(nameX, y, 15f, LineHeight);
			var sliderRect = new Rect(sliderX, y, sliderWidth, LineHeight);

			EditorGUI.LabelField(labelRect, label);

			EditorGUI.BeginDisabledGroup(!enabled);
			float val = EditorGUI.Slider(sliderRect, GUIContent.none, current, min, max);
			EditorGUI.EndDisabledGroup();

			y += LineHeight + Spacing;
			return val;
		}
	}
}
