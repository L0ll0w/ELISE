using Gamelogic.Extensions;
using Gamelogic.Fx.PostProcessing;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	/// <summary>
	/// The base class for all post process effects. 
	/// </summary>
	/// <remarks>
	/// See <see cref="PostProcess{TShaderProperties}"/> or <see cref="SeparablePostProcess{TShaderProperties}"/> for
	/// more complete implementations.
	/// </remarks>
	public class PostProcess : GLMonoBehaviour
	{
	}
	
	/// <summary>
	/// ⭐ Contains basic functionality for a full screen post process effect.
	/// </summary>
	/// <remarks>
	/// Intended for the built-in render pipeline.
	/// 
	/// <b>To define a new post process</b>
	///
	/// If it is a separable effect, see <see cref="SeparablePostProcess{TShaderProperties}"/> instead.
	/// 
	/// 1. Define a new class (for example, `ToneMapProperties` that extends from <see cref="ShaderProperties"/>
	/// and override the relevant methods and properties.
	/// 2. Define `ToneMapPostProcess` that extends from
	/// `PostProcess{ToneMapProperties}`.
	///
	/// You can then use `ToneMapPostProcess` like any other post process.
	/// </remarks>
	[ExecuteInEditMode]
	public class PostProcess<TShaderProperties> 
		: PostProcess, IPostProcess
		where TShaderProperties : ShaderProperties, new()
	{
		[Tooltip("Whether the properties should be set each frame.")]
		[SerializeField] private bool setEachFrame = true;
		
		[Tooltip("Shader properties that configure this post-processing effect.")]
	[SerializeField] private TShaderProperties properties;
		
		private bool hasBeenSet = false;
		private Material screenMaterial;
		
		/// <summary>
		/// The shader properties for this post process.
		/// </summary>
		public TShaderProperties Properties => properties;
		
		/* Why public and not protected? This allows use to hook up post processes in complex ways.
		*/
		/// <summary>
		/// Renders the post process effect.
		/// </summary>
		/// <param name="sourceTexture">The full screen source texture.</param>
		/// <param name="destTexture">The render target for the post process.</param>
		public virtual void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
		{
			if (properties.Shader != null)
			{
				if (setEachFrame || !hasBeenSet)
				{
					SetMaterialProperties(EffectMaterial);
					hasBeenSet = true;
				}

				Graphics.Blit(sourceTexture, destTexture, EffectMaterial);
			}
			else
			{
				Graphics.Blit(sourceTexture, destTexture);
			}
		}
		
		/// <summary>
		/// Gets the shader by name. 
		/// </summary>
		/// <remarks>
		/// This is helpful while developing shaders, to get a new copy of it. 
		/// </remarks>
		[InspectorButton]
		public void ReloadShader() => properties.OnEnable();
		
		/* Internal to make it easier to support tooling.
		*/
		/// <summary>
		/// The material used to apply the post process effect.
		/// </summary>
		internal Material EffectMaterial
		{
			get
			{
				if (properties.Shader == null)
				{
					Debug.LogError("No shader set for post process " + name);
					return null;
				}

				if (screenMaterial == null)
				{
					screenMaterial = new Material(properties.Shader)
					{
						hideFlags = HideFlags.DontSave
					};
				}

				return screenMaterial;
			}
		}

		private void Start()
		{
			if (properties.Shader == null || !properties.Shader.isSupported)
			{
				enabled = false;
			}
		}
		
		private void OnEnable() => properties.OnEnable();

		private void OnValidate() => properties.OnValidate();

		private void OnDisable()
		{
			if (screenMaterial != null)
			{
				DestroyImmediate(screenMaterial);
			}
		}

		/// <summary>
		/// Applies all the properties defined for this post process in the inspector to the <see cref="EffectMaterial"/>. 
		/// </summary>
		private void SetMaterialProperties(Material effectMaterial)
		{
			properties.SetMaterialProperties(effectMaterial);
		}
	}
}
