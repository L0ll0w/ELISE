using UnityEditor;

using Gamelogic.Extensions.Editor.Internal;

namespace Gamelogic.Fx.Dithering.Editor.Internal
{
	/// <summary>
	/// Menu items for the Dithering asset, and a method to run the welcome window on startup.
	/// </summary>
	internal static class DitheringTools
	{
		private const string DitheringToolsMenuRoot = Constants.ToolsMenuRoot + "⚙  Fx Dithering/";
		
		private static readonly AssetConfig Config = new()
		{
			assetDisplayName = "Gamelogic Fx Dithering",
			packageId = "za.co.gamelogic.fx.dithering",
			packageVersion = "4.0.0",
			documentationUrl = "https://gamelogic.co.za/documentation/fx/dithering/docs/index.html",
			youTubeChannelUrl = "https://www.youtube.com/@GamelogicCoZa",
			shownKey = "za.co.gamelogic.fx.dithering.WelcomeWindowShown",
			
			uninstallList = new[]
			{
				new Package { displayName = "Gamelogic Fx Dithering", id = "za.co.gamelogic.fx.dithering" },
				new Package { displayName = "Gamelogic Fx", id = "za.co.gamelogic.fx" },
				new Package { displayName = "Gamelogic Extensions", id = "za.co.gamelogic.extensions" },
			}
		};
		
		[MenuItem(DitheringToolsMenuRoot + AssetTools.OpenWelcomeWindowMenuItem, priority = 0)]
		private static void OpenWelcomeWindow() => AssetTools.OpenWelcomeWindow(Config);
		
		[MenuItem(DitheringToolsMenuRoot + AssetTools.ResetWelcomeWindowMenuItem, priority = 1)]
		private static void ResetWelcomeWindow() => AssetTools.ResetWelcomeWindow(Config.shownKey);

		[MenuItem(DitheringToolsMenuRoot + AssetTools.ImportSamplesMenuItem, priority = 100)]
		private static void ImportSamples() => AssetTools.ImportSamples(Config.packageId, Config.packageVersion);

		[MenuItem(DitheringToolsMenuRoot + AssetTools.ViewDocumentationMenuItem, priority = 101)]
		private static void OpenDocumentation() => AssetTools.OpenDocumentation(Config.documentationUrl);

		[MenuItem(DitheringToolsMenuRoot + AssetTools.ViewYouTubeChannelMenuItem, priority = 102)]
		private static void OpenYouTubeChannel() => AssetTools.OpenYouTubeChannel(Config.youTubeChannelUrl);

		[MenuItem(DitheringToolsMenuRoot + AssetTools.UninstallInstructionsMenuItem, priority = 201)]
		private static void OpenUninstallInstructions() => AssetTools.OpenUninstallInstructions(Config.uninstallList);
		
		static DitheringTools() => ExtensionsTools.IsMainAsset = false;

		[InitializeOnLoadMethod]
		private static void ShowOnStartup()
		{
			if (WelcomeWindow.HasBeenShown(Config.shownKey))
			{
				return;
			}

			EditorApplication.delayCall += () => AssetTools.OpenWelcomeWindow(Config);
		}
	}
}
