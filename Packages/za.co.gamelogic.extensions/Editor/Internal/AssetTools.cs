using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Methods for handling asset related actions such as importing samples, opening documentation, and resetting the welcome window.
	/// </summary>
	/// <remarks>
	/// To use it: define a new static class modeled on <see cref="ExtensionsTools"/>. 
	/// </remarks>
	/*	Design note: Public so any asset can use it.
	*/
	public static class AssetTools
	{
		private const string ResourcesRoot = "Packages/za.co.gamelogic.extensions/Editor/Resources/";
		private const string LogoTextureName = "GamelogicLogo";
		
		/// <summary>The menu item suffix for the import samples action.</summary>
		public const string ImportSamplesMenuItem = "Import Samples";
		/// <summary>The menu item suffix for the view documentation action.</summary>
		public const string ViewDocumentationMenuItem = "View Documentation";
		/// <summary>The menu item suffix for the view YouTube channel action.</summary>
		public const string ViewYouTubeChannelMenuItem = "View YouTube Channel";
		/// <summary>The menu item suffix for the uninstall instructions action.</summary>
		public const string UninstallInstructionsMenuItem = "Uninstall Instructions";
		/// <summary>The menu item suffix for the reset welcome window action.</summary>
		public const string ResetWelcomeWindowMenuItem = "Reset Welcome Window";
		/// <summary>The menu item suffix for the open welcome window action.</summary>
		public const string OpenWelcomeWindowMenuItem = "Open Welcome Window";

		/// <summary>Imports all samples for the specified package, overriding any previously imported samples.</summary>
		/// <param name="packageID">The ID of the package.</param>
		/// <param name="packageVersion">The version of the package.</param>
		public static void ImportSamples(string packageID, string packageVersion)
		{
			var samples = Sample.FindByPackage(packageID, packageVersion);

			foreach (var sample in samples)
			{
				sample.Import(Sample.ImportOptions.OverridePreviousImports);
			}
		}

		/// <summary>Opens the documentation URL in the default browser.</summary>
		/// <param name="url">The documentation URL to open.</param>
		public static void OpenDocumentation(string url) => Application.OpenURL(url);

		/// <summary>Opens the YouTube channel URL in the default browser.</summary>
		/// <param name="url">The YouTube channel URL to open.</param>
		public static void OpenYouTubeChannel(string url) => Application.OpenURL(url);

		/// <summary>Opens the uninstall instructions window for the specified packages.</summary>
		/// <param name="packages">The list of packages to include in the uninstall instructions.</param>
		public static void OpenUninstallInstructions(IReadOnlyList<Package> packages) => UninstallInstructionsWindow.Open(packages);

		/// <summary>Resets the welcome window so it shows again on the next editor startup.</summary>
		/// <param name="shownKey">The editor prefs key that tracks whether the window has been shown.</param>
		public static void ResetWelcomeWindow(string shownKey)
		{
			WelcomeWindow.ResetShownState(shownKey);

			EditorUtility.DisplayDialog(
				"Reset Welcome Window",
				"The welcome window will be shown again on next startup.",
				"OK");
		}

		/// <summary>Opens the welcome window for the specified asset configuration.</summary>
		/// <param name="config">The asset configuration to display in the window.</param>
		public static void OpenWelcomeWindow(AssetConfig config) => WelcomeWindow.Open(config);

		public static void OpenEmailSupportWindow()
		{
			EmailSupportWindow.Open();
		}
		
		public static Texture2D LoadIcon(string imageName)
		{
			string fullPath = $"{ResourcesRoot}/{imageName}.png";
			return AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
		}

		public static Texture2D LoadLogo() => LoadIcon(LogoTextureName);
	}
}
