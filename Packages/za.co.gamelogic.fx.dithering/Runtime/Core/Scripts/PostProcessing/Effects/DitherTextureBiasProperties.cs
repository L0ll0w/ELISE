using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Dithering.Internal;
using Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal;
using Gamelogic.Fx.PostProcessing;
using Unity.Mathematics;
using UnityEngine;
using ShaderPropertyThrowHelper = Gamelogic.Fx.Internal.ShaderPropertyThrowHelper;

namespace Gamelogic.Fx.Dithering.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the dither texture bias post-processing effect.
	/// </summary>
	[Serializable]
	public class DitherTextureBiasProperties : ShaderProperties
	{
		private static readonly int DitherAmountMinID = Shader.PropertyToID("_DitherAmountMin");
		private static readonly int DitherAmountMaxID = Shader.PropertyToID("_DitherAmountMax");
		private static readonly int LevelCountID = Shader.PropertyToID("_LevelCount");
		private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
		private static readonly int MipMapBiasID = Shader.PropertyToID("_MipMapBias");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.DitherTextureBias;

		[Tooltip("The dither pattern texture and its tiling settings.")]
		[SerializeField] private TextureTiling ditherPattern = new();

		[Tooltip("Mip map bias applied when sampling the dither pattern texture.")]
		[SerializeField] private float mipMapBias = 0.5f;

		[Tooltip("Minimum dither bias added to the color before quantization.")]
		[Range(-1f, 1f)]
		[SerializeField] private float ditherAmountMin = -0.5f;

		[Tooltip("Maximum dither bias added to the color before quantization.")]
		[Range(-1f, 1f)]
		[SerializeField] private float ditherAmountMax = 0.5f;

		[Tooltip("Number of quantization levels per color channel (R, G, B).")]
		[SerializeField] private int3 levelCount = new int3(2, 2, 2);

		[Tooltip("Controls how smoothly the dither transitions between quantization levels.")]
		[Range(0f, 1f)]
		[SerializeField] private float smoothness = 0f;

		[Header("UV Mapping")]
		[Tooltip("Whether to use a UV map texture to drive screen-space UV coordinates.")]
		[SerializeField] private bool useUvMap = false;
		[Tooltip("The UV map texture and tiling settings.")]
		[SerializeField] private TextureTiling uvMap = null;
		[Tooltip("Whether to apply depth-based compensation to the UV map scale.")]
		[SerializeField] private bool applyDepthCompensation = false;

		/// <summary>Gets or sets the dither pattern texture.</summary>
		public Texture DitherPatternTexture
		{
			get => ditherPattern.Texture;
			set => ditherPattern.Texture = value;
		}

		/// <summary>Gets or sets the tiling scale of the dither pattern texture.</summary>
		public Vector2 DitherPatternTilingScale
		{
			get => ditherPattern.TilingScale;
			set => ditherPattern.TilingScale = value;
		}

		/// <summary>Gets or sets the tiling offset of the dither pattern texture.</summary>
		public Vector2 DitherPatternTilingOffset
		{
			get => ditherPattern.TilingOffset;
			set => ditherPattern.TilingOffset = value;
		}

		/// <summary>Gets or sets the number of quantization levels per color channel.</summary>
		public int3 LevelCount
		{
			get => levelCount;
			set => levelCount = value;
		}

		/// <summary>Gets or sets the minimum dither bias, in the range <c>[-1, 1]</c>.</summary>
		public float DitherAmountMin
		{
			get => ditherAmountMin;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));
				ditherAmountMin = value;
			}
		}

		/// <summary>Gets or sets the maximum dither bias, in the range <c>[-1, 1]</c>.</summary>
		public float DitherAmountMax
		{
			get => ditherAmountMax;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));
				ditherAmountMax = value;
			}
		}

		/// <summary>Gets or sets the smoothness of the dither transition, in the range <c>[0, 1]</c>.</summary>
		public float Smoothness
		{
			get => smoothness;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfOutOfRange(value, 0f, 1f, nameof(value));
				smoothness = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetTextureTiling("_DitherPatternTex", ditherPattern);
			effectMaterial.SetFloat(DitherAmountMinID, ditherAmountMin);
			effectMaterial.SetFloat(DitherAmountMaxID, ditherAmountMax);
			effectMaterial.SetVector(LevelCountID, new Vector4(levelCount.x, levelCount.y, levelCount.z, 1f));
			effectMaterial.SetFloat(SmoothnessID, smoothness);
			
			effectMaterial.SetFloat(MipMapBiasID, mipMapBias);
			
			if (useUvMap)
			{
				effectMaterial.EnableKeyword("USE_UV_MAP");
				effectMaterial.SetTextureTiling("_UVMap", uvMap);
			}
			else
			{
				effectMaterial.DisableKeyword("USE_UV_MAP");
			}
			
			if (applyDepthCompensation)
			{
				effectMaterial.EnableKeyword("APPLY_DEPTH_COMPENSATION");
			}
			else
			{
				effectMaterial.DisableKeyword("APPLY_DEPTH_COMPENSATION");
			}
		}
	}
}
