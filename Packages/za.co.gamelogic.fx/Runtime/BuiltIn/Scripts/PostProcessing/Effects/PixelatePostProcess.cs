using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that applies a pixelation effect to the image
	/// by sampling blocks of pixels instead of individual texels.
	/// See <see href="../common/docs/effects-reference-common.html#pixelate"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.Pixelate)]
	public sealed class PixelatePostProcess : PostProcess<PixelateProperties>
	{
	}
}
