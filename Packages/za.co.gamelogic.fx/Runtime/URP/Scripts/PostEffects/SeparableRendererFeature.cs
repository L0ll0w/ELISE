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

namespace Gamelogic.Fx.URP.PostProcessing
{
	/// <summary>
	/// Non-generic base class for all separable renderer features.
	/// </summary>
	public abstract class SeparableRendererFeature : ScriptableRendererFeature
	{
	}

	/// <summary>
	/// Implements a separable post-processing effect for the Universal Render Pipeline using two render passes.
	/// </summary>
	/// <typeparam name="TShaderProperties">
	/// The type that defines the shader properties and kernel configuration for this separable effect.
	/// </typeparam>
	/// <remarks>
	/// This renderer feature is the URP counterpart to the built-in
	/// <c>SeparablePostProcess&lt;TShaderProperties&gt;</c>.
	/// <para/>
	/// A separable effect is executed in two one-dimensional passes:
	/// <list type="bullet">
	/// <item>A horizontal pass with direction <c>(1, 0)</c>.</item>
	/// <item>A vertical pass with direction <c>(0, 1)</c>.</item>
	/// </list>
	/// This reduces the cost of filtering operations such as box blurs and Gaussian blurs
	/// compared to a full two-dimensional kernel.
	/// <para/>
	/// The feature creates two materials from the same shader and enqueues two render passes
	/// at the configured <see cref="RenderPassEvent"/>. Kernel parameters and other shader
	/// properties are applied automatically before each pass.
	/// <para/>
	/// On Unity 6 and newer with render graph enabled, passes use <c>RecordRenderGraph</c>
	/// and <see cref="UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.AddBlitPass"/>.
	/// On older versions or when compatibility mode is active, the legacy <c>Execute</c> path
	/// is used with <c>CommandBuffer.Blit</c>.
	/// <para/>
	/// To implement a new separable URP post-process:
	/// <list type="number">
	/// <item>Create a subclass of <see cref="SeparableShaderProperties"/> defining the kernel and shader parameters.</item>
	/// <item>Create a concrete renderer feature inheriting from <see cref="SeparableRendererFeature{TShaderProperties}"/>.</item>
	/// <item>Add the renderer feature to a URP renderer asset.</item>
	/// </list>
	/// </remarks>
	public class SeparableRendererFeature<TShaderProperties>
		: SeparableRendererFeature
		where TShaderProperties : SeparableShaderProperties, new()
	{
#if GAMELOGIC_USE_RENDER_GRAPH
		private sealed class SeparablePassWrapper : RenderGraph_PostEffectPass
#else
		private sealed class SeparablePassWrapper : PostEffectPass
#endif
		{
			private readonly Vector2 direction;
			private readonly int directionId = Shader.PropertyToID("_Direction");
			private readonly Action<Material> userProperties;
			
			public SeparablePassWrapper(
				string commandName,
				RenderPassEvent @event,
				Material material,
				Vector2 direction,
				Action<Material> userProperties)
				: base(material, @event, false, false)
			{
				CommandName = commandName;
				this.direction = direction;
				this.userProperties = userProperties;
			}

			protected override string CommandName { get; }

			protected override void SetMaterialProperties(Material material)
			{
				material.SetVector(directionId, direction);
				userProperties(material);
			}
		}

		/// <summary>
		/// Determines when the passes are inserted into the URP render pipeline.
		/// Defaults to <see cref="RenderPassEvent.AfterRenderingPostProcessing"/>.
		/// </summary>
		[Tooltip(ToolTipStrings.WhenToRender)]
		[SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

		[Tooltip(ToolTipStrings.CameraScope)]
		[SerializeField] private CameraScope cameraScope = CameraScope.Base;

		/// <inheritdoc cref="PostProcessRendererFeature{TShaderProperties}.CustomCameraScopePredicate"/>
		public Func<CameraData, bool> CustomCameraScopePredicate { get; set; }

		[Tooltip(ToolTipStrings.PostEffectProperties)]
		[SerializeField] private TShaderProperties properties = new TShaderProperties();

		private Material horizontalMaterial;
		private Material verticalMaterial;
		private SeparablePassWrapper horizontalPass;
		private SeparablePassWrapper verticalPass;
		
		public void OnValidate() => properties.OnValidate();

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

			properties.OnEnable();

			if (properties.Shader == null)
			{
				Debug.LogError($"Shader not found: {properties.ShaderName}. {GetType().Name} will not be created.");
				return;
			}

			horizontalMaterial = new Material(properties.Shader);
			verticalMaterial = new Material(properties.Shader);

			horizontalPass = new SeparablePassWrapper(
				"Separable Horizontal",
				injectionPoint,
				horizontalMaterial,
				new Vector2(1, 0),
				SetKernelAndOtherProperties);

			verticalPass = new SeparablePassWrapper(
				"Separable Vertical",
				injectionPoint,
				verticalMaterial,
				new Vector2(0, 1),
				SetKernelAndOtherProperties);
		}

		/// <inheritdoc/>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			// Can happen, for example, when changing scenes in the editor
			if (
				horizontalPass == null
				|| verticalPass == null
				|| horizontalMaterial == null
				|| verticalMaterial == null)
			{
				Create();
			}

			if (horizontalPass == null || verticalPass == null)
			{
				return;
			}

			if (!Utils.ShouldExecute(ref renderingData.cameraData, cameraScope, CustomCameraScopePredicate))
			{
				return;
			}

#if !GAMELOGIC_USE_RENDER_GRAPH
			horizontalPass.SetRenderer(renderer);
			verticalPass.SetRenderer(renderer);
#endif
			renderer.EnqueuePass(horizontalPass);
			renderer.EnqueuePass(verticalPass);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CoreUtils.Destroy(horizontalMaterial);
				CoreUtils.Destroy(verticalMaterial);
				horizontalMaterial = null;
				verticalMaterial = null;
			}

			base.Dispose(disposing);
		}

		private void SetKernelAndOtherProperties(Material material)
		{
			material.SetKernel(properties.Kernel);
			properties.SetMaterialProperties(material);
		}
	}
}
#endif


/*
#if GAMELOGIC_HAS_URP
using Gamelogic.Fx.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.URP.PostProcessing
{
	/// <summary>
	/// Base class for all separable renderer features.
	/// </summary>
	public abstract class SeparableRendererFeature : ScriptableRendererFeature
	{
	}
	
	/// <summary>
	/// Implements a separable post-processing effect for the Universal Render Pipeline using two render passes.
	/// </summary>
	/// <typeparam name="TShaderProperties">
	/// The type that defines the shader properties and kernel configuration for this separable effect.
	/// </typeparam>
	/// <remarks>
	/// This renderer feature is the URP counterpart to the built-in
	/// <c>SeparablePostProcess&lt;TShaderProperties&gt;</c>.
	/// 
	/// A separable effect is executed in two one-dimensional passes:
	/// - A horizontal pass with direction <c>(1, 0)</c>.
	/// - A vertical pass with direction <c>(0, 1)</c>.
	///
	/// This reduces the cost of filtering operations such as box blurs and Gaussian blurs compared to
	/// a full two-dimensional kernel.
	/// 
	/// The feature creates two materials from the same shader and enqueues two render passes at the
	/// configured <see cref="RenderPassEvent"/>. Kernel parameters and other shader properties are applied
	/// automatically before each pass.
	/// 
	/// To implement a new separable URP post-process:
	/// 
	/// 1. Create a subclass of <see cref="SeparableShaderProperties"/> defining the kernel and shader parameters.
	/// 2. Create a concrete renderer feature inheriting from <see cref="SeparableRendererFeature{TShaderProperties}"/>.
	/// 3. Add the renderer feature to a URP renderer asset.
	/// 
	/// </remarks>

	public class SeparableRendererFeature<TShaderProperties> 
		: ScriptableRendererFeature
		where TShaderProperties : SeparableShaderProperties, new()
	{
		/// <summary>
		/// Determines when the passes are inserted into the URP render pipeline.
		/// Defaults to <see cref="RenderPassEvent.AfterRenderingPostProcessing"/>.
		/// </summary>
		[SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
		
		[SerializeField] private TShaderProperties properties = new TShaderProperties();
		
		private Material horizontalMaterial;
		private Material verticalMaterial;
		private ScriptableRenderPass horizontalPass;
		private ScriptableRenderPass verticalPass;

		public void OnValidate() => properties.OnValidate();

		/// <inheritdoc/>
		public override void Create()
		{
			if(properties == null)
			{
				/*	Happens when the render feature is added. Create DOES get called again,
					and the second time the mapProperties is not null.
				#1# 
				return;
			}
			
			properties.OnEnable();
			
			if(properties.Shader == null)
			{
				Debug.LogError($"Shader not found: {properties.ShaderName}");
				return;
			}

			horizontalMaterial = new Material(properties.Shader);
			verticalMaterial = new Material(properties.Shader);

			horizontalPass = new SeparablePassWrapper(
				"Separable Horizontal",
				injectionPoint,
				horizontalMaterial,
				new Vector2(1, 0),
				SetKernelAndOtherProperties
			);

			verticalPass = new SeparablePassWrapper(
				"Separable Vertical",
				injectionPoint,
				verticalMaterial,
				new Vector2(0, 1),
				SetKernelAndOtherProperties
			);
		}
		
		/// <inheritdoc/>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			((PostEffectPass)horizontalPass).SetRenderer(renderer);
			renderer.EnqueuePass(horizontalPass);

			((PostEffectPass)verticalPass).SetRenderer(renderer);
			renderer.EnqueuePass(verticalPass);
		}
		
		private void SetKernelAndOtherProperties(Material material)
		{
			material.SetKernel(properties.Kernel);
			properties.SetMaterialProperties(material);
		}
	}
}
#endif
*/
