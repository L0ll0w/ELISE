using UnityEngine;
using Gamelogic.Fx.BuiltIn.PostProcessing;
using Gamelogic.Fx.Dithering.PostProcessing.Effects;
using Constants = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.Constants;
using HelpURLs = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.HelpURLs;

namespace Gamelogic.Fx.Dithering.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// Post process effect that applies quantization with a dither texture bias pattern.
	/// See <see href="../dithering/docs/effects-reference-dithering.html#dither-texture-bias"/>
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.DitherTextureBias)]
	public sealed class DitherTextureBiasPostProcess : PostProcess<DitherTextureBiasProperties>
	{
	}
}
