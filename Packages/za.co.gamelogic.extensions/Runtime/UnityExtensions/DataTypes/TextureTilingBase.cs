using System;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Base class for a texture along with configurable tiling parameters.
	/// </summary>
	/// <remarks>
	/// This class is meant for inspector values, especially ones that help configure shaders, such as in
	/// post-processing code.
	///
	/// It provides scale and offset values for UV tiling. It can optionally adjust the horizontal scale to
	/// match the current screen aspect ratio.
	/// </remarks>
	[Serializable]
	public abstract class TextureTilingBase
	{
		[Tooltip("The UV scale applied to the texture.")]
		[SerializeField] private Vector2 tilingScale = Vector2.one;
		[Tooltip("The UV offset applied to the texture.")]
		[SerializeField] private Vector2 tilingOffset = Vector2.zero;
		[Tooltip("When enabled, scales the X tiling by the screen aspect ratio (width / height).")]
		[SerializeField] private bool useScreenAspect = false;

		/// <summary>
		/// Gets the tiling scale and offset combined into a single vector in the format:
		/// (scale.x, scale.y, offset.x, offset.y).
		/// </summary>
		/// <remarks>
		/// This is suitable for setting the <c>_Texture_ST</c> shader property.
		/// </remarks>
		public Vector4 Tiling
		{
			get => new Vector4(tilingScale.x, tilingScale.y, tilingOffset.x, tilingOffset.y);
			set
			{
				tilingScale = new Vector2(value.x, value.y);
				tilingOffset = new Vector2(value.z, value.w);
			}
		}

		/// <summary>
		/// Gets the tiling scale.
		/// </summary>
		public Vector2 TilingScale
		{
			get => tilingScale;
			set => tilingScale = value;
		}

		/// <summary>
		/// Gets the tiling offset.
		/// </summary>
		public Vector2 TilingOffset
		{
			get => tilingOffset;
			set => tilingOffset = value;
		}

		/// <summary>
		/// Gets the effective tiling values, optionally adjusting the horizontal scale based on screen aspect ratio.
		/// </summary>
		/// <remarks>
		/// When <c>useScreenAspect</c> is enabled, the X scale is multiplied by
		/// <c>Screen.width / Screen.height</c> so the texture maintains visual consistency
		/// across different aspect ratios.
		/// </remarks>
		public Vector4 CalculatedTiling =>
			useScreenAspect
				? new Vector4(tilingScale.x * Screen.width / Screen.height, tilingScale.y, tilingOffset.x, tilingOffset.y)
				: Tiling;
	}
}
