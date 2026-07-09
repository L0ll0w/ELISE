using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using Gamelogic.Fx.PostProcessing;
using UnityEngine;

namespace Gamelogic.Fx.Mapping
{
	/// <summary>
	/// Defines a set of properties used to render a scene map into a texture.
	/// </summary>
	/// <remarks>
	/// A map encodes per-object or per-pixel information (such as IDs, UVs, depth-related data, or custom attributes)
	/// into a render texture that can later be consumed by post-processing effects.
	/// 
	/// <see cref="MapProperties"/> acts as the configuration layer between map-rendering infrastructure
	/// (such as <c>ObjectInfoRenderer</c> in the built-in pipeline or <c>GLMapRendererFeature</c> in URP)
	/// and the shaders used to generate the map.
	/// 
	/// Implementations typically:
	/// 1. Specify the main map shader via <see cref="MapProperties.ShaderName"/>.
	/// 2. Optionally specify a background shader via <see cref="MapProperties.BackgroundShaderName"/>.
	/// 3. Expose serialized fields that control how objects write data into the map.
	/// 4. Apply per-material and per-renderer state in the appropriate override methods.
	/// </remarks>
	[Serializable]
	public abstract class MapProperties : ShaderProperties
	{
		[Tooltip(ToolTipStrings.AutomaticallySet)]
		[ReadOnly]
		[SerializeField] private Shader backgroundShader;
	
		/// <summary>
		/// The name of the shader used to render the background when rendering this object info.
		/// </summary>
		/// <remarks>
		/// It is OK for this to return null if no background shader is needed, in which the <see cref="BackgroundColor"/> will appear.
		///
		/// This property should always return the same value during the lifetime of this <see cref="MapProperties"/>.
		/// </remarks>
		protected virtual string BackgroundShaderName => null;
	
		/// <summary>
		/// Gets the resolved background shader.
		/// </summary>
		/// <value>
		/// The background <see cref="UnityEngine.Shader"/>, or <see langword="null"/> if no background pass is used.
		/// </value>
		public Shader BackgroundShader => backgroundShader;

		/// <summary>
		/// The color used to clear the render texture before any rendering takes place. 
		/// </summary>
		public virtual Color BackgroundColor => Color.black;

		/// <summary>
		/// Applies background-specific properties to the background material.
		/// </summary>
		/// <param name="material">
		/// The material created from <see cref="BackgroundShader"/>.
		/// </param>
		/// <remarks>
		/// Override this method to configure background shader uniforms such as clear color,
		/// gradients, or other map-wide background parameters.
		/// </remarks>
		public virtual void SetBackgroundMaterialProperties(Material material)
		{ // Do nothing by default
		}

		/// <summary>
		/// Applies per-renderer properties before drawing a renderer into the map.
		/// </summary>
		/// <param name="block">
		/// The <see cref="MaterialPropertyBlock"/> used for this draw call.
		/// </param>
		/// <param name="renderer">
		/// The renderer currently being drawn into the map.
		/// </param>
		/// <remarks>
		/// Override this method to set renderer-specific data such as object IDs,
		/// per-object colors, UV transforms, or custom attributes.
		/// </remarks>
		public virtual void SetRendererProperties(MaterialPropertyBlock block, Renderer renderer)
		{ // Do nothing by default
		}

		/// <summary>
		/// Called when the map properties are enabled.
		/// </summary>
		/// <remarks>
		/// The default implementation resolves both the main map shader
		/// (via <see cref="ShaderProperties.OnEnable"/>) and the optional background shader.
		/// Subclasses overriding this method should normally call <c>base.OnEnable()</c>.
		/// </remarks>
		public override void OnEnable()
		{
			base.OnEnable();

			if (BackgroundShaderName != null)
			{
				backgroundShader = ShaderNames.GetShader(BackgroundShaderName);
			}
		}
	}
}
