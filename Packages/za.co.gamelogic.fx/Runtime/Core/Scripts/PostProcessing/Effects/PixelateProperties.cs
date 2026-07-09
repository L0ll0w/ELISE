using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the pixelate post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class PixelateProperties : ShaderProperties
	{
		private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
		private static readonly int PixelOffsetID = Shader.PropertyToID("_PixelOffset");
		
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Pixelate;

		[Tooltip("Width and height of each rendered pixel block in screen pixels.")]
		[SerializeField] private Vector2 pixelSize = new Vector2(2, 2);

		[Tooltip("Offset of the pixel grid in screen pixels.")]
		[SerializeField] private Vector2 pixelOffset = new Vector2(0, 0);
		
		/// <summary>
		/// Width and height of each rendered pixel block in screen pixels.
		/// </summary>
		public Vector2 PixelSize
		{
			get => pixelSize;
			set => pixelSize = value;
		}
		
		/// <summary>
		/// Offset of the pixel grid in screen pixels. 
		/// </summary>
		/// 
		public Vector2 PixelOffset
		{
			get => pixelOffset;
			set => pixelOffset = value;
		}
		
		/// <inheritdoc/>
		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetVector(PixelSizeID, pixelSize);
			effectMaterial.SetVector(PixelOffsetID, pixelOffset);
		}
	}
}
