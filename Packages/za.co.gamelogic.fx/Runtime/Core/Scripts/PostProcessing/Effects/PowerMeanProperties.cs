using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the power-mean (generalized mean) post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class PowerMeanProperties : SeparableShaderProperties
	{
		private static readonly int PowerID = Shader.PropertyToID("_Power");

		[Tooltip("The exponent for the power mean. 1 = arithmetic mean, 2 = quadratic mean. Negative values compute harmonic-type means.")]
		[SerializeField] private float power = 2.0f;
		
		/// <summary>
		/// The power to use in the power mean calculation.
		/// </summary>
		public float Power
		{
			get => power;
			set => power = value;
		}

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.PowerMean;

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(PowerID, power);
		}
	}
}
