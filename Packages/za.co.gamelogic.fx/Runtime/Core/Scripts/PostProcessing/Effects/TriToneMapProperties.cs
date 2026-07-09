using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the tri-tone map post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class TriToneMapProperties : ShaderProperties
	{
		private static readonly int LowColorID = Shader.PropertyToID("_LowColor");
		private static readonly int MidColorID = Shader.PropertyToID("_MidColor");
		private static readonly int HighColorID = Shader.PropertyToID("_HighColor");
		private static readonly int LowValueID = Shader.PropertyToID("_LowValue");
		private static readonly int MidValueID = Shader.PropertyToID("_MidValue");
		private static readonly int HighValueID = Shader.PropertyToID("_HighValue");
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.TriToneMap;

		[Header("Colors")]
		[Tooltip("Color mapped to the shadow (low luminance) region.")]
		[SerializeField] private Color lowColor = new Color(0.4f, 0, 0.3f, 1f);

		[Tooltip("Color mapped to the midtone region.")]
		[SerializeField] private Color midColor = new Color(1f, 0.5f, 0.0f, 0f);

		[Tooltip("Color mapped to the highlight (high luminance) region.")]
		[SerializeField] private Color highColor = new Color(.9f, 1f, 0.5f, 1f);

		[Header("Thresholds")]
		[Tooltip("Lower luminance threshold; pixels below this map to the low color.")]
		[Range(0f, 1f)]
		[SerializeField] private float lowValue = 0.1f;

		[Tooltip("Mid luminance threshold; pixels between low and mid map to the mid color.")]
		[Range(0f, 1f)]
		[SerializeField] private float midValue = 0.6f;

		[Tooltip("Upper luminance threshold; pixels above this map to the high color.")]
		[Range(0f, 1f)]
		[SerializeField] private float highValue = 1f;

		/// <summary>
		/// Color mapped to the shadow (low luminance) region.
		/// </summary>
		public Color LowColor
		{
			get => lowColor;
			set => lowColor = value;
		}

		/// <summary>
		/// Color mapped to the midtone region.
		/// </summary>
		public Color MidColor
		{
			get => midColor;
			set => midColor = value;
		}

		/// <summary>
		/// Color mapped to the highlight (high luminance) region.
		/// </summary>
		public Color HighColor
		{
			get => highColor;
			set => highColor = value;
		}

		/// <summary>
		/// Lower luminance threshold in the range [0, 1].
		/// </summary>
		public float LowValue
		{
			get => lowValue;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				lowValue = value;
			}
		}

		/// <summary>
		/// Mid luminance threshold in the range [0, 1].
		/// </summary>
		public float MidValue
		{
			get => midValue;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				midValue = value;
			}
		}

		/// <summary>
		/// Upper luminance threshold in the range [0, 1].
		/// </summary>
		public float HighValue
		{
			get => highValue;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				highValue = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetColor(LowColorID, lowColor);
			effectMaterial.SetColor(MidColorID, midColor);
			effectMaterial.SetColor(HighColorID, highColor);
		
			effectMaterial.SetFloat(LowValueID, lowValue);
			effectMaterial.SetFloat(MidValueID, midValue);
			effectMaterial.SetFloat(HighValueID, highValue);
		}
	}
}
