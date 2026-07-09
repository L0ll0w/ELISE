using System;
using System.Collections.Generic;
using DelaunatorSharp;
using Gamelogic.Extensions;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gamelogic.Fx.Editor.TextureGenerator
{
	[ReuseCandidate]
	internal static class PoissonDiskSampler
	{
		/// <summary>
		/// Generates Poisson-disk samples inside a rectangle using Bridson's algorithm.
		/// </summary>
		/// <param name="width">Width of the region.</param>
		/// <param name="height">Height of the region.</param>
		/// <param name="radius">Minimum distance between points.</param>
		/// <param name="k">Number of attempts for each active point.</param>

		public static List<Vector2> Generate(float width, float height, float radius, int k = 30)
		{
			float cellSize = radius / Mathf.Sqrt(2f);

			int gridWidth = Mathf.CeilToInt(width / cellSize);
			int gridHeight = Mathf.CeilToInt(height / cellSize);

			var grid = new Vector2?[gridWidth * gridHeight];
			var samples = new List<Vector2>();
			var activeList = new List<Vector2>();

			int Index(int x, int y) => y * gridWidth + x;

			var first = new Vector2(Random.value * width, Random.value * height);

			samples.Add(first);
			activeList.Add(first);

			int gx = (int)(first.x / cellSize);
			int gy = (int)(first.y / cellSize);
			grid[Index(gx, gy)] = first;

			while (activeList.Count > 0)
			{
				int idx = Random.Range(0, activeList.Count);
				Vector2 center = activeList[idx];
				bool found = false;

				for (int i = 0; i < k; i++)
				{
					float angle = Random.value * Mathf.PI * 2f;
					float dist = radius * (1f + Random.value);
					Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

					if (candidate.x < 0 || candidate.x >= width || candidate.y < 0 || candidate.y >= height)
					{
						continue;
					}
					
					int cgx = (int)(candidate.x / cellSize);
					int cgy = (int)(candidate.y / cellSize);

					bool ok = true;

					for (int y = Mathf.Max(0, cgy - 2); y <= Mathf.Min(gridHeight - 1, cgy + 2); y++)
					{
						for (int x = Mathf.Max(0, cgx - 2); x <= Mathf.Min(gridWidth - 1, cgx + 2); x++)
						{
							var p = grid[Index(x, y)];
						
							if (p.HasValue)
							{
								if ((p.Value - candidate).sqrMagnitude < radius * radius)
								{
									ok = false;
									break;
								}
							}
						}
						if (!ok) break;
					}

					if (ok)
					{
						samples.Add(candidate);
						activeList.Add(candidate);
						grid[Index(cgx, cgy)] = candidate;
						found = true;
						break;
					}
				}

				// If no point found, remove from active list
				if (!found)
				{
					activeList.RemoveAt(idx);
				}
			}

			return samples;
		}
	}

	[ReuseCandidate]
	internal static class DSatur
	{
		/// <summary>
		/// Colors the graph using the DSATUR algorithm.
		/// </summary>
		/// <param name="adjacency">Adjacency list for each vertex.</param>
		/// <param name="colorCount">The number of colors available.</param>
		/// <returns>An array where each entry is a color index in the range <c>[0, colorCount-1]</c>.</returns>
		public static int[] ColorGraph(List<HashSet<int>> adjacency, int colorCount)
		{
			int n = adjacency.Count;

			var colors = new int[n];
			for (int i = 0; i < n; i++)
				colors[i] = -1; // uncolored

			// Saturation degree (# of distinct neighbor colors)
			var saturation = new int[n];

			// Degree (precomputed)
			var degree = new int[n];
			for (int i = 0; i < n; i++)
				degree[i] = adjacency[i].Count;

			// Step 1: pick highest-degree vertex
			int current = 0;
			for (int i = 1; i < n; i++)
				if (degree[i] > degree[current])
					current = i;

			for (int step = 0; step < n; step++)
			{
				// Color current vertex with the smallest available color
				colors[current] = ChooseColor(current, adjacency, colors, colorCount);

				// Update saturation of neighbors
				foreach (int nb in adjacency[current])
				{
					if (colors[nb] == -1)
						saturation[nb] = CountDistinctNeighborColors(nb, adjacency, colors);
				}

				// Pick next vertex: highest saturation, ties broken by degree
				current = -1;
				int bestSat = -1;
				int bestDeg = -1;

				for (int i = 0; i < n; i++)
				{
					if (colors[i] != -1) continue;

					int sat = saturation[i];
					int deg = degree[i];

					if (sat > bestSat || (sat == bestSat && deg > bestDeg))
					{
						bestSat = sat;
						bestDeg = deg;
						current = i;
					}
				}

				if (current == -1)
					break; // all colored
			}

			return colors;
		}

		private static int ChooseColor(int v, List<HashSet<int>> adjacency, int[] colors, int colorCount)
		{
			// mark forbidden colors
			bool[] forbidden = new bool[colorCount];

			foreach (int nb in adjacency[v])
			{
				int c = colors[nb];
				if (c >= 0 && c < colorCount)
					forbidden[c] = true;
			}

			for (int c = 0; c < colorCount; c++)
				if (!forbidden[c])
					return c;

			// should never happen for planar graph with colorCount >= 4
			return 0;
		}

		private static int CountDistinctNeighborColors(int v, List<HashSet<int>> adjacency, int[] colors)
		{
			var seen = new HashSet<int>();

			foreach (int nb in adjacency[v])
			{
				int c = colors[nb];
				if (c != -1)
					seen.Add(c);
			}

			return seen.Count;
		}
	}

	/// <summary>
	/// Helpers for constructing and rasterizing Voronoi diagrams from a Delaunay triangulation.
	/// </summary>
	[ReuseCandidate]
	internal static class VoronoiHelper
	{
		/// <summary>
		/// Builds a list of Voronoi polygons from the given Delaunay triangulation, clamped to the specified bounds.
		/// </summary>
		/// <param name="delaunator">The Delaunay triangulation.</param>
		/// <param name="minX">The minimum X bound for clamping circumcenters.</param>
		/// <param name="minY">The minimum Y bound for clamping circumcenters.</param>
		/// <param name="maxX">The maximum X bound for clamping circumcenters.</param>
		/// <param name="maxY">The maximum Y bound for clamping circumcenters.</param>
		/// <returns>A list of polygon vertex lists, one per Delaunay point.</returns>
		public static List<List<Vector2>> BuildVoronoiPolygons(Delaunator delaunator, float minX, float minY, float maxX, float maxY)
		{
			int triangleCount = delaunator.Triangles.Length / 3;

			// Compute circumcenters for all triangles
			Vector2[] circumcenters = new Vector2[triangleCount];
			for (int t = 0; t < triangleCount; t++)
			{
				circumcenters[t] = CircumcenterOfTriangle(delaunator, t);
			}

			int pointCount = delaunator.Points.Length;

			var polygons = new List<List<Vector2>>(pointCount);
			for (int i = 0; i < pointCount; i++)
			{
				polygons.Add(new List<Vector2>());
			}

			// For each point, gather adjacent triangles
			var perSiteCenters = new List<(Vector2 cc, float angle)>[pointCount];
			for (int i = 0; i < pointCount; i++)
			{
				perSiteCenters[i] = new List<(Vector2, float)>();
			}

			int[] triangles = delaunator.Triangles;

			for (int triIndex = 0; triIndex < triangleCount; triIndex++)
			{
				int a = triangles[triIndex * 3 + 0];
				int b = triangles[triIndex * 3 + 1];
				int c = triangles[triIndex * 3 + 2];

				Vector2 cc = circumcenters[triIndex];

				perSiteCenters[a].Add((cc, 0));
				perSiteCenters[b].Add((cc, 0));
				perSiteCenters[c].Add((cc, 0));
			}

			// Compute angles around each site + sort CCW
			for (int i = 0; i < pointCount; i++)
			{
				Vector2 p = new Vector2((float)delaunator.Points[i].X, (float)delaunator.Points[i].Y);

				for (int k = 0; k < perSiteCenters[i].Count; k++)
				{
					var (cc, _) = perSiteCenters[i][k];
					float angle = Mathf.Atan2(cc.y - p.y, cc.x - p.x);
					perSiteCenters[i][k] = (cc, angle);
				}

				perSiteCenters[i].Sort((a, b) => a.angle.CompareTo(b.angle));

				
				foreach (var entry in perSiteCenters[i])
				{
					polygons[i].Add(Clamp(entry.cc));
				}
			}

			return polygons;

			Vector2 Clamp(Vector2 v)
			{
				v.x = Mathf.Clamp(v.x, minX, maxX);
				v.y = Mathf.Clamp(v.y, minY, maxY);
				return v;
			}
		}

		private static Vector2 CircumcenterOfTriangle(Delaunator delaunator, int triangleIndex)
		{
			int t0 = delaunator.Triangles[triangleIndex * 3 + 0];
			int t1 = delaunator.Triangles[triangleIndex * 3 + 1];
			int t2 = delaunator.Triangles[triangleIndex * 3 + 2];

			var a = new Vector2((float)delaunator.Points[t0].X, (float)delaunator.Points[t0].Y);
			var b = new Vector2((float)delaunator.Points[t1].X, (float)delaunator.Points[t1].Y);
			var c = new Vector2((float)delaunator.Points[t2].X, (float)delaunator.Points[t2].Y);

			return Circumcenter(a, b, c);
		}

		private static Vector2 Circumcenter(Vector2 a, Vector2 b, Vector2 c)
		{
			float d = 2 * (
				a.x * (b.y - c.y) 
				+ b.x * (c.y - a.y) 
				+ c.x * (a.y - b.y));

			float ux = (
				(a.x * a.x + a.y * a.y) * (b.y - c.y)
				+ (b.x * b.x + b.y * b.y) * (c.y - a.y)
				+ (c.x * c.x + c.y * c.y) * (a.y - b.y)) / d;

			float uy = (
				(a.x * a.x + a.y * a.y) * (c.x - b.x) 
				+ (b.x * b.x + b.y * b.y) * (a.x - c.x) 
				+ (c.x * c.x + c.y * c.y) * (b.x - a.x)) / d;

			return new Vector2(ux, uy);
		}

		/// <summary>
		/// Rasterizes Voronoi polygons into a texture, coloring each region from the given palette.
		/// </summary>
		/// <param name="polygons">The Voronoi polygon vertex lists.</param>
		/// <param name="textureSize">The output texture dimensions in pixels.</param>
		/// <param name="colorIndices">Color index per polygon, indexing into <paramref name="palette"/>.</param>
		/// <param name="palette">The palette of colors to use.</param>
		/// <returns>The rasterized texture.</returns>
		public static Texture2D Rasterize(List<List<Vector2>> polygons, Vector2Int textureSize, int[] colorIndices, Color[] palette)
		{
			var texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
			var buffer = new Color[textureSize.x * textureSize.y];

			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = Color.black;
			}

			var regionColors = new Color[polygons.Count];
			for (int i = 0; i < polygons.Count; i++)
			{
				regionColors[i] = ColorFromIndex(colorIndices[i], palette);
			}

			for (int i = 0; i < polygons.Count; i++)
			{
				var poly = polygons[i];
				if (poly.Count < 3) continue;

				var o = poly[0];

				for (int k = 1; k < poly.Count - 1; k++)
				{
					RasterTriangle(o, poly[k], poly[k + 1], regionColors[i], buffer, textureSize);
				}
			}

			texture.SetPixels(buffer);
			texture.Apply();
			return texture;
		}

		private static void RasterTriangle(
			Vector2 a, 
			Vector2 b, 
			Vector2 c, 
			Color color, 
			Color[] buffer,
			Vector2Int textureSize)
		{
			int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
			int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
			int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
			int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
			
			minX = Mathf.Clamp(minX, 0, textureSize.x - 1);
			maxX = Mathf.Clamp(maxX, 0, textureSize.x - 1);
			minY = Mathf.Clamp(minY, 0, textureSize.y - 1);
			maxY = Mathf.Clamp(maxY, 0, textureSize.y - 1);

			for (int y = minY; y <= maxY; y++)
			{
				for (int x = minX; x <= maxX; x++)
				{
					var point = new Vector2(x + 0.5f, y + 0.5f);

					if (PointInTriangle(point, a, b, c))
					{
						buffer[y * textureSize.x + x] = color;
					}
				}
			}
		}

		private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
		{
			float dX = point.x - c.x;
			float dY = point.y - c.y;
			float dX21 = b.x - c.x;
			float dY21 = b.y - c.y;
			float dX31 = a.x - c.x;
			float dY31 = a.y - c.y;

			float denom = dY21 * dX31 - dX21 * dY31;
			float v = (dY21 * dX - dX21 * dY) / denom;
			float w = (dX31 * dY - dY31 * dX) / denom;
			float u = 1f - v - w;

			return (u >= 0f && v >= 0f && w >= 0f);
		}
		
		/// <summary>
		/// Builds a graph adjacency list from the Delaunay triangulation's half-edge structure.
		/// </summary>
		/// <param name="d">The Delaunay triangulation.</param>
		/// <returns>A list of adjacency sets, one per point.</returns>
		public static List<HashSet<int>> BuildAdjacency(Delaunator d)
		{
			int n = d.Points.Length;
			var adjacents = new List<HashSet<int>>(n);

			for (int i = 0; i < n; i++)
			{
				adjacents.Add(new HashSet<int>());
			}

			int[] triangles = d.Triangles;
			int[] halfedges = d.Halfedges;

			int triangleCount = triangles.Length;

			for (int e = 0; e < triangleCount; e++)
			{
				int p0 = triangles[e];

				int opposite = halfedges[e];
				
				if (opposite != -1)
				{
					int p1 = triangles[opposite];
					if (p0 != p1)
					{
						adjacents[p0].Add(p1);
						adjacents[p1].Add(p0);
					}
				}
			}

			return adjacents;
		}

		private static Color ColorFromIndex(int index, Color[] palette)
		{
			if (index < 0 || index >= palette.Length)
			{
				return Color.magenta;
			}

			return palette[index];
		}
	}
	
	
	internal class Vector2Point : IPoint
	{
		public double X { get; set; }
		public double Y { get; set; }

		public Vector2Point(Vector2 v)
		{
			X = v.x;
			Y = v.y;
		}
	}
	
	/// <summary>
	/// Editor window for generating checkerboard textures.
	/// </summary>
	/// <remarks>
	/// This tool can be accessed from the menu: Gamelogic|Tools|Texture Generation|Pattern Texture Generator.
	/// </remarks>
	public sealed class PatternTextureGenerator : TextureGeneratorWindow
	{
		private enum PatternType
		{
			WhiteNoise,
			MultiChannelWhiteNoise,
			ColorListNoise,
			CheckerBoard,
			Grid,
			HueLightnessSheet,
			SineGrid,
			MultiSineGrid,
			ValueNoise,
			MultiChannelValueNoise,
			Voronoi
		}

		private enum ChannelView
		{
			All,
			Red,
			Green,
			Blue
		}

		private const string ToolName = "Pattern Texture Generator";
		private const string ToolMenuPath = TextureGenerationRoot + ToolName;
		private PatternType patternType = PatternType.CheckerBoard;
		private ChannelView channelView = ChannelView.All;
		private Vector2Int cellDimensions = new Vector2Int(32, 32);
		private Vector2Int thickness = new Vector2Int(2, 2);
		private Color color1 = Color.black;
		private Color color2 = Color.white;
		private float multiChannelMin = 0f;
		private float multiChannelMax = 1f;
		private Color colorListBackgroundColor;
		
		private float poissonRadius = 50f;

		private readonly List<Color> colorListForegroundColors =
			new List<Color>() { Color.red, Color.green, Color.blue };

		private float colorListForegroundProbability = 0.1f; // [0, 1]
		private float frequency = 1f;
		private int frequencyPower = 2;
		private float log10Gamma = 0;

		// Multi-sine parameters
		private float frequency1 = 1f;
		private float frequency2 = 2f;
		private float frequency3 = 4f;

		private float amplitude1 = 4/7f;
		private float amplitude2 = 2/7f;
		private float amplitude3 = 1/7f;

		private Vector2 offset1 = Vector2.zero;
		private Vector2 offset2 = new Vector2(.333f, .666f);
		private Vector2 offset3 = new Vector2(.666f, .333f);

		[MenuItem(ToolMenuPath)]
		public static void ShowWindow() => GetWindow<PatternTextureGenerator>(ToolName);

		protected override void DrawPropertiesGui()
		{
			Header("Colors");

			patternType = (PatternType)EditorGUILayout.EnumPopup("Pattern Type", patternType);

			Header("Pattern Settings");

			bool isTwoColorPattern =
				patternType == PatternType.CheckerBoard ||
				patternType == PatternType.Grid ||
				patternType == PatternType.SineGrid ||
				patternType == PatternType.MultiSineGrid ||
				patternType == PatternType.ValueNoise;
			
			if (isTwoColorPattern)
			{
				color1 = EditorGUILayout.ColorField("Color 1", color1);
				color2 = EditorGUILayout.ColorField("Color 2", color2);
			}

			switch (patternType)
			{
				case PatternType.CheckerBoard:
				case PatternType.Grid:
					cellDimensions = EditorGUILayout.Vector2IntField("Cell Dimensions", cellDimensions);
					break;
			}

			if (patternType == PatternType.Grid)
			{
				thickness = EditorGUILayout.Vector2IntField("Line Thickness", thickness);
			}

			if (patternType == PatternType.SineGrid)
			{
				frequency = EditorGUILayout.FloatField("Frequency", frequency);
			}

			if (patternType == PatternType.MultiSineGrid)
			{
				EditorGUILayout.LabelField("Sine 1", EditorStyles.boldLabel);
				frequency1 = EditorGUILayout.FloatField("Frequency 1", frequency1);
				amplitude1 = EditorGUILayout.FloatField("Amplitude 1", amplitude1);
				offset1 = EditorGUILayout.Vector2Field("Offset 1", offset1);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Sine 2", EditorStyles.boldLabel);
				frequency2 = EditorGUILayout.FloatField("Frequency 2", frequency2);
				amplitude2 = EditorGUILayout.FloatField("Amplitude 2", amplitude2);
				offset2 = EditorGUILayout.Vector2Field("Offset 2", offset2);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Sine 3", EditorStyles.boldLabel);
				frequency3 = EditorGUILayout.FloatField("Frequency 3", frequency3);
				amplitude3 = EditorGUILayout.FloatField("Amplitude 3", amplitude3);
				offset3 = EditorGUILayout.Vector2Field("Offset 3", offset3);
			}

			if (patternType == PatternType.MultiChannelWhiteNoise)
			{
				DrawMinMaxSlider();

				EditorGUILayout.BeginHorizontal();
				DrawChannelViewButton(ChannelView.All);
				DrawChannelViewButton(ChannelView.Red);
				DrawChannelViewButton(ChannelView.Green);
				DrawChannelViewButton(ChannelView.Blue);
				EditorGUILayout.EndHorizontal();
			}
			
			if (patternType == PatternType.Voronoi)
			{
				Header("Voronoi Settings");

				poissonRadius = EditorGUILayout.FloatField("Poisson Radius", poissonRadius);
				DrawColorList();
			}


			if (patternType == PatternType.ColorListNoise)
			{
				colorListBackgroundColor = EditorGUILayout.ColorField("Background Color", colorListBackgroundColor);
				DrawColorList();
				colorListForegroundProbability = EditorGUILayout.Slider("Foreground Probability",
					colorListForegroundProbability, leftValue: 0f, rightValue: 1f);
			}

			if (patternType == PatternType.ValueNoise || patternType == PatternType.MultiChannelValueNoise)
			{
				frequencyPower = EditorGUILayout.IntSlider("Frequency Power", frequencyPower, 0, 12);
				log10Gamma = EditorGUILayout.Slider("Log Gamma", log10Gamma, -1, 1);
			}

			Header("Output Settings");
		}

		private void DrawColorList()
		{
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Foreground Colors", EditorStyles.boldLabel);

			for (int i = colorListForegroundColors.Count - 1; i >= 0; i--)
			{
				EditorGUILayout.BeginHorizontal();
				int count = colorListForegroundColors.Count;
				colorListForegroundColors[i] =
					EditorGUILayout.ColorField($"Foreground Color {count - i}", colorListForegroundColors[i]);

				if (GUILayout.Button("-", GUILayout.Width(20)) && colorListForegroundColors.Count > 1)
				{
					colorListForegroundColors.RemoveAt(i);
				}

				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("+ Add Color"))
			{
				colorListForegroundColors.Insert(0, Color.white);
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawMinMaxSlider()
		{
			const float minLimit = -1f;
			const float maxLimit = 1f;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Value Range", GUILayout.Width(90));

			multiChannelMin = EditorGUILayout.FloatField(multiChannelMin, GUILayout.Width(50));
			EditorGUILayout.MinMaxSlider(ref multiChannelMin, ref multiChannelMax, minLimit, maxLimit);
			multiChannelMax = EditorGUILayout.FloatField(multiChannelMax, GUILayout.Width(50));

			EditorGUILayout.EndHorizontal();

			// Ensure min ≤ max. Swap if values are inverted.
			if (multiChannelMin > multiChannelMax)
			{
				(multiChannelMin, multiChannelMax) = (multiChannelMax, multiChannelMin);
			}

			multiChannelMin = Mathf.Clamp(multiChannelMin, minLimit, maxLimit);
			multiChannelMax = Mathf.Clamp(multiChannelMax, minLimit, maxLimit);
		}

		private void DrawChannelViewButton(ChannelView view)
		{
			if (GUILayout.Button(view.ToString()))
			{
				channelView = view;
			}
		}

		protected override Texture2D GenerateTexture(Vector2Int dimensions)
		{
			switch (patternType)
			{
				case PatternType.CheckerBoard:
					return GenerateTexture_CheckerBoard(dimensions);
				case PatternType.Grid:
					return GenerateTexture_Grid(dimensions);
				case PatternType.WhiteNoise:
					return GenerateTexture_WhiteNoise(dimensions);
				case PatternType.MultiChannelWhiteNoise:
					return GenerateTexture_MultiChannelWhiteNoise(dimensions);
				case PatternType.ColorListNoise:
					return GenerateTexture_ColorListNoise(dimensions);
				case PatternType.HueLightnessSheet:
					return GenerateTexture_HueLightnessSheet(dimensions);
				case PatternType.SineGrid:
					return GenerateTexture_SineGrid(dimensions);
				case PatternType.MultiSineGrid:
					return GenerateTexture_MultiSineGrid(dimensions);
				case PatternType.ValueNoise:
					return GenerateTexture_ValueNoise(dimensions);
				case PatternType.MultiChannelValueNoise:
					return GenerateTexture_MultiChannelValueNoise(dimensions);
				case PatternType.Voronoi:
					return GenerateTexture_Voronoi(dimensions);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Texture2D GenerateTexture_ValueNoise(Vector2Int dimensions)
		{
			int textureWidth = dimensions.x;
			int textureHeight = dimensions.y;

			var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
			int cellSizeX = Mathf.Max(1, textureWidth / (1 << (frequencyPower)));
			int cellSizeY = Mathf.Max(1, textureHeight / (1 << (frequencyPower)));

			int cellCountX = textureWidth / cellSizeX;
			int cellCountY = textureHeight / cellSizeY;

			float[,] whiteNoise = new float[cellCountX, cellCountY];
			
			SampleNoise();
			Interpolate();

			texture.Apply();
			texture.wrapMode = TextureWrapMode.Repeat;

			return texture;

			void SampleNoise()
			{
				float invGamma = Mathf.Pow(10, -log10Gamma);
				
				for (int j = 0; j < cellCountY; j++)
				{
					for (int i = 0; i < cellCountX; i++)
					{
						whiteNoise[i, j] = Mathf.Pow(Random.value, invGamma);
					}
				}
			}

			void Interpolate()
			{
				for (int pixelY = 0; pixelY < textureHeight; pixelY++)
				{
					for (int pixelX = 0; pixelX < textureWidth; pixelX++)
					{
						int cellX = pixelX / cellSizeX;
						int cellY = pixelY / cellSizeY;

						int nextCellX = (cellX + 1) % cellCountX;
						int nextCellY = (cellY + 1) % cellCountY;

						float bottomLeft = whiteNoise[cellX, cellY];
						float bottomRight = whiteNoise[nextCellX, cellY];
						float topLeft = whiteNoise[cellX, nextCellY];
						float topRight = whiteNoise[nextCellX, nextCellY];

						float cellFracPartX = (pixelX % cellSizeX) / (float) cellSizeX;
						float cellFracPartY = (pixelY % cellSizeY) / (float) cellSizeY;

						float value = GLMathf.Bilerp(
							bottomLeft,
							bottomRight,
							topLeft,
							topRight,
							cellFracPartX,
							cellFracPartY);

						texture.SetPixel(pixelX, pixelY, Color.Lerp(color1, color2, value));
					}
				}
			}
		}

		private Texture2D GenerateTexture_MultiChannelValueNoise(Vector2Int dimensions)
		{
			int textureWidth = dimensions.x;
			int textureHeight = dimensions.y;

			var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

			int cellSizeX = Mathf.Max(1, textureWidth / (1 << (frequencyPower)));
			int cellSizeY = Mathf.Max(1, textureHeight / (1 << (frequencyPower)));

			int cellCountX = textureWidth / cellSizeX;
			int cellCountY = textureHeight / cellSizeY;

			float[,] whiteNoiseR = new float[cellCountX, cellCountY];
			float[,] whiteNoiseG = new float[cellCountX, cellCountY];
			float[,] whiteNoiseB = new float[cellCountX, cellCountY];
			
			SampleNoise();
			
			Interpolate();

			texture.Apply();
			texture.wrapMode = TextureWrapMode.Repeat;

			return texture;

			void SampleNoise()
			{
				var gammaInv = Mathf.Pow(10, -log10Gamma);
				
				for (int j = 0; j < cellCountY; j++)
				{
					for (int i = 0; i < cellCountX; i++)
					{
						whiteNoiseR[i, j] = Mathf.Pow(Random.value, gammaInv);
						whiteNoiseG[i, j] = Mathf.Pow(Random.value, gammaInv);
						whiteNoiseB[i, j] = Mathf.Pow(Random.value, gammaInv);
					}
				}
			}

			void Interpolate()
			{
				for (int pixelY = 0; pixelY < textureHeight; pixelY++)
				{
					for (int pixelX = 0; pixelX < textureWidth; pixelX++)
					{
						int cellX = pixelX / cellSizeX;
						int cellY = pixelY / cellSizeY;

						int nextCellX = (cellX + 1) % cellCountX;
						int nextCellY = (cellY + 1) % cellCountY;

						float cellFracPartX = (pixelX % cellSizeX) / (float)cellSizeX;
						float cellFracPartY = (pixelY % cellSizeY) / (float)cellSizeY;

						float r = GLMathf.Bilerp(
							whiteNoiseR[cellX, cellY],
							whiteNoiseR[nextCellX, cellY],
							whiteNoiseR[cellX, nextCellY],
							whiteNoiseR[nextCellX, nextCellY],
							cellFracPartX, cellFracPartY);

						float g = GLMathf.Bilerp(
							whiteNoiseG[cellX, cellY],
							whiteNoiseG[nextCellX, cellY],
							whiteNoiseG[cellX, nextCellY],
							whiteNoiseG[nextCellX, nextCellY],
							cellFracPartX, cellFracPartY);

						float b = GLMathf.Bilerp(
							whiteNoiseB[cellX, cellY],
							whiteNoiseB[nextCellX, cellY],
							whiteNoiseB[cellX, nextCellY],
							whiteNoiseB[nextCellX, nextCellY],
							cellFracPartX, cellFracPartY);

						texture.SetPixel(pixelX, pixelY, new Color(r, g, b, 1f));
					}
				}
			}
		}

		private Texture2D GenerateTexture_CheckerBoard(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int x = 0; x < dimensions.x; x++)
			{
				for (int y = 0; y < dimensions.y; y++)
				{
					bool isColor1 = ((x / cellDimensions.x) + (y / cellDimensions.y)) % 2 == 0;
					texture.SetPixel(x, y, isColor1 ? color1 : color2);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}
		
		private Texture2D GenerateTexture_Grid(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int y = 0; y < dimensions.y; y++)
			{
				for (int x = 0; x < dimensions.x; x++)
				{
					int cellX = x % cellDimensions.x;
					int cellY = y % cellDimensions.y;

					bool isLine = cellX < thickness.x || cellY < thickness.y;
					texture.SetPixel(x, y, isLine ? color2 : color1);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}


		private Texture2D GenerateTexture_WhiteNoise(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int x = 0; x < dimensions.x; x++)
			{
				for (int y = 0; y < dimensions.y; y++)
				{
					float randomValue = Random.value; // Random value between 0 and 1
					Color randomColor = Color.Lerp(color1, color2, randomValue);
					texture.SetPixel(x, y, randomColor);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}

		private Texture2D GenerateTexture_MultiChannelWhiteNoise(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, mipChain: false);

			for (int x = 0; x < dimensions.x; x++)
			{
				for (int y = 0; y < dimensions.y; y++)
				{
					float randomRedValue = Mathf.Lerp(multiChannelMin, multiChannelMax, Random.value);
					float randomGreenValue = Mathf.Lerp(multiChannelMin, multiChannelMax, Random.value);
					float randomBlueValue = Mathf.Lerp(multiChannelMin, multiChannelMax, Random.value);

					Color randomColor;
					switch (channelView)
					{
						case ChannelView.Red:
							randomColor = new Color(randomRedValue, 0f, 0f, 1f);
							break;
						case ChannelView.Green:
							randomColor = new Color(0f, randomGreenValue, 0f, 1f);
							break;
						case ChannelView.Blue:
							randomColor = new Color(0f, 0f, randomBlueValue, 1f);
							break;
						default:
							randomColor = new Color(randomRedValue, randomGreenValue, randomBlueValue, 1f);
							break;
					}

					texture.SetPixel(x, y, randomColor);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();

			return texture;
		}

		private Texture2D GenerateTexture_ColorListNoise(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, mipChain: false);
			var texturePixels = new Color[texture.width * texture.height];
			int pixelCount = texturePixels.Length;
			int foregroundColorsCount = colorListForegroundColors.Count;

			for (int i = 0; i < pixelCount; i++)
			{
				if (Random.value < colorListForegroundProbability)
				{
					texturePixels[i] = colorListForegroundColors[Random.Range(0, foregroundColorsCount)];
				}
				else
				{
					texturePixels[i] = colorListBackgroundColor;
				}
			}

			texture.SetPixels(texturePixels);
			texture.Apply();

			return texture;
		}

		private Texture2D GenerateTexture_HueLightnessSheet(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int x = 0; x < dimensions.x; x++)
			{
				for (int y = 0; y < dimensions.y; y++)
				{
					float u = x / (dimensions.x - 1f);
					float v = y / (dimensions.y - 1f);
					var hsl = Color.HSVToRGB(u, 1f, 1f);

					var color = v < 0.5
						? Color.Lerp(Color.black, hsl, v * 2f)
						: Color.Lerp(hsl, Color.white, (v - 0.5f) * 2f);

					texture.SetPixel(x, y, color);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}

		private Texture2D GenerateTexture_SineGrid(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			for (int j = 0; j < dimensions.y; j++)
			{
				for (int i = 0; i < dimensions.x; i++)
				{
					float x = (float)i / dimensions.x;
					float y = (float)j / dimensions.y;

					float value = Mathf.Sin(2 * Mathf.PI * frequency * x) * Mathf.Sin(2 * Mathf.PI * frequency * y);
					value = (value + 1f) * 0.5f; // Normalize to [0, 1]

					Color color = Color.Lerp(color1, color2, value);
					texture.SetPixel(i, j, color);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}

		private Texture2D GenerateTexture_MultiSineGrid(Vector2Int dimensions)
		{
			var texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);

			float angle1 = 2 * Mathf.PI * frequency1;
			float angle2 = 2 * Mathf.PI * frequency2;
			float angle3 = 2 * Mathf.PI * frequency3;
			
			for (int j = 0; j < dimensions.y; j++)
			{
				for (int i = 0; i < dimensions.x; i++)
				{
					float x = (float)i / dimensions.x;
					float y = (float)j / dimensions.y;

					float v1 = amplitude1 * Mathf.Sin(angle1 * x + offset1.x) * Mathf.Sin(angle1 * y + offset1.y);
					float v2 = amplitude2 * Mathf.Sin(angle2 * x + offset2.x) * Mathf.Sin(angle2 * y + offset2.y);
					float v3 = amplitude3 * Mathf.Sin(angle3 * x + offset3.x) * Mathf.Sin(angle3 * y + offset3.y);

					float value = v1 + v2 + v3;
					value = (value + 1f) * 0.5f;

					var c = Color.Lerp(color1, color2, value);
					texture.SetPixel(i, j, c);
				}
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			return texture;
		}
		
		private Texture2D GenerateTexture_Voronoi(Vector2Int dimensions)
		{
			float margin = poissonRadius * 4f;

			float minX = -margin;
			float minY = -margin;
			float maxX = dimensions.x + margin;
			float maxY = dimensions.y + margin;

			var sites = PoissonDiskSampler.Generate(
				dimensions.x + 2 * margin,
				dimensions.y + 2 * margin,
				poissonRadius
			);

			for (int i = 0; i < sites.Count; i++)
				sites[i] += new Vector2(minX, minY);

			var pts = new IPoint[sites.Count];
			for (int i = 0; i < sites.Count; i++)
				pts[i] = new Vector2Point(sites[i]);

			var d = new Delaunator(pts);

			var adjacency = VoronoiHelper.BuildAdjacency(d);
			var colorIndices = DSatur.ColorGraph(adjacency, colorListForegroundColors.Count);

			var polys = VoronoiHelper.BuildVoronoiPolygons(d, minX, minY, maxX, maxY);

			return VoronoiHelper.Rasterize(polys, dimensions, colorIndices, colorListForegroundColors.ToArray());
		}

	}
}
