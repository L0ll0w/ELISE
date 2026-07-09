// ReSharper disable InconsistentNaming

using System.Collections.Generic;

namespace Gamelogic.Extensions.Samples
{
	public static class MatrixPresets
	{
		public const string Title = "Matrix Presets";
		public const string RetrievalKey = nameof(MatrixPresets);
		
		public static readonly IDictionary<string, MatrixFloat> Presets = new Dictionary<string, MatrixFloat>()
		{
			["2x2 Ones"] = new MatrixFloat(2, 2, 1),
			["2x2 Zeros"] = new MatrixFloat(2, 2, 0),
			["2x2 Identity"] = new MatrixFloat(2, 2, new[] { 1f, 0, 0, 1 }),
			
			["3x3 Ones"] = new MatrixFloat(3, 3, 1),
			["3x3 Zeros"] = new MatrixFloat(3, 3, 0),
			["3x3 Identity"] = new MatrixFloat(3, 3, new[]
			{
				1f, 0, 0,
				0, 1, 0,
				0, 0, 1
			}),
			
			["4x4 Ones"] = new MatrixFloat(4, 4, 1),
			["4x4 Zeros"] = new MatrixFloat(4, 4, 0),
			["4x4 Identity"] = new MatrixFloat(4, 4, new[]
			{
				1f, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1
			})
		};
	}
}
