using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.Mapping.Maps
{
	/// <summary>
	/// Properties for rendering a normal map.
	/// See <see href="../common/docs/map-renderers-reference-common.html#normal-map"/>.
	/// </summary>
	[Serializable]
	public sealed class NormalMapProperties : MapProperties
	{
		/// <summary>
		/// Specifies how normals are encoded into the map texture's color channels.
		/// </summary>
		public enum NormalEncoding
		{
			/// <summary>
			/// Normals are encoded to a color using the formula
			/// <code>
			/// (normal + (1,1,1)) / 2
			/// </code>
			/// </summary>
			Spherical = 0,
			
			/// <summary>
			/// The same as <see cref="Spherical"/> encoding, but normals are flipped if they point away from the vector (0,0,1).
			/// </summary>
			Hemispherical = 1,
		}

		private static readonly Color DefaultBackgroundColor = new Color(0.5f, 0.5f, 0f);
	
		private static readonly int EncodingID = Shader.PropertyToID("_Encoding");
		
		public override string ShaderName => Constants.MapsRoot + ShaderNames.NormalMap;

		/// <inheritdoc />
		public override Color BackgroundColor => ComputeBackgroundColor();

		[Tooltip("The encoding used for the normal map.")]
		[SerializeField] private NormalEncoding encoding = NormalEncoding.Spherical;
		
		/// <summary>
		/// Gets and sets the encoding used for the normal map.
		/// </summary>
		public NormalEncoding Encoding
		{
			get => encoding;
			set => encoding = value;
		}

		/// <inheritdoc />
		public override void SetMaterialProperties(Material material) => material.SetInteger(EncodingID, (int)encoding);

		private Color ComputeBackgroundColor()
		{
			var mainCamera = Camera.main;
			
			if (mainCamera == null)
			{
				return DefaultBackgroundColor;
			}
			
			var normal = -mainCamera.transform.forward.normalized;

			if (encoding == NormalEncoding.Hemispherical)
			{
				var reference = -Vector3.forward;
				if (Vector3.Dot(normal, reference) < 0f)
				{
					normal = -normal;
				}
			}

			var encoded = normal * 0.5f + Vector3.one * 0.5f;
			return new Color(encoded.x, encoded.y, encoded.z, 1f);
		}
	}
}
