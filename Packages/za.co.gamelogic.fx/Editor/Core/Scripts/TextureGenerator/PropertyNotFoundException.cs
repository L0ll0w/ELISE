using System;
using UnityEditor;

namespace Gamelogic.Fx.EditorTextureGenerator
{
	/// <summary>
	/// Thrown when looking for a property (for example, in a serialized object) and the property is not found.
	/// </summary>
	// Reuse candidate
	public sealed class PropertyNotFoundException : Exception
	{
		/// <summary>
		/// Initializes a new instance of <see cref="PropertyNotFoundException"/>.
		/// </summary>
		/// <param name="propertyName">The name of the property that was not found.</param>
		/// <param name="property">The serialized object that was searched.</param>
		public PropertyNotFoundException(string propertyName, SerializedObject property)
			: base($"Property {propertyName} not found in {property}.")
		{}
	}
}
