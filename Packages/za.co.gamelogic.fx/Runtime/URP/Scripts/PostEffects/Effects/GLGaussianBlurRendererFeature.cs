#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a smooth, natural-looking blur using a Gaussian weight curve.
	/// See <see href="../common/docs/effects-reference-common.html#gaussian-blur"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.GaussianBlur)]
	public sealed class GLGaussianBlurRendererFeature : SeparableRendererFeature<GaussianBlurProperties>
	{
	}
}
#endif
