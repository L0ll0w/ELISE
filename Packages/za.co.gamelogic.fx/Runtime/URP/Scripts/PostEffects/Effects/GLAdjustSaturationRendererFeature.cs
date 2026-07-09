#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that adjusts the saturation of the image.
	/// See <see href="../common/docs/effects-reference-common.html#adjust-saturation"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.AdjustSaturation)]
	public sealed class GLAdjustSaturationRendererFeature : PostProcessRendererFeature<AdjustSaturationProperties>
	{
	}
}
#endif
