using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the adjust-saturation post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class AdjustSaturationProperties : ShaderProperties
	{
		private static readonly int SaturationID = Shader.PropertyToID("_Saturation");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.AdjustSaturation;

		[Tooltip(
			"Saturation.\n1 = unchanged\n0 = completely grayscale.\nValues below 1 desaturate.\nValues above 1 enhance saturation.")]
		[Range(0f, 2f)]
		[SerializeField]
		private float saturation = 1.0f;

		/// <summary>
		/// Saturation.
		/// </summary>
		/// <remarks>
		/// 1 = unchanged
		/// 0 = completely grayscale.
		/// Values below 1 desaturate.
		/// Values above 1 enhance saturation.
		/// </remarks>
		public float Saturation
		{
			get => saturation;
			set
			{
				value.ThrowIfNegative(nameof(value));
				saturation = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(SaturationID, saturation);
		}
	}
}
