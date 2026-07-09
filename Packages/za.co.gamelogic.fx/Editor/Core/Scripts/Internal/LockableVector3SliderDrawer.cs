using Gamelogic.Fx.Internal;
using UnityEditor;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Property drawer for <see cref="LockableVector3"/> fields, rendering each component as a slider.
	/// </summary>
	[CustomPropertyDrawer(typeof(LockableVector3))]
	internal sealed class LockableVector3SliderDrawer : LockableVectorSliderDrawerBase
	{
		protected override int ComponentCount => 3;
	}
}
