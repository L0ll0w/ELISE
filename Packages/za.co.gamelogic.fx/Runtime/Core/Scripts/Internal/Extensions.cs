using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	[ReuseCandidate(MoveToWhere = "Extensions")]
	internal static class Extensions
	{
		public static Vector4 ToVector4XYZ(this Vector3 vector, float w = 0)
		{
			return new Vector4(vector.x, vector.y, vector.z, w);
		}

		public static IEnumerable<T> SelectIndices<T>(this IList<T> list, params int[] indexes) 
			=> indexes.Select(index => list[index]);
	
		public static Vector2Int GetDimensions(this Texture2D texture)
		{
			return new Vector2Int(texture.width, texture.height);
		}
	
		public static Vector2Int GetDimensions<T>(T[,] array) 
			=> new Vector2Int(array.GetLength(0), array.GetLength(1));

		public static void Set<T>(this T[,] array, Vector2Int position, T value)
		{
			if (position.x < 0 || position.x >= array.GetLength(0) || 
				position.y < 0 || position.y >= array.GetLength(1))
			{
				throw new System.IndexOutOfRangeException("Position is out of bounds of the array.");
			}
		
			array[position.x, position.y] = value;
		}
	
		public static T Get<T>(this T[,] array, Vector2Int position)
		{
			if (position.x < 0 || position.x >= array.GetLength(0) || 
				position.y < 0 || position.y >= array.GetLength(1))
			{
				throw new System.IndexOutOfRangeException("Position is out of bounds of the array.");
			}
		
			return array[position.x, position.y];
		}
	
		public static void Fill<T>(this T[,] grid, T item)
		{
			foreach(var point in Range(GetDimensions(grid)))
			{
				grid[point.x, point.y] = item;
			}
		}
	
		public static Vector2 ToVector(this int2 vector) => new Vector2(vector.x, vector.y);
		public static Vector3 ToVector(this int3 vector) => new Vector3(vector.x, vector.y, vector.z);
		
		public static Vector4 ToVector4XYZ(this int3 vector, float w = 0) => new Vector4(vector.x, vector.y, vector.z, w);
	
		public static Vector4 ToVector(this int4 vector) => new Vector4(vector.x, vector.y, vector.z, vector.w);
		
		public static IEnumerable<Vector2Int> Range(this Vector2Int startInclusive, Vector2Int endExclusive)
		{
			for (int x = startInclusive.x; x < endExclusive.x; x++)
			{
				for (int y = startInclusive.y; y < endExclusive.y; y++)
				{
					yield return new Vector2Int(x, y);
				}
			}
		}
		
		public static IEnumerable<Vector2Int> Range(Vector2Int endExclusive) => Vector2Int.zero.Range(endExclusive);
		
		[ReuseCandidate(MoveToWhere = "Extensions")]
		internal static IList<T> ForceCount<T>(this IList<T> list, int count)
		{
			while (list.Count < count)
			{
				list.Add(default);
			}

			while (list.Count > count)
			{
				list.RemoveAt(list.Count - 1);
			}

			return list;
		}
	}
}
