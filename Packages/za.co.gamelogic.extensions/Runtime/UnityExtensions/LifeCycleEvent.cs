using System;
using JetBrains.Annotations;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Flags for Unity lifecycle events.
	/// </summary>
	/// <remarks>
	/// Use this to specify what events logic should execute in. This is useful when you want this to be configurable in
	/// the inspector. <see cref="LifeCycleEventExtensions.IfMatchesExecute"/> for an example.  
	/// </remarks>
	/// <seealso cref="LifeCycleEventExtensions"/>
	[Flags]
	public enum LifeCycleEvent
	{
		/// <summary>No lifecycle event.</summary>
		None = 0,
		/// <summary>The <c>Awake</c> Unity lifecycle event.</summary>
		Awake = 1,
		/// <summary>The <c>OnEnable</c> Unity lifecycle event.</summary>
		OnEnable = 1 << 1,
		/// <summary>The <c>Start</c> Unity lifecycle event.</summary>
		Start = 1 << 2,
		/// <summary>The <c>Update</c> Unity lifecycle event.</summary>
		Update = 1 << 3,
		/// <summary>The <c>LateUpdate</c> Unity lifecycle event.</summary>
		LateUpdate = 1 << 4,
		/// <summary>The <c>FixedUpdate</c> Unity lifecycle event.</summary>
		FixedUpdate = 1 << 5,
		/// <summary>The <c>OnDisable</c> Unity lifecycle event.</summary>
		OnDisable = 1 << 6,
		/// <summary>The <c>OnDestroy</c> Unity lifecycle event.</summary>
		OnDestroy = 1 << 7,
		/// <summary>A user-defined event that does not correspond to a built-in Unity lifecycle method.</summary>
		UserDefined = 1 << 8,
	}

	/// <summary>
	/// Provides extension methods for <see cref="LifeCycleEvent"/>.
	/// </summary>
	public static class LifeCycleEventExtensions
	{
		/// <summary>
		/// Executes an action if the current event matches a given event.
		/// </summary>
		/// <param name="eventToMatch">The event to match.</param>
		/// <param name="currentEvent">The current event.</param>
		/// <param name="action">The action to execute.</param>
		/// <example>
		/// In this example, the designer can configure in the inspector when to restart the game.
		/// 
		/// [!code-csharp[](../../Assets/DocumentationCode/LifeCycleExample.cs#Documentation_LifeCycleExample)]
		///
		/// </example>
		/* Design note: It would read better if this was an extension method on the action. However, since the actions
			are likely to be method groups, this would have been awkward. This syntax also works better with lambda 
			expressions.
		*/
		public static void IfMatchesExecute(
			this LifeCycleEvent eventToMatch, 
			LifeCycleEvent currentEvent, 
			[NotNull] Action action)
		{
			if (!eventToMatch.Matches(currentEvent))
			{
				return;
			}
			
			action.ThrowIfNull(nameof(action));
			action();
		}

		/// <summary>Returns whether <paramref name="eventToMatch"/> includes any flags from <paramref name="currentEvent"/>.</summary>
		/// <param name="eventToMatch">The event or combination of events to test against.</param>
		/// <param name="currentEvent">The event that is currently occurring.</param>
		/// <returns><see langword="true"/> if any flag in <paramref name="currentEvent"/> is set in <paramref name="eventToMatch"/>; otherwise, <see langword="false"/>.</returns>
		public static bool Matches(this LifeCycleEvent eventToMatch, LifeCycleEvent currentEvent)
			=> (currentEvent & eventToMatch) != 0;
	}
}
