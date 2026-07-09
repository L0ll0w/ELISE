using System;
using System.Collections.Generic;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Represents a key-value pair used for the implementation of <see cref="FixedKeyDictionary{TKey,TValue}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <remarks>
	/// We would almost be able to get away with standard <see cref="KeyValuePair{TKey,TValue}"/> but that is
	/// not serializable by Unity.
	/// </remarks>
	[Version(4, 5, 0)]
	[Serializable]
	public class KeyValue<TKey, TValue> //TODO: better name
	{
		/// <summary>The key of this pair.</summary>
		public TKey key;
		/// <summary>The value of this pair.</summary>
		public TValue value;

		/// <summary>Initializes a new instance with the given key and value.</summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public KeyValue(TKey key, TValue value)
		{
			this.key = key;
			this.value = value;
		}

		/// <summary>Deconstructs this pair into its key and value components.</summary>
		/// <param name="outKey">The key component.</param>
		/// <param name="outValue">The value component.</param>
		public void Deconstruct(out TKey outKey, out TValue outValue)
		{
			outKey = key;
			outValue = value;
		}
	}
}
