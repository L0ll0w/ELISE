#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that converts the image to grayscale using luminosity.
	/// See <see href="../common/docs/effects-reference-common.html#desaturate"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.Desaturate)]
	public sealed class GLDesaturateRendererFeature : PostProcessRendererFeature<DesaturateProperties>
	{
	}
}
#endif
