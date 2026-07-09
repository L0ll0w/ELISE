using System;
using Gamelogic.Fx.Internal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gamelogic.Fx.Mapping.Maps
{
	/// <summary>
	/// Properties for rendering a UV map of the scene.
	/// See <see href="../common/docs/map-renderers-reference-common.html#uv-map"/>.
	/// </summary>
	[Serializable]
	public sealed class UVMapProperties : MapProperties
	{
		private const string MainTexSTPropertyName = "_MainTex_ST";
		private static readonly int TilingID = Shader.PropertyToID("_Tiling");
		private static readonly int MainTexSTID = Shader.PropertyToID(MainTexSTPropertyName);

		[Header("Background")] 
		[Tooltip("Scale of the UV coordinates for the background.")]
		[SerializeField] private Vector2 uvBackgroundScale = Vector2.one;
		
		[Tooltip("Offset of the UV coordinates for the background.")]
		[SerializeField] private Vector2 uvBackgroundOffset = Vector2.zero;

		[Header("Renderer")]
		[Tooltip("Whether to adjust the UV coordinates based on the renderer material's tiling.")]
		[SerializeField] private bool adjustByMaterialTiling;
		
		[FormerlySerializedAs("adjustByRendererDistance")]
		[Tooltip("Whether to adjust the UV coordinates based on the renderer's distance from the camera.")]
		[SerializeField] private bool adjustByDistance;
		
		[FormerlySerializedAs("adjustByRendererScale")] 
		[Tooltip("Whether to adjust the UV coordinates based on scale of the game object the renderer is attached to.")]
		[SerializeField] private bool adjustByScale;

		/// <inheritdoc/>
		public override string ShaderName => Constants.MapsRoot + ShaderNames.UVMap;
		
		/// <inheritdoc/>
		protected override string BackgroundShaderName => Constants.MapsRoot + ShaderNames.UVBackground;

		/// <summary>
		/// Get and sets the scale of the UV coordinates for the background.
		/// </summary>
		public Vector2 UVBackgroundScale
		{
			get => uvBackgroundScale;
			set => uvBackgroundScale = value;
		}

		/// <summary>
		/// Gets and sets the offset of the UV coordinates for the background.
		/// </summary>
		public Vector2 UVBackgroundOffset
		{
			get => uvBackgroundOffset;
			set => uvBackgroundOffset = value;
		}

		/// <summary>
		/// Gets and sets whether to adjust the UV coordinates based on the renderer material's tiling.
		/// </summary>
		public bool AdjustByMaterialTiling
		{
			get => adjustByMaterialTiling;
			set => adjustByMaterialTiling = value;
		}

		/// <summary>
		/// Gets and sets whether to adjust the UV coordinates based on the renderer's distance from the camera.
		/// </summary>
		public bool AdjustByDistance
		{
			get => adjustByDistance;
			set => adjustByDistance = value;
		}

		/// <summary>
		/// Gets and sets whether to adjust the UV coordinates based on scale of the game object the renderer is attached to.
		/// </summary>
		public bool AdjustByScale
		{
			get => adjustByScale;
			set => adjustByScale = value;
		}

		/// <inheritdoc/>
		public override void SetBackgroundMaterialProperties(Material material)
		{
			var tiling = new Vector4(uvBackgroundScale.x, uvBackgroundScale.y, uvBackgroundOffset.x, uvBackgroundOffset.y);
			material.SetVector(TilingID, tiling);
		}

		/// <inheritdoc/>
		public override void SetMaterialProperties(Material material)
		{
			if (adjustByMaterialTiling)
			{
				material.EnableKeyword("ADJUST_BY_MATERIAL_TILING");
			}
			else
			{
				material.DisableKeyword("ADJUST_BY_MATERIAL_TILING");
			}

			if (adjustByDistance)
			{
				material.EnableKeyword("ADJUST_BY_OBJECT_DISTANCE");
			}
			else
			{
				material.DisableKeyword("ADJUST_BY_OBJECT_DISTANCE");
			}

			if (adjustByScale)
			{
				material.EnableKeyword("ADJUST_BY_OBJECT_SCALE");
			}
			else
			{
				material.DisableKeyword("ADJUST_BY_OBJECT_SCALE");
			}
		}
		
		public override void SetRendererProperties(MaterialPropertyBlock block, Renderer renderer)
		{
			if (!adjustByMaterialTiling) return;
			var st = renderer.sharedMaterial.GetVector(MainTexSTPropertyName);
			block.SetVector(MainTexSTID, st);
		}
	}
}
