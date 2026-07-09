#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping.Maps;
using UnityEngine;

namespace Gamelogic.Fx.URP.Mapping.Maps
{
	/// <summary>
	/// Feature for rendering a depth map of the scene.
	/// See <see href="../common/docs/map-renderers-reference-common.html#depth-map"/>.
	/// </summary>
	[HelpURL(Constants.MapsHelpURLRoot + HelpURLs.DepthMap)]
	public sealed class GLDepthMapRendererFeature: GLMapRendererFeature<DepthMapProperties>
	{
	}
}
#endif
