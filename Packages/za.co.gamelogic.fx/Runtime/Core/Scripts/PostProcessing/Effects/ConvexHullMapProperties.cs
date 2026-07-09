using System;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the convex-hull map post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class ConvexHullMapProperties : ShaderProperties
	{
		private static readonly int LevelCountID = Shader.PropertyToID("_LevelCount");
		private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
		private const string PrimaryColorBaseName = "_PrimaryColor";
		private const int MaxPrimaryColorCount = 10;

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.ConvexHullMap;

		[Tooltip("Number of refinement iterations. Higher values produce stronger palette projection.")]
		[Min(1)]
		[SerializeField] private int levelCount = 3;

		[Tooltip("Background reference color used as the starting point for convex-hull projection.")]
		[SerializeField] private Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		[Tooltip("The palette colors that define the convex hull. Pixel colors are iteratively projected toward these anchors.")]
		[SerializeField] private ColorShaderPropertyList primaryColors = Constants.DefaultPrimaryColorsCopy;

		/// <summary>
		/// Gets or sets the number of refinement iterations used for convex-hull projection.
		/// Higher values move colors progressively closer to the convex hull defined by the primary colors.
		/// </summary>
		public int LevelCount
		{
			get => levelCount;
			set
			{
				value.ThrowIfNotPositive(nameof(value));
				levelCount = value;
			}
		}

		/// <summary>
		/// Gets or sets the background reference color used as the starting point
		/// for the convex-hull projection process.
		/// </summary>
		public Color BackgroundColor
		{
			get => backgroundColor;
			set => backgroundColor = value;
		}

		/// <summary>
		/// Gets or sets the collection of primary colors that define the convex hull.
		/// Pixel colors are iteratively projected toward these palette anchors.
		/// </summary>
		public IEnumerable<Color> PrimaryColors
		{
			get => primaryColors;
			set => primaryColors.Colors = value?.Take(MaxPrimaryColorCount);
		}


		//public override void Awake() => UpdateBaseName();

		public override void OnEnable()
		{
			base.OnEnable();
			UpdateBaseName();
		}

		private void UpdateBaseName() => primaryColors.SetBaseName(PrimaryColorBaseName);

		public override void SetMaterialProperties(Material effectMaterial)
		{
#if UNITY_2021_2_OR_NEWER
			effectMaterial.SetInteger(LevelCountID, levelCount);
#else
			effectMaterial.SetInt(LevelCountID, levelCount);
#endif
			effectMaterial.SetColor(BackgroundColorID, backgroundColor);
			effectMaterial.SetColors(primaryColors);
		}

		public override void OnValidate()
		{
			primaryColors.ValidateMaxCount(MaxPrimaryColorCount);
			UpdateBaseName();
		}
	}
}
