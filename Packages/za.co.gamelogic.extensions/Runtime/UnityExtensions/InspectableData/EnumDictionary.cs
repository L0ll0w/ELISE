using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Represents a dictionary with keys of an enumerated type.
	/// </summary>
	/// <typeparam name="TEnum">A type of enum. An error will be thrown if this type is not an enum type.</typeparam>
	/// <typeparam name="TValue">The value type. For the dictionary to be serializable, this type must also be
	/// serializable.</typeparam>
	/// <remarks>
	/// This class differs from an ordinary dictionary
	/// - it is serializable
	/// - does not provide constant time access to elements
	/// - it always has all values of the enum as keys
	///
	/// It is meant to be used with enums with a smallish number of elements. Because of the last property above, it
	/// can cause problems if the enum type used for keys changes. This can corrupt the dictionary. Keep the enum type
	/// stable, or inspect all serialized instances after changes have been made.   
	/// </remarks>
	[Version(4, 5, 0)]
	[Serializable]
	public class EnumDictionary<TEnum, TValue> : IReadOnlyDictionary<TEnum, TValue>
	{
		[Tooltip("The enum keys of this dictionary.")]
		[SerializeField] private TEnum[] keys;
		[Tooltip("The values of this dictionary, in the same order as the keys.")]
		[SerializeField] private TValue[] values;

		/// <summary>Gets or sets the value associated with the specified key.</summary>
		/// <param name="key">The enum key to look up.</param>
		/// <exception cref="KeyNotFoundException"><paramref name="key"/> is not present in the dictionary.</exception>
		public TValue this[TEnum key]
		{
			get
			{
				if (!TryGetValue(key, out var value))
				{
					throw new KeyNotFoundException($"Key {key} not found in dictionary");
				}

				return value;
			}
			set
			{
				int index = IndexOfKey(key);
				if (index == -1)
				{
					throw new KeyNotFoundException($"Key {key} not found in dictionary");
				}

				values[index] = value;
			}
		}

		/// <inheritdoc />
		public IEnumerable<TEnum> Keys => keys;

		/// <inheritdoc />
		public IEnumerable<TValue> Values => values;

		/*	This performs the check that TEnum is an enum type. */
		static EnumDictionary()
		{
			if (!typeof(TEnum).IsEnum)
			{
				throw new TypeArgumentException(nameof(TEnum), "Must be an enumerated type");
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumDictionary{TEnum,TValue}"/> class.
		/// </summary>
		public EnumDictionary()
		{
			keys = (TEnum[])Enum.GetValues(typeof(TEnum));
			values = new TValue[keys.Length];
		}

		/// <inheritdoc />
		public bool ContainsKey(TEnum key) => IndexOfKey(key) != -1;

		/// <inheritdoc />
		public bool TryGetValue(TEnum key, out TValue value)
		{
			int index = IndexOfKey(key);

			if (index == -1)
			{
				value = default;
				return false;
			}

			value = values[index];
			return true;
		}

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<TEnum, TValue>> GetEnumerator()
			=> keys.Select((t, i) => new KeyValuePair<TEnum, TValue>(t, values[i])).GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc />
		public int Count => keys.Length;

		private int IndexOfKey(TEnum key) => Array.IndexOf(keys, key);
	}
}
