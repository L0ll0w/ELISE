using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Algorithms;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Represents a dictionary with a fixed set of keys. 
	/// </summary>
	/// <remarks>
	/// This is not meant to be used for large dictionaries, as it does not provide constant time access to elements.
	///
	/// A list is used to store elements so that they are more easily serialized (since Unity 2020.1).
	///
	/// However, it is not possible for a key to appear twice.
	///
	/// Furthermore, the usual way to use this is to map <i>all</i> IDs to GameObjects; this is done by calling
	/// <see cref="Validate"/> with a list of all IDs in the order they should appear. This will ensure that all IDs are
	/// in the dictionary, and that <see cref="Keys"/> (and their corresponding <see cref="Values"/>) are in the correct
	/// order.
	///
	/// It is possible for values to be null.
	///
	/// Null is not allowed as a key.
	///
	/// We would have liked to set the keys in the constructor, but since we are building this class to work with Unity's
	/// serialization, a method that sets the keys is used instead (<see cref="Validate"/>). This can be called from
	/// OnValidate in the component where it is being used. This means setting the keys happens at edit time, not runtime.  
	///
	/// During development, keys may change; calling <see cref="Clear"/> (which appears as an inspector button) will
	/// reset IDs.  
	/// </remarks>
	[Version(4, 5, 0)]
	[Serializable]
	public class FixedKeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
	{
		
		// ReSharper disable once FieldCanBeMadeReadOnly.Local - Unity serialization
		[Tooltip("The key-value pairs stored in this dictionary.")]
		[SerializeField] protected List<KeyValue<TKey, TValue>> pairs = new List<KeyValue<TKey, TValue>>();

		[Tooltip("The ordered list of keys used to validate and sort the dictionary.")]
		[SerializeField] protected List<TKey> keys;
		
		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key whose value is to be retrieved or assigned.</param>
		/// <returns>The value associated with the given key.</returns>
		/// <exception cref="ArgumentException">Thrown when the key is null or empty.</exception>
		/// <exception cref="KeyNotFoundException">Thrown when the key does not exist.</exception>
		public TValue this[TKey key]
		{
			get
			{
				if (string.IsNullOrEmpty(key.ToString())) 
				{
					throw new ArgumentException("Key cannot be null or empty.", nameof(key));
				}

				if (pairs.TryFirst(p => p.key.Equals(key), out var pair))
				{
					return pair.value;
				}

				throw new KeyNotFoundException($"Key {key} not found.");
			}
			set
			{
				if (string.IsNullOrEmpty(key.ToString())) 
				{
					throw new ArgumentException("Key cannot be null or empty.", nameof(key));
				}

				Remove(key);
				pairs.Add(new KeyValue<TKey, TValue>(key, value));
			}
		}

		/// <summary>
		/// Gets a collection containing the keys of the dictionary.
		/// </summary>
		public ICollection<TKey> Keys => pairs.ConvertAll(p => p.key);
		
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		/// <summary>
		/// Gets a collection containing the values of the dictionary.
		/// </summary>
		public ICollection<TValue> Values => pairs.ConvertAll(p => p.value);
		
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		
		/// <summary>
		/// Gets the number of key–value pairs contained in the dictionary.
		/// </summary>
		public int Count => pairs.Count;

		/// <summary>
		/// Gets a value indicating whether the dictionary is read-only.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Adds a key–value pair to the dictionary.
		/// </summary>
		/// <param name="key">The key to add.</param>
		/// <param name="value">The value to associate with the key.</param>
		/// <exception cref="ArgumentException">Thrown when the key already exists.</exception>
		public void Add(TKey key, TValue value)
		{
			key.ThrowIfNull(nameof(key));

			if (ContainsKey(key))
			{
				throw new ArgumentException("An element with the same key already exists.");
			}

			pairs.Add(new KeyValue<TKey, TValue>(key, value));
		}

		/// <summary>
		/// Determines whether the dictionary contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate.</param>
		/// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
		public bool ContainsKey(TKey key)
		{
			key.ThrowIfNull(nameof(key));
			return pairs.Exists(p => Equals(p.key, key));
		}

		/// <summary>
		/// Removes the entry with the specified key.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <returns><c>true</c> if the key was removed; otherwise <c>false</c>.</returns>
		public bool Remove(TKey key)
		{
			key.ThrowIfNull(nameof(key));

			if (pairs.TryFirst(p => p.key.Equals(key), out var pair))
			{
				pairs.Remove(pair);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Attempts to retrieve the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key whose value is to be retrieved.</param>
		/// <param name="value">When this method returns, contains the value if found; otherwise the default value.</param>
		/// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			key.ThrowIfNull(nameof(key));

			if (pairs.TryFirst(p => p.key.Equals(key), out var pair))
			{
				value = pair.value;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Adds the specified key–value pair to the dictionary.
		/// </summary>
		/// <param name="item">A key–value pair to add.</param>
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			item.Key.ThrowIfNull($"{nameof(item)}.{nameof(item.Key)}");

			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Removes all key–value pairs from the dictionary.
		/// </summary>
		public void Clear() => pairs.Clear();

		/// <summary>
		/// Determines whether the dictionary contains the specified key–value pair.
		/// </summary>
		/// <param name="item">The key–value pair to locate.</param>
		/// <returns><c>true</c> if the pair exists; otherwise <c>false</c>.</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			item.Key.ThrowIfNull($"{nameof(item)}.{nameof(item.Key)}");

			return pairs.Exists(p => Equals(p.key, item.Key) && Equals(p.value, item.Value));
		}

		/// <summary>
		/// Copies the elements of the dictionary to an array, starting at the specified index.
		/// </summary>
		/// <param name="array">The array that receives the copied elements.</param>
		/// <param name="arrayIndex">The index at which copying begins.</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			for (int i = 0; i < pairs.Count; i++)
			{
				array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(pairs[i].key, pairs[i].value);
			}
		}

		/// <summary>
		/// Removes the specified key–value pair.
		/// </summary>
		/// <param name="item">The key–value pair to remove.</param>
		/// <returns><c>true</c> if the pair was removed; otherwise <c>false</c>.</returns>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			item.Key.ThrowIfNull($"{nameof(item)}.{nameof(item.Key)}");
			return Remove(item.Key);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the dictionary.
		/// </summary>
		/// <returns>An enumerator for the dictionary.</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
			=> pairs.Select(pair => new KeyValuePair<TKey, TValue>(pair.key, pair.value)).GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Validates the dictionary by ensuring that all provided keys exist and appear in the specified order.
		/// Missing keys are added with default values, and existing keys are re-ordered.
		/// </summary>
		/// <param name="idsInOrder">The ordered list of keys that define the desired dictionary structure.</param>
		/// <returns>A list containing keys that appear more than once in the input sequence.</returns>

		// TODO: This method does not remove keys that are not present in idsInOrder. Should it?
		public List<TKey> Validate(IEnumerable<TKey> idsInOrder)
		{
			var inOrder = idsInOrder as TKey[] ?? idsInOrder.ToArray();

			foreach (var id in inOrder)
			{
				if (!ContainsKey(id))
				{
					Add(id, default);
				}
			}

			var orderMap = inOrder
				.Select((item, index) => new { item, index })
				.ToDictionary(x => x.item, x => x.index);

			pairs.Sort((a, b) => orderMap[a.key].CompareTo(orderMap[b.key]));

			return inOrder.Where(id => pairs.Count(pair => Equals(pair.key, id)) > 1).ToList();
		}
	}
}
