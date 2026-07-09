using Gamelogic.Extensions;
using UnityEditor;

namespace Gamelogic.Fx.Dithering.Editor
{
	/// <summary>
	/// Class for loading dither matrix presets used by the interface. 
	/// </summary>
	[InitializeOnLoad]
	internal sealed class DitherMatrixPresetsLoader
	{
		static DitherMatrixPresetsLoader()
		{
			PropertyDrawerData.RegisterValuesRetriever(
				nameof(DitherMatrixPresets),
				() => DitherMatrixPresets.All
			);
		}
	}
}
