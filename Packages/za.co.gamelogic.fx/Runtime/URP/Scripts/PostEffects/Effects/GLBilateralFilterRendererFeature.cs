#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that smooths the image using an edge-preserving, noise-reducing filter.
	/// See <see href="../common/docs/effects-reference-common.html#bilateral-filter"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.BilateralFilter)]
	public sealed class GLBilateralFilterRendererFeature : SeparableRendererFeature <BilateralFilterProperties>
	{
	}
}
#endif
