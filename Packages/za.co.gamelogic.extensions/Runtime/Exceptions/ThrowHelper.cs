using System;
using Gamelogic.Extensions.Internal;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Support;
using JetBrains.Annotations;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Class that provides helper methods for throwing exceptions.
	/// </summary>
	[Version(4, 0, 0)]
	public static class ThrowHelper
	{
		internal static readonly Exception UnreachableCodeException =
			new InvalidOperationException("Unreachable code.");
		
		internal static readonly InvalidOperationException CollectionEmptyException 
			= new InvalidOperationException(ErrorMessages.ContainerEmpty);
		
		internal static readonly InvalidOperationException CollectionFullException 
			= new InvalidOperationException(ErrorMessages.ContainerFull);
		
		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if the given argument is <see langword="null"/>.
		/// </summary>
		/// <param name="argument">An argument to check.</param>
		/// <param name="argName">The name of the argument.</param>
		/// <exception cref="ArgumentNullException"><paramref name="argument"/> is <see langword="null"/>.</exception>
		public static void ThrowIfNull(this object argument, string argName)
		{
			if (argument == null)
			{
				throw new ArgumentNullException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the integer is negative.
		/// </summary>
		/// <param name="argument">The integer to check.</param>
		/// <param name="argName">The name of the argument.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="argument"/> is negative.</exception>
		public static void ThrowIfNegative(this int argument, string argName)
		{
			if (argument < 0)
			{
				throw new ArgumentOutOfRangeException(argName, argument, ErrorMessages.ArgumentCannotBeNegative);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the float is negative.
		/// </summary>
		/// <param name="argument">The float to check.</param>
		/// <param name="argName">The name of the argument.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="argument"/> is negative.</exception>
		public static void ThrowIfNegative(this float argument, string argName)
		{
			if (argument < 0)
			{
				throw new ArgumentOutOfRangeException(argName, argument, ErrorMessages.ArgumentCannotBeNegative);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the integer is not positive (i.e. zero or negative).
		/// </summary>
		/// <param name="argument">The integer to check.</param>
		/// <param name="argName">The name of the argument.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="argument"/> is zero or negative.</exception>
		[AssertionMethod]
		public static void ThrowIfNotPositive(this int argument, string argName = null)
		{
			if (argument <= 0)
			{
				throw new ArgumentOutOfRangeException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the integer is out of range.
		/// </summary>
		/// <param name="argument">The integer to check.</param>
		/// <param name="minInclusive">The minimum value of the range, included.</param>
		/// <param name="maxExclusive">The maximum value of the range, not included.</param>
		/// <param name="argName">The name of the argument.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="argument"/> is less than <paramref name="minInclusive"/> or greater than or equal to <paramref name="maxExclusive"/>.</exception>
		public static void ThrowIfOutOfRange(this int argument, int minInclusive, int maxExclusive, string argName)
		{
			if (argument < minInclusive || argument >= maxExclusive)
			{
				throw new ArgumentOutOfRangeException(argName, argument, GetMessage());
			}

			return;

			string GetMessage() => string.Format(ErrorMessages.ArgumentMustBeInRange, minInclusive, maxExclusive);
		}

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> indicating the container is empty.
		/// </summary>
		/// <exception cref="InvalidOperationException">Always thrown.</exception>
		[DoesNotReturn]
		public static void ThrowContainerEmpty() 
			=> throw CollectionEmptyException;

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> indicating the container is full.
		/// </summary>
		/// <exception cref="InvalidOperationException">Always thrown.</exception>
		[DoesNotReturn]
		public static void ThrowContainerFull() 
			=> throw CollectionFullException;

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> if the array is empty.
		/// </summary>
		/// <typeparam name="T">The element type of the array.</typeparam>
		/// <param name="list">The array to check.</param>
		/// <param name="listArgName">The name of the argument.</param>
		/// <returns>The original array if it is not empty.</returns>
		/// <exception cref="InvalidOperationException"><paramref name="list"/> is empty.</exception>
		public static T[] ThrowIfEmpty<T>(this T[] list, string listArgName = null)
		{
			if (list.Length == 0)
			{
				ThrowContainerEmpty();
			}

			return list;
		}

		/// <summary>
		/// Throws an <see cref="InvalidOperationException"/> if the list is empty.
		/// </summary>
		/// <typeparam name="T">The element type of the list.</typeparam>
		/// <param name="list">The list to check.</param>
		/// <param name="listArgName">The name of the argument.</param>
		/// <returns>The original list if it is not empty.</returns>
		/// <exception cref="InvalidOperationException"><paramref name="list"/> is empty.</exception>
		public static IList<T> ThrowIfEmpty<T>(this IList<T> list, string listArgName = null)
		{
			if (!list.Any())
			{
				ThrowContainerEmpty();
			}

			return list;
		}

		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if the object is <see langword="null"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="obj">The object to check.</param>
		/// <param name="objArgName">The name of the argument.</param>
		/// <returns>The original object if it is not <see langword="null"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
		public static T ThrowIfNull<T>(
			[NotNull, NoEnumeration] this T obj, 
			string objArgName = null)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(objArgName);
			}

			return obj;
		}

		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if the string is <see langword="null"/> or empty.
		/// </summary>
		/// <param name="obj">The string to check.</param>
		/// <param name="objArgName">The name of the argument.</param>
		/// <returns>The original string if it is not <see langword="null"/> or empty.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/> or empty.</exception>
		public static string ThrowIfNullOrEmpty(
			[NotNull, NoEnumeration] this string obj, 
			string objArgName = null)
		{
			if (string.IsNullOrEmpty(obj))
			{
				throw new ArgumentNullException(objArgName);
			}

			return obj;
		}

		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if the string is <see langword="null"/>, empty, or whitespace.
		/// </summary>
		/// <param name="obj">The string to check.</param>
		/// <param name="objArgName">The name of the argument.</param>
		/// <returns>The original string if it is not <see langword="null"/>, empty, or whitespace.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>, empty, or whitespace.</exception>
		public static string ThrowIfNullOrWhiteSpace(
			[NotNull, NoEnumeration] this string obj, 
			string objArgName = null)
		{
			if (string.IsNullOrWhiteSpace(obj))
			{
				throw new ArgumentNullException(objArgName);
			}

			return obj;
		}

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if <paramref name="targetType"/> is not assignable from <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The source type to check assignability from.</typeparam>
		/// <param name="targetType">The target type.</param>
		/// <returns>The original <paramref name="targetType"/> if the check passes.</returns>
		/// <exception cref="ArgumentException"><paramref name="targetType"/> is not assignable from <typeparamref name="T"/>.</exception>
		[Version(4, 2, 0)]
		public static Type ThrowIfNotAssignableFrom<T>(this Type targetType)
		{
			if (!targetType.IsAssignableFrom(typeof(T)))
			{
				throw new ArgumentException(string.Format(ErrorMessages.TypeNotAssignableFromType, targetType, typeof(T)));
			}

			return targetType;
		}

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if <paramref name="targetType"/> is not assignable from <paramref name="sourceType"/>.
		/// </summary>
		/// <param name="targetType">The target type.</param>
		/// <param name="sourceType">The source type to check assignability from.</param>
		/// <returns>The original <paramref name="targetType"/> if the check passes.</returns>
		/// <exception cref="ArgumentException"><paramref name="targetType"/> is not assignable from <paramref name="sourceType"/>.</exception>
		[Version(4, 2, 0)]
		public static Type ThrowIfNotAssignableFrom(this Type targetType, Type sourceType)
		{
			if (!targetType.IsAssignableFrom(sourceType))
			{
				throw new ArgumentException(string.Format(ErrorMessages.TypeNotAssignableFromType, targetType, sourceType));
			}

			return targetType;
		}
	}
}
