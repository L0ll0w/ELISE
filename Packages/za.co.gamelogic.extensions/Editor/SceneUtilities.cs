using System;
using System.Linq;
using Gamelogic.Extensions.Algorithms;
using UnityEditor;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>Provides utility methods for working with scenes in the Unity Editor.</summary>
	[Version(4, 5, 0)]
	public static class SceneUtilities
	{
		/// <summary>
		/// Gets the path of a scene from its name, using the build settings. The scene need not be loaded. 
		/// </summary>
		/// <param name="name">The name of the scene.</param>
		/// <returns>The path of the scene.</returns>
		/// <exception cref="Exception">No scene with given name found or duplicate scenes found.</exception>
		public static string GetScenePathByName(string name)
		{
			string GetName(EditorBuildSettingsScene s) => System.IO.Path.GetFileNameWithoutExtension(s.path);
			return EditorBuildSettings
				.scenes.Where(s => GetName(s) == name)
				.AsCountable()
				.Single().path;
		}
	}
}