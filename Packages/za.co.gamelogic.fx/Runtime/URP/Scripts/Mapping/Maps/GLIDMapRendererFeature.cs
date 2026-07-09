#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping.Maps;
using UnityEngine;

namespace Gamelogic.Fx.URP.Mapping.Maps
{
	/// <summary>
	/// Feature for rendering an ID map of the scene.
	/// See <see href="../common/docs/map-renderers-reference-common.html#id-map"/>.
	/// </summary>
	[HelpURL(Constants.MapsHelpURLRoot + HelpURLs.IDMap)]
	public sealed class GLIDMapRendererFeature : GLMapRendererFeature<IDMapProperties>
	{
	}
}
#endif
