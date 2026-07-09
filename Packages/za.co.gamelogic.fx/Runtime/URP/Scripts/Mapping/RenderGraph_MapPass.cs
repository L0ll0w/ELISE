#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE
using System.Collections.Generic;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.URP.Mapping
{
	internal sealed class RenderGraph_MapPass : ScriptableRenderPass
	{
		private class PassData
		{
			internal TextureHandle colorTexture;
			internal TextureHandle depthTexture;
			internal Material backgroundMaterial;
			internal MapProperties mapProperties;
			internal Material material;
			internal IReadOnlyList<Renderer> renderers;
			internal int layerMask;
		}
		
		private readonly Material material;
		private readonly Material backgroundMaterial;
		private readonly MapProperties mapProperties;
		private readonly int layerMask;
		private RTHandle colorHandle;

		/*	This is not a robust way to set the renderers.
		
			From this class's perspective, the contents can be invalidated externally; we really should make a copy.
			However, this is not good for performance. Since this is an internal mechanism, the risk is acceptable.  
		*/
		internal IReadOnlyList<Renderer> Renderers { get; set; }

		internal RenderGraph_MapPass(MapProperties mapProperties, int layerMask)
		{
			this.mapProperties = mapProperties;
			this.layerMask = layerMask;
			material = CoreUtils.CreateEngineMaterial(mapProperties.Shader);

			if (mapProperties.BackgroundShader != null)
			{
				backgroundMaterial = CoreUtils.CreateEngineMaterial(mapProperties.BackgroundShader);
			}
		}

		internal void SetTarget(RTHandle handle)
		{
			colorHandle = handle;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (Renderers == null || Renderers.Count == 0)
			{
				return;
			}
			
			var cameraData = frameData.Get<UniversalCameraData>();

			bool skipOverlay
				= cameraData.renderType == CameraRenderType.Overlay
					&& ExperimentalSettings.Mapping.SkipInOverlayCameras;

			if (skipOverlay)
			{
				return;
			}

			var colorTexture = renderGraph.ImportTexture(colorHandle);

			var depthDesc = new TextureDesc(colorHandle.rt.width, colorHandle.rt.height)
			{
				depthBufferBits = DepthBits.Depth24,
				name = "Map Depth"
			};
			var depthTexture = renderGraph.CreateTexture(depthDesc);

			mapProperties.SetMaterialProperties(material);

			using (var builder = renderGraph.AddUnsafePass<PassData>("Map Renderers", out var passData))
			{
				passData.colorTexture = colorTexture;
				passData.depthTexture = depthTexture;
				passData.backgroundMaterial = backgroundMaterial;
				passData.mapProperties = mapProperties;
				passData.material = material;
				passData.renderers = Renderers;
				passData.layerMask = layerMask;

				builder.UseTexture(colorTexture, AccessFlags.Write);
				builder.UseTexture(depthTexture, AccessFlags.Write);
				builder.AllowPassCulling(false);

				builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

					cmd.SetRenderTarget(data.colorTexture, data.depthTexture);
					cmd.ClearRenderTarget(true, true, data.mapProperties.BackgroundColor);

					FxUtils.DrawBackground(cmd, data.backgroundMaterial, data.mapProperties);

					data.mapProperties.SetMaterialProperties(data.material);

					foreach (var renderer in data.renderers)
					{
						if ((data.layerMask & (1 << renderer.gameObject.layer)) == 0)
						{
							continue;
						}

						var mesh = FxUtils.GetMesh(renderer);

						if (mesh == null)
						{
							continue;
						}

						var block = new MaterialPropertyBlock();
						data.mapProperties.SetRendererProperties(block, renderer);

						for (int i = 0; i < renderer.sharedMaterials.Length; i++)
						{
							cmd.DrawMesh(mesh, renderer.localToWorldMatrix, data.material, i, 0, block);
						}
					}
				});
			}
		}

		internal void Dispose()
		{
			CoreUtils.Destroy(material);
			CoreUtils.Destroy(backgroundMaterial);
		}
	}
}
#endif
