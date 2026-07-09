using System;
using UnityEditor;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Thrown when looking for a property (for example, in a serialized object) and the property is not found.
	/// </summary>
	[Version(4, 5, 0)]
	public sealed class PropertyNotFoundException : Exception
	{
		/// <summary>Initializes a new instance with the name of the missing property and the serialized object that was searched.</summary>
		/// <param name="propertyName">The name of the property that was not found.</param>
		/// <param name="property">The serialized object that was searched.</param>
		public PropertyNotFoundException(string propertyName, SerializedObject property)
			: base($"Property {propertyName} not found in {property}.")
		{}
	}
}