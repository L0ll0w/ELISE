using Gamelogic.Fx.Internal;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a uniform blur by averaging neighbor pixels colors.
	/// See <see href="../common/docs/effects-reference-common.html#box-blur"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.BoxBlur)]
	[Version(1, 1, 0)]
	public sealed class BoxBlurPostProcess : SeparablePostProcess<BoxBlurProperties>
	{
	}
}
