// Copyright Gamelogic Pty Ltd (c) http://www.gamelogic.co.za

using System;
using Gamelogic.Extensions.Internal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Provides useful extension methods for GameObjects.
	/// </summary>
	[Version(3, 0, 0)]
	public static class GameObjectExtensions
	{
		/// <summary>
		/// Gets a component of the given type on the game object, or fail if no such component can be found.
		/// </summary>
		/// <param name="go">The game object to check.</param>
		/// <typeparam name="T">The type of component to get.</typeparam>
		/// <returns>A component of type T attached to the given game object if it exists.</returns>
		/// <exception cref="InvalidOperationException">no component of the required type exist on the given game object.
		/// </exception>
		/// <remarks>Use this method when you are sure that the component exists on the game object. 
		/// </remarks>
		public static T GetRequiredComponent<T>(this GameObject go) where T : Component
		{
			var retrievedComponent = go.GetComponent<T>();

			if (retrievedComponent == null)
			{
				throw new InvalidOperationException(
					message: $"GameObject \"{go.name}\" does not have a component of type {typeof(T)}");
			}

			return retrievedComponent;
		}
		
		/// <summary>
		/// Gets a component of the given type in one of the children, or fail if no such component can be found.
		/// </summary>
		/// <param name="go">The game object to check.</param>
		/// <typeparam name="T">The type of component to get.</typeparam>
		/// <returns>A component of type T attached to the given game object if it exists.</returns>
		/// <exception cref="InvalidOperationException">no component of the required type exist on any of the given game
		/// object's children.</exception>
		/// <remarks>Use this method when you are sure that the component exists on the game object. 
		/// </remarks>
		public static T GetRequiredComponentInChildren<T>(this GameObject go) where T : Component
		{
			var retrievedComponent = go.GetComponentInChildren<T>();

			if (retrievedComponent == null)
			{
				throw new InvalidOperationException(
					message: $"GameObject \"{go.name}\" does not have a component of type {typeof(T)}");
			}

			return retrievedComponent;
		}

		/// <summary>
		/// Finds the first object of the given component type in the current scene.
		/// </summary>
		/// <returns>
		/// The first instance of a component of type <typeparamref name="T"/> found in the scene,
		/// or <c>null</c> if no instance exists.
		/// </returns>
		/// <remarks>
		/// Uses <c>FindFirstObjectByType</c> on newer Unity versions and <c>FindObjectOfType</c>
		/// on older ones, ensuring a consistent API across engine releases.
		/// </remarks>
		/* Design note: This method abstracts the difference introduced in Unity 6000.
			Why internal? Users should use the public API of Unity itself, this is to abstract an implementation detail.
			
			Same below.
		*/
		[Version(4, 4, 3)]
		internal static T FindSingleObjectOfType<T>() where T : UnityObject
#if UNITY_6000_0_OR_NEWER
			=> UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
			=> UnityObject.FindObjectOfType<T>();
#endif
		
		/// <summary>
		/// Finds all objects of the given component type in the current scene.
		/// </summary>
		/// <remarks>
		/// Returns all instances of the component in the scene.
		/// Uses <c>FindObjectsByType</c> on newer Unity versions and <c>FindObjectsOfType</c> on older ones.
		/// </remarks>
		
		[Version(4, 4, 3)]
		internal static T[] FindAllObjectsOfType<T>() where T : Component
#if UNITY_6000_0_OR_NEWER
			=> UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
			=> UnityObject.FindObjectsOfType<T>();
#endif
	}
}
