using Gamelogic.Fx.Internal;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/* This class is the model for how to implement a sequence of properties (in this case colors).
	*/
	
	/// <summary>
	/// A post process that gradually shifts colors toward the provided primaries,
	/// producing posterized, clustered, or palette–constrained looks.
	/// See <see href="../common/docs/effects-reference-common.html#convex-hull-map"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.ConvexHullMap)]
	[Version(1, 1, 0)]
	public sealed class ConvexHullMapPostProcess : PostProcess<ConvexHullMapProperties>
	{
	}
}
