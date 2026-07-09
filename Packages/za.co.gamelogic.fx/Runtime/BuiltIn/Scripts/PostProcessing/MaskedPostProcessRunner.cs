using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	/// <summary>
	/// Runs all post-processes found in children of a given root on the <see cref="Camera"/> this script is
	/// attached to, but applies a mask and effect strength. 
	/// </summary>
	/// <inheritdoc cref="PostProcessRunner"/>
	[ExecuteInEditMode]
	public sealed class MaskedPostProcessRunner : GLMonoBehaviour
	{
		private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
		private static readonly int OverlayTexID = Shader.PropertyToID("_OverlayTex");
		private static readonly int MaskTexID = Shader.PropertyToID("_MaskTex");

		[ReadOnly]
		[SerializeField] private Shader blendTextureShader;
		
		[Tooltip("The root transform whose children are scanned for enabled post-process components.")]
		[ValidateNotNull]
		[SerializeField] private Transform postProcessRoot = null;

		[Tooltip("Optional mask texture controlling where the effect is applied. White = full effect, black = no effect.")]
		[SerializeField] private Texture2D maskTexture = null;

		[Tooltip("Overall strength of the post-process effect. 0 = no effect, 1 = full effect.")]
		[Range(0f, 1f)]
		[SerializeField] private float effectStrength = 1.0f;
		
		private Material blendTextureMaterial;
		
		private Material BlendTextureMaterial
		{
			get
			{
				if (blendTextureMaterial != null)
				{
					return blendTextureMaterial;
				}
				
				if( blendTextureShader == null)
				{
					Debug.LogError("No blend texture shader set for " + name);
					return null;
				}
					
				blendTextureMaterial = new Material(blendTextureShader)
				{
					hideFlags = HideFlags.DontSave
				};
				
				return blendTextureMaterial;
			}
		}

#if UNITY_EDITOR 
		// Only for debugging
		// Not serialized in older versions of Unity. 
		// ReSharper disable once Unity.RedundantSerializeFieldAttribute  
		[SerializeField, ReadOnly]
#endif
		private IPostProcess[] postProcesses;

		public void OnRenderImage(RenderTexture sourceTexture, RenderTexture destinationTexture)
		{
			if (postProcessRoot == null || BlendTextureMaterial == null)
			{
				Graphics.Blit(sourceTexture, destinationTexture);
				return;
			}

			postProcesses = postProcessRoot
				.GetComponentsInChildren<MonoBehaviour>(includeInactive: false)
				.Where(component => component.enabled)
				.Where(component => component is IPostProcess)
				.Cast<IPostProcess>()
				.ToArray();

			if (!postProcesses.Any())
			{
				Graphics.Blit(sourceTexture, destinationTexture);
				return;
			}

			var currentSource = sourceTexture;
			var descriptor = sourceTexture.descriptor;

			foreach (var postProcess in postProcesses)
			{
				var temporaryTexture = RenderTexture.GetTemporary(descriptor);
				postProcess.OnRenderImage(currentSource, temporaryTexture);

				// Release the current source if it was a temporary texture
				if (currentSource != sourceTexture)
				{
					RenderTexture.ReleaseTemporary(currentSource);
				}

				// Swap textures
				currentSource = temporaryTexture;
			}
			
			SetBlendMaterialProperties(currentSource);
			Graphics.Blit(sourceTexture, destinationTexture, BlendTextureMaterial);
			
			// Release the last temporary texture
			if (currentSource != sourceTexture)
			{
				RenderTexture.ReleaseTemporary(currentSource);
			}
		}

		public void OnEnable()
		{
			blendTextureShader = ShaderNames.GetShader(Constants.ShaderNameRoot + ShaderNames.BlendTexture);
		}

		public void OnDisable()
		{
			if (blendTextureMaterial != null)
			{
				DestroyImmediate(blendTextureMaterial);
			}
		}
		
		private void SetBlendMaterialProperties(RenderTexture currentSource)
		{
			BlendTextureMaterial.EnableKeyword("IGNORE_OVERLAY_ALPHA");
			BlendTextureMaterial.SetTexture(OverlayTexID, currentSource);
			BlendTextureMaterial.SetFloat(OpacityID, effectStrength);

			if (maskTexture == null)
			{
				BlendTextureMaterial.DisableKeyword("USE_MASK");
			}
			else
			{
				BlendTextureMaterial.EnableKeyword("USE_MASK");
				BlendTextureMaterial.SetTexture(MaskTexID, maskTexture);
			}
		}
	}
}
