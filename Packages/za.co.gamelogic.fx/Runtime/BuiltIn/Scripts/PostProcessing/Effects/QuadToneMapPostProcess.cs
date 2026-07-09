using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing.Effects;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing.Effects
{
	/// <summary>
	/// A smooth 4-way threshold shader using inverse lerp (linear blend).
	/// Interpolates between Low–Mid0, Mid0–Mid1, and Mid1–High based on lightness.
	/// Values below LowValue use LowColor, above HighValue use HighColor.
	/// </summary>
	[HelpURL(Constants.HelpURLRoot + HelpURLs.QuadToneMap)]
	public sealed class QuadToneMapPostProcess : PostProcess<QuadToneMapProperties>
	{
	}
}
