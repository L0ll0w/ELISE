#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping.Maps;
using UnityEngine;

namespace Gamelogic.Fx.URP.Mapping.Maps
{
	/// <summary>
	/// Feature for rendering a normal map of the scene.
	/// See <see href="../common/docs/map-renderers-reference-common.html#normal-map"/>.
	/// </summary>
	[HelpURL(Constants.MapsHelpURLRoot + HelpURLs.NormalMap)]
	public sealed class GLNormalMapRendererFeature : GLMapRendererFeature<NormalMapProperties>
	{
	}
}
#endif