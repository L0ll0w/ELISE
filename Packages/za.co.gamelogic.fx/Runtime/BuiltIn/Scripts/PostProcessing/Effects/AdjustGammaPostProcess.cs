using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that adjusts the gamma of the image.
	/// See <see href="../common/docs/effects-reference-common.html#adjust-gamma"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.AdjustGamma)]
	public sealed class AdjustGammaPostProcess : PostProcess<AdjustGammaProperties>
	{
	}
}
