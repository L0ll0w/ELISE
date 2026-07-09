#if GAMELOGIC_HAS_URP
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using Gamelogic.Fx.URP.PostProcessing;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that pushes each pixel’s color toward chosen primaries using convex-hull projection,
	/// but blends them with Mixbox for more natural, smoother color transitions than normal interpolation.
	/// See <see href="../common/docs/effects-reference-common.html#mixbox-convex-hull-map"/>.
	/// </summary>
	[RequiresMixbox]
	[HelpURL(Constants.HelpURLRoot + HelpURLs.MixboxConvexHullMap)]
	[Version(1, 0, 0)]
	public sealed class GLMixboxConvexHullMapRendererFeature : PostProcessRendererFeature<MixboxConvexHullMapProperties>
	{
	}
}
#endif
