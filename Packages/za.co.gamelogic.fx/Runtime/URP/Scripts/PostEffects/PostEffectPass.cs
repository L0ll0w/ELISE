#if GAMELOGIC_HAS_URP

#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE
#define GAMELOGIC_USE_RENDER_GRAPH
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.URP.PostProcessing
{
	using UnityEngine.Experimental.Rendering;

	/// <summary>
	/// Implements a basic render pass for <see cref="PostProcessRendererFeature"/>.
	/// </summary>
	
	#if GAMELOGIC_USE_RENDER_GRAPH
	[System.Obsolete("Use RenderGraph_PostEffectPass instead")] 
	#endif
	public abstract class PostEffectPass : ScriptableRenderPass
	{
		private readonly Material material;
		private readonly bool requiresNormals;
		private readonly bool requiresDepth;
		
		private readonly CommandBuffer commandBuffer = new CommandBuffer();
		private RTHandle temporaryRT;

		private ScriptableRenderer renderer;
		
#if !GAMELOGIC_USE_RENDER_GRAPH
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			if (requiresDepth)
			{
				ConfigureInput(ScriptableRenderPassInput.Depth);
			}
			
			if (requiresNormals)
			{
				ConfigureInput(ScriptableRenderPassInput.Normal);
			}
		}
#endif
		
		/// <summary>
		/// Initializes a new instance of the <see cref="PostEffectPass"/> class.
		/// </summary>
		/// <param name="material">The material to use for the effect.</param>
		/// <param name="injectionPoint">The render pass event to inject the effect at.</param>
		/// <param name="requiresNormals">Whether this effect requires normals texture.</param>
		/// <param name="requiresDepth">Whether this effect requires depth texture.</param>
		protected PostEffectPass(
			Material material,
			RenderPassEvent injectionPoint,
			bool requiresNormals = false,
			bool requiresDepth = false)
		{
			this.material = material;
			renderPassEvent = injectionPoint;
			this.requiresNormals = requiresNormals;
			this.requiresDepth = requiresDepth;
		}
		
		/// <summary>
		/// The renderer that will render this pass. 
		/// </summary>
		/// <param name="newRenderer">The scriptable renderer for the current frame.</param>
		/// <remarks>
		/// <see cref="PostProcessRendererFeature"/> uses this to pass the renderer so that the pass can retrieve the camera color target
		/// for blitting. 
		/// </remarks>
		internal void SetRenderer(ScriptableRenderer newRenderer) => renderer = newRenderer;

		/// <inheritdoc/>
#if !GAMELOGIC_USE_RENDER_GRAPH
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (material == null)
			{
				return;
			}
			
			commandBuffer.name = CommandName;

			SetMaterialProperties(material);

			// camera buffer info
			var desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.depthBufferBits = 0;

			GraphicsFormat gfxFormat = GraphicsFormatUtility.GetGraphicsFormat(
				desc.colorFormat,
				renderingData.cameraData.isHdrEnabled
					? RenderTextureReadWrite.Linear
					: RenderTextureReadWrite.sRGB
			);

			// Allocate RTHandle only if needed
			if (temporaryRT == null 
				|| temporaryRT.rt.width != desc.width 
				|| temporaryRT.rt.height != desc.height 
				|| temporaryRT.rt.graphicsFormat != gfxFormat)
			{
				temporaryRT?.Release();

				temporaryRT = RTHandles.Alloc(
					desc.width,
					desc.height,
					slices: 1,
					depthBufferBits: 0,
					colorFormat: gfxFormat,
					filterMode: FilterMode.Bilinear,
					wrapMode: TextureWrapMode.Clamp,
					name: "_TempTex"
				);
			}

			// Unity 2021 camera color is NOT an RTHandle.
			// Therefore, Blitter cannot be used; only CommandBuffer.Blit.

			// source → temp
			#if UNITY_2022_1_OR_NEWER
				var targetId = renderer.cameraColorTargetHandle;
			#else
				var targetId = renderer.cameraColorTarget;
			#endif
			
			commandBuffer.Blit(targetId, temporaryRT, material);

			// temp → source
			commandBuffer.Blit(temporaryRT, targetId);

			context.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();
		}
#endif
		
		/// <summary>
		/// The name of the command buffer used for this pass.
		/// </summary>
		protected abstract string CommandName { get; }
		
		/// <summary>
		/// This sets properties on the given material.
		/// </summary>
		/// <param name="material">The material to set properties on.</param>
		/// <remarks>
		/// Implementors: override this to set the properties needed for your effect.
		/// </remarks>
		protected abstract void SetMaterialProperties(Material material);
	}
}
#endif
