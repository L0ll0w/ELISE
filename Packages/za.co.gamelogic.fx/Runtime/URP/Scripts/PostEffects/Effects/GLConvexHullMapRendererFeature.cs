#if GAMELOGIC_HAS_URP
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that gradually shifts colors toward the provided primaries,
	/// producing posterized, clustered, or palette–constrained looks.
	/// See <see href="../common/docs/effects-reference-common.html#convex-hull-map"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.ConvexHullMap)]
	[Version(1, 0, 0)]
	public sealed class GLConvexHullMapRendererFeature : PostProcessRendererFeature<ConvexHullMapProperties>
	{
	}
}
#endif
