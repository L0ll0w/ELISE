using System;
using UnityEditor;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Provides extension methods for <see cref="SerializedProperty"/>.
	/// </summary>
	[Version(4, 5, 0)]
	public static class SerializedPropertyExtensions
	{
		/// <summary>
		/// Tries to find a property in a serialized object; throws if the property is not found.
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

		/// <summary>
		/// Displays an enum popup and stores the selected value as a string
		/// in the given <see cref="SerializedProperty"/>.
		/// </summary>
		/// <typeparam name="TEnum">The enum type.</typeparam>
		/// <param name="enumProp">The property that stores the value as a string.</param>
		private static void EnumAsString<TEnum>(SerializedProperty enumProp) where TEnum : struct, Enum
		{
			if(Enum.TryParse<TEnum>(enumProp.stringValue, out var currentValue))
			{
				currentValue = (TEnum)EditorGUILayout.EnumPopup(currentValue);
			}
			else
			{
				currentValue = default;
			}

			enumProp.stringValue = currentValue.ToString();
		}
	}
}