using System;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Constants = Gamelogic.Fx.Dithering.PostProcessing.Effects.Internal.Constants;
using ShaderNames = Gamelogic.Fx.Dithering.Internal.ShaderNames;

namespace Gamelogic.Fx.Dithering.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the dither matrix bias post-processing effect.
	/// </summary>
	[Serializable]
	public class DitherMatrixBiasProperties : ShaderProperties
	{
		private static readonly int MatrixRID = Shader.PropertyToID("_MatrixR");
		private static readonly int MatrixRWidthID = Shader.PropertyToID("_MatrixRWidth");
		private static readonly int MatrixRHeightID = Shader.PropertyToID("_MatrixRHeight");
		
		private static readonly int MatrixGID = Shader.PropertyToID("_MatrixG");
		private static readonly int MatrixGWidthID = Shader.PropertyToID("_MatrixGWidth");
		private static readonly int MatrixGHeightID = Shader.PropertyToID("_MatrixGHeight");
		
		private static readonly int MatrixBID = Shader.PropertyToID("_MatrixB");
		private static readonly int MatrixBWidthID = Shader.PropertyToID("_MatrixBWidth");
		private static readonly int MatrixBHeightID = Shader.PropertyToID("_MatrixBHeight");
		
		private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
		private static readonly int PixelOffsetID = Shader.PropertyToID("_PixelOffset");
		private static readonly int DitherAmountMinID = Shader.PropertyToID("_DitherAmountMin");
		private static readonly int DitherAmountMaxID = Shader.PropertyToID("_DitherAmountMax");
		private static readonly int LevelCountID = Shader.PropertyToID("_LevelCount");
		private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");

		[Tooltip("Number of quantization levels per color channel (R, G, B).")]
		[SerializeField] private int3 levelCount = math.int3(2, 2, 2);

		[Tooltip("Dither matrix used for the red channel.")]
		[Presets(nameof(DitherMatrixPresets), nameof(DitherMatrixPresets))]
		[SerializeField] private FloatMatrix matrixR = DitherMatrixPresets.Checker.Clone();

		[Tooltip("Dither matrix used for the green channel.")]
		[Presets(nameof(DitherMatrixPresets), nameof(DitherMatrixPresets))]
		[SerializeField] private FloatMatrix matrixG = DitherMatrixPresets.Checker.Clone();

		[Tooltip("Dither matrix used for the blue channel.")]
		[Presets(nameof(DitherMatrixPresets), nameof(DitherMatrixPresets))]
		[SerializeField] private FloatMatrix matrixB = DitherMatrixPresets.Checker.Clone();

		[Tooltip("Size of the dither pattern in pixels.")]
		[SerializeField] private Vector2 pixelSize = Vector2.one;
		
		[Tooltip("Offset of the pixel grid in screen pixels.")]
		[SerializeField] private Vector2 pixelOffset = new Vector2(0, 0);

		[FormerlySerializedAs("pixelOffset")]
		[Tooltip("Pixel offset applied when tiling the dither matrix.")]
		[SerializeField] private Vector3Int matrixOffset;

		[Tooltip("Minimum dither bias added to the color before quantization.")]
		[Range(-1f, 1f)]
		[SerializeField] private float ditherAmountMin = -0.1f;

		[Tooltip("Maximum dither bias added to the color before quantization.")]
		[Range(-1f, 1f)]
		[SerializeField] private float ditherAmountMax = 0.1f;

		[Tooltip("Controls how smoothly the dither transitions between quantization levels.")]
		[Range(0f, 1f)]
		[SerializeField] private float smoothness = 0.1f;

		[Header("UV Mapping")]
		[Tooltip("Whether to use a UV map texture to drive screen-space UV coordinates.")]
		[SerializeField] private bool useUvMap = false;
		[Tooltip("The UV map texture and tiling settings.")]
		[SerializeField] private TextureTiling uvMap = null;
		[Tooltip("Whether to apply depth-based compensation to the UV map scale.")]
		[SerializeField] private bool applyDepthCompensation = false;


		/// <summary>Gets or sets the number of quantization levels per color channel.</summary>
		public int3 LevelCount
		{
			get => levelCount;
			set => levelCount = value;
		}

		/// <summary>Gets or sets the dither matrix for the red channel.</summary>
		public FloatMatrix MatrixR
		{
			get => matrixR;
			set => matrixR = value;
		}

		/// <summary>Gets or sets the dither matrix for the green channel.</summary>
		public FloatMatrix MatrixG
		{
			get => matrixG;
			set => matrixG = value;
		}

		/// <summary>Gets or sets the dither matrix for the blue channel.</summary>
		public FloatMatrix MatrixB
		{
			get => matrixB;
			set => matrixB = value;
		}

		/// <summary>Gets or sets the size of the dither pattern in pixels.</summary>
		public Vector2 PixelSize
		{
			get => pixelSize;
			set => pixelSize = value;
		}
		
		/// <summary>
		/// Gets or sets the pixel offset of the dither pattern.
		/// </summary>
		public Vector2 PixelOffset
		{
			get => pixelOffset;
			set => pixelOffset = value;
		}

		/// <summary>Gets or sets the minimum dither bias, in the range <c>[-1, 1]</c>.</summary>
		public float DitherAmountMin
		{
			get => ditherAmountMin;
			set
			{
				value.ThrowIfOutOfRange(-1f, 1f, nameof(value));
				ditherAmountMin = value;
			}
		}

		/// <summary>Gets or sets the maximum dither bias, in the range <c>[-1, 1]</c>.</summary>
		public float DitherAmountMax
		{
			get => ditherAmountMax;
			set
			{
				value.ThrowIfOutOfRange(-1f, 1f, nameof(value));
				ditherAmountMax = value;
			}
		}

		/// <summary>Gets or sets the smoothness of the dither transition, in the range <c>[0, 1]</c>.</summary>
		public float Smoothness
		{
			get => smoothness;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				smoothness = value;
			}
		}

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.DitherMatrixBias;
		
		public override void SetMaterialProperties(Material effectMaterial)
		{
			if (matrixR.Width == 0) return;
			if (matrixG.Width == 0) return;
			if (matrixB.Width == 0) return;
			
			if (matrixR.Height == 0) return;
			if (matrixG.Height == 0) return;
			if (matrixB.Height == 0) return;
			
			effectMaterial.SetInt(MatrixRWidthID, matrixR.Width);
			effectMaterial.SetInt(MatrixRHeightID, matrixR.Height);
			effectMaterial.SetFloatArray(MatrixRID, matrixR.Normalize().Values.ToArray());
			
			effectMaterial.SetInt(MatrixGWidthID, matrixG.Width);
			effectMaterial.SetInt(MatrixGHeightID, matrixG.Height);
			effectMaterial.SetFloatArray(MatrixGID, matrixG.Normalize().Values.ToArray());
			
			effectMaterial.SetInt(MatrixBWidthID, matrixB.Width);
			effectMaterial.SetInt(MatrixBHeightID, matrixB.Height);
			effectMaterial.SetFloatArray(MatrixBID, matrixB.Normalize().Values.ToArray());
			
			effectMaterial.SetVector(PixelSizeID, pixelSize);
			effectMaterial.SetVector(PixelOffsetID, pixelOffset);
			effectMaterial.SetFloat(DitherAmountMinID, ditherAmountMin);
			effectMaterial.SetFloat(DitherAmountMaxID, ditherAmountMax);
			effectMaterial.SetFloat(SmoothnessID, smoothness);
			effectMaterial.SetVector(LevelCountID, levelCount.ToVector4XYZ());
			
			effectMaterial.SetVector("_MatrixOffset", new Vector4(matrixOffset.x, matrixOffset.y, matrixOffset.z, 0));

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
