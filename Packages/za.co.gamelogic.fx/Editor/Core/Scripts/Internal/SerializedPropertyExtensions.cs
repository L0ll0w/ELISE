using System;
using System.Reflection;
using UnityEditor;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Extension methods for <see cref="SerializedProperty"/> to retrieve the underlying managed object.
	/// </summary>
	public static class SerializedPropertyExtensions
	{
		/// <summary>
		/// Returns the managed object referenced by a <see cref="SerializedProperty"/>, traversing
		/// arrays and nested fields as needed.
		/// </summary>
		/// <param name="property">The serialized property to resolve.</param>
		/// <returns>The underlying managed object, or <see langword="null"/> if it cannot be resolved.</returns>
		public static object GetTargetObjectOfProperty(this SerializedProperty property)
		{
			if (property == null)
			{
				return null;
			}

			object obj = property.serializedObject.targetObject;
			string[] elements = property.propertyPath.Replace(".Array.data[", "[").Split('.');

			foreach (string element in elements)
			{
				if (element.Contains("["))
				{
					string elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
					int index = Convert.ToInt32(element[(element.IndexOf("[", StringComparison.Ordinal) + 1)..^1]);
					obj = GetIndexedValue(obj, elementName, index);
				}
				else
				{
					obj = GetMemberValue(obj, element);
				}
			}

			return obj;
		}

		private static object GetMemberValue(object source, string name)
		{
			if (source == null)
			{
				return null;
			}

			Type type = source.GetType();

			while (type != null)
			{
				FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					return field.GetValue(source);
				}

				PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					return property.GetValue(source);
				}

				type = type.BaseType;
			}

			return null;
		}

		private static object GetIndexedValue(object source, string name, int index)
		{
			var enumerable = GetMemberValue(source, name) as System.Collections.IEnumerable;
			if (enumerable == null)
			{
				return null;
			}

			var enumerator = enumerable.GetEnumerator();
			for (int i = 0; i <= index; i++)
			{
				if (!enumerator.MoveNext())
				{
					return null;
				}
			}

			return enumerator.Current;
		}
	}

}
