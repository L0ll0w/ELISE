// Copyright Gamelogic (c) http://www.gamelogic.co.za

using Gamelogic.Extensions.Internal;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Wraps a SerializedProperty, and provides additional functions, such as
	/// tooltips and a more powerful Find method.
	/// </summary>
	[Version(1, 2, 0)]
	public class GLSerializedProperty
	{
		/// <summary>Gets or sets the underlying <see cref="UnityEditor.SerializedProperty"/>.</summary>
		public SerializedProperty SerializedProperty { get; set; }

		/// <summary>Gets or sets a custom tooltip string to display for this property.</summary>
		public string CustomTooltip { get; set; }

		/// <summary>Gets the type of the underlying serialized property.</summary>
		public SerializedPropertyType PropertyType => SerializedProperty.propertyType;

		/// <summary>Gets or sets the object reference value of this property.</summary>
		public Object ObjectReferenceValue
		{
			get => SerializedProperty.objectReferenceValue;
			set => SerializedProperty.objectReferenceValue = value;
		}

		/// <summary>Gets or sets the enum index value of this property.</summary>
		public int EnumValueIndex
		{
			get => SerializedProperty.enumValueIndex;
			set => SerializedProperty.enumValueIndex = value;
		}

		/// <summary>Gets the display names of the enum values for this property.</summary>
		public string[] EnumNames => SerializedProperty.enumNames;

		/// <summary>Gets or sets the boolean value of this property.</summary>
		public bool BoolValue
		{
			get => SerializedProperty.boolValue;
			set => SerializedProperty.boolValue = value;
		}

		/// <summary>Gets or sets the integer value of this property.</summary>
		public int IntValue
		{
			get => SerializedProperty.intValue;
			set => SerializedProperty.intValue = value;
		}

		/// <summary>Gets or sets the float value of this property.</summary>
		public float FloatValue
		{
			get => SerializedProperty.floatValue;
			set => SerializedProperty.floatValue = value;
		}

		/// <summary>Gets or sets the string value of this property.</summary>
		public string StringValue
		{
			get => SerializedProperty.stringValue;
			set => SerializedProperty.stringValue = value;
		}

		/// <summary>Finds a child property relative to this property by name.</summary>
		/// <param name="name">The name of the child property to find.</param>
		/// <returns>A <see cref="GLSerializedProperty"/> wrapping the found child property.</returns>
		public GLSerializedProperty FindPropertyRelative(string name)
		{
			var property = SerializedProperty.FindPropertyRelative(name);

			return new GLSerializedProperty
			{
				SerializedProperty = property
			};
		}
	}
}
