// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// A response curve with outputs of Color.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveColor : ResponseCurveBase<Color>
	{
		#region Static Methods

		/// <summary>
		/// Creates a <see cref="ResponseCurveColor"/> that linearly interpolates between two colors over a given input range.
		/// </summary>
		/// <param name="x0">The minimum input value.</param>
		/// <param name="x1">The maximum input value.</param>
		/// <param name="y0">The output color at <paramref name="x0"/>.</param>
		/// <param name="y1">The output color at <paramref name="x1"/>.</param>
		/// <returns>A new <see cref="ResponseCurveColor"/>.</returns>
		public static ResponseCurveColor GetLerp(float x0, float x1, Color y0, Color y1)
		{
			var input = new List<float>();
			var output = new List<Color>();

			input.Add(x0);
			input.Add(x1);

			output.Add(y0);
			output.Add(y1);

			var responseCurve = new ResponseCurveColor(input, output);

			return responseCurve;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveColor"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output colors.</param>
		public ResponseCurveColor(IEnumerable<float> inputSamples, IEnumerable<Color> outputSamples)
			: base(inputSamples, outputSamples)
		{}

		#endregion

		#region Protected Methods

		protected override Color Lerp(Color outputSampleMin, Color outputSampleMax, float t)
		{
			Color output = Color.Lerp(outputSampleMin, outputSampleMax, t);
			return output;
		}

		#endregion
	}
}
