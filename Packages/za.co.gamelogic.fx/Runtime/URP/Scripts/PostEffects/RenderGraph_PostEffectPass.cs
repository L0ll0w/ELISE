#if GAMELOGIC_HAS_URP

#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Gamelogic.Fx.URP.PostProcessing
{
	/// <summary>
	/// Implements a basic render graph pass for <see cref="PostProcessRendererFeature"/>.
	/// </summary>
	public abstract class RenderGraph_PostEffectPass : ScriptableRenderPass
	{
		private readonly Material material;
		private readonly bool requiresNormals;
		private readonly bool requiresDepth;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="RenderGraph_PostEffectPass"/> class.
		/// </summary>
		protected RenderGraph_PostEffectPass(
			Material material,
			RenderPassEvent injectionPoint,
			bool requiresNormals,
			bool requiresDepth)
		{
			this.material = material;
			renderPassEvent = injectionPoint;
			this.requiresNormals = requiresNormals;
			this.requiresDepth = requiresDepth;

			var newInput = ScriptableRenderPassInput.None;
			
			if (requiresNormals)
			{
				newInput |= ScriptableRenderPassInput.Normal;
			}

			if (requiresDepth)
			{
				newInput |= ScriptableRenderPassInput.Depth;
			}
			ConfigureInput(newInput);
		}

		/// <inheritdoc/>
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var cameraData = frameData.Get<UniversalCameraData>();

			bool skipOverlay 
				= cameraData.renderType == CameraRenderType.Overlay 
					&& ExperimentalSettings.PostProcessing.SkipInOverlayCameras;
			
			if (skipOverlay)
			{
				return;
			}

			var resourceData = frameData.Get<UniversalResourceData>();
			var source = resourceData.activeColorTexture;
			var desc = cameraData.cameraTargetDescriptor;
			desc.depthBufferBits = 0;
			desc.msaaSamples = 1;

			var destination 
				= UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_RenderGraphTempTex", false);

			SetMaterialProperties(material);

			var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
			
			using (var builder = renderGraph.AddBlitPass(blitParams, CommandName, returnBuilder: true))
			{
				if (requiresDepth)
				{
					builder.UseTexture(resourceData.cameraDepthTexture);
				}
				
				if (requiresNormals)
				{
					builder.UseTexture(resourceData.cameraNormalsTexture);
				}
			}
			
			/*	I'm uncertain whether this is the intended (or best) way to do it.
				
				Before I had the line 
				resourceData.cameraColor = destination;
				instead of the two lines below. 
				
				That works fine for a single camera, or when an overlay camera is used and the effects are executed _before_
				post-processing.
				
				It was not working when an overlay camera was also used and the effects are executed _after_ post-processing.
				
				From the Unity docs it looks like the single line above is supposed to work, by they also use the two lines 
				below in a sample. 
			*/
			var copyBackParams = new RenderGraphUtils.BlitMaterialParameters(destination, source, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
			renderGraph.AddBlitPass(copyBackParams, "CopyBack");
		}

		/// <summary>
		/// The name of the command buffer used for this pass.
		/// </summary>
		protected abstract string CommandName { get; }

		/// <summary>
		/// Sets properties on the material before the pass executes.
		/// </summary>
		protected abstract void SetMaterialProperties(Material material);
	}
}

#endif 
#endif
