// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// A response curve with outputs of Vector4.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveVector4 : ResponseCurveBase<Vector4>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveVector4"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output vectors.</param>
		public ResponseCurveVector4(IEnumerable<float> inputSamples, IEnumerable<Vector4> outputSamples)
			: base(inputSamples, outputSamples)
		{
		}

		#endregion

		#region Protected Methods

		protected override Vector4 Lerp(Vector4 outputSampleMin, Vector4 outputSampleMax, float t)
		{
			return Vector4.Lerp(outputSampleMin, outputSampleMax, t);
		}

		#endregion
	}
}
