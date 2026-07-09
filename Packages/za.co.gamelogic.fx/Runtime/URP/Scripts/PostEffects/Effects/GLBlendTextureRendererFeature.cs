#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that blends a texture over the scene at a configurable opacity.
	/// See <see href="../common/docs/effects-reference-common.html#blend-texture"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.BlendTexture)]
	public sealed class GLBlendTextureRendererFeature : PostProcessRendererFeature<BlendTextureProperties>
	{
	}
}
#endif
