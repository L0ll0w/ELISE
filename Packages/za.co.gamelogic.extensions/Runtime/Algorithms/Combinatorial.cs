// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// Provides combinatorial algorithms for generating tuples, combinations, permutations, and partitions.
	/// </summary>
	/// <remarks>
	/// Except for <see cref="PowerSet{T}"/>, all methods are implemented after Knuth, described in
	/// The Art of Computer Programming, Volume 4.
	/// </remarks>
	[Version(1, 4, 0)]
	public static class Combinatorial
	{
		#region Static Methods

		#region Partitions		
		/// <summary>
		/// Returns all the set partitions of the given list.
		/// </summary>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		/// <param name="list">The list to partition.</param>
		/// <returns>An enumerable of partitions, where each partition is a collection of non-empty, non-overlapping subsets that together contain all elements.</returns>
		/// <remarks>If the list is (a b c), then ((a) (b c)) and ((a) (b) (c)) are examples of partitions.</remarks>
		public static IEnumerable<IEnumerable<IEnumerable<T>>> Partitions<T>(this IEnumerable<T> list)
		{
			var listArray = list.ToArray();
			int elementCount = listArray.Length;
			var indexes = RestrictedGrowthStrings(elementCount);

			return indexes.Select(index => AccessByRestrictedGrowthString(listArray, index));
		}
		#endregion

		#region PowerSet		
		/// <summary>
		/// Returns the power set of the input, that is, all possible subsets of the input.
		/// </summary>
		/// <typeparam name="T">The type of elements in the input.</typeparam>
		/// <param name="input">The input sequence.</param>
		/// <returns>An enumerable of all 2^n subsets of the input.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
		public static IEnumerable<IEnumerable<T>> PowerSet<T>(this IEnumerable<T> input)
		{
			input.ThrowIfNull("input");

			return PowerSetImpl(input.ToList());
		}

		#endregion

		#endregion

		#region Tuples

		/// <summary>
		/// Generates all possible tuples of length n with digits 0 to n-1.
		/// </summary>
		/// <param name="n">The length of tuples to generate. All digits are also between 0 and n-1.</param>
		/// <returns>An enumerable of all n-tuples with digits in [0, n).</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative.</exception>
		public static IEnumerable<int[]> Tuples(int n)
		{
			if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "must be non negative");

			var radixes = RangeArray(n);

			return MultiRadixTuplesImpl(radixes, e => e);
		}

		/// <summary>
		/// Generates all tuples with mixed radixes.
		/// </summary>
		/// <param name="radixes">The array of radixes (maximum value + 1) for each position in the tuple.</param>
		/// <returns>An enumerable of all tuples where each position i ranges from 0 to radixes[i]-1.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="radixes"/> is <see langword="null"/>.</exception>
		public static IEnumerable<int[]> MultiRadixTuples(int[] radixes)
		{
			radixes.ThrowIfNull("radixes");

			return MultiRadixTuplesImpl(radixes, e => e);
		}

		/// <summary>
		/// Generates all combinations by selecting one element from each inner enumerable in every possible way.
		/// </summary>
		/// <typeparam name="T">The type of elements.</typeparam>
		/// <param name="elements">A collection of sequences; one element is chosen from each sequence per tuple.</param>
		/// <returns>An enumerable of all possible selections.</returns>
		public static IEnumerable<T[]> MultiRadixTuples<T>(this IEnumerable<IEnumerable<T>> elements)
		{
			var elementArray = elements.Select(l => (IList<T>)l.ToArray()).ToArray();
			var indexes = MultiRadixTuples(elementArray.Select(l => l.Count));

			return indexes.Select(index => AccessByIndex(elementArray, index));
		}

		//This is really n of n combinations
		[Experimental]
		public static IEnumerable<T[]> Tuples<T>(this IEnumerable<T> list)
		{
			list.ThrowIfNull("list");

			var objList = list.ToArray();
			var indexes = Tuples(objList.Length);

			return indexes.Select(index => index.Select(i => objList[i]));
		}

		/// <summary>
		/// Generates n-tuples of integers 0 to n-1 and applies the selector to them.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">n;must be non negative</exception>
		[Experimental]
		public static IEnumerable<T> Tuples<T>(int n, Func<int[], T> select)
		{
			if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "must be non negative");
		
			select.ThrowIfNull("select");
			var radixes = new int[n].Init(n);

			return MultiRadixTuplesImpl(radixes, select);
		}

		#endregion

		#region Permutations

		/// <summary>
		/// Generates all permutations of the numbers 0 to n - 1.
		/// </summary>
		public static IEnumerable<int[]> Permutations(int n)
		{
			if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "must be non negative");

			return PermutationsImpl(n, e => e);
		}


		/// <summary>
		/// Generates all permutations of the list of elements.
		/// </summary>
		public static IEnumerable<T[]> Permutations<T>(this IEnumerable<T> list)
		{
			list.ThrowIfNull("list");
		
			var items = list.ToArray();
			var permutationIndexes = Permutations(items.Length);

			return permutationIndexes.Select(
				permutationIndex => permutationIndex.Select(i => items[i]));
		}

		#endregion

		#region Combinations		
		/// <summary>
		/// Generates all combinations of m elements selected from the list.
		/// </summary>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		/// <param name="list">The list to select from.</param>
		/// <param name="m">The number of elements in each combination.</param>
		/// <returns>IEnumerable&lt;T[]&gt;.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// m;must be nonnegative
		/// or
		/// m;must be smaller than n
		/// </exception>
		public static IEnumerable<T[]> Combinations<T>(this IEnumerable<T> list, int m)
		{
			list.ThrowIfNull("list");

			var listArray = list.ToArray();

			if (m < 0) throw new ArgumentOutOfRangeException(nameof(m), m, "must be nonnegative");
			if (m > listArray.Length) throw new ArgumentOutOfRangeException(nameof(m), m, "must be smaller than n");

			var combinationIndexes = CombinationsImpl(m, listArray.Length, e => e);

			return combinationIndexes.Select(
				combinationIndex => combinationIndex.Select(i => listArray[i]));
		}

		/// <summary>
		/// Generates all combinations of m elements from n and applies a selector to each.
		/// </summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="m">The number of elements in each combination.</param>
		/// <param name="n">The total number of elements to choose from.</param>
		/// <param name="select">A function applied to each combination's index array to produce the result.</param>
		/// <returns>An enumerable of results for all C(n, m) combinations.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="m"/> or <paramref name="n"/> is negative, or <paramref name="m"/> is greater than <paramref name="n"/>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="select"/> is <see langword="null"/>.</exception>
		public static IEnumerable<T> Combinations<T>(int m, int n, Func<int[], T> select)
		{
			if (m < 0) throw new ArgumentOutOfRangeException(nameof(m), m, "must be nonnegative");
			if(n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "must be nonnegative");
			if (m > n) throw new ArgumentOutOfRangeException(nameof(m), m, "must be smaller than n");

			select.ThrowIfNull("select");

			return CombinationsImpl(m, n, select);
		}

		#endregion

		#region Implementation

		private static IEnumerable<T> CombinationsImpl<T>(int m, int n, Func<int[], T> select)
		{
			var combination = RangeArray(m + 2);

			combination[m] = n;
			combination[m + 1] = 0;

			while (true)
			{
				yield return select(combination.Take(m));
				int j = 0;

				while (combination[j] + 1 == combination[j + 1])
				{
					combination[j] = j;
					j++;

					if (j >= m) yield break;
				}

				combination[j]++;
			}
		}

		private static IEnumerable<T> MultiRadixTuplesImpl<T>(IList<int> radixes, Func<int[], T> select)
		{
			var tuple = new int[radixes.Count].Init(0);

			while (true)
			{
				yield return select(tuple.Copy());

				int currentIndex = radixes.Count - 1;
				tuple[currentIndex]++;

				while (tuple[currentIndex] >= radixes[currentIndex]) //while we overflow, we carry over
				{
					currentIndex--;

					if (currentIndex < 0) yield break;

					tuple[currentIndex]++;
					tuple[currentIndex + 1] = 0;
				}
			}
		}

		private static IEnumerable<T> PermutationsImpl<T>(int n, Func<int[], T> select)
		{
			if (n == 0)
			{
				yield break;
			}

			var permutation = RangeArray(n);

			if (n == 1)
			{
				yield return select(permutation.Copy());	
				yield break;
			}

			while (true)
			{
				int index = n - 2;

				GLDebug.Assert(index >= 0, "index must be greater than or equal to 0");
				GLDebug.Assert(index + 1 < permutation.Length, "index + 1 must be smaller than permutation length, but index == " + index);

				while (permutation[index] >= permutation[index + 1]) //while we overflow, we carry over
				{
					index--;

					if (index < 0) yield break;
				}

				// Find the right most index where the value
				// is smaller than at the current index 
				int index2 = n - 1;

				while (permutation[index] >= permutation[index2])
				{
					index2--;
				}

				Swap(permutation, index, index2);

				ReverseRange(permutation, index + 1, n);

				yield return select(permutation.Copy());	
			}
		}


		// P417
		// ReSharper disable once CognitiveComplexity
		// Too complex to refactor without unit tests
		private static IEnumerable<int[]> RestrictedGrowthStrings(int n)
		{
			//H1
			int[] a = new int[n].Init(0);
			int[] b = new int[n - 1].Init(1);
			int m = 1;

			while (true)
			{
				//H2
				yield return a;

				if (a[n - 1] == m)
				{
					//H4
					int j = n - 2;

					while (a[j] == b[j])
					{
						j--;
					}

					//H5
					if (j == 0)
					{
						yield break;
					}
		
					a[j]++;

					//H6
					m = b[j] + (a[j] == b[j] ? 1 : 0);
					j++;

					while (j < n - 1)
					{
						a[j] = 0;
						b[j] = m;
						j++;
					}

					a[n - 1] = 0;
				}
				else
				{
					//H3
					a[n - 1]++;
				}
			}
		}

		//indexes[m][n] means object at m should be in set n in the result
		private static IEnumerable<IEnumerable<T>> AccessByRestrictedGrowthString<T>(IList<T> values, IList<int> indexes)
		{
			var setCount = indexes.Max() + 1;
			var sets = new IList<T>[setCount];

			for (int i = 0; i < setCount; i++)
			{
				sets[i] = new List<T>();
			}

			for (int i = 0; i < indexes.Count; i++)
			{
				sets[indexes[i]].Add(values[i]);
			}

			return sets;
		}

		//indexes
		private static T[] AccessByIndex<T>(IList<IList<T>> values, IList<int> indexes)
		{
			var set = new T[values.Count];

			for (int i = 0; i < values.Count; i++)
			{
				set[i] = values[i][indexes[i]];
			}

			return set;
		}

		//From http://stackoverflow.com/questions/19890781/creating-a-power-set-of-a-sequence
		private static IEnumerable<IEnumerable<T>> PowerSetImpl<T>(IList<T> input)
		{
			int n = input.Count;

			// Power set contains 2^N subsets.
			int powerSetCount = 1 << n;

			for (int setMask = 0; setMask < powerSetCount; setMask++)
			{
				var s = new List<T>();

				for (int i = 0; i < n; i++)
				{
					// Checking whether i-th element of input collection should go to the current subset.
					if ((setMask & (1 << i)) > 0)
					{
						s.Add(input[i]);
					}
				}

				yield return s;
			}
		}

		#endregion

		#region Array Extensions

		private static T[] Init<T>(this T[] array, T item)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = item;
			}

			return array;
		}

		private static T[] Take<T>(this T[] array, int m)
		{
			var newArray = new T[Mathf.Min(array.Length, m)];

			for (int i = 0; i < newArray.Length; i++)
			{
				newArray[i] = array[i];
			}

			return newArray;
		}

		private static TResult[] Select<TSource, TResult>(this TSource[] array, Func<TSource, TResult> select)
		{
			var newArray = new TResult[array.Length];

			for (int i = 0; i < array.Length; i++)
			{
				newArray[i] = select(array[i]);
			}

			return newArray;
		}

		private static int[] RangeArray(int n)
		{
			int[] newArray = new int[n];

			for (int i = 0; i < n; i++)
			{
				newArray[i] = i;
			}

			return newArray;
		}

		private static T[] Copy<T>(this T[] array)
		{
			return array.ToArray();
		}

		private static void Swap<T>(IList<T> permutation, int index1, int index2) 
			=> (permutation[index1], permutation[index2]) = (permutation[index2], permutation[index1]);

		//reverses the elements in range [startIndex endIndex)
		private static void ReverseRange<T>(T[] permutation, int startIndex, int endIndex)
		{
			int leftIndex = startIndex;
			int rightIndex = endIndex - 1;

			while (leftIndex < rightIndex)
			{
				Swap(permutation, rightIndex, leftIndex);

				rightIndex--;
				leftIndex++;
			}
		}

		#endregion
	}
}
