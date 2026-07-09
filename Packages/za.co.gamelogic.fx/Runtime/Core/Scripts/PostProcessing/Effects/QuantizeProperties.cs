using System;
using Gamelogic.Fx.Internal;
using Unity.Mathematics;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the quantize post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class QuantizeProperties : ShaderProperties
	{
		private static readonly int LevelCountID = Shader.PropertyToID("_LevelCount");
		private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Quantize;

		// TODO Implement UI for locking and limiting to correct range.
		[Tooltip("Number of quantization levels (per channel).")]
		[SerializeField] private int3 levelCount = new int3(2, 2, 2);

		[Tooltip("Smoothing between quantized bands. 0 = sharp steps, 1 = fully smooth.")]
		[Range(0.0f, 1.0f)]
		[SerializeField] private float smoothness = 0.0f;

		/// <summary>
		/// Number of quantization levels per RGB channel.
		/// </summary>
		public int3 Levels
		{
			get => levelCount;
			set => levelCount = value;
		}

		/// <summary>
		/// Smoothing between quantized bands in the range [0, 1].
		/// </summary>
		/// <remarks>
		/// 0 produces sharp discrete steps; 1 produces a fully smooth gradient.
		/// </remarks>
		public float Smoothness
		{
			get => smoothness;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				smoothness = value;
			}
		}
	
		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetVector(LevelCountID, levelCount.ToVector());
			effectMaterial.SetFloat(SmoothnessID, smoothness);
		}
	}
}
