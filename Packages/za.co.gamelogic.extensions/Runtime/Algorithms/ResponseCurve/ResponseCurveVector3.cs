// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// A response curve with outputs of Vector3.
	/// </summary>
	[Version(1, 2, 0)]
	public class ResponseCurveVector3:ResponseCurveBase<Vector3>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ResponseCurveVector3"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output vectors.</param>
		public ResponseCurveVector3(IEnumerable<float> inputSamples, IEnumerable<Vector3> outputSamples) : base(inputSamples, outputSamples)
		{
		}

		#endregion

		#region Protected Methods

		protected override Vector3 Lerp(Vector3 outputSampleMin, Vector3 outputSampleMax, float t)
		{
			return Vector3.Lerp(outputSampleMin, outputSampleMax, t);
		}

		#endregion
	}
}
