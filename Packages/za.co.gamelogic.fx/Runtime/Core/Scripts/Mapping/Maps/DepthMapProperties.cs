using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.Mapping.Maps
{
	/// <summary>
	/// Properties for rendering a depth map.
	/// See <see href="../common/docs/map-renderers-reference-common.html#depth-map"/>.
	/// </summary>
	[Serializable]
	public sealed class DepthMapProperties : MapProperties
	{
		/// <summary>
		/// The depth rendering mode.
		/// </summary>
		public enum DepthEncoding
		{
			/// <summary>
			/// Depth is mapped linearly between near and far plane.
			/// </summary>
			Linear = 0,
		
			/// <summary>
			/// Depth is mapped logarithmically between near and far plane.
			/// </summary>
			Logarithmic = 1,
		}
		
		private static readonly int RenderModeID = Shader.PropertyToID("_RenderMode");
		private static readonly int FarPlaneID = Shader.PropertyToID("_FarPlane");
		private static readonly int NearPlaneID = Shader.PropertyToID("_NearPlane");
		
		[Tooltip("The depth encoding.")]
		[SerializeField] private DepthEncoding encoding = DepthEncoding.Linear;

		/// <inheritdoc />
		public override string ShaderName => Constants.MapsRoot + ShaderNames.DepthMap;

		/// <inheritdoc />
		public override Color BackgroundColor => Color.white;

		/// <summary>
		/// Gets and sets the depth encoding.
		/// </summary>
		public DepthEncoding Encoding
		{
			get => encoding;
			set => encoding = value;
		}

		public override void SetMaterialProperties(Material material)
		{
			Camera cam = Camera.current != null ? Camera.current : Camera.main;

			if (cam == null)
			{
				// Sensible fallback to avoid division-by-zero in shader
				material.SetFloat(NearPlaneID, 0.1f);
				material.SetFloat(FarPlaneID, 1000f);
				return;
			}
			
			material.SetInteger(RenderModeID, (int)encoding);
			material.SetFloat(NearPlaneID, cam.nearClipPlane);
			material.SetFloat(FarPlaneID, cam.farClipPlane);
		}
	}
}
