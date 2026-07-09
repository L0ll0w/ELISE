#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that adjusts the gamma of the image.
	/// See <see href="../common/docs/effects-reference-common.html#adjust-gamma"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.AdjustGamma)]
	public sealed class GLAdjustGammaRendererFeature : PostProcessRendererFeature<AdjustGammaProperties>
	{
	}
}
#endif
