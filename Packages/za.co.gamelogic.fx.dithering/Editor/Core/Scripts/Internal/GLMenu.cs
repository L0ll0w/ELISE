using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Dithering.Editor.Internal
{
	/// <summary>
	/// Menu items that open Gamelogic Fx Dithering documentation links.
	/// </summary>
	internal static class GLMenu
	{
		[MenuItem("Help/Gamelogic/Fx.Dithering/Documentation")]
		public static void OpenFxDitheringAPI() => Application.OpenURL("https://www.gamelogic.co.za/documentation/fx/dithering/");
	}
}
