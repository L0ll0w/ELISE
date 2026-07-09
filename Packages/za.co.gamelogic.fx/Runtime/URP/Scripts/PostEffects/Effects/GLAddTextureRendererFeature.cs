#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that overlays a texture on top of the scene, respecting transparency.
	/// See <see href="../common/docs/effects-reference-common.html#add-texture"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.AddTexture)]
	public sealed class GLAddTextureRendererFeature : PostProcessRendererFeature<AddTextureProperties>
	{
	}
}
#endif
