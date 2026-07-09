using System;
using UnityEngine;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Represents a two-dimensional matrix stored internally as a flat float array.
	/// The matrix layout and interpretation are controlled by the <see cref="dimensions"/> field.
	/// </summary>
	[Serializable]
	public sealed class MatrixArray
	{
		/// <summary>
		/// The width and height of the matrix. Each component must be positive.
		/// </summary>
		public Vector2Int dimensions = new Vector2Int(4, 4);

		/// <summary>
		/// The matrix values stored in row-major order as a one-dimensional array.
		/// The expected length is <c>dimensions.x * dimensions.y</c>.
		/// </summary>
		public float[] values;
	}
}
