using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the add-texture post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class AddTextureProperties : ShaderProperties
	{
		private static readonly int MinID = Shader.PropertyToID("_Min");
		private static readonly int MaxID = Shader.PropertyToID("_Max");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.AddTexture;
	
		[Tooltip("The texture to add on top of the scene.")]
		[SerializeField] private TextureTiling overlayTexture = null;

		[Tooltip("The minimum RGB offset added to each pixel (per channel).")]
		[LockableVectorRange(-2, 2)]
		[SerializeField] private LockableVector3 minRGB = new LockableVector3() { vector = -0.1f * Vector3.one, locked = true };

		[Tooltip("The maximum RGB offset added to each pixel (per channel).")]
		[LockableVectorRange(-2, 2)]
		[SerializeField] private LockableVector3 maxRGB = new LockableVector3() { vector = 0.1f * Vector3.one, locked = true };

		/// <summary>
		/// The overlay texture to additively blend onto the scene.
		/// </summary>
		public Texture OverlayTexture
		{
			get => overlayTexture.Texture;
			set => overlayTexture.Texture = value;
		}

		/// <summary>
		/// The tiling scale of the overlay texture.
		/// </summary>
		public Vector2 OverlayTextureTilingScale
		{
			get => overlayTexture.TilingScale;
			set => overlayTexture.TilingScale = value;
		}

		/// <summary>
		/// The tiling offset of the overlay texture.
		/// </summary>
		public Vector2 OverlayTextureTilingOffset
		{
			get => overlayTexture.TilingOffset;
			set => overlayTexture.TilingOffset = value;
		}

		/// <summary>
		/// The minimum per-channel RGB offset added to each pixel.
		/// </summary>
		public Vector3 MinRGB
		{
			get => minRGB.vector;
			set => minRGB.vector = value;
		}

		/// <summary>
		/// The maximum per-channel RGB offset added to each pixel.
		/// </summary>
		public Vector3 MaxRGB
		{
			get => maxRGB.vector;
			set => maxRGB.vector = value;
		}
		
		/// <inheritdoc/>
		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetTextureTiling("_OverlayTex", overlayTexture);
			effectMaterial.SetVector(MinID, minRGB.vector);
			effectMaterial.SetVector(MaxID, maxRGB.vector);
		}
	}
}
