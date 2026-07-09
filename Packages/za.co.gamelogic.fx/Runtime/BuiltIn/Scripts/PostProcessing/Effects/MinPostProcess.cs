using Gamelogic.Fx.Internal;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A post process that replaces each pixel with the minimum color value found in its neighborhood.
	/// See <see href="../common/docs/effects-reference-common.html#min-filter"/>.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.MinFilter)]
	[Version(1, 1, 0)]
	public sealed class MinPostProcess : SeparablePostProcess<MinProperties>
	{
	}
}
