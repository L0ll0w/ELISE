using Gamelogic.Fx.BuiltIn.PostProcessing;
using Gamelogic.Fx.Dithering.PostProcessing.Effects;
using UnityEngine;
using Constants = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.Constants;
using HelpURLs = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.HelpURLs;

namespace Gamelogic.Fx.Dithering.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a data-driven dither matrix.
	/// See <see href="../dithering/docs/effects-reference-dithering.html#dither-matrix-bias"/>
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.DitherMatrixBias)]
	public sealed class DitherMatrixBiasPostProcess : PostProcess<DitherMatrixBiasProperties>
	{
	}
}
