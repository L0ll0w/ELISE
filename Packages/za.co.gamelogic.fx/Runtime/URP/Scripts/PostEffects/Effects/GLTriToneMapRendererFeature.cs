#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// Maps the image’s tones to three colors (low, mid, and high) based on lightness,
	/// smoothly blending between them using inverse linear interpolation.
	/// See <see href="../common/docs/effects-reference-common.html#tri-tone-map"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.TriToneMap)]
	public sealed class GLTriToneMapRendererFeature : PostProcessRendererFeature<TriToneMapProperties>
	{
	}
}
#endif
