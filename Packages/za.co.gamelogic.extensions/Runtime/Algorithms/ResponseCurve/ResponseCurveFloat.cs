// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// A response curve with outputs of float.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveFloat : ResponseCurveBase<float>
	{
		#region Static Methods

		/// <summary>
		/// Creates a <see cref="ResponseCurveFloat"/> that linearly interpolates between two output values over a given input range.
		/// </summary>
		/// <param name="x0">The minimum input value.</param>
		/// <param name="x1">The maximum input value.</param>
		/// <param name="y0">The output value at <paramref name="x0"/>.</param>
		/// <param name="y1">The output value at <paramref name="x1"/>.</param>
		/// <returns>A new <see cref="ResponseCurveFloat"/>.</returns>
		public static ResponseCurveFloat GetLerp(float x0, float x1, float y0, float y1)
		{
			var input = new List<float>{x0, x1};
			var output = new List<float>{y0, y1};
			
			var responseCurve = new ResponseCurveFloat(input, output);

			return responseCurve;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveFloat"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output values.</param>
		public ResponseCurveFloat(IEnumerable<float> inputSamples, IEnumerable<float> outputSamples)
			: base(inputSamples, outputSamples)
		{}

		#endregion

		#region Protected Methods

		protected override float Lerp(float outputSampleMin, float outputSampleMax, float t)
		{
			return outputSampleMin + (outputSampleMax - outputSampleMin) * Mathf.Clamp01(t);
		}

		#endregion
	}

	/// <summary>
	/// A response curve with outputs of <see langword="int"/>.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveInt : ResponseCurveBase<int>
	{
		#region Static Methods

		/// <summary>
		/// Creates a <see cref="ResponseCurveInt"/> that linearly interpolates (and rounds) between two output values over a given input range.
		/// </summary>
		/// <param name="x0">The minimum input value.</param>
		/// <param name="x1">The maximum input value.</param>
		/// <param name="y0">The output value at <paramref name="x0"/>.</param>
		/// <param name="y1">The output value at <paramref name="x1"/>.</param>
		/// <returns>A new <see cref="ResponseCurveInt"/>.</returns>
		public static ResponseCurveInt GetLerp(float x0, float x1, int y0, int y1)
		{
			var input = new List<float> { x0, x1 };
			var output = new List<int> { y0, y1 };

			var responseCurve = new ResponseCurveInt(input, output);

			return responseCurve;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveInt"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output values.</param>
		public ResponseCurveInt(IEnumerable<float> inputSamples, IEnumerable<int> outputSamples)
			: base(inputSamples, outputSamples)
		{ }

		#endregion

		#region Protected Methods

		protected override int Lerp(int outputSampleMin, int outputSampleMax, float t)
		{
			return Mathf.RoundToInt(outputSampleMin + (outputSampleMax - outputSampleMin) * Mathf.Clamp01(t));
		}

		#endregion
	}

}
