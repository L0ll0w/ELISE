using Gamelogic.Fx.Internal;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that blurs the image by computing a power mean (p-norm mean) of neighboring pixels.
	/// See <see href="../common/docs/effects-reference-common.html#power-mean"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.PowerMean)]

	[Version(1, 1, 0)]
	public sealed class PowerMeanPostProcess : SeparablePostProcess<PowerMeanProperties>
	{
	}
}
