// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Collections.Generic;

namespace Gamelogic.Extensions.Algorithms
{

	/// <summary>
	/// Provides a factory method for creating step response curves.
	/// </summary>
	public class StepResponse
	{
		/// <summary>
		/// Indicates on which side of the sample boundary the step value is taken.
		/// </summary>
		public enum StepType
		{
			/// <summary>The value from the left (lower) sample is used in the transition region.</summary>
			Left,
			/// <summary>The value from the left sample is used when t &lt; 0.5, otherwise the right sample is used.</summary>
			Mid,
			/// <summary>The value from the right (upper) sample is used in the transition region.</summary>
			Right
		}

		/// <summary>
		/// Gets the step response that returns y0 for all inputs less than x, and y1 for 
		/// all inputs greater than or equal to x.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y0">The y0.</param>
		/// <param name="y1">The y1.</param>
		/// <returns>StepResponse.</returns>
		public static StepResponse<T> GetStep<T>(float x, T y0, T y1)
		{
			var input = new List<float> { x - 1, x};
			var output = new List<T> { y0, y1 };

			return new StepResponse<T>(input, output, StepType.Right);
		}
	}

	/// <summary>
	/// A response curve that maps inputs to discrete output values using a step (non-interpolating) approach.
	/// </summary>
	/// <typeparam name="T">The type of the output values.</typeparam>
	public class StepResponse<T> : ResponseCurveBase<T>
	{
		/// <summary>
		/// Gets the step response that returns y0 for all inputs less than x, and y1 for 
		/// all inputs greater than or equal to x.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y0">The y0.</param>
		/// <param name="y1">The y1.</param>
		/// <returns>StepResponse.</returns>
		public static StepResponse<T> GetStep(float x, T y0, T y1)
		{
			var input = new List<float> { x - 1, x };
			var output = new List<T> { y0, y1 };

			return new StepResponse<T>(input, output, StepResponse.StepType.Right);
		}

		private readonly StepResponse.StepType stepType;

		/// <summary>
		/// Initializes a new instance of the <see cref="StepResponse{T}"/> class.
		/// </summary>
		/// <param name="inputSamples">Strictly increasing input sample points.</param>
		/// <param name="outputSamples">Corresponding output values for each step region.</param>
		/// <param name="stepType">Determines which output value is used in the transition region.</param>
		public StepResponse(IEnumerable<float> inputSamples, IEnumerable<T> outputSamples, StepResponse.StepType stepType = StepResponse.StepType.Left)
			: base(inputSamples, outputSamples)
		{
			this.stepType = stepType;
		}

		protected override T Lerp(T outputSampleMin, T outputSampleMax, float t)
		{
			switch (stepType)
			{
				default:
				case StepResponse.StepType.Left:
					return outputSampleMin;
				case StepResponse.StepType.Right:
					return outputSampleMax;
				case StepResponse.StepType.Mid:
					return (t < 0.5f) ? outputSampleMin : outputSampleMax;
			}
		}
	}
}
