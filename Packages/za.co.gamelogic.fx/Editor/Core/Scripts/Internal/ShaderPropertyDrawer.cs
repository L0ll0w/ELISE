using System.Reflection;
using Gamelogic.Fx.Internal;
using UnityEngine;
using UnityEditor;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// The custom property drawer for the <see cref="ShaderProperty"/> class.
	/// </summary>
	/// <remarks>
	/// This drawer can flag a warning in the UI if the property is not present in the material returned by a property in the containing
	/// objects called <c>EffectMaterial</c>.
	/// </remarks>
	[CustomPropertyDrawer(typeof(ShaderProperty))]
	internal sealed class ShaderPropertyDrawer : PropertyDrawer
	{
		// ReSharper disable once StringLiteralTypo
		private static readonly Texture2D WarningIcon =
			EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (var nameProp = property.FindPropertyRelative("name"))
			{
				using (var typeProp = property.FindPropertyRelative("shaderPropertyType"))
				{
					string propertyName = nameProp.stringValue;
					string type = typeProp.stringValue;

					const float warningIconSize = 16f;
					const float maxTypeWidth = 100f;
					float remainingWidth = position.width - maxTypeWidth - warningIconSize - 5;
					float halfRemainingWidth = remainingWidth / 2f;

					var warningRect = new Rect(position.x, position.y + (position.height - warningIconSize) / 2,
						warningIconSize,
						warningIconSize);
					var nameRect = new Rect(position.x + warningIconSize + 5, position.y,
						halfRemainingWidth - warningIconSize,
						position.height);
					var typeRect = new Rect(nameRect.xMax, position.y, Mathf.Min(maxTypeWidth, position.width / 3f),
						position.height);
					var valueRect = new Rect(typeRect.xMax, position.y, halfRemainingWidth, position.height);

					bool isKeyword = type == ShaderPropertyType.Keyword || type == ShaderPropertyType.KeywordSet;

					var material = GetMaterial(property);
					Rect? tooltipRect = null;

					// Arrays cannot be checked
					if (material != null && !isKeyword && !IsArrayType(type) && !material.HasProperty(propertyName))
					{
						GUI.DrawTexture(warningRect, WarningIcon);
						tooltipRect = warningRect;
					}

					EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
					EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

					DrawPropertyValueField(valueRect, property, type);

					if (tooltipRect.HasValue)
					{
						Tooltip(tooltipRect.Value, "This property is not used by the shader.");
					}
				}
			}
		}

		private void DrawPropertyValueField(Rect valueRect, SerializedProperty property, string type)
		{
			switch (type)
			{
				case ShaderPropertyType.Bool:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("boolValue"), GUIContent.none);
					break;
				case ShaderPropertyType.Int:
				case ShaderPropertyType.Integer:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("intValue"), GUIContent.none);
					break;
				case ShaderPropertyType.Float:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("floatValue"), GUIContent.none);
					break;
				case ShaderPropertyType.Texture:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("textureValue"), GUIContent.none);
					break;
				case ShaderPropertyType.Color:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("colorValue"), GUIContent.none);
					break;
				case ShaderPropertyType.Vector:
				case ShaderPropertyType.ScreenTextureSize:
					var vectorProp = property.FindPropertyRelative("vectorValue");
					DrawVector4(valueRect, vectorProp);
					break;
				case ShaderPropertyType.Keyword:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("keywordValue"), GUIContent.none);
					break;
				case ShaderPropertyType.KeywordSet:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("keywordSetValue.keywords"), true);
					break;
				case ShaderPropertyType.FloatArray:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("floatArrayValue"), true);
					break;
				case ShaderPropertyType.IntegerArray:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("intArrayValue"), true);
					break;
				case ShaderPropertyType.MatrixArray:
					EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("matrixArrayValue"), true);
					break;
				case ShaderPropertyType.FloatArrayCurve:
					var curveProp = property.FindPropertyRelative("curveValue");
					EditorGUI.CurveField(valueRect, curveProp, Color.green, new Rect(0, 0, 1, 1), GUIContent.none);
					break;
			}
		}

		private static void DrawVector4(Rect valueRect, SerializedProperty vectorProp)
		{
			var newValue = EditorGUI.Vector4Field(valueRect, GUIContent.none, vectorProp.vector4Value);
			vectorProp.vector4Value = newValue;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			using (var typeProperty = property.FindPropertyRelative("shaderPropertyType"))
			{
				var type = typeProperty.stringValue;

				switch (type)
				{
					case ShaderPropertyType.KeywordSet:
						return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("keywordSetValue.keywords"), true);
					case ShaderPropertyType.FloatArray:
						return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("floatArrayValue"), true);
					case ShaderPropertyType.IntegerArray:
						return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("intArrayValue"), true);
					case ShaderPropertyType.MatrixArray:
						return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("matrixArrayValue"), true);
					default:
						return EditorGUIUtility.singleLineHeight;
				}
			}
		}

		private static Material GetMaterial(SerializedProperty property)
		{
			var serializedObject = property.serializedObject;
			var obj = serializedObject.targetObject;
			const string propertyName = "EffectMaterial";

			var type = obj.GetType();
			
			const BindingFlags bindingFlags = 
				BindingFlags.Instance 
				| BindingFlags.Public 
				| BindingFlags.NonPublic;
				
			var propertyInfo = type.GetProperty(propertyName, bindingFlags);

			if (propertyInfo == null)
			{
				return null;
			}

			return propertyInfo.GetValue(obj) as Material;
		}

		private static void Tooltip(Rect rect, string message)
		{
			if (Event.current.type != EventType.Repaint || !rect.Contains(Event.current.mousePosition))
			{
				return;
			}

			bool isDarkTheme = EditorGUIUtility.isProSkin;
			var warningTextColor = isDarkTheme ? new Color(1f, .8f, 0.2f) : new Color(1f, 0.8f, 0f);

			var style = new GUIStyle(EditorStyles.helpBox)
			{
				wordWrap = true,
				fontSize = 12,
				padding = new RectOffset(5, 5, 5, 5),
				normal = { textColor = warningTextColor }
			};

			var textSize = style.CalcSize(new GUIContent(message));
			float tooltipWidth = Mathf.Clamp(textSize.x, 150, 400);
			float tooltipHeight = style.CalcHeight(new GUIContent(message), tooltipWidth);

			var mousePosition = Event.current.mousePosition;
			var tooltipRect = new Rect(mousePosition.x + 15, mousePosition.y + 10, tooltipWidth, tooltipHeight);

			GUI.Label(tooltipRect, message, style);
		}

		private static bool IsArrayType(string type)
			=> type == ShaderPropertyType.FloatArray 
				|| type == ShaderPropertyType.MatrixArray
				|| type == ShaderPropertyType.IntegerArray;
	}
}
