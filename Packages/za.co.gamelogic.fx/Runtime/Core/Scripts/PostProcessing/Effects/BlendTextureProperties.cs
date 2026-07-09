using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the blend-texture post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class BlendTextureProperties : ShaderProperties
	{
		private const string UseMaskKeyword = "USE_MASK";
		private static readonly int OpacityID = Shader.PropertyToID("_Opacity");

		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.BlendTexture;

		[Tooltip("The texture to blend over the scene.")]
		[FormerlySerializedAs("texture")]
		[FormerlySerializedAs("textureTiling")]
		[SerializeField] private TextureTiling overlayTexture = null;

		[Tooltip("Overall opacity of the blended texture. 0 = invisible, 1 = fully opaque.")]
		[SerializeField, Range(0f, 1f)] private float opacity = 1.0f;

		[Tooltip("When enabled, uses the mask texture to control per-pixel blending.")]
		[SerializeField] private bool useMask = false;

		[Tooltip("Mask texture controlling where the overlay is applied. White = full blend, black = no blend.")]
		[SerializeField] private TextureTiling maskTexture = null;

		/// <summary>
		/// The texture to blend over the scene.
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
		/// Overall opacity of the blended texture in the range [0, 1].
		/// </summary>
		public float Opacity
		{
			get => opacity;
			set
			{
				value.ThrowIfOutOfRange(0f, 1f, nameof(value));
				opacity = value;
			}
		}
		
		public override void SetMaterialProperties(Material effectMaterial)
		{
			effectMaterial.SetTextureTiling("_OverlayTex", overlayTexture);
			effectMaterial.SetFloat(OpacityID, opacity);
			
			if(useMask)
			{
				effectMaterial.EnableKeyword(UseMaskKeyword);
				effectMaterial.SetTextureTiling("_MaskTex", maskTexture);
			}
			else
			{
				effectMaterial.DisableKeyword(UseMaskKeyword);
			}
		}
	}
}
