using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Used to mark a string in the inspector as a editable-readonly field.
	/// </summary>
	/// <remarks>
	/// This is useful to add documentation to scenes, typically on a top-level GameObject. The <see cref="ReadMe"/>
	/// component is a convenient component that has such a field and can be added to any GameObject to add editable
	/// information to a GameObject. 
	/// </remarks>
	public class InspectorTextAttribute : PropertyAttribute
	{
		// This attribute doesn't need any additional properties
	}
}