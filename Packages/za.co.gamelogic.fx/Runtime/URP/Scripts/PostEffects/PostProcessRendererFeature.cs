#if GAMELOGIC_HAS_URP

#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE
#define GAMELOGIC_USE_RENDER_GRAPH
#endif

using System;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing;
using Gamelogic.Fx.URP.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Gamelogic.Fx.URP.PostProcessing
{
	/// <summary>
	/// Non-generic base class for all Gamelogic Fx post-process renderer features.
	/// </summary>
	/// <remarks>
	/// Needed for tools to work.
	/// </remarks>
	/*	Why no GL prefix?  
			To prevent collisions with other code that may have implemented similar render features. 
		Why the RendererFeature suffix?
			To prevent collisions with our BuiltIn classes (otherwise the AddRendererFeature may try to add the other class). 
	*/
	public abstract class PostProcessRendererFeature : ScriptableRendererFeature
	{
	}

	/// <summary>
	/// Implements a simple URP post-processing effect using a single shader and a single render pass.
	/// </summary>
	/// <typeparam name="TShaderProperties">
	/// The type that defines and applies the shader properties for this effect.
	/// </typeparam>
	/// <remarks>
	/// This class is the URP counterpart to the built-in
	/// <c>PostProcess&lt;TShaderProperties&gt;</c> component.
	/// <para/>
	/// It manages shader lookup, material creation, and render-pass injection,
	/// delegating all shader configuration to the associated
	/// <see cref="ShaderProperties"/> instance.
	/// <para/>
	/// On Unity 6 and newer with render graph enabled, the pass uses
	/// <c>RecordRenderGraph</c> and <see cref="UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.AddBlitPass"/>.
	/// On older versions or when compatibility mode is active, the pass uses
	/// the legacy <c>Execute</c> path with <c>CommandBuffer.Blit</c>.
	/// <para/>
	/// To define a new URP post-process effect:
	/// <list type="number">
	/// <item>Create a subclass of <see cref="ShaderProperties"/> that defines the shader name and parameters.</item>
	/// <item>Create a concrete renderer feature inheriting from <see cref="PostProcessRendererFeature{TShaderProperties}"/>.</item>
	/// <item>Add the renderer feature to a URP renderer asset.</item>
	/// </list>
	/// </remarks>
	public class PostProcessRendererFeature<TShaderProperties>
		: PostProcessRendererFeature
		where TShaderProperties : ShaderProperties, new()
	{
#if GAMELOGIC_USE_RENDER_GRAPH
		private sealed class SimplePassImpl : RenderGraph_PostEffectPass
		{
			private readonly Action<Material> setMaterialProperties;

			public SimplePassImpl(
				string name,
				RenderPassEvent injectionPoint,
				Material material,
				Action<Material> setMaterialProperties,
				bool requiresNormals,
				bool requiresDepth)
				: base(material, injectionPoint, requiresNormals, requiresDepth)
			{
				CommandName = name;
				this.setMaterialProperties = setMaterialProperties;
			}

			protected override string CommandName { get; }
			protected override void SetMaterialProperties(Material material) => setMaterialProperties(material);
		}
#else
		private sealed class SimplePassImpl : PostEffectPass
		{
			private readonly Action<Material> setMaterialProperties;

			public SimplePassImpl(
				string name,
				RenderPassEvent injectionPoint,
				Material material,
				Action<Material> setMaterialProperties,
				bool requiresNormals,
				bool requiresDepth)
				: base(material, injectionPoint, requiresNormals, requiresDepth)
			{
				CommandName = name;
				this.setMaterialProperties = setMaterialProperties;
			}

			protected override string CommandName { get; }
			protected override void SetMaterialProperties(Material material) => setMaterialProperties(material);
		}
#endif

		[Tooltip(ToolTipStrings.WhenToRender)]
		[SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

		[Tooltip(ToolTipStrings.CameraScope)]
		[SerializeField] private CameraScope cameraScope = CameraScope.Base;

		[Tooltip(ToolTipStrings.PostEffectProperties)]
		[SerializeField] private TShaderProperties properties;
		
		private Material material;
		private ScriptableRenderPass pass;
		
		public TShaderProperties Properties => properties;
		
		/// <summary>
		/// A predicate that determines whether this feature should execute for a given camera.
		/// Only used when <see cref="CameraScope"/> is set to <see cref="CameraScope.Custom"/>.
		/// </summary>
		/// <remarks>
		/// Assign this from a <see cref="MonoBehaviour"/>, for example in <c>Awake</c>:
		/// <code>
		/// var data = GetComponent&lt;UniversalAdditionalCameraData&gt;();
		/// var feature = data.scriptableRenderer.GetRendererFeature&lt;MyFeature&gt;();
		/// feature.CustomCameraScopePredicate = cameraData => cameraData.camera.CompareTag("MyTag");
		/// </code>
		/// If the predicate is not assigned, the feature will not execute on any camera.
		/// </remarks>
		public Func<CameraData, bool> CustomCameraScopePredicate { get; set; }

		/// <inheritdoc/>
		public override void Create()
		{
			if (properties == null)
			{
				/*	Happens when the render feature is first added. Create DOES get called again,
					and the second time properties is not null.
				*/
				return;
			}

			properties.OnValidate();
			properties.OnEnable();

			if (properties.Shader == null)
			{
				Debug.LogError($"Shader not found: {properties.ShaderName}. {GetType().Name} will not be created.");
				return;
			}

			material = new Material(properties.Shader);
			pass = new SimplePassImpl(
				properties.ShaderName,
				injectionPoint,
				material,
				properties.SetMaterialProperties,
				properties.RequiresNormalsTexture,
				properties.RequiresDepthTexture);
		}

		/// <inheritdoc/>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			// Can happen, for example, when changing scenes in the editor
			if (pass == null || material == null)
			{
				Create();
			}

			if (pass == null)
			{
				return;
			}

			if (!Utils.ShouldExecute(ref renderingData.cameraData, cameraScope, CustomCameraScopePredicate))
			{
				return;
			}

#if !GAMELOGIC_USE_RENDER_GRAPH
			((PostEffectPass)pass).SetRenderer(renderer);
#endif
			renderer.EnqueuePass(pass);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing && material != null)
			{
				CoreUtils.Destroy(material);
				material = null;
			}

			base.Dispose(disposing);
		}
	}
}
#endif
