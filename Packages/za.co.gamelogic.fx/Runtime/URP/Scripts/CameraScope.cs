using Gamelogic.Extensions;

namespace Gamelogic.Fx.URP
{
	/// <summary>
	/// Specifies which cameras in a stack a render pass should execute on in a stack.
	/// </summary>
	public enum CameraScope
	{
		/// <summary>Executes only on the base camera.</summary>
		Base,

		/// <summary>
		/// Executes only on the camera that resolves the final output.
		/// This is the last overlay camera, or the base camera if there are no overlays.
		/// </summary>
		Final,
		
		/// <summary>Executes on every camera in the stack.</summary>
		All,
		
		/// <summary>
		/// Executes on cameras matching a user-supplied predicate.
		/// Assign <c>CustomCameraScopePredicate</c> on the renderer feature to provide the predicate.
		/// If no predicate is assigned, the feature will not execute on any camera.
		/// </summary>
		Custom,
	}
}
