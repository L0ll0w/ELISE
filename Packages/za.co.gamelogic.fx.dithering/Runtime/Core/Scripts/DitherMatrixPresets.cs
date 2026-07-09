using System.Collections.Generic;

namespace Gamelogic.Fx.Dithering
{
	/// <summary>
	/// Contains preset dither matrices.
	/// </summary>
	public static class DitherMatrixPresets
	{
		/// <summary>A 2×2 checkerboard dither pattern.</summary>
		public static readonly FloatMatrix Checker =
			new FloatMatrix(2, 2, new float[]
			{
				0, 1,
				1, 0
			});

		/// <summary>A 2×2 Bayer ordered dither matrix.</summary>
		public static readonly FloatMatrix Bayer2 =
			new FloatMatrix(2, 2, new float[]
			{
				0, 2,
				1, 3
			});

		/// <summary>A 4×4 Bayer ordered dither matrix.</summary>
		public static readonly FloatMatrix Bayer4 =
			new FloatMatrix(4, 4, new float[]
			{
				0, 8, 2, 10,
				12, 4, 14, 6,
				3, 11, 1, 9,
				15, 7, 13, 5
			});

		/// <summary>An 8×8 Bayer ordered dither matrix.</summary>
		public static readonly FloatMatrix Bayer8 =
			new FloatMatrix(8, 8, new float[]
			{
				0, 48, 12, 60, 3, 51, 15, 63,
				32, 16, 44, 28, 35, 19, 47, 31,
				8, 56, 4, 52, 11, 59, 7, 55,
				40, 24, 36, 20, 43, 27, 39, 23,
				2, 50, 14, 62, 1, 49, 13, 61,
				34, 18, 46, 30, 33, 17, 45, 29,
				10, 58, 6, 54, 9, 57, 5, 53,
				42, 26, 38, 22, 41, 25, 37, 21
			});

		/// <summary>A 1×4 horizontal Bayer dither strip.</summary>
		public static readonly FloatMatrix BayerH4 =
			new FloatMatrix(1, 4, new float[]
			{
				0,
				2,
				1,
				3,
			});

		/// <summary>A 4×1 vertical Bayer dither strip.</summary>
		public static readonly FloatMatrix BayerV4 =
			new FloatMatrix(4, 1, new float[]
			{
				0, 2, 1, 3,
			});

		/// <summary>A 4×4 diagonal Bayer dither matrix (variant D).</summary>
		public static readonly FloatMatrix BayerD4 =
			new FloatMatrix(4, 4, new float[]
			{
				0, 2, 1, 3,
				3, 0, 2, 1,
				1, 3, 0, 2,
				2, 1, 3, 0
			});

		/// <summary>A 4×4 diagonal Bayer dither matrix (variant E).</summary>
		public static readonly FloatMatrix BayerE4 =
			new FloatMatrix(4, 4, new float[]
			{
				0, 2, 1, 3,
				2, 1, 3, 0,
				1, 3, 0, 2,
				3, 0, 2, 1
			});
		
		/// <summary>A 1×8 horizontal Bayer dither strip.</summary>
		public static readonly FloatMatrix BayerH8 =
			new FloatMatrix(1, 8, new float[]
			{
				0, 4, 2, 6, 1, 5, 3, 7
			});
		
		/// <summary>An 8×1 vertical Bayer dither strip.</summary>
		public static readonly FloatMatrix BayerV8 =
			new FloatMatrix(8, 1, new float[]
			{
				0,
				4,
				2,
				6,
				1,
				5,
				3,
				7
			});
		
		/// <summary>An 8×8 diagonal Bayer dither matrix (variant D).</summary>
		public static readonly FloatMatrix BayerD8 =
			new FloatMatrix(8, 8, new float[]
			{
				0, 4, 1, 5, 2, 6, 3, 7,
				5, 1, 4, 0, 7, 3, 6, 2,
				2, 6, 3, 7, 1, 5, 0, 4,
				7, 3, 6, 2, 5, 1, 4, 0,

				1, 5, 2, 6, 3, 7, 0, 4,
				4, 0, 5, 1, 6, 2, 7, 3,
				3, 7, 0, 4, 1, 5, 2, 6,
				6, 2, 7, 3, 4, 0, 5, 1
			});

		
		/// <summary>An 8×8 diagonal Bayer dither matrix (variant E).</summary>
		public static readonly FloatMatrix BayerE8 =
			new FloatMatrix(8, 8, new float[]
			{
				7, 3, 6, 2, 5, 1, 4, 0,
				3, 7, 2, 6, 1, 5, 0, 4,
				6, 2, 7, 3, 4, 0, 5, 1,
				2, 6, 3, 7, 0, 4, 1, 5,

				5, 1, 4, 0, 7, 3, 6, 2,
				1, 5, 0, 4, 3, 7, 2, 6,
				4, 0, 5, 1, 6, 2, 7, 3,
				0, 4, 1, 5, 2, 6, 3, 7
			});

		/// <summary>A 6×6 dot-cluster dither matrix.</summary>
		public static readonly FloatMatrix Dot =
			new FloatMatrix(6, 6, new float[]
			{
				9, 16, 12, 8, 1, 5,
				13, 17, 15, 4, 0, 2,
				10, 14, 11, 7, 3, 6,
				8, 1, 5, 9, 16, 12,
				4, 0, 2, 13, 17, 15,
				7, 3, 6, 10, 14, 11
			});

		/// <summary>A 4×4 blue-noise dither matrix.</summary>
		public static readonly FloatMatrix Blue =
			new FloatMatrix(4, 4, new[]
			{
				0.271f, 1.000f, 0.685f, 0.584f,
				0.085f, 0.162f, 0.000f, 0.800f,
				0.405f, 0.896f, 0.050f, 0.983f,
				0.799f, 0.027f, 0.261f, 0.060f
			});
		
		/// <summary>A 4×4 tapered horizontal dither matrix.</summary>
		public static readonly FloatMatrix TaperedH =
			new FloatMatrix(4, 4, new float[]
			{
				0, 1, 2, 3,
				8, 9, 10, 11,
				4, 5, 6, 7,
				12, 13, 14, 15
			});

		/// <summary>A 4×4 tapered vertical dither matrix.</summary>
		public static readonly FloatMatrix TaperedV =
			new FloatMatrix(4, 4, new float[]
			{
				0, 8, 4, 12,
				1, 9, 5, 13,
				2, 10, 6, 14,
				3, 11, 7, 15
			});

		/// <summary>A 4×4 tapered diagonal dither matrix (variant D).</summary>
		public static readonly FloatMatrix TaperedD =
			new FloatMatrix(4, 4, new float[]
			{
				0, 9, 6, 15,
				8, 5, 14, 3,
				4, 13, 2, 11,
				12, 1, 10, 7

			});

		/// <summary>A 4×4 tapered diagonal dither matrix (variant E).</summary>
		public static readonly FloatMatrix TaperedE =
			new FloatMatrix(4, 4, new float[]
			{
				15, 6, 9, 0,
				3, 14, 5, 8,
				11, 2, 13, 4,
				7, 10, 1, 12
			});

		/// <summary>A 4×4 hatch-pattern dither matrix (variant 1).</summary>
		public static readonly FloatMatrix Hatch1 =
			new FloatMatrix(4, 4, new float[]
			{
				0, 8, 4, 12,
				2, 10, 6, 14,
				1, 9, 5, 13,
				3, 11, 7, 15
			});

		/// <summary>A 4×4 hatch-pattern dither matrix (variant 2).</summary>
		public static readonly FloatMatrix Hatch2 =
			new FloatMatrix(4, 4, new float[]
			{
				4, 6, 3, 6,
				4, 7, 3, 7,
				4, 5, 3, 5,
				4, 7, 3, 7
			});

		/// <summary>A 4×4 hatch-pattern dither matrix (variant 3).</summary>
		public static readonly FloatMatrix Hatch3 =
			new FloatMatrix(4, 4, new float[]
			{
				4, 2, 0, 5,
				3, 2, 0, 3,
				1, 1, 0, 1,
				4, 2, 0, 6
			});

		/// <summary>A 4×4 hatch-pattern dither matrix (variant 4).</summary>
		public static readonly FloatMatrix Hatch4 =
			new FloatMatrix(4, 4, new float[]
			{
				4, 5, 2, 5,
				4, 3, 6, 2,
				2, 6, 6, 6,
				4, 2, 6, 3
			});

		/// <summary>A 4×4 hatch-pattern dither matrix (variant 5).</summary>
		public static readonly FloatMatrix Hatch5 =
			new FloatMatrix(4, 4, new float[]
			{
				2, 5, 5, 5,
				4, 3, 6, 2,
				4, 6, 2, 6,
				4, 2, 6, 3
			});

		/// <summary>A 6×6 hatch-pattern dither matrix (variant 6).</summary>
		public static readonly FloatMatrix Hatch6 =
			new FloatMatrix(6, 6, new float[]
			{
				0, 3, 3, 3, 3, 3,
				2, 1, 4, 4, 4, 0,
				2, 4, 1, 4, 0, 4,
				2, 4, 4, 0, 4, 4,
				2, 4, 0, 4, 1, 4,
				2, 0, 4, 4, 4, 1
			});

		/// <summary>
		/// A dictionary of all preset matrices, keyed by display name, suitable for use in editor popups.
		/// </summary>
		public static readonly Dictionary<string, FloatMatrix> All =
			new Dictionary<string, FloatMatrix>
			{
				{ "Checker 2×2", Checker },
				{ "Bayer 2×2", Bayer2 },
				{ "Bayer 4×4", Bayer4 },
				{ "Bayer 8×8", Bayer8 },
				
				{ "Bayer H4 (Horizontal)", BayerH4 },
				{ "Bayer V4 (Vertical)", BayerV4 },
				{ "Bayer D4 (Diagonal)", BayerD4 },
				{ "Bayer E4 (Diagonal)", BayerE4 },
				
				{ "Bayer H8 (Horizontal)", BayerH8 },
				{ "Bayer V8 (Vertical)", BayerV8 },
				{ "Bayer D8 (Diagonal)", BayerD8 },
				{ "Bayer E8 (Diagonal)", BayerE8 },
				
				{ "Dot 6×6", Dot },
				{ "Blue Noise 4×4", Blue },
				
				{ "Tapered H 4×4", TaperedH },
				{ "Tapered V 4×4", TaperedV },
				{ "Tapered D 4×4", TaperedD },
				{ "Tapered E 4×4", TaperedE },
				
				{ "Hatch1 4×4", Hatch1 },
				{ "Hatch2 4×4", Hatch2 },
				{ "Hatch3 4×4", Hatch3 },
				{ "Hatch4 4×4", Hatch4 },
				{ "Hatch5 4×4", Hatch5 },
				{ "Hatch6 6×6", Hatch6 }
			};
	}
}
