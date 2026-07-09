using System;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Extensions.Algorithms;
using Gamelogic.Fx.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gamelogic.Fx
{
	/// <summary>
	/// A serializable data-driven dither matrix.
	/// Stores an array of normalized float values along with matrix dimensions.
	/// </summary>
	[Serializable]
	[ReuseCandidate(MoveToWhere = nameof(Extensions))]
	public sealed class FloatMatrix : IFloatMatrix
	{
		private const float Epsilon = 0.0001f;
		
		// Used by property drawer
		internal const string WidthFieldName = nameof(width);
		internal const string HeightFieldName = nameof(height);
		internal const string ValuesFieldName = nameof(values);

		/// <summary>
		/// Represents a matrix with zero width and height.
		/// </summary>
		public static readonly IFloatMatrix Empty = new FloatMatrix(0, 0, Array.Empty<float>());
		
		[SerializeField] private int width = 4;
		[SerializeField] private int height = 4;
		[SerializeField] private float[] values = new float[16];

		/// <summary>
		/// Gets the width of this matrix. 
		/// </summary>
		public int Width => width;
		
		/// <summary>
		/// Gets the height of this matrix. 
		/// </summary>
		public int Height => height;
		
		/// <summary>
		/// Gets the values of this matrix, as a one dimensional array.
		/// </summary>
		public IEnumerable<float> Values => values ?? Enumerable.Empty<float>();

		/// <summary>
		/// Gets the number of values in this matrix. 
		/// </summary>
		public int Length => values?.Length ?? 0;
		
		/// <summary>
		/// Gets nd sets the value at the specified coordinates.
		/// </summary>
		/// <param name="x">The x-coordinate of the matrix.</param>
		/// <param name="y">The y-coordinate of the matrix.</param>
		public float this[int x, int y]
		{
			get
			{
				x.ThrowIfOutOfRange(0, width, nameof(x));
				y.ThrowIfOutOfRange(0, height, nameof(y));

				int index = y * width + x;
				Assert.IsTrue(index < Length);
				Assert.IsNotNull(values); //will throw index out of range first
				
				return values[index];
			}
			
			set
			{
				x.ThrowIfOutOfRange(0, width, nameof(x));
				y.ThrowIfOutOfRange(0, height, nameof(y));

				int index = y * width + x;
				Assert.IsTrue(index < Length);
				Assert.IsNotNull(values); //will throw index out of range first

				values[index] = value;
			}
		}

		/// <summary>
		/// Gets the value at the specified coordinates.
		/// </summary>
		/// <param name="index">The 2D index into the matrix.</param>
		public float this[int2 index] => this[index.x, index.y];

		/// <summary>
		/// Initializes a new instance of the <see cref="FloatMatrix"/> class.
		/// </summary>
		/// <param name="width">The width of the matrix.</param>
		/// <param name="height">The height of the matrix.</param>
		/// <param name="values">The values to initialize the matrix with. If null, or omitted, the matrix is initialized
		/// with zeros.</param>
		/// <exception cref="ArgumentException"></exception>
		public FloatMatrix(int width, int height, float[] values = null)
		{
			ShaderPropertyThrowHelper.ThrowIfNegative(width, nameof(width));
			ShaderPropertyThrowHelper.ThrowIfNegative(height, nameof(height));

			if (values == null)
			{
				this.values = new float[width * height];
			}
			else
			{
				if (values.Length != width * height)
				{
					throw new ArgumentException($"Values length {values.Length} does not match specified dimensions {width}x{height}.");
				}

				this.values = values.ToArray();
			}
			
			this.width = width;
			this.height = height;
		}
		
		/// <summary>
		/// Sets the values and dimensions of this matrix from another matrix.
		/// </summary>
		/// <param name="other">The other matrix to copy from.</param>
		public void SetFrom(IFloatMatrix other)
		{
			other.ThrowIfNull(nameof(other));
			
			Assert.IsTrue(other.IsValid(), "The other matrix is corrupted.");
			
			width = other.Width;
			height = other.Height;
			values = other.Values.ToArray();
		}
		
		/// <summary>
		/// Adds a column to the matrix at the end. 
		/// </summary>
		public void AddColumn() => ResizeUnchecked(width + 1, height);
		
		/// <summary>
		/// Adds a row to the matrix at the end.
		/// </summary>
		public void AddRow() => ResizeUnchecked(width, height + 1);
		
		/// <summary>
		/// Removes the last column from the matrix if there is one.
		/// </summary>
		public void RemoveColumn()
		{
			if (Width > 0)
			{
				ResizeUnchecked(width - 1, height);
			}
		}
		
		/// <summary>
		/// Removes the last row from the matrix if there is one.
		/// </summary>
		public void RemoveRow()
		{
			if (Height > 0)
			{
				ResizeUnchecked(width, height - 1);
			}
		}
		
		/// <summary>
		/// Normalizes the values in this matrix to be between 0 and 1.
		/// </summary>
		/// <remarks>
		/// If the matrix is empty, an empty matrix is returned.
		///
		/// If all values in the matrix are the same, a matrix of the same dimensions filled with zeros is returned.
		///
		/// Otherwise, a new matrix with the same dimensions is returned, with values normalized between 0 and 1.
		/// </remarks>
		public IFloatMatrix Normalize()
		{
			if (Length == 0)
			{
				return Empty;
			}
			
			(float minValue, float maxValue) = values.MinMax();
			float range = maxValue - minValue;

			return 
				range < Epsilon 
					? new FloatMatrix(width, height) 
					: new FloatMatrix(width, height, values.Select(x => (x - minValue) / range).ToArray());
		}
		
		/// <summary>
		/// Returns whether all values in this matrix are effectively the same.
		/// </summary>
		/// <remarks>
		/// This can be called before normalization to check if normalization would produce a matrix of zeros.
		/// </remarks>
		public bool IsConstant()
		{
			if (Length == 0)
			{
				return true;
			}
			
			(float minValue, float maxValue) = values.MinMax();
			float range = maxValue - minValue;

			return range < Epsilon;
		}
		
		private void ResizeUnchecked(int newWidth, int newHeight)
		{
			Assert.IsTrue(width >= 0);
			Assert.IsTrue(height >= 0);
			
			if (newWidth == 0 || newHeight == 0)
			{
				values = Array.Empty<float>();
				width = 0;
				height = 0;
			
				return;
			}
			
			float[] newValues = new float[newWidth * newHeight];
			
			if (Length > 0)
			{
				int widthToCopy = Mathf.Min(width, newWidth);
				int heightToCopy = Mathf.Min(height, newHeight);

				for (int y = 0; y < heightToCopy; y++)
				{
					for (int x = 0; x < widthToCopy; x++)
					{
						int newIndex = x + newWidth * y;
						int index = x + width * y;
						newValues[newIndex] = values[index];
					}
				}
			}

			values = newValues;
			width = newWidth;
			height = newHeight;
		}

		/// <summary>
		/// Returns whether this matrix is in a valid state (dimensions and value array are consistent).
		/// </summary>
		public bool IsValid()
		{
			if (width < 0 || height < 0)
			{
				return false;
			}
			
			if (values.Length != width * height)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Creates a deep copy of this matrix.
		/// </summary>
		public FloatMatrix Clone() => new FloatMatrix(width, height, values.ToArray());
	}
}
