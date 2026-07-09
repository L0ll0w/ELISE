#if GAMELOGIC_HAS_URP

#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE
#define GAMELOGIC_USE_RENDER_GRAPH
#endif

using System.Collections.Generic;
using System.Linq;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.URP.Mapping
{
	/// <summary>
	/// Renders map objects into a render texture using layer mask culling.
	/// </summary>
#if GAMELOGIC_USE_RENDER_GRAPH
	[System.Obsolete("Use RenderGraph_MapPass instead")]
#endif
	internal sealed class MapPass : ScriptableRenderPass
	{
		private RenderTexture target;
		private readonly Material material;
		private readonly Material backgroundMaterial;
		private readonly MapProperties mapProperties;
		private readonly int layerMask;

		/*	This is not a robust way to set the renderers.
		
			From this class's perspective, the contents can be invalidated externally; we really should make a copy.
			However, this is not good for performance. Since this is an internal mechanism, the risk is acceptable.  
		*/
		internal IReadOnlyList<Renderer> Renderers { get; set; }

		internal MapPass(MapProperties mapProperties, int layerMask)
		{
			this.mapProperties = mapProperties;
			this.layerMask = layerMask;
			material = CoreUtils.CreateEngineMaterial(mapProperties.Shader);

			if (mapProperties.BackgroundShader != null)
			{
				backgroundMaterial = CoreUtils.CreateEngineMaterial(mapProperties.BackgroundShader);
			}
		}

		internal void SetTarget(RenderTexture texture)
		{
			target = texture;
		}

		/// <inheritdoc/>
#if !GAMELOGIC_USE_RENDER_GRAPH
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (Renderers == null || !Renderers.Any())
			{
				return;
			}
			
			var buffer = CommandBufferPool.Get("Map Renderers");
			int depthId = Shader.PropertyToID("_MapDepth");
			buffer.GetTemporaryRT(depthId, target.width, target.height, 24);
			buffer.SetRenderTarget(target, new RenderTargetIdentifier(depthId));
			
			FxUtils.DrawBackground(buffer, backgroundMaterial, mapProperties);
			context.ExecuteCommandBuffer(buffer);
			CommandBufferPool.Release(buffer);
			
			mapProperties.SetMaterialProperties(material);

			var buffer2 = CommandBufferPool.Get();
			buffer2.SetRenderTarget(target, new RenderTargetIdentifier(depthId));

			foreach (var renderer in Renderers)
			{
				if ((layerMask & (1 << renderer.gameObject.layer)) == 0)
				{
					continue;
				}

				var mesh = FxUtils.GetMesh(renderer);

				if (mesh == null)
				{
					continue;
				}

				var block = new MaterialPropertyBlock();
				mapProperties.SetRendererProperties(block, renderer);

				for (int i = 0; i < renderer.sharedMaterials.Length; i++)
				{
					buffer2.DrawMesh(mesh, renderer.localToWorldMatrix, material, i, 0, block);
				}
			}

			context.ExecuteCommandBuffer(buffer2);
			CommandBufferPool.Release(buffer2);
			
			var releaseBuffer = CommandBufferPool.Get();
			releaseBuffer.ReleaseTemporaryRT(depthId);
			context.ExecuteCommandBuffer(releaseBuffer);
			CommandBufferPool.Release(releaseBuffer);
		}
#endif
		
		internal void Dispose()
		{
			CoreUtils.Destroy(material);
			CoreUtils.Destroy(backgroundMaterial);
		}
	}
}
#endif
