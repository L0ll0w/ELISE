using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.Mapping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Gamelogic.Fx.BuiltIn.Mapping
{
	/// <summary>
	/// Non-generic base class for all map renderers.
	/// </summary>
	/// <seealso cref="MapRenderer{TMapProperties}"/>
	public class MapRenderer : GLMonoBehaviour
	{
	}
	
	/// <summary>
	/// Renders maps using a command buffer and a dedicated camera.
	/// </summary>
	/// <typeparam name="TMapProperties">The type of map properties used to configure the map rendering.</typeparam>
	/// <remarks>
	/// A map encodes scene information in a texture that can be used by post effects.
	///
	/// To use a map renderer (a concrete subclass of this generic class):
	/// 1. Add the component to a GameObject in your scene.
	/// 2. Configure the properties. 
	///
	/// To render your own type of map:
	/// 1. Create a shader that can render the information you need.
	/// 2. (Optional) Create a background shader to render the background of the map.
	/// 3. Create a new class inheriting from <see cref="MapProperties"/>, that sets up the material properties for your shader
	/// (and background shader if needed). 
	/// 4. Create extend from <see cref="MapRenderer{TMapProperties}"/> class using your new <see cref="MapProperties"/>
	/// type as the generic parameter. Generally the class can be empty (a concrete type is needed to show up in the inspector).
	///
	/// If your camera properties change (through animation or scripting) you need to set <see cref="updateCameraEachFrame"/>
	/// to <see langword="true"/>. You may need to set the execution order to make sure this script executes after the
	/// code that updates the camera. 
	///
	/// If your source camera itself changes, you set <see cref="SourceCamera"/>.  
	/// </remarks>
	[ExecuteInEditMode]
	public class MapRenderer<TMapProperties> 
		: MapRenderer 
		where TMapProperties : MapProperties, new()
	{
		#region Constants
		private const CameraEvent WhenToRender = CameraEvent.BeforeImageEffects;
		#endregion
		
		#region Inspector Fields
		/* Readonly since it is initialized by the script, but prefer not to hide it because it helps debugging. */
		[Tooltip(ToolTipStrings.AutomaticallySet)]
		[ReadOnly]
		[SerializeField] private Camera renderCamera;
		
		/*	Why is editor separate?	*/
		[Tooltip(ToolTipStrings.UpdateRenderListEachFrameInPlayMode)]
		[SerializeField] private bool updateRenderListEachFrame = false;
	
		[Tooltip(ToolTipStrings.UpdateRenderListEachFrameInEditMode)]
		[SerializeField] private bool updateRenderListEachFrameInEditor = true;
		
		[Tooltip(ToolTipStrings.SourceCamera)]
		[ValidateNotNull]
		[SerializeField] private Camera sourceCamera;

		[Tooltip(ToolTipStrings.UpdateCameraEachFrame)]
		[SerializeField] private bool updateCameraEachFrame = false;
		
		[Tooltip("Layer mask used to select which objects are rendered into the map.")]
		[SerializeField] private LayerMask layerMask = -1;
	
		[Tooltip(ToolTipStrings.RenderTarget)]
		[ValidateNotNull]
		[SerializeField] private RenderTexture target;

		[FormerlySerializedAs("map")]
		[FormerlySerializedAs("objectInfo")] 
		[Tooltip(ToolTipStrings.MapProperties)]
		[SerializeField] private TMapProperties mapProperties = new();
		#endregion
		
		#region Fields
		private readonly List<Renderer> renderers = new();
		private Material material;
		private Material backgroundMaterial;
		private CommandBuffer commandBuffer;
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets and sets the source camera used to render the map. 
		/// </summary>
		public Camera SourceCamera
		{
			get => sourceCamera;
			
			set
			{
				sourceCamera = value;
				CreateOrUpdateCamera();
			}
		}

		/// <summary>
		/// Gets and sets the layer mask used to select which objects are rendered into the map.
		/// </summary>
		public LayerMask LayerMask
		{
			get => layerMask;

			set
			{
				layerMask = value;
				CreateOrUpdateCamera();
			}
		}

		/// <summary>
		/// Gets and sets whether to update the camera properties each frame.
		/// </summary>
		public bool UpdateCameraEachFrame
		{
			get => updateCameraEachFrame;
			set => updateCameraEachFrame = value;
		}

		/// <summary>
		/// Gets and sets whether to update the list of renderers each frame in playmode. 
		/// </summary>
		public bool UpdateRenderListEachFrame
		{
			get => updateRenderListEachFrame;
			set => updateRenderListEachFrame = value;
		}

		/// <summary>
		/// Gets and sets whether to update the list of renderers each frame in edit mode. This only has an effect when
		/// not playing.
		/// </summary>
		public bool UpdateRenderListEachFrameInEditor
		{
			get => updateRenderListEachFrameInEditor;
			set => updateRenderListEachFrameInEditor = value;
		}

		/// <summary>
		/// Gets the map properties for this map renderer. 
		/// </summary>
		public TMapProperties MapProperties => mapProperties;
		#endregion

		#region API methods
		/// <summary>
		/// Updates the list of renderers to be drawn.
		/// </summary>
		/// <remarks>
		/// This can potentially be slow, so be careful calling it each frame. 
		/// </remarks>
		[InspectorButton]
		public void RefreshRendererList() => FxUtils.RefreshRendererList(renderers);
		#endregion

		#region Messages
		private void OnEnable()
		{
			mapProperties.OnEnable();
			SetupDrawBackend();
			CreateOrUpdateMaterials();
			CreateOrUpdateCamera();
			RefreshRendererList();

			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
	
		private void Update()
		{
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
			
			BuildCommandBuffer();
		}
		
		private void LateUpdate()
		{
			/* Runs in LateUpdate since camera behavior is often in late update.
				It may still be the case that this executes before the camera is updated - this can be fixed with execution
				order. 
			*/
			if (updateCameraEachFrame)
			{
				CreateOrUpdateCamera();
			}
			
			renderCamera.Render();
		}

		private void OnDisable() => Cleanup();
		#endregion

		#region Helper Methods
		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshRendererList();
		
		private void SetupDrawBackend()
		{
			commandBuffer = new CommandBuffer { name = $"{GetType().Name}_Draw" };
		}

		private void CreateOrUpdateCamera()
		{
			// Can be null early (OnValidate before OnEnable)
			if (commandBuffer == null)
			{
				return;
			}

			if (renderCamera == null)
			{
				renderCamera = new GameObject($"{GetType().Name}_Camera").AddComponent<Camera>();
				renderCamera.transform.SetParent(transform, false);
				renderCamera.enabled = false;
			}

			renderCamera.CopyFrom(sourceCamera);
			renderCamera.cullingMask = layerMask;


			var buffers = renderCamera.GetCommandBuffers(WhenToRender);
		
			if (buffers == null || !buffers.Contains(commandBuffer))
			{
				renderCamera.AddCommandBuffer(WhenToRender, commandBuffer);
			}
		}

		[InspectorButton]
		private void Rebind()
		{
			Cleanup();
			OnEnable();
		}
		
		private void BuildCommandBuffer()
		{
			if (material == null)
			{
				return;
			}

			commandBuffer.Clear();
			int depthId = Shader.PropertyToID("_MapDepth");
			commandBuffer.GetTemporaryRT(depthId, target.width, target.height, 24);
			commandBuffer.SetRenderTarget(target.colorBuffer, new RenderTargetIdentifier(depthId));

			FxUtils.DrawBackground(commandBuffer, backgroundMaterial, mapProperties);

			mapProperties.SetMaterialProperties(material);
			
			foreach (var objRenderer in renderers)
			{
				if ((layerMask & (1 << objRenderer.gameObject.layer)) == 0)
				{
					continue;
				}

				var mesh = FxUtils.GetMesh(objRenderer);
				
				if (mesh == null)
				{
					continue;
				}

				var block = new MaterialPropertyBlock();
				mapProperties.SetRendererProperties(block, objRenderer);

				int subMeshCount = objRenderer.sharedMaterials.Length;
				
				for (int submeshIndex = 0; submeshIndex < subMeshCount; submeshIndex++)
				{
					commandBuffer.DrawMesh(mesh, objRenderer.localToWorldMatrix, material, submeshIndex, 0, block);
				}
				
				commandBuffer.ReleaseTemporaryRT(depthId);
			}
		}
		
		private void CreateOrUpdateMaterials()
		{
			if(mapProperties.Shader == null)
			{
				Debug.LogError("Map shader is null.");
				return;
			}

			FxUtils.UpdateMaterial(ref material, mapProperties.Shader);
			
			//It's OK for background shader to be null
			FxUtils.UpdateMaterial(ref backgroundMaterial, mapProperties.BackgroundShader); 
		}
		
		private void Cleanup()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			
			if (renderCamera != null && commandBuffer != null)
			{
				renderCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
			}
		
			if (renderCamera != null)
			{
				DestroyUniversal(renderCamera.gameObject);
			}
			
			DestroyUniversal(material);
			DestroyUniversal(backgroundMaterial);
		
			if (commandBuffer != null)
			{
				commandBuffer.Release();
				commandBuffer = null;
			}
		}
		
		#endregion
	}
}
