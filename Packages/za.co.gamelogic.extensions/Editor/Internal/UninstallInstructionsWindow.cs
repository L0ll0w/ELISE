using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Simple window that shows instructions for uninstalling the asset, based on the list of packages provided in the
	/// config. This is necessary because some assets consist of multiple packages that must be uninstalled in a specific
	/// order.
	/// </summary>
	internal class UninstallInstructionsWindow : EditorWindow
	{
		// All these fields are serialized so windows remain good even when scripts recompile
		[SerializeField] private List<Package> packages;
		[SerializeField] private GUIStyle richTextLabel;

		/// <summary>Opens the uninstall instructions window for the specified list of packages.</summary>
		/// <param name="packages">The packages to display uninstall instructions for.</param>
		public static void Open(IReadOnlyList<Package> packages)
		{
			var window = GetWindow<UninstallInstructionsWindow>(true, "Uninstall Instructions", true);
			window.packages = packages.ToList();
			window.minSize = new Vector2(480, 320);
			window.maxSize = new Vector2(480, 420);
			window.richTextLabel = new GUIStyle(EditorStyles.label) { richText = true , wordWrap = true };
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(16, 16, 16, 16) });

			if (packages == null || packages.Count == 0)
			{
				DrawNoPackages();
			}
			else if (packages.Count == 1)
			{
				DrawSinglePackage();
			}
			else
			{
				DrawMultiplePackages();
			}

			GUILayout.EndVertical();
		}

		private void DrawNoPackages()
		{
			EditorGUILayout.HelpBox(
				"No package list configured for this asset. This is probably a bug — please contact support@gamelogic.co.za.",
				MessageType.Error);
		}

		private void DrawSinglePackage()
		{
			EditorGUILayout.LabelField("To uninstall this asset:", EditorStyles.boldLabel);
			EditorGUILayout.Space(8);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("1. Open the Package Manager (<b>Window > Package Manager</b>).", richTextLabel);
			if (GUILayout.Button("Open", GUILayout.Height(24)))
			{
				EditorApplication.ExecuteMenuItem("Window/Package Manager");
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.LabelField("2. Go to the <b>In Project</b> tab.", richTextLabel);
			EditorGUILayout.LabelField($"3. Click on the package:");
			EditorGUILayout.Space(4);
			DrawPackageEntry(packages[0]);
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("4. In the panel on the right, click the Remove button.");
		}

		private void DrawMultiplePackages()
		{
			EditorGUILayout.LabelField(
				"This asset has more than one package. They must be uninstalled one by one in the correct order.",
				EditorStyles.wordWrappedLabel);
			EditorGUILayout.Space(12);
			EditorGUILayout.LabelField("To uninstall this asset:", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("1. Open the Package Manager (<b>Window > Package Manager</b>).", richTextLabel);
			if (GUILayout.Button("Open", GUILayout.Height(24)))
			{
				EditorApplication.ExecuteMenuItem("Window/Package Manager");
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("2. Go to the <b>In Project</b> tab.", richTextLabel);
			EditorGUILayout.LabelField("3. For each package, in the order below:");
			EditorGUILayout.LabelField("      a. Select it.");
			EditorGUILayout.LabelField("      b. In the panel on the right, click the Remove button.");
			EditorGUILayout.Space(12);
			
			EditorGUILayout.LabelField("Packages to uninstall (in order):", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);
			for (int i = 0; i < packages.Count; i++)
			{
				DrawPackageEntry(packages[i], i + 1);
			}
			
		}

		private void DrawPackageEntry(Package packageId, int index = 0)
		{
			string label = index > 0
				? $"{index}. {packageId.displayName} (<i>{packageId.id}</i>)"
				: $"{packageId.displayName} (<i>{packageId.id}</i>)";

			EditorGUILayout.LabelField(label, richTextLabel);
		}
	}
}
