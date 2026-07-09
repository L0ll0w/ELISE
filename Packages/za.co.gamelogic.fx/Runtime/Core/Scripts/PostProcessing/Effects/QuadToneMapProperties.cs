using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the quad-tone map post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class QuadToneMapProperties : ShaderProperties
	{
		private static readonly int LowColorID = Shader.PropertyToID("_LowColor");
		private static readonly int Mid0ColorID = Shader.PropertyToID("_Mid0Color");
		private static readonly int Mid1ColorID = Shader.PropertyToID("_Mid1Color");
		private static readonly int HighColorID = Shader.PropertyToID("_HighColor");
		private static readonly int LowValueID = Shader.PropertyToID("_LowValue");
		private static readonly int Mid0ValueID = Shader.PropertyToID("_Mid0Value");
		private static readonly int Mid1ValueID = Shader.PropertyToID("_Mid1Value");
		private static readonly int HighValueID = Shader.PropertyToID("_HighValue");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.QuadToneMap;

		[Tooltip("Color used for values below lowValue")]
		[SerializeField] private Color lowColor = Color.black;

		[Tooltip("Color used for the first mid range (between lowValue and mid0Value).")]
		[SerializeField] private Color mid0Color = Color.gray;

		[Tooltip("Color used for the second mid range (between mid0Value and mid1Value).")]
		[SerializeField] private Color mid1Color = Color.gray;

		[Tooltip("Color used for values above mid1Value.")]
		[SerializeField] private Color highColor = Color.white;

		[Tooltip("Lower threshold for lightness.")]
		[Range(0f, 1f)]
		[SerializeField] private float lowValue = 0.0f;

		[Tooltip("Threshold separating low and mid0 regions.")]
		[Range(0f, 1f)]
		[SerializeField] private float mid0Value = 0.33f;

		[Tooltip("Threshold separating mid0 and mid1 regions.")]
		[Range(0f, 1f)]
		[SerializeField] private float mid1Value = 0.66f;

		[Tooltip("Upper threshold for lightness.")]
		[Range(0f, 1f)]
		[SerializeField] private float highValue = 1.0f;

		/// <summary>
		/// Color used for values below LowValue.
		/// </summary>
		public Color LowColor
		{
			get => lowColor;
			set => lowColor = value;
		}

		/// <summary>
		/// Color used for the first mid range (between LowValue and Mid0Value).
		/// </summary>
		public Color Mid0Color
		{
			get => mid0Color;
			set => mid0Color = value;
		}

		/// <summary>
		/// Color used for the second mid range (between Mid0Value and Mid1Value).
		/// </summary>
		public Color Mid1Color
		{
			get => mid1Color;
			set => mid1Color = value;
		}

		/// <summary>
		/// Color used for values above Mid1Value.
		/// </summary>
		public Color HighColor
		{
			get => highColor;
			set => highColor = value;
		}

		/// <summary>
		/// Lower threshold for lightness.
		/// </summary>
		public float LowValue
		{
			get => lowValue;
			set
			{
				value.ThrowIfNegative(nameof(value));
				lowValue = value;
			}
		}

		/// <summary>
		/// Threshold separating low and mid0 regions.
		/// </summary>
		public float Mid0Value
		{
			get => mid0Value;
			set
			{
				value.ThrowIfNegative(nameof(value));
				mid0Value = value;
			}
		}

		/// <summary>
		/// Threshold separating mid0 and mid1 regions.
		/// </summary>
		public float Mid1Value
		{
			get => mid1Value;
			set
			{
				value.ThrowIfNegative(nameof(value));
				mid1Value = value;
			}
		}

		/// <summary>
		/// Upper threshold for lightness.
		/// </summary>
		public float HighValue
		{
			get => highValue;
			set
			{
				value.ThrowIfNegative(nameof(value));
				highValue = value;
			}
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetColor(LowColorID, lowColor);
			effectMaterial.SetColor(Mid0ColorID, mid0Color);
			effectMaterial.SetColor(Mid1ColorID, mid1Color);
			effectMaterial.SetColor(HighColorID, highColor);
			effectMaterial.SetFloat(LowValueID, lowValue);
			effectMaterial.SetFloat(Mid0ValueID, mid0Value);
			effectMaterial.SetFloat(Mid1ValueID, mid1Value);
			effectMaterial.SetFloat(HighValueID, highValue);
		}
	}
}
