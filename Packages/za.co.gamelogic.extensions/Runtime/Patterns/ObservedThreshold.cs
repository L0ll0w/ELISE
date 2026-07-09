using System;
using UnityEngine;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Tracks a floating value and raises an event when it crosses a given threshold.
	/// </summary>
	[Version(4, 5, 0)]
	public class ObservedThreshold
	{
		private readonly ObservedValue<float> observedValue;
		private readonly ObservedValue<bool> isBelowThreshold;

		/// <summary>
		/// Raised when the value goes from below to above the threshold,
		/// or from above to below. The event passes the new state
		/// (true = below threshold).
		/// </summary>
		public event Action<bool> ThresholdCrossed;

		/// <summary>
		/// The current value being observed.
		/// </summary>
		public float Value
		{
			get => observedValue.Value;
			set => observedValue.Value = value;
		}

		/// <summary>
		/// Creates a new observed threshold that watches a value and
		/// notifies when it crosses the given threshold. 
		/// An optional transform function can be applied before comparison.
		/// </summary>
		/// <param name="value">Initial value.</param>
		/// <param name="threshold">The threshold to compare against.</param>
		/// <param name="transform">Optional value transform before comparison.</param>
		public ObservedThreshold(float value, float threshold, Func<float, float> transform = null)
		{
			observedValue = new ObservedValue<float>(value);
			isBelowThreshold = new ObservedValue<bool>(value < threshold);

			observedValue.OnValueChange += OnValueChange;
			isBelowThreshold.OnValueChange += IsBelowThresholdChange;

			return;

			void OnValueChange() => isBelowThreshold.Value = Transform(observedValue.Value) < threshold;
			void IsBelowThresholdChange() => ThresholdCrossed?.Invoke(isBelowThreshold.Value);
			float Transform(float input) => transform?.Invoke(input) ?? input;
		}
	}
}
