using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the adjust-gamma post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class AdjustGammaProperties : ShaderProperties
	{
		private static readonly int GammaID = Shader.PropertyToID("_Gamma");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.AdjustGamma;
		
		[Tooltip("Gamma correction.\n1 = no change.\nLower values brighten midtones, higher values darken midtones.")]
		[Range(0.1f, 5f)]
		[SerializeField] private float gamma = 1.0f;

		/// <summary>
		/// Gamma correction.
		/// </summary>
		/// <remarks>
		/// 1 = no change.
		///
		/// Lower values brighten midtones, higher values darken midtones.
		/// </remarks>
		public float Gamma
		{
			get => gamma;
			set
			{
				value.ThrowIfNegative(nameof(value));
				gamma = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(GammaID, gamma);
		}
	}
}
