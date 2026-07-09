using Gamelogic.Fx.Internal;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a smooth, natural-looking blur using a Gaussian weight curve.
	/// See <see href="../common/docs/effects-reference-common.html#gaussian-blur"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.GaussianBlur)]
	[Version(1, 1, 0)]
	public sealed class GaussianBlurPostProcess : SeparablePostProcess<GaussianBlurProperties>
	{
	}
}
