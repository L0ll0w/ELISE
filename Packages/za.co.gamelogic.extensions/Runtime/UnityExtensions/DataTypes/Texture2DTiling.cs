using System;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Represents a <see cref="UnityEngine.Texture2D"/> along with configurable tiling parameters.
	/// </summary>
	/// <remarks>
	/// This class is meant for inspector values, especially ones that help configure shaders, such as in
	/// post-processing code.
	/// </remarks>
	[Serializable]
	public sealed class Texture2DTiling : TextureTilingBase
	{
		[Tooltip("The texture to tile.")]
		[SerializeField] private Texture2D texture = null;

		/// <summary>
		/// Gets the texture.
		/// </summary>
		public Texture2D Texture
		{
			get => texture;
			set => texture = value;
		}
	}
}
