using UnityEngine;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Marks a string field as a scene name in the inspector, rendering it as a popup of scenes in the build settings.
	/// </summary>
	[Version(4, 5, 0)]
	public class SceneNameAttribute : PropertyAttribute { }
}
