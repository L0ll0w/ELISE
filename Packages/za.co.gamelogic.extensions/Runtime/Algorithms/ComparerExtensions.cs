using System.Collections.Generic;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Algorithms
{
	/// <summary>
	/// Provides extension methods for <see cref="IComparer{T}"/>.
	/// </summary>
	[Version(3, 0, 0)]
	public static class ComparerExtensions
	{
		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/> according to the comparer.
		/// </summary>
		/// <typeparam name="T">The type being compared.</typeparam>
		/// <param name="comparer">The comparer to use.</param>
		/// <param name="a">The first value.</param>
		/// <param name="b">The second value.</param>
		/// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
		public static bool Less<T>(this IComparer<T> comparer, T a, T b) => comparer.Compare(a, b) < 0;
	}
}
