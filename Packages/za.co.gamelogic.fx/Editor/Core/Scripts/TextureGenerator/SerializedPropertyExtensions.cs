using UnityEditor;

namespace Gamelogic.Fx.EditorTextureGenerator
{
	/// <summary>
	/// Provides extension methods for <see cref="SerializedProperty"/>.
	/// </summary>
	// Reuse candidate
	public static class SerializedPropertyExtensions
	{
		/// <summary>
		/// Tries to find a property in a serialized object. If the property is not found, an exception is thrown.
		/// </summary>
		/// <param name="serializedObject">The serialized object to search in.</param>
		/// <param name="propertyName">The name of the property to find.</param>
		/// <returns>The found property.</returns>
		/// <exception cref="PropertyNotFoundException">The property was not found.</exception>
		public static SerializedProperty FindRequiredProperty(this SerializedObject serializedObject, string propertyName)
		{
			var property = serializedObject.FindProperty(propertyName);
			if (property == null)
			{
				throw new PropertyNotFoundException(propertyName, serializedObject);
			}

			return property;
		}
	}
}
