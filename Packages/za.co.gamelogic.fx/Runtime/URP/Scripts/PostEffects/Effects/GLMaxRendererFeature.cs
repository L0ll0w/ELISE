#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that replaces each pixel with the maximum color value found in its neighborhood.
	/// See <see href="../common/docs/effects-reference-common.html#max-filter"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.MaxFilter)]
	public sealed class GLMaxRendererFeature : SeparableRendererFeature<MaxProperties>
	{
	}
}
#endif
