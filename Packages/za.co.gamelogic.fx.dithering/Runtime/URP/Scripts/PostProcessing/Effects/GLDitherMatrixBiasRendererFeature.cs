#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.URP.PostProcessing;
using UnityEngine;
using Constants = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.Constants;
using HelpURLs = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.HelpURLs;
using Gamelogic.Fx.Dithering.PostProcessing.Effects;

namespace Gamelogic.Fx.Dithering.URP
{
	/// <summary>
	/// A post process that applies a data-driven dither matrix.
	/// See <see href="../dithering/docs/effects-reference-dithering.html#dither-matrix-bias"/>
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.DitherMatrixBias)]
	public sealed class GLDitherMatrixBiasRendererFeature : PostProcessRendererFeature<DitherMatrixBiasProperties>
	{
	}
}
#endif
