#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// Inverts the colors of the image.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.Invert)]
	public sealed class GLInvertRendererFeature : PostProcessRendererFeature<InvertProperties>
	{
	}
}
#endif
