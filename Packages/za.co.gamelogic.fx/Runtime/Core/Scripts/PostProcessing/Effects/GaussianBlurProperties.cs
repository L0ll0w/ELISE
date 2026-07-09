using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the Gaussian blur post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class GaussianBlurProperties : SeparableShaderProperties
	{
		private static readonly int SigmaID = Shader.PropertyToID("_Sigma");

		[Tooltip("Standard deviation of the Gaussian kernel. Higher values produce a wider, softer blur.")]
		[SerializeField] private float sigma = 1f;

		/// <summary>
		/// Standard deviation of the Gaussian kernel.
		/// </summary>
		/// <remarks>
		/// Higher values produce a wider, softer blur.
		/// </remarks>
		public float Sigma
		{
			get => sigma;
			set => sigma = value;
		}

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.GaussianBlur;

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(SigmaID, sigma);
		}
	}
}
