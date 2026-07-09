using System.Collections.Generic;
using Unity.Mathematics;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Represents a read-only view of a two-dimensional matrix of float values.
	/// </summary>
	public interface IFloatMatrix
	{
		/// <summary>
		/// Sets the values and dimensions of this matrix from another matrix.
		/// </summary>
		/// <param name="other">The source matrix to copy from.</param>
		void SetFrom(IFloatMatrix other);

		/// <summary>
		/// Gets the width of the matrix.
		/// </summary>
		int Width { get; }

		/// <summary>
		/// Gets the height of the matrix.
		/// </summary>
		int Height { get; }

		/// <summary>
		/// Gets the total number of values in the matrix.
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Gets the matrix values as a flat, row-major sequence.
		/// </summary>
		IEnumerable<float> Values { get; }

		/// <summary>
		/// Returns whether this matrix is in a valid state (dimensions and value array are consistent).
		/// </summary>
		bool IsValid();

		/// <summary>
		/// Gets the value at the specified column and row.
		/// </summary>
		/// <param name="x">The column index.</param>
		/// <param name="y">The row index.</param>
		float this[int x, int y] { get; }

		/// <summary>
		/// Gets the value at the specified 2D index.
		/// </summary>
		/// <param name="index">The column (x) and row (y) index.</param>
		float this[int2 index] { get; }
	}
}