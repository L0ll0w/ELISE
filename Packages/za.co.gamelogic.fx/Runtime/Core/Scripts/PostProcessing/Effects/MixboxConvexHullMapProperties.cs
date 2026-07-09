using System;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the Mixbox convex-hull map post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class MixboxConvexHullMapProperties : ShaderProperties
	{
		private static readonly int MixBoxLUTID = Shader.PropertyToID("_MixboxLUT");
		private static readonly int LevelCountID = Shader.PropertyToID("_LevelCount");
		private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
		private const string PrimaryColorBaseName = "_PrimaryColor";

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.MixboxConvexHullMap;

		[Tooltip("The Mixbox LUT texture required for pigment-based color blending.")]
		[SerializeField] private Texture2D mixboxLUT = null;

		[Tooltip("Number of refinement iterations. Higher values produce stronger palette projection.")]
		[Min(0)]
		[SerializeField] private int levelCount = 3;

		[Tooltip("Background reference color used as the starting point for Mixbox convex-hull projection.")]
		[SerializeField] private Color backgroundColor = Constants.DefaultBackgroundColor;

		[Tooltip("The palette colors that define the convex hull. Pixel colors are blended toward these using Mixbox.")]
		[SerializeField] private ColorShaderPropertyList primaryColors = Constants.DefaultPrimaryColorsCopy;

		/// <summary>
		/// Gets or sets the number of refinement iterations used for Mixbox convex-hull projection.
		/// Higher values progressively move colors closer to the convex hull defined by the primary colors.
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
		/// for Mixbox convex-hull projection.
		/// </summary>
		public Color BackgroundColor
		{
			get => backgroundColor;
			set => backgroundColor = value;
		}

		/// <summary>
		/// Gets or sets the collection of primary colors that define the convex hull.
		/// Pixel colors are iteratively blended toward these palette anchors using Mixbox.
		/// </summary>
		public IEnumerable<Color> PrimaryColors
		{
			get => primaryColors;
			set => primaryColors.Colors = value?.Take(Constants.MaxPrimaryColors);
		}

		
		//public override void Awake() => UpdateBaseName();
		
		public override void OnEnable()
		{
			base.OnEnable();
			UpdateBaseName();
		}

		public override void OnValidate()
		{
			primaryColors.ValidateMaxCount(Constants.MaxPrimaryColors);
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
			effectMaterial.SetTexture(MixBoxLUTID, mixboxLUT);
			effectMaterial.SetColor(BackgroundColorID, backgroundColor);
			effectMaterial.SetColors(primaryColors);
		}
	}
}
