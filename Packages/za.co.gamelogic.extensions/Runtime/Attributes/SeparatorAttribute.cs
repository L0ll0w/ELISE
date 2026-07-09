// Copyright Gamelogic Pty Ltd (c) http://www.gamelogic.co.za

using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Draws a horizontal separator line above the field in the inspector.
	/// </summary>
	[Version(4, 3, 0)]
	public class SeparatorAttribute : PropertyAttribute
	{
		/// <summary>Gets the height of the separator line in pixels.</summary>
		public int Height { get; }

		/// <summary>Gets the color of the separator line.</summary>
		public Color Color { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SeparatorAttribute"/> class using the default height and color.
		/// </summary>
		public SeparatorAttribute()
		{
			Height = PropertyDrawerData.SeparatorHeight;
			Color = PropertyDrawerData.SeparatorColor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SeparatorAttribute"/> class with a specified color and default height.
		/// </summary>
		/// <param name="color">The hex color string for the separator. Falls back to the default color if invalid.</param>
		public SeparatorAttribute(string color)
		{
			Height = PropertyDrawerData.SeparatorHeight;
			Color =
				color == null || !ColorExtensions.TryParseHex(color, out var rgbColor)
					? PropertyDrawerData.SeparatorColor
					: rgbColor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SeparatorAttribute"/> class with a specified height and default color.
		/// </summary>
		/// <param name="height">The height of the separator line in pixels.</param>
		public SeparatorAttribute(int height)
		{
			Height = height;
			Color = PropertyDrawerData.SeparatorColor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SeparatorAttribute"/> class with a specified color and height.
		/// </summary>
		/// <param name="color">The hex color string for the separator. Falls back to black if invalid.</param>
		/// <param name="height">The height of the separator line in pixels.</param>
		public SeparatorAttribute(string color, int height)
		{
			Height = height;
			Color = 
				color == null || !ColorExtensions.TryParseHex(color, out var c) 
					? Color.black 
					: c;
		}
	}
}
