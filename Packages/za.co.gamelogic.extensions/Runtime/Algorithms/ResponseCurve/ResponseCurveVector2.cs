// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// A response curve with outputs of Vector2.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveVector2 : ResponseCurveBase<Vector2>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveVector2"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output vectors.</param>
		public ResponseCurveVector2(IEnumerable<float> inputSamples, IEnumerable<Vector2> outputSamples)
			: base(inputSamples, outputSamples)
		{
		}

		#endregion

		#region Protected Methods

		protected override Vector2 Lerp(Vector2 outputSampleMin, Vector2 outputSampleMax, float t)
		{
			return Vector2.Lerp(outputSampleMin, outputSampleMax, t);
		}

		#endregion
	}
}
