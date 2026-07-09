using System;
using Gamelogic.Fx.Internal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	[Serializable]
	public sealed class SimpleOutlineProperties : ShaderProperties
	{
		private static readonly int JumpSizeID = Shader.PropertyToID("_JumpSize");
		private static readonly int EdgeFactorID = Shader.PropertyToID("_EdgeFactor");
		private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
		private static readonly int LineColorID = Shader.PropertyToID("_LineColor");
		private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
		private static readonly int OutlineSourceTexID = Shader.PropertyToID("_OutlineSourceTex");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.SimpleOutline;

		[Header("Source Texture")]
		[Tooltip("Select what will be used to detect the outline.")]
		[SerializeField] private OutlineSource outlineSource = OutlineSource.CameraColor;
		
		[FormerlySerializedAs("sourceTexture")]
		[Tooltip("Alternate source texture for edge detection.")]
		[SerializeField] private Texture outlineSourceTexture = null;
		
		[Header("Outline Settings")]
		
		[Tooltip("Factor used to multiply sample offsets. A way to fake bigger kernel sizes.")]
		[Min(0f)]
		[SerializeField] private float jumpSize = 1.0f;

		[Tooltip("Factor used to multiply the edge value before taking a threshold. Use this to make tweaking the threshold more convenient.")]
		[Min(0f)]
		[SerializeField] private float edgeFactor = 1.0f;

		[Tooltip("Threshold above which a pixel is considered an edge.")]
		[Min(0f)]
		[SerializeField] private float threshold = 0.5f;

		[Tooltip("Color used to draw the detected edges.\nAlpha determines how much of the original\nimage shines through.")]
		[SerializeField] private Color lineColor = new Color(0f, 0f, 0f, 1f);

		[Tooltip("Background color when no edge is detected.\nAlpha determines how much of the original\nimage shines through.")]
		[SerializeField] private Color backgroundColor = new Color(1f, 1f, 1f, 1f);

		/// <summary>
		/// Factor used to multiply sample offsets.
		/// </summary>
		/// <remarks>
		/// A way to fake bigger kernel sizes.
		/// </remarks>
		public float JumpSize
		{
			get => jumpSize;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfNegative(value, nameof(value));
				jumpSize = value;
			}
		}

		/// <summary>
		/// Factor used to multiply the edge value before taking a threshold.
		/// </summary>
		public float EdgeFactor
		{
			get => edgeFactor;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfNegative(value, nameof(value));
				edgeFactor = value;
			}
		}

		/// <summary>
		/// Threshold above which a pixel is considered an edge.
		/// </summary>
		public float Threshold
		{
			get => threshold;
			set
			{
				ShaderPropertyThrowHelper.ThrowIfNegative(value, nameof(value));
				threshold = value;
			}
		}

		/// <summary>
		/// Color used to draw the detected edges.
		/// </summary>
		/// <remarks>
		/// Alpha determines how much of the original image shines through.
		/// </remarks>
		public Color LineColor
		{
			get => lineColor;
			set => lineColor = value;
		}

		/// <summary>
		/// Background color when no edge is detected.
		/// </summary>
		/// <remarks>
		/// Alpha determines how much of the original image shines through.
		/// </remarks>
		public Color BackgroundColor
		{
			get => backgroundColor;
			set => backgroundColor = value;
		}
		
		/// <summary>
		/// Enable to use a different texture as the source for edge detection.
		/// </summary>
		public OutlineSource OutlineSource
		{
			get => outlineSource;
			set => outlineSource = value;
		}
		
		/// <summary>
		/// Alternate source texture for edge detection.
		/// </summary>
		public Texture OutlineSourceTexture
		{
			get => outlineSourceTexture;
			set => outlineSourceTexture = value;
		}

		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetFloat(JumpSizeID, jumpSize);
			effectMaterial.SetFloat(EdgeFactorID, edgeFactor);
			effectMaterial.SetFloat(ThresholdID, threshold);
			effectMaterial.SetColor(LineColorID, lineColor);
			effectMaterial.SetColor(BackgroundColorID, backgroundColor);
			
			string keyword = Constants.GetKeyword(outlineSource );
			
			effectMaterial.SetKeywordOfGroup(
				keyword,
				Constants.OutlineSourceKeywordGroup
			);
			
			if(outlineSource == OutlineSource.AlternateTexture)
			{
				effectMaterial.SetTexture(OutlineSourceTexID, outlineSourceTexture);
			}
		}

		public override bool RequiresDepthTexture => true;
		
		public override bool RequiresNormalsTexture => true;
	}
}
