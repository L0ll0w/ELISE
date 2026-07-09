using Gamelogic.Extensions;
using Gamelogic.Extensions.Internal;
using Gamelogic.Fx.PostProcessing;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	
	/// <summary>
	/// The base class for all separable post process effects.
	/// </summary>
	/// <remarks>
	/// See <see cref="SeparablePostProcess{TShaderProperties}"/> for a more complete implementation.
	/// </remarks>
	public class SeparablePostProcess : PostProcess
	{
		protected static readonly Vector2 Horizontal = new Vector2(1, 0);
		protected static readonly Vector2 Vertical = new Vector2(0, 1);
		protected static readonly int DirectionID = Shader.PropertyToID("_Direction");
	}
	
	/// <summary>
	/// Encapsulates a post process based on a separable filter, such as a box blur. 
	/// </summary>
	/// <remarks>
	/// An effect is separable if it can be implemented as two passes, one in the horizontal direction and one in the
	/// vertical, such as box blur or Gaussian blur.
	///
	/// *To implement a new separable post process effect*
	///
	/// 1. Define a new class (for example, `BlurProperties` that extends from <see cref="SeparableShaderProperties"/> and
	/// override the relevant methods and properties. 
	/// 2. Define `BlurPostProcess` that extends from `SeparablePostProcess{BlurProperties}`
	///
	/// You can then use `BlurPostProcess` like any other post process.
	/// </remarks>
	[Version(1, 1, 0)]
	[ExecuteInEditMode]
	public class SeparablePostProcess<TShaderProperties> 
		: SeparablePostProcess, IPostProcess
		where TShaderProperties : SeparableShaderProperties
	{
		[Tooltip("When enabled, shader properties are applied every frame. Disable for better performance when properties don't change.")]
		[SerializeField] private bool setEachFrame = true;

		[Tooltip("Shader properties that configure this separable post-processing effect.")]
		[SerializeField] private TShaderProperties properties;
		
		private Material screenMaterial;

		private TShaderProperties Properties => properties;

		/* Why internal? So property drawers can do checks, similar to EffectMaterial in PostProcess.
		*/
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
		
		public void Start()
		{
			if (properties.Shader == null || !properties.Shader.isSupported)
			{
				enabled = false;
			}
		}
		
		public void OnEnable() => properties.OnEnable();

		public void OnValidate() => properties.OnValidate();
		
		public void OnDisable()
		{
			if (screenMaterial != null)
			{
				DestroyImmediate(screenMaterial);
			}
		}
		
		/// <inheritdoc />
		public virtual void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
		{
			if (properties.Shader == null)
			{
				Graphics.Blit(sourceTexture, destTexture);
				return;
			}

			var descriptor = sourceTexture.descriptor;
			var renderTexture = RenderTexture.GetTemporary(descriptor);

			Pass(Direction.Horizontal, sourceTexture, renderTexture);
			Pass(Direction.Vertical, renderTexture, destTexture);
			
			RenderTexture.ReleaseTemporary(renderTexture);
		}
		
		/// <summary>
		/// Sets the shader-specific properties on the effect material before each pass.
		/// </summary>
		/// <param name="effectMaterial">The material to configure.</param>
		/// <remarks>
		/// Implementors should override this method to set the shader's specific properties.
		///
		/// The method is called twice, once for each direction.
		/// The direction vector is automatically set to <c>(1, 0)</c> for the first pass and <c>(0, 1)</c> for the second
		/// pass, and so are the kernel properties: <c>KernelSize</c>, <c>KernelOffset</c>, <c>JumpSize</c>.  
		/// </remarks>
		protected virtual void SetMaterialProperties(Material effectMaterial)
		{
			properties.SetMaterialProperties(effectMaterial);
		}

		/// <summary>
		/// Gets the shader by name. 
		/// </summary>
		/// <remarks>
		/// This is helpful while developing shaders, to get a new copy of it. 
		/// </remarks>
		[InspectorButton]
		private void ReloadShader() => properties.OnEnable();
		
		private void Pass(Direction direction, RenderTexture source, RenderTexture destination)
		{
			EffectMaterial.SetVector(DirectionID, direction == Direction.Horizontal ? Horizontal : Vertical);

			if (setEachFrame)
			{
				EffectMaterial.SetKernel(properties.Kernel);
				SetMaterialProperties(EffectMaterial);
			}
			
			Graphics.Blit(source, destination, EffectMaterial);
		}
	}
}
