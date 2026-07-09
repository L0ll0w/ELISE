#if GAMELOGIC_HAS_URP 
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.URP.PostProcessing.Effects
{
	/// <summary>
	/// A post process that produces an outline.
	/// See <see href="../common/docs/effects-reference-common.html#simple-outline"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.SimpleOutline)]
	public sealed class GLSimpleOutlineRendererFeature : PostProcessRendererFeature<SimpleOutlineProperties>
	{
	}
}
#endif
