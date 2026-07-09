using UnityEditor;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Menu items for the Extensions asset, and a method to run the welcome window on startup.
	/// </summary>
	public static class ExtensionsTools
	{
		private const string ExtensionsToolsMenuRoot = Constants.ToolsMenuRoot + "⚙  Extensions/";
		
		// ReSharper disable once FieldCanBeMadeReadOnly.Global
		// ReSharper disable once MemberCanBePrivate.Global
		public static bool IsMainAsset = true;
		
		private static readonly AssetConfig Config = new AssetConfig
		{
			assetDisplayName = "Gamelogic Extensions",
			packageId = "za.co.gamelogic.extensions",
			packageVersion = "4.6.1",
			documentationUrl = "https://www.gamelogic.co.za/documentation/extensions/",
			youTubeChannelUrl = "https://www.youtube.com/@GamelogicCoZa",
			shownKey = "za.co.gamelogic.extensions.WelcomeWindowShown",
			
			uninstallList = new[]
			{
				new Package { displayName = "Gamelogic Extensions", id = "za.co.gamelogic.extensions" }
			}
		};
		
		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.OpenWelcomeWindowMenuItem, priority = 0)]
		private static void OpenWelcomeWindow() => AssetTools.OpenWelcomeWindow(Config);
		
		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.ResetWelcomeWindowMenuItem, priority = 1)]
		private static void ResetWelcomeWindow() => AssetTools.ResetWelcomeWindow(Config.shownKey);

		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.ImportSamplesMenuItem, priority = 100)]
		private static void ImportSamples() => AssetTools.ImportSamples(Config.packageId, Config.packageVersion);

		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.ViewDocumentationMenuItem, priority = 101)]
		private static void OpenDocumentation() => AssetTools.OpenDocumentation(Config.documentationUrl);

		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.ViewYouTubeChannelMenuItem, priority = 102)]
		private static void OpenYouTubeChannel() => AssetTools.OpenYouTubeChannel(Config.youTubeChannelUrl);

		[MenuItem(ExtensionsToolsMenuRoot + AssetTools.UninstallInstructionsMenuItem, priority = 200)]
		private static void OpenUninstallInstructions() => AssetTools.OpenUninstallInstructions(Config.uninstallList);

		[MenuItem(Constants.ToolsMenuRoot + "Contact Support", priority = 201)]
		private static void OpenEmailSupportWindow() => AssetTools.OpenEmailSupportWindow();
		
		[InitializeOnLoadMethod]
		private static void ShowOnStartup()
		{
			if (WelcomeWindow.HasBeenShown(Config.shownKey))
			{
				return;
			}

			/* Extensions is used by all assets. When Extensions is used by itself, its OK to show this window. But when
				used with other assets, it is better that the user sees only one welcome window, and that of the main asset.
				
				This is not ideal, but a reasonable compromise.
			*/
			if (!IsMainAsset)
			{
				return;
			}

			EditorApplication.delayCall += () => AssetTools.OpenWelcomeWindow(Config);
		}
	}
}
