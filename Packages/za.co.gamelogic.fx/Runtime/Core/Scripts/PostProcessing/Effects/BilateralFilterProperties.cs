using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the bilateral filter post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class BilateralFilterProperties : SeparableShaderProperties
	{
		private static readonly int SpatialSigmaID = Shader.PropertyToID("_SpatialSigma");
		private static readonly int RangeSigmaID = Shader.PropertyToID("_RangeSigma");

		[Tooltip("Controls the spatial extent of the Gaussian kernel. Higher values consider farther pixels.")]
		[SerializeField] private float spatialSigma = 2.0f;

		[Tooltip("Controls the color/intensity range for edge-preserving blending. Lower values preserve more edges.")]
		[SerializeField] private float rangeSigma = 0.1f;

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.BilateralFilter;

		/// <summary>
		/// Controls the spatial extent of the Gaussian kernel.
		/// </summary>
		/// <remarks>
		/// Higher values consider farther pixels.
		/// </remarks>
		public float SpatialSigma
		{
			get => spatialSigma;
			set
			{
				value.ThrowIfNotPositive(nameof(value));
				spatialSigma = value;
			}
		}

		/// <summary>
		/// Controls the color/intensity range for edge-preserving blending.
		/// </summary>
		/// <remarks>
		/// Lower values preserve more edges.
		/// </remarks>
		public float RangeSigma
		{
			get => rangeSigma;
			set
			{
				value.ThrowIfNotPositive(nameof(value));
				rangeSigma = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(SpatialSigmaID, spatialSigma);
			effectMaterial.SetFloat(RangeSigmaID, rangeSigma);
		}
	}
}
