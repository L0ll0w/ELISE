#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a pixelation effect to the image
	/// by sampling blocks of pixels instead of individual texels.
	/// See <see href="../common/docs/effects-reference-common.html#pixelate"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.Pixelate)]
	public sealed class GLPixelateRendererFeature : PostProcessRendererFeature<PixelateProperties>
	{
	}
}
#endif
