// Copyright Gamelogic Pty Ltd (c) http://www.gamelogic.co.za

using System;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Use this attribute to specify the range for a <see cref="MinMaxFloat"/> field, property, parameter or return value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	[Version(4, 2, 0)]
	public class MinMaxRangeAttribute : Attribute
	{
		private float min, max;

		/// <summary>Initializes a new instance with the specified float range.</summary>
		/// <param name="min">The minimum value of the range.</param>
		/// <param name="max">The maximum value of the range.</param>
		public MinMaxRangeAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}

		/// <summary>Initializes a new instance with the specified integer range.</summary>
		/// <param name="min">The minimum value of the range.</param>
		/// <param name="max">The maximum value of the range.</param>
		public MinMaxRangeAttribute(int min, int max)
		{
			this.min = min;
			this.max = max;
		}

		/// <summary>Returns the minimum and maximum values of this range.</summary>
		/// <returns>A tuple containing the minimum and maximum values.</returns>
		public (float min, float max) GetRange()
		{
			return (min, max);
		}
	}
}
