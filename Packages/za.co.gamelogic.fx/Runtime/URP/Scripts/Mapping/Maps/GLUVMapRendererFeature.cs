#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping.Maps;
using UnityEngine;

namespace Gamelogic.Fx.URP.Mapping.Maps
{
	/// <summary>
	/// Feature for rendering a UV map of the scene.
	/// See <see href="../common/docs/map-renderers-reference-common.html#uv-map"/>.
	/// </summary>
	[HelpURL(Constants.MapsHelpURLRoot + HelpURLs.UVMap)]
	public sealed class GLUVMapRendererFeature : GLMapRendererFeature<UVMapProperties>
	{
	}
}
#endif
