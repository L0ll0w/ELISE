using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Gamelogic.Extensions.Algorithms;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// A matrix of float values. 
	/// </summary>
	/// <remarks>
	/// This type is meant to be used in the inspector, and has a <see cref="UnityEditor.PropertyDrawer"/> implemented.
	/// It is mutable, and not suitable for mathematical operations. The mutation methods provided are to implement the
	/// drawer tools.
	///
	/// You can register presets using <see cref="PropertyDrawerData.RegisterValuesRetriever"/>. The retriever function
	/// should return an <see cref="IDictionary{TKey,TValue}"/> with keys of type <see cref="string"/> and values of
	/// type <see cref="MatrixFloat"/>. 
	///
	/// >[!WARNING]
	/// >This type is mutable, so be careful how to use it. If you modify it in code, it is better to <see cref="Clone"/>
	/// >the original, or use <see cref="SetFrom"/> to initialize your copy.   
	/// </remarks>
	/// 
	[Serializable]
	[Version(4, 5, 0)]
	public sealed class MatrixFloat : IEnumerable<float>, ISerializationCallbackReceiver, ISetableFrom<MatrixFloat>
	{
		private const float Epsilon = 0.00001f; // Kind of arbitrary...
		
		// Used by property drawer
		internal const string WidthFieldName = nameof(width);
		internal const string HeightFieldName = nameof(height);
		internal const string ValuesFieldName = nameof(values);
		
		[Tooltip("The number of columns in this matrix.")]
		[SerializeField] private int width = 4;
		[Tooltip("The number of rows in this matrix.")]
		[SerializeField] private int height = 4;
		[Tooltip("The flat array of matrix values, stored in row-major order.")]
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
		/// Gets the number of values in this matrix.
		/// </summary>
		public int Count => values.Length;
		
		/// <summary>
		/// Gets or sets the value at the specified coordinates.
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
				Assert.IsTrue(index < Count);
				Assert.IsNotNull(values); //will throw index out of range first

				return values[index];
			}
			set
			{
				x.ThrowIfOutOfRange(0, width, nameof(x));
				y.ThrowIfOutOfRange(0, height, nameof(y));

				int index = y * width + x;
				Assert.IsTrue(index < Count);
				Assert.IsNotNull(values); //will throw index out of range first

				values[index] = value;
			}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="MatrixFloat"/> class with the
		/// specified dimensions and fills the matrix with the given initial value.
		/// </summary>
		/// <param name="width">The width of the matrix.</param>
		/// <param name="height">The height of the matrix.</param>
		/// <param name="initialValue">The value used to initialize all elements.</param>
		/// <exception cref="ArgumentException">
		/// <paramref name="width"/> or <paramref name="height"/> is negative.
		/// </exception>
		public MatrixFloat(int width, int height, float initialValue)
		{
			width.ThrowIfNegative(nameof(width));
			height.ThrowIfNegative(nameof(height));
			
			this.width = width;
			this.height = height;
			values = new float[width*height];
			values.Fill(initialValue);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="MatrixFloat"/> class.
		/// </summary>
		/// <param name="width">The width of the matrix.</param>
		/// <param name="height">The height of the matrix.</param>
		/// <param name="values">The values to initialize the matrix with. If null, or omitted, the matrix is initialized
		/// with zeros.</param>
		/// <exception cref="ArgumentException">
		/// <paramref name="width"/> or <paramref name="height"/> is negative.
		/// </exception>
		public MatrixFloat(int width, int height, float[] values = null)
		{
			width.ThrowIfNegative(nameof(width));
			height.ThrowIfNegative(nameof(height));

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
		/// Sets all slots of the matrix to the given values. 
		/// </summary>
		/// <param name="value">The value to set all slots to.</param>
		public void Fill(float value)
		{
			if (Count == 0)
			{
				return;
			}
			
			Assert.IsNotNull(values);
			
			values.Fill(value);
		}
		
		/// <summary>
		/// Applies the given function to each value in this matrix.
		/// </summary>
		/// <param name="func">
		/// A function that takes the current value and returns the value to
		/// store in its place.
		/// </param>
		/// <remarks>
		/// This method modifies the matrix in place.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="func"/> is null.
		/// </exception>
		public void Apply(Func<float, float> func)
		{
			func.ThrowIfNull(nameof(func));
			
			if (Count == 0)
			{
				return;
			}
			
			Assert.IsNotNull(values);
			values.Apply(func);
		}
		
		/// <summary>
		/// Sets the values and dimensions of this matrix from another matrix.
		/// </summary>
		/// <param name="other">The other matrix to copy from.</param>
		public void SetFrom(MatrixFloat other)
		{
			other.ThrowIfNull(nameof(other));
			
			Assert.IsTrue(other.IsValid(), "The other matrix is corrupted.");
			
			width = other.width;
			height = other.height;
			values = other.values.ToArray();
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
		/// If the matrix is empty, the matrix is filled with 0s.
		///
		/// If all values in the matrix are the same, the matrix is filled with 0s .
		///
		/// Otherwise, the values are normalized between 0 and 1.
		/// </remarks>
		public void Normalize()
		{
			if (Count == 0)
			{
				return;
			}
			
			Assert.IsNotNull(values); // will return Empty first
			
			(float minValue, float maxValue) = values.MinMax();
			float range = maxValue - minValue;

			
			if(range < Epsilon)
			{
				values.Fill(0);
			}
			else
			{
				values.Apply(x => (x - minValue) / range);
			}
		}
		
		/// <summary>
		/// Returns whether all values in this matrix are effectively the same.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if all values are within epsilon of each other; otherwise, <see langword="false"/>.
		/// This can be checked before normalization to determine if normalization would produce a matrix of zeros.
		/// </returns>
		public bool IsConstant()
		{
			if (Count == 0)
			{
				return true;
			}
			
			Assert.IsNotNull(values); // will return above first
			
			(float minValue, float maxValue) = values.MinMax();
			float range = maxValue - minValue;

			return range < Epsilon;
		}

		/// <summary>
		/// Creates a copy of this matrix with the same width, height, and values in corresponding places.
		/// </summary>
		/// <returns>A new <see cref="MatrixFloat"/> with the same dimensions and values.</returns>
		public MatrixFloat Clone()
		{
			if (Count == 0)
			{
				return CreateEmpty();
			}
			return new MatrixFloat(width, height, values.ToArray());
		}

		/// <summary>
		/// Creates a new empty <see cref="MatrixFloat"/>.
		/// </summary>
		/// <returns>A <see cref="MatrixFloat"/> with width and height equal to 0.</returns>
		// We cannot provide an empty constant, since this type is mutable. 
		public static MatrixFloat CreateEmpty() => new MatrixFloat(0, 0, Array.Empty<float>());
		
		/// <inheritdoc/>
		public IEnumerator<float> GetEnumerator()
		{
			if (values == null)
			{
				yield break;
			}

			for (int i = 0; i < values.Length; i++)
			{
				yield return values[i];
			}
		}
		
		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		
		/// <inheritdoc/>
		public void OnBeforeSerialize()
		{
			// Nothing needed.
		}

		/// <inheritdoc/>
		public void OnAfterDeserialize()
		{
			if (width < 0)
			{
				width = 0;
			}

			if (height < 0)
			{
				height = 0;
			}

			int expected = width * height;
			
			if (values == null || Count != expected)
			{
				values = new float[expected];
			}
		}
		
		private void ResizeUnchecked(int newWidth, int newHeight)
		{
			Assert.IsTrue(IsValid(), "Matrix corrupted before resize.");
			
			if(newWidth == 0 || newHeight == 0)
			{
				values = Array.Empty<float>();
				width = 0;
				height = 0;
			
				return;
			}
			
			float[] newValues = new float[newWidth * newHeight];
			
			if(Count > 0)
			{
				int widthToCopy = Mathf.Min(width, newWidth);
				int heightToCopy = Mathf.Min(height, newHeight);

				int sizeToCopy = widthToCopy * heightToCopy;

				if (sizeToCopy > 0)
				{
					Assert.IsNotNull(values);
				}
				// otherwise will skip loops 

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

		private bool IsValid()
		{
			if (values == null)
			{
				return false;
			}
			
			if(width < 0 || height < 0)
			{
				return false;
			}
			
			if(values.Length != width * height)
			{
				return false;
			}

			return true;
		}

		
	}
}
