using System;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Represents a <see cref="UnityEngine.Texture"/> along with configurable tiling parameters.
	/// </summary>
	/// <remarks>
	/// This class is meant for inspector values, especially ones that help configure shaders, such as in
	/// post-processing code. Use this when you need to support any texture type, such as
	/// <see cref="RenderTexture"/> or <see cref="Texture2DArray"/>.
	/// </remarks>
	[Serializable]
	public sealed class TextureTiling : TextureTilingBase
	{
		[Tooltip("The texture to tile.")]
		[SerializeField] private Texture texture = null;

		/// <summary>
		/// Gets the texture.
		/// </summary>
		public Texture Texture
		{
			get => texture;
			set => texture = value;
		}
	}
}
