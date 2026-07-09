using System;
using Gamelogic.Fx.EditorTextureGenerator;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.TextureGenerator
{
	/// <summary>
	/// Editor window for generating gradient textures.
	/// </summary>
	/// <remarks>
	/// This tool can be accessed from the menu: Gamelogic|Tools|Texture Generation|Gradient Texture Generator.
	/// </remarks>
	public sealed class GradientTextureGenerator : TextureGeneratorWindow
	{
		private enum Direction
		{
			X,
			Y,
			Radial,
			Angular
		}

		private enum GradientType
		{
			SingleColor,
			Gradient,
			Curve,
			Sine
		}
		
		private const string ToolName = "Gradient Texture Generator";
		private const string ToolMenuPath = TextureGenerationRoot + ToolName;
		
		private Gradient gradient = new Gradient()
		{
			colorKeys = new[]
			{
				new GradientColorKey(Color.black, 0f),
				new GradientColorKey(Color.white, 1f)
			},
			alphaKeys = new[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		private AnimationCurve colorCurve = new AnimationCurve()
		{
			keys = new[]
			{
				new Keyframe(0, 0)
				{
					outTangent = 1,
				},
				new Keyframe(1, 1)
				{
					inTangent = 1
				}
			},
			
			postWrapMode = WrapMode.Clamp,
			preWrapMode = WrapMode.Clamp,
		};

		private AnimationCurve alphaCurve = new AnimationCurve()
		{
			keys = new[]
			{
				new Keyframe(0, 1),
				new Keyframe(1, 1)
			},

			postWrapMode = WrapMode.Clamp,
			preWrapMode = WrapMode.Clamp,
		};
		
		private Color singleColor = Color.white;
		
		private ColorList colorList;
		private SerializedObject serializedObject;
		private SerializedProperty colorsProperty;
		
		private GradientType gradientType = GradientType.Curve;
		private Direction direction = Direction.X;

		private bool discreteSteps = false;
		private int steps = 4;
		private bool flipT = false;
		private float offsetAngle = 0;
		private bool circularGradient;
		
		private float sineFrequency = 1f;
		private float phaseFraction = 0;
		
		private float sineAmplitude = 0.5f;
		private float sineAmplitudeOffset = 0.5f;
		
		
		/// <summary>
		/// Shows an instance of the <see cref="GradientTextureGenerator"/> window.
		/// </summary>
		[MenuItem(ToolMenuPath)]
		public static void ShowWindow() => GetWindow<GradientTextureGenerator>(ToolName);

		private void InitSerializedObjects()
		{
			if (colorList != null)
			{
				return;
			}
			
			colorList = CreateInstance<ColorList>();
			serializedObject = new SerializedObject(colorList);
			colorsProperty = serializedObject.FindRequiredProperty(colorList.ColorsFieldName);
		}

		private void UpdateSerializedObjects() => serializedObject.Update();

		/// <inheritdoc/>
		protected override void DrawPropertiesGui()
		{
			InitSerializedObjects();
			UpdateSerializedObjects();
			gradientType = (GradientType)EditorGUILayout.EnumPopup("Gradient Type", gradientType);
			EditorGUILayout.Space();

			Header("Sample Settings");
			DrawSingleColorControls();
			DrawGradientControls();
			
			DrawCurveControls();
			DrawSineControls();
			DrawDiscreteStepControls();
			EditorGUILayout.Space();

			Header("Generation Settings");
			DrawDirectionControls();
			DrawAngularControls();

			serializedObject.ApplyModifiedProperties();
			return;

			void DrawSingleColorControls()
			{
				if (gradientType != GradientType.SingleColor)
				{
					return;
				}

				singleColor = EditorGUILayout.ColorField(singleColor);
			}

			void DrawGradientControls()
			{
				if (gradientType != GradientType.Gradient)
				{
					return;
				}

				gradient = EditorGUILayout.GradientField("Gradient", gradient);
			}

			void DrawCurveControls()
			{
				if (gradientType != GradientType.Curve)
				{
					return;
				}

				colorCurve = EditorGUILayout.CurveField("Curve", colorCurve);
				alphaCurve = EditorGUILayout.CurveField("Alpha Curve", alphaCurve);
				EditorGUILayout.PropertyField(colorsProperty, new GUIContent("Colors"), true);
			}
			
			void DrawSineControls()
			{
				if (gradientType != GradientType.Sine)
				{
					return;
				}

				sineFrequency = EditorGUILayout.FloatField("Frequency", sineFrequency);
				phaseFraction = EditorGUILayout.Slider("Phase Fraction", phaseFraction, 0, 1);
				
				sineAmplitude = EditorGUILayout.FloatField("Amplitude", sineAmplitude);
				sineAmplitudeOffset = EditorGUILayout.FloatField("Amplitude Offset", sineAmplitudeOffset);
				EditorGUILayout.PropertyField(colorsProperty, new GUIContent("Colors"), true);
			}

			void DrawDiscreteStepControls()
			{
				EditorGUI.BeginDisabledGroup(gradientType == GradientType.SingleColor);
				discreteSteps = EditorGUILayout.Toggle("Discrete Steps", discreteSteps);
				EditorGUI.BeginDisabledGroup(!discreteSteps);
				steps = EditorGUILayout.IntField("Steps", steps);
				steps = Mathf.Max(1, steps); // Ensure steps is at least 1

				circularGradient = EditorGUILayout.Toggle("Circular gradient", circularGradient);

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			void DrawDirectionControls()
			{
				EditorGUI.BeginDisabledGroup(gradientType == GradientType.SingleColor);
				direction = (Direction)EditorGUILayout.EnumPopup("Direction", direction);
				flipT = EditorGUILayout.Toggle("Flip T", flipT);
				EditorGUI.EndDisabledGroup();
			}

			void DrawAngularControls()
			{
				if (direction != Direction.Angular)
				{
					return;
				}

				EditorGUI.BeginDisabledGroup(gradientType == GradientType.SingleColor);
				offsetAngle = EditorGUILayout.FloatField("Offset Angle (Degrees)", offsetAngle);
				EditorGUI.EndDisabledGroup();
			}
		}

		protected override Texture2D GenerateTexture(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int x = 0; x < dimensions.x; x++)
			{
				for (int y = 0; y < dimensions.y; y++)
				{
					float t = GetT(x, y, dimensions.x, dimensions.y);
					t = GetDiscreteT(t);
					t = GetFlipT(t);
					var color = GetColor(t);
					texture.SetPixel(x, y, color);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}

		private float GetFlipT(float t) => flipT ? 1 - t : t;

		private float GetT(int x, int y, int width, int height)
		{
			switch (direction)
			{
				case Direction.X:
					return x / (width - 1f);
				case Direction.Y:
					return y / (height - 1f);
				case Direction.Radial:
					return 2 * Mathf.Sqrt(Sqr(x / (width - 1f) - 0.5f) + Sqr(y / (height - 1f) - 0.5f));
				case Direction.Angular:
					return Mod(Mathf.Atan2(y - height / 2f, x - width / 2f) / (2 * Mathf.PI) + offsetAngle / 360f, 1f);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private float Mod(float x, float y) => x - y * Mathf.Floor(x / y);

		/// <summary>
		/// Calculates the square of a number.
		/// </summary>
		/// <param name="x">The number to square.</param>
		/// <returns>The square of the number.</returns>
		// Reuse candidate
		private float Sqr(float x) => x * x;

		private float GetDiscreteT(float t)
		{
			if (!discreteSteps)
			{
				return t;
			}

			if (steps == 1)
			{
				return 0;
			}

			float scaled = t * steps;
			float xn = Mathf.FloorToInt(scaled);
			float stepIndex = Mathf.Clamp(xn, 0, steps - 1);
			float stepT = stepIndex / (circularGradient ? steps : steps - 1);
			return stepT;
		}

		private Color GetColor(float t)
		{
			switch (gradientType)
			{
				case GradientType.SingleColor:
					return singleColor;
				case GradientType.Gradient:
					return gradient.Evaluate(t);
				case GradientType.Curve:
					return colorList.Evaluate(colorCurve.Evaluate(t));
				case GradientType.Sine:
					return colorList.Evaluate(EvaluateSine(t));
				default:
					throw new ArgumentOutOfRangeException(
						$"No implementation for {gradientType} of type {gradientType.GetType()}");
			}
		}

		private float EvaluateSine(float t) 
			=> Mathf.Clamp01(Mathf.Sin(2 * Mathf.PI * (phaseFraction + sineFrequency * t)) * sineAmplitude + sineAmplitudeOffset);
	}
}
