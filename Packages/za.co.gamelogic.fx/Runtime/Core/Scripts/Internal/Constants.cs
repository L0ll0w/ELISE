using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	internal static class Constants
	{
		public const string MainTexPropertyName = "_MainTex";
		public const string MainTexTilingPropertyName = MainTexPropertyName + "_ST";
		public const string ShaderNameRoot = "Gamelogic/Fx/";
		public const string MapsRoot = ShaderNameRoot + "Maps/";
		
		public const string HelpURLRoot = "https://www.gamelogic.co.za/documentation/fx/common/docs/effects-reference-common.html#";
		public const string MapsHelpURLRoot = "https://www.gamelogic.co.za/documentation/fx/common/docs/map-renderers-reference-common.html#";
		
		public static Color DefaultBackgroundColor = new Color(0.2f, 0.6f, 1f);
		public static ColorShaderPropertyList DefaultPrimaryColorsCopy => new ColorShaderPropertyList
		{
			Colors = new[]
			{
				new Color(0, 0, 0),
				new Color(0.6f, 0, 0.5f),
				new Color(1, 0.80f, 0),
				new Color(0, 0.4f, .6f),
				new Color(1f, 1f, 1f),
				
			}
		};
		
		public const int MaxPrimaryColors = 10;
		
		internal static readonly string[] OutlineSourceKeywordGroup =
		{
			"_",
			"USE_OUTLINE_SOURCE_TEX",
			"USE_NORMALS_TEXTURE",
			"USE_DEPTH_TEXTURE"
		};
		
		internal static string GetKeyword(OutlineSource source) => OutlineSourceKeywordGroup[(int) source];
	}
}
