#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that reduces the number of distinct color values in the image,
	/// creating a posterized or stylized effect.
	/// See <see href="../common/docs/effects-reference-common.html#quantize"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.Quantize)]
	public sealed class GLQuantizeRendererFeature : PostProcessRendererFeature<QuantizeProperties>
	{
	}
}
#endif
