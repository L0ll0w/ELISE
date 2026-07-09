using Gamelogic.Fx.Internal;
using UnityEditor;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Property drawer for <see cref="LockableVector2"/> fields, rendering each component as a slider.
	/// </summary>
	[CustomPropertyDrawer(typeof(LockableVector2))]
	internal sealed class LockableVector2SliderDrawer : LockableVectorSliderDrawerBase
	{
		protected override int ComponentCount => 2;
	}
}
