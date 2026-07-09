#if GAMELOGIC_HAS_URP

#if GAMELOGIC_HAS_URP_RENDER_GRAPH && !GAMELOGIC_URP_COMPATIBILITY_MODE
#define GAMELOGIC_USE_RENDER_GRAPH
#endif

using System;
using System.Collections.Generic;
using Gamelogic.Extensions;
using Gamelogic.Fx.Mapping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Gamelogic.Fx.Internal;
using UnityEngine.Serialization;
using Gamelogic.Fx.URP.Internal;

namespace Gamelogic.Fx.URP.Mapping
{
	/// <summary>
	/// Non-generic base class for map render features.
	/// </summary>
	// Dummy for names, but also useful to have a non-generic base class. 
	public abstract class GLMapRendererFeature : ScriptableRendererFeature
	{
	}

	/// <summary>
	/// A render feature that renders a map to a render texture.
	/// </summary>
	/// <typeparam name="TMapProperties">The type of map properties used to configure the map rendering.</typeparam>
	/// <remarks>
	/// A map encodes scene information in a texture that can be used by post effects.
	///
	/// To use a map render feature (a concrete subclass of this generic class):
	/// 1. Add the render feature to your <see cref="ScriptableRenderer"/> asset.
	/// 2. Configure the properties. 
	///
	/// To render your own type of map:
	/// 1. Create a shader that can render the information you need.
	/// 2. (Optional) Create a background shader to render the background of the map.
	/// 3. Create a new class inheriting from <see cref="MapProperties"/>, that sets up the material properties for your shader
	/// (and background shader if needed). 
	/// 4. Extend from <see cref="GLMapRendererFeature{TMapProperties}"/> using your new <see cref="MapProperties"/>
	/// type as the generic parameter. Generally the class can be empty (a concrete type is needed to show up in the inspector).
	/// </remarks>
	public class GLMapRendererFeature<TMapProperties> : GLMapRendererFeature where TMapProperties : MapProperties
	{
		[Tooltip(ToolTipStrings.WhenToRender)]
		[SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
		
		/*	Why is editor separate?	*/
		[Tooltip(ToolTipStrings.UpdateRenderListEachFrameInPlayMode)]
		[SerializeField] private bool updateRenderListEachFrame = false;
	
		[Tooltip(ToolTipStrings.UpdateRenderListEachFrameInEditMode)]
		[SerializeField] private bool updateRenderListEachFrameInEditor = true;
		
		[Tooltip(ToolTipStrings.RenderTarget)]
		[ValidateNotNull]
		[SerializeField] private RenderTexture targetTexture;

		[Tooltip(ToolTipStrings.CameraScope)]
		[SerializeField] private CameraScope cameraScope = CameraScope.Base;

		/// <inheritdoc cref="PostProcessing.PostProcessRendererFeature{TShaderProperties}.CustomCameraScopePredicate"/>
		public Func<CameraData, bool> CustomCameraScopePredicate { get; set; }

		[Tooltip(ToolTipStrings.MapLayerMask)]
		[SerializeField] private LayerMask layerMask = -1;

		[FormerlySerializedAs("map")]
		[Tooltip(ToolTipStrings.MapProperties)]
		[SerializeField] private TMapProperties mapProperties;

		
#if GAMELOGIC_USE_RENDER_GRAPH
		private RTHandle colorHandle;
		private RenderGraph_MapPass mapPass;
#else
		private MapPass mapPass;
#endif
		
		private readonly List<Renderer> renderers = new();
		
		/// <inheritdoc/>
		public override void Create()
		{
			if (mapProperties == null)
			{
				/*	Happens when the render feature is added. Create DOES get called again,
					and the second time the mapProperties is not null.
				*/ 
				return;
			}
			
			mapProperties.OnEnable();
			
			if (mapProperties.Shader == null)
			{
				Debug.LogError($"{nameof(GLMapRendererFeature)}: Shader is null. Cannot create render pass.");
				return;
			}

#if GAMELOGIC_USE_RENDER_GRAPH
			mapPass = new RenderGraph_MapPass(mapProperties, layerMask)
			{
				renderPassEvent = passEvent
			};
#else
			mapPass = new MapPass(mapProperties, layerMask)
			{
				renderPassEvent = passEvent
			};
#endif
			RefreshRendererList();
		}
		
		public void RefreshRendererList()
		{
			FxUtils.RefreshRendererList(renderers);
			mapPass.Renderers = renderers;
		}

		/// <inheritdoc/>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (targetTexture == null)
			{
				return;
			}
			var cameraType = renderingData.cameraData.cameraType;
			if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
			{
				return;
			}

			if (!Utils.ShouldExecute(ref renderingData.cameraData, cameraScope, CustomCameraScopePredicate))
			{
				return;
			}
			
#if UNITY_EDITOR
			bool shouldUpdate =
				Application.isPlaying
					? updateRenderListEachFrame
					: updateRenderListEachFrameInEditor;
			
			if (shouldUpdate)
			{
				RefreshRendererList();
			}
#else 
			if (updateRenderListEachFrame)
			{
				RefreshRendererList();
			}
#endif
			
#if !GAMELOGIC_USE_RENDER_GRAPH
			mapPass.SetTarget(targetTexture);
#else
			colorHandle = RTHandles.Alloc(targetTexture);
			mapPass.SetTarget(colorHandle);
#endif
			renderer.EnqueuePass(mapPass);
		}
		
		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			mapPass?.Dispose();
			mapPass = null;
#if GAMELOGIC_USE_RENDER_GRAPH
			colorHandle?.Release();
			colorHandle = null;
#endif
		}
	}
}
#endif
