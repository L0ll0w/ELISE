using System.IO;
using UnityEditor;
using UnityEngine;

using Gamelogic.Extensions.Support; 

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Welcome window shown on first import of a Gamelogic asset.
	/// </summary>
	public class WelcomeWindow : EditorWindow
	{
		private const string ImportIconName = "Import";
		private const string ExternalLinkIconName = "ExternalLink";
		private const string InfoIconName = "Info";
		private const string ResetIconName = "Reset";
		private const string EmailIconName = "Email";

		// All these fields are serialized so the window remains valid even when scripts recompile
		[SerializeField] private AssetConfig config;

		[SerializeField] private Texture2D logo;
		[SerializeField] private Texture2D importIcon;
		[SerializeField] private Texture2D externalLinkIcon;
		[SerializeField] private Texture2D infoIcon;
		[SerializeField] private Texture2D resetIcon;
		[SerializeField] private Texture2D emailIcon;

		/// <summary>Opens the welcome window for the specified asset configuration and marks it as shown.</summary>
		/// <param name="config">The asset configuration containing display name, URLs, and the shown key.</param>
		public static void Open(AssetConfig config)
		{
			MarkAsShown(config.shownKey);

			var window = GetWindow<WelcomeWindow>(true, config.assetDisplayName, true);
			window.config = config;
			window.minSize = new Vector2(480, 620);
			window.maxSize = new Vector2(480, 620);
			window.LoadResources();
		}

		/// <summary>Returns whether the welcome window has been shown for the given key in this project.</summary>
		/// <param name="shownKey">The unique key identifying this asset's shown state.</param>
		public static bool HasBeenShown(string shownKey) =>
			File.Exists(MarkerPath(shownKey));

		/// <summary>Resets the shown state for the given key so the window will appear again on next startup.</summary>
		/// <param name="shownKey">The unique key identifying this asset's shown state.</param>
		public static void ResetShownState(string shownKey)
		{
			if (File.Exists(MarkerPath(shownKey)))
			{
				File.Delete(MarkerPath(shownKey));
			}
		}

		private static string MarkerPath(string shownKey) =>
			$"Library/{shownKey}.shown";

		private static void MarkAsShown(string shownKey) =>
			File.WriteAllText(MarkerPath(shownKey), "");

		private void OnEnable()
		{
			if (logo == null)
			{
				LoadResources();
			}
		}

		private void LoadResources()
		{
			logo = AssetTools.LoadLogo();
			importIcon = AssetTools.LoadIcon(ImportIconName);
			externalLinkIcon = AssetTools.LoadIcon(ExternalLinkIconName);
			infoIcon = AssetTools.LoadIcon(InfoIconName);
			resetIcon = AssetTools.LoadIcon(ResetIconName);
			emailIcon = AssetTools.LoadIcon(EmailIconName);
		}

		private void OnGUI()
		{
			if (config == null)
			{
				EditorGUILayout.HelpBox("Configuration lost after recompile. Please reopen this window.", MessageType.Warning);
				return;
			}

			GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(16, 16, 0, 16) });
			DrawHeaderWithLogo();
			DrawWelcomeMessage();
			DrawActions();
			DrawSupportEmail(emailIcon);
			GUILayout.Space(12);
			GLEditorGUI.Splitter(Branding.GrayA);
			DrawContextFeatures();	GUILayout.EndVertical();
		}

		private void DrawHeaderWithLogo()
		{
			const int logoWidth = 136;
			const int logoHeight = 30;

			EditorGUILayout.Space(10);

			if (logo != null)
			{
				var rowRect = EditorGUILayout.GetControlRect(false, logoHeight);
				var logoRect = new Rect(rowRect.xMax - logoWidth, rowRect.y, logoWidth, logoHeight);
				GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);
				EditorGUILayout.Space(24);
			}

			var titleStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 14,
				alignment = TextAnchor.MiddleLeft,
			};

			EditorGUILayout.LabelField($"Welcome to {config.assetDisplayName}", titleStyle);
			EditorGUILayout.Space(12);
		}

		private void DrawWelcomeMessage()
		{
			EditorGUILayout.HelpBox(
				"Thank you for installing " + config.assetDisplayName + "!\n\n" +
				"Import the samples to get started, or head to the documentation for guides and API reference.",
				MessageType.Info, true);

			EditorGUILayout.Space(12);
		}

		private void DrawActions()
		{
			if (DrawButton("Import Samples", importIcon))
			{
				AssetTools.ImportSamples(config.packageId, config.packageVersion);
			}

			EditorGUILayout.Space(4);

			if (DrawButton("Documentation", externalLinkIcon))
			{
				AssetTools.OpenDocumentation(config.documentationUrl);
			}

			EditorGUILayout.Space(4);

			if (!string.IsNullOrEmpty(config.youTubeChannelUrl))
			{
				if (DrawButton("YouTube Channel", externalLinkIcon))
				{
					AssetTools.OpenYouTubeChannel(config.youTubeChannelUrl);
				}

				EditorGUILayout.Space(4);
			}

			if (DrawButton("Uninstall Instructions", infoIcon))
			{
				AssetTools.OpenUninstallInstructions(config.uninstallList);
			}

			EditorGUILayout.Space(4);

			if (DrawButton("Reset Welcome Window", resetIcon))
			{
				AssetTools.ResetWelcomeWindow(config.shownKey);
			}
		}

		/// <summary>Draws a support email row with an icon, email address label, and copy-to-clipboard button.</summary>
		/// <param name="emailIcon">The icon to display next to the email address, or <see langword="null"/> to omit.</param>
		internal static void DrawSupportEmail(Texture2D emailIcon)
		{
			const float iconSize = 16;
			const float copyButtonSize = 24;

			EditorGUILayout.Space(8);

			var rowRect = EditorGUILayout.GetControlRect(false, iconSize);
			float x = rowRect.x + 12;

			if (emailIcon != null)
			{
				GUI.DrawTexture(new Rect(x, rowRect.y, iconSize, iconSize), emailIcon, ScaleMode.ScaleToFit);
				x += iconSize + 10;
			}

			var labelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft,
			};

			GUI.Label(new Rect(x, rowRect.y, 200, iconSize), Constants.SupportEmail, labelStyle);
			x += 160 + 6;

			var content = new GUIContent("", EditorGUIUtility.IconContent("Clipboard").image, "Copy email address");

			if (GUI.Button(new Rect(x, rowRect.y - 2, copyButtonSize, copyButtonSize), content))
			{
				EditorGUIUtility.systemCopyBuffer = Constants.SupportEmail;
			}
		}

		private static bool DrawButton(string label, Texture2D icon)
		{
			var rect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.button, GUILayout.Height(30));
			bool clicked = GUI.Button(rect, label, new GUIStyle(GUI.skin.button)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(40, 10, 0, 0),
			});

			if (icon != null)
			{
				const float iconSize = 16;
				var iconRect = new Rect(
					rect.x + 10,
					rect.y + (rect.height - iconSize) * 0.5f,
					iconSize,
					iconSize);

				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			}

			return clicked;
		}

		private void DrawContextFeatures()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			GLEditorGUI.Header("Context");
			
			GUILayout.TextArea("Used for debugging!", new GUIStyle(EditorStyles.label)
			{
				fontStyle = FontStyle.Italic,
			});
			
			foreach (string key in AssetConfig.ContextFeatures.Keys)
			{
				bool defined = AssetConfig.ContextFeatures[key];
				
				var style = new GUIStyle(EditorStyles.label)
				{
					normal = { textColor = defined ? Branding.Apple : Branding.Coral },
					fontSize = 10
				};

				EditorGUILayout.LabelField(defined ? $"{key} [Yes]" : $"{key} [No]", style);
			}
			
			EditorGUILayout.EndVertical();
		}
	}
}
