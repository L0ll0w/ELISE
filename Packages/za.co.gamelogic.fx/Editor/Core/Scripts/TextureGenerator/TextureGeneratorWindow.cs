using System.IO;
using Gamelogic.Fx.Editor.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.TextureGenerator
{
	/// <summary>
	/// A window that can be used as a base class for texture generation windows. See the code of
	/// <see cref="GradientTextureGenerator"/> for an example of how to implement a texture generation window.
	/// </summary>
	public abstract class TextureGeneratorWindow : EditorWindow
	{
		protected const string TextureGenerationRoot = Constants.ToolsRoot + "Texture Generation/";
		private const int DefaultImageSize = 256;
		private const string NoTexture = "No texture was created.";
		private const string TextureDimensionsDontMatch = "Created texture dimensions do not match those requested.";
		
		private static readonly Vector2Int PreviewImageSizeMax = new Vector2Int(256, 256);
		
		
		private Vector2Int imageDimensions = new Vector2Int(DefaultImageSize, DefaultImageSize);
		private Texture2D previewTexture;
		
		Vector2Int PreviewDimensions => Vector2Int.Min(PreviewImageSizeMax, imageDimensions);
		
		public void OnGUI()
		{
			//UpdateSerializedObjects();
			EditorGUI.BeginChangeCheck();
			DrawPropertiesGui();
			DrawImageSizeControls(ref imageDimensions);

			if (EditorGUI.EndChangeCheck() || previewTexture == null)
			{
				GeneratePreviewTexture();
			}

			DrawPreviewTexture();

			if (GUILayout.Button("Save Texture"))
			{
				GenerateAndSaveTexture();
			}
		}
		
		public void OnDestroy()
		{
			if (previewTexture != null)
			{
				DestroyImmediate(previewTexture);
			}
		}
		
		/// <summary>
		/// Draws a header label.
		/// </summary>
		/// <param name="text">The header text.</param>
		// Reuse candidate
		protected static void Header(string text) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel);

		/// <summary>
		/// Draw controls for the specific texture window. 
		/// </summary>
		/// <remarks>
		/// You do not need to draw the image dimensions controls, the preview texture or the save button. 
		/// </remarks>
		protected abstract void DrawPropertiesGui();

		/// <summary>
		/// Generates the texture based on the current settings.
		/// </summary>
		/// <param name="dimensions">The pixel dimensions of the texture to generate.</param>
		/// <returns>The generated texture.</returns>
		protected abstract Texture2D GenerateTexture(Vector2Int dimensions);

		private static void DrawImageSizeControls(ref Vector2Int imageDimensions)
		{
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("512x512"))
			{
				imageDimensions = new Vector2Int(512, 512);
			}
			if(GUILayout.Button("256x256"))
			{
				imageDimensions = new Vector2Int(256, 256);
			}
			if(GUILayout.Button("256x8"))
			{
				imageDimensions = new Vector2Int(256, 8);
			}
				
			EditorGUILayout.EndHorizontal();
			imageDimensions = EditorGUILayout.Vector2IntField("Image Dimensions", imageDimensions);
		}

		private void DrawPreviewTexture()
		{
			if (previewTexture == null)
			{
				return;
			}

			EditorGUILayout.Space();
			Header("Preview");
			var previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));
			EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
		}

		private void GenerateAndSaveTexture()
		{
			string path = EditorUtility.SaveFilePanel("Save Texture", string.Empty, "GradientTexture.png", "png");

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			var texture = GenerateTexture(imageDimensions);

			if (texture != null)
			{
				if (texture.width != imageDimensions.x || texture.height != imageDimensions.y)
				{
					Debug.LogWarning(TextureDimensionsDontMatch);
				}
				
				SaveTexture(texture, path);
				DestroyImmediate(texture);
			}
			else
			{
				Debug.LogWarning(NoTexture);
			}
		}
		
		private static void SaveTexture(Texture2D texture, string path)
		{
			texture.wrapMode = TextureWrapMode.Clamp;
			byte[] bytes = texture.EncodeToPNG();
			File.WriteAllBytes(path, bytes);
			AssetDatabase.Refresh();
		}

		private void GeneratePreviewTexture()
		{
			if (previewTexture != null)
			{
				DestroyImmediate(previewTexture);
			}

			previewTexture = GenerateTexture(PreviewDimensions);

			if (previewTexture != null)
			{
				if (previewTexture.width != PreviewDimensions.x || previewTexture.height != PreviewDimensions.y)
				{
					Debug.LogWarning(TextureDimensionsDontMatch);
				}
			}
			else
			{
				Debug.LogWarning(NoTexture);
			}
			
			Repaint();
		}
	}
}
