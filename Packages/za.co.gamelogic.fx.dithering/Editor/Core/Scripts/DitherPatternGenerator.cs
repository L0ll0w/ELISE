using Gamelogic.Fx.Editor.TextureGenerator;
using Gamelogic.Fx.EditorTextureGenerator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gamelogic.Fx.Dithering.Editor
{
	/// <summary>
	/// Editor window that generates textures based on a FloatMatrix pattern.
	/// </summary>
	/// <remarks>
	/// Accessible from: Gamelogic|Tools|Texture Generation|Dither Pattern Generator
	/// </remarks>
	public sealed class DitherPatternGenerator : TextureGeneratorWindow
	{
		private enum PatternMode
		{
			Grayscale,
			RGB
		}

		private const string ToolName = "Dither Pattern Generator";
		private const string ToolMenuPath = TextureGenerationRoot + ToolName;

		private PatternMode mode = PatternMode.Grayscale;

		private SerializedObject serializedObject;
		private SerializedObject serializedObjectR;
		private SerializedObject serializedObjectG;
		private SerializedObject serializedObjectB;

		private WrappedFloatMatrix matrix;
		private SerializedProperty matrixProperty;

		private WrappedFloatMatrix matrixR;
		private WrappedFloatMatrix matrixG;
		private WrappedFloatMatrix matrixB;

		private SerializedProperty matrixRProperty;
		private SerializedProperty matrixGProperty;
		private SerializedProperty matrixBProperty;

		/// <summary>
		/// Shows the Dither Pattern Generator window.
		/// </summary>
		[MenuItem(ToolMenuPath)]
		public static void ShowWindow() => GetWindow<DitherPatternGenerator>(ToolName);

		/// <inheritdoc />
		protected override void DrawPropertiesGui()
		{
			InitSerializedObjects();
			UpdateSerializedObjects();

			mode = (PatternMode)EditorGUILayout.EnumPopup("Mode", mode);
			EditorGUILayout.Space();
			switch (mode)
			{
				case PatternMode.RGB:
					Header("Dither Matrix RGB");
					EditorGUILayout.PropertyField(matrixRProperty, new GUIContent("Matrix R"), true);
					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(matrixGProperty, new GUIContent("Matrix G"), true);
					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(matrixBProperty, new GUIContent("Matrix B"), true);

					serializedObjectR.ApplyModifiedProperties();
					serializedObjectG.ApplyModifiedProperties();
					serializedObjectB.ApplyModifiedProperties();
					
					break;

				case PatternMode.Grayscale:
					Header("Dither Matrix (Grayscale)");
					EditorGUILayout.PropertyField(matrixProperty, new GUIContent("Matrix"), true);
					
					serializedObject.ApplyModifiedProperties();
					break;
			}
		}

		/// <inheritdoc />
		protected override Texture2D GenerateTexture(Vector2Int dimensions) 
			=> mode switch
			{
				PatternMode.Grayscale => GenerateGreyscaleTexture(dimensions),
				PatternMode.RGB => GenerateRGBTexture(dimensions),
				_ => throw new System.ArgumentOutOfRangeException()
			};

		private void UpdateSerializedObjects()
		{
			serializedObject.Update();
			serializedObjectR.Update();
			serializedObjectG.Update();
			serializedObjectB.Update();
		}

		private Texture2D GenerateGreyscaleTexture(Vector2Int dimensions)
		{
			var normalized = matrix.matrix.Normalize();
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			// UI prevents empty matrices
			Assert.IsTrue(normalized.Length > 0);

			for (int y = 0; y < dimensions.y; y++)
			{
				for (int x = 0; x < dimensions.x; x++)
				{
					float value = GetWrapped(normalized, x, y);
					var color = new Color(value, value, value, 1f);

					texture.SetPixel(x, y, color);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}

		private void InitSerializedObjects()
		{
			Init(ref matrix, ref serializedObject, ref matrixProperty);
			Init(ref matrixR, ref serializedObjectR, ref matrixRProperty);
			Init(ref matrixG, ref serializedObjectG, ref matrixGProperty);
			Init(ref matrixB, ref serializedObjectB, ref matrixBProperty);
		}

		private static void Init(
			ref WrappedFloatMatrix matrix,
			ref SerializedObject serializedObject, 
			ref SerializedProperty serializedProperty)
		{
			if (matrix != null)
			{
				return;
			}
			
			matrix = CreateInstance<WrappedFloatMatrix>();
			serializedObject = new SerializedObject(matrix);
			serializedProperty = serializedObject.FindRequiredProperty(WrappedFloatMatrix.MatrixFieldName);
		}

		private Texture2D GenerateRGBTexture(Vector2Int dimensions)
		{
			var matrixRNormalized = matrixR.matrix.Normalize();
			var matrixGNormalized = matrixG.matrix.Normalize();
			var matricBNormalized = matrixB.matrix.Normalize();

			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			// UI prevents empty matrices
			Assert.IsTrue(matrixRNormalized.Length > 0);
			Assert.IsTrue(matrixGNormalized.Length > 0);
			Assert.IsTrue(matricBNormalized.Length > 0);

			for (int y = 0; y < dimensions.y; y++)
			{
				for (int x = 0; x < dimensions.x; x++)
				{
					float r = GetWrapped(matrixRNormalized, x, y);
					float g = GetWrapped(matrixGNormalized, x, y);
					float b = GetWrapped(matricBNormalized, x, y);

					texture.SetPixel(x, y, new Color(r, g, b, 1f));
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}
		
		private static float GetWrapped(IFloatMatrix matrix, int x, int y) 
			=> matrix[x % matrix.Width, y % matrix.Height];
	}
}
