using System;
using System.Diagnostics;

namespace Gamelogic.Fx.Internal
{
	internal static class ShaderPropertyThrowHelper
	{
		/// <summary>
		/// The name of the conditional compilation symbol used to ensure that
		/// validation methods are executed only when running inside the Unity Editor.
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public const string UNITY_EDITOR = nameof(UNITY_EDITOR);

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if the value is negative.
		/// This check is executed only in the Unity Editor.
		/// </summary>
		/// <param name="value">The value to validate.</param>
		/// <param name="argName">The name of the argument being validated.</param>
		[Conditional(UNITY_EDITOR)]
		public static void ThrowIfNegative(this float value, string argName)
		{
			if (value < 0)
			{
				throw new ArgumentException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if the value is not strictly positive.
		/// This check is executed only in the Unity Editor.
		/// </summary>
		/// <param name="value">The value to validate.</param>
		/// <param name="argName">The name of the argument being validated.</param>
		[Conditional(UNITY_EDITOR)]
		public static void ThrowIfNotPositive(this float value, string argName)
		{
			if (value <= 0)
			{
				throw new ArgumentException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if the value is negative.
		/// This check is executed only in the Unity Editor.
		/// </summary>
		/// <param name="value">The value to validate.</param>
		/// <param name="argName">The name of the argument being validated.</param>
		[Conditional(UNITY_EDITOR)]
		public static void ThrowIfNegative(this int value, string argName)
		{
			if (value < 0)
			{
				throw new ArgumentException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the value is outside the given range.
		/// This check is executed only in the Unity Editor.
		/// </summary>
		/// <param name="value">The value to validate.</param>
		/// <param name="min">The minimum allowed value.</param>
		/// <param name="max">The maximum allowed value.</param>
		/// <param name="argName">The name of the argument being validated.</param>
		[Conditional(UNITY_EDITOR)]
		public static void ThrowIfOutOfRange(this float value, float min, float max, string argName)
		{
			if (value < min || value > max)
			{
				throw new ArgumentOutOfRangeException(argName);
			}
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the value does not fall
		/// within the specified half-open integer index range <c>[minInclusive, maxExclusive)</c>.
		/// This check is executed only in the Unity Editor.
		/// </summary>
		/// <param name="value">The index value to validate.</param>
		/// <param name="minInclusive">The minimum allowed index (inclusive).</param>
		/// <param name="maxExclusive">The maximum allowed index (exclusive).</param>
		/// <param name="argName">The name of the argument being validated.</param>
		[Conditional(UNITY_EDITOR)]
		public static void ThrowIfOutOfIndexRange(this int value, int minInclusive, int maxExclusive, string argName)
		{
			if (value < minInclusive || value >= maxExclusive)
			{
				throw new ArgumentOutOfRangeException(argName);
			}
		}

		/// <summary>
		/// Clamps <paramref name="value"/> to <paramref name="max"/> and logs a warning if it exceeds the maximum.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="max">The maximum allowed value.</param>
		/// <param name="argName">The name of the argument being validated, used in the warning message.</param>
		public static void WarnIfExceeds(this ref int value, int max, string argName)
		{
			if (value > max)
			{
				Console.WriteLine($"Warning: {argName} exceeds maximum of {max}. Clamping to maximum.");
				value = max;
			}
		}
	}
}
