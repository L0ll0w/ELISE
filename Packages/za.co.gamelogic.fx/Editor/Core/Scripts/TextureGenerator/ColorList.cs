// Author: Herman Tulleken
// www.gamelogic.co.za

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamelogic.Fx.Editor.TextureGenerator
{
	/// <summary>
	/// Wraps a list of colors in a <see cref="ScriptableObject"/>.
	/// </summary>
	/*	Design Note: This class is used to wrap a list of colors so we get the nice
		reorderable list in the editor window. 
	*/
	[Serializable]
	public sealed class ColorList : ScriptableObject
	{
		/// <summary>
		/// The wrapped list of colors.
		/// </summary>
		[ColorUsage(false, false)]
		public List<Color> colors = new List<Color>()
		{
			Color.black,
			Color.white
		};
		
		/// <summary>
		/// The field name of the colors list.
		/// </summary>
		/*	Design Note: This property is used to get the name of the colors field
			so we can get the property of the serialized object by name. 
		*/
		public string ColorsFieldName => nameof(colors);

		/// <summary>
		/// Calculates a new color interpreting the list of colors as an evenly spaced linear gradient.
		/// </summary>
		/// <param name="t">The interpolation parameter.</param>
		/// <returns>
		/// If the list has one color, that color is returned. If the list has two colors, the linear
		/// interpolation between the two colors is returned. If the list has more than two colors, the
		/// linear interpolation between the two colors that t falls between is returned. For example, if there are three
		/// colors and t = 0.25, the return color is the color midway between the first and second colors. 
		/// </returns>
		public Color Evaluate(float t)
		{
			if (colors.Count == 0)
			{
				return Color.white;
			}

			t = Mathf.Clamp01(t);

			float scaledIndex = t * (colors.Count - 1);
			int lowerIndex = Mathf.FloorToInt(scaledIndex);
			int upperIndex = Mathf.CeilToInt(scaledIndex);

			lowerIndex = Mathf.Clamp(lowerIndex, 0, colors.Count - 1);
			upperIndex = Mathf.Clamp(upperIndex, 0, colors.Count - 1);

			float fraction = scaledIndex - lowerIndex;

			return Color.Lerp(colors[lowerIndex], colors[upperIndex], fraction);
		}
	}
}
