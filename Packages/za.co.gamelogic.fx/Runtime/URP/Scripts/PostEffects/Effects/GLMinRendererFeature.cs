#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that replaces each pixel with the minimum color value found in its neighborhood.
	/// See <see href="../common/docs/effects-reference-common.html#min-filter"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.MinFilter)]
	public sealed class GLMinRendererFeature : SeparableRendererFeature<MinProperties>
	{
	}
}
#endif
