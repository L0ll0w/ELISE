using System;
using UnityEngine;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Observes a value and keeps an automatically updated transformed version of it.
	/// </summary>
	/// <typeparam name="TValue">The type of the input value to observe.</typeparam>
	/// <typeparam name="TTransformedValue">The type of the transformed output value.</typeparam>
	[Version(4, 5, 0)]
	public class ObservedTransformedValue<TValue, TTransformedValue>
	{
		/// <summary>
		/// Raised when the transformed value changes.
		/// </summary>
		public event Action TransformedValueChanged;

		private readonly ObservedValue<TTransformedValue> transformedValue;
		private TValue currentValue;
		private readonly Func<TValue, TTransformedValue> transform;

		/// <summary>
		/// The current input value. Setting it automatically updates the transformed value.
		/// </summary>
		public TValue Value
		{
			get => currentValue;
			set
			{
				currentValue = value;
				transformedValue.Value = transform(value);
			}
		}
		
		/// <summary>
		/// The current transformed value. This is automatically updated whenever <see cref="Value"/> changes.
		/// </summary>
		[Version(4, 6, 0)]
		public TTransformedValue TransformedValue => transformedValue.Value;

		/// <summary>
		/// Creates a new observed transformed value. Whenever the input value changes,
		/// the transformed value is recalculated and the change event is triggered.
		/// </summary>
		/// <param name="initialValue">The initial input value.</param>
		/// <param name="transform">Function used to compute the transformed value.</param>
		public ObservedTransformedValue(TValue initialValue, Func<TValue, TTransformedValue> transform)
		{
			this.transform = transform;
			currentValue = initialValue;
			transformedValue = new ObservedValue<TTransformedValue>(transform(initialValue));
			transformedValue.OnValueChange += TransformedValueChanged;
		}
	}
}
