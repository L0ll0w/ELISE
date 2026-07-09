// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Class for representing a bounded range.
	/// </summary>
	[Version(1, 2, 0)]
	[Serializable]
	public class MinMaxInt
	{
		#region Public Fields

		/// <summary>The minimum value of the range.</summary>
		[UnityEngine.Tooltip("The minimum value of the range.")]
		public int min = 0;

		/// <summary>The maximum value of the range.</summary>
		[UnityEngine.Tooltip("The maximum value of the range.")]
		public int max = 1;

		#endregion

		/// <summary>Initializes a new instance with a range of [0, 1].</summary>
		public MinMaxInt()
		{
			min = 0;
			max = 1;
		}

		/// <summary>Initializes a new instance with the specified range.</summary>
		/// <param name="min">The minimum value.</param>
		/// <param name="max">The maximum value.</param>
		public MinMaxInt(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
	}
}
