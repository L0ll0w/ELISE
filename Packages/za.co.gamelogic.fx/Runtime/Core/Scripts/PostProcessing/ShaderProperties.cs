using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing
{
	/// <summary>
	/// Defines a serializable set of properties used to configure a specific post-processing shader.
	/// </summary>
	/// <remarks>
	/// A <see cref="ShaderProperties"/> instance acts as the bridge between inspector-exposed settings
	/// and the underlying shader uniforms.
	/// <para/>
	/// When enabled, this class attempts to load the shader specified by <see cref="ShaderName"/>
	/// and stores a reference to it so the shader is included correctly in builds.
	/// <para/>
	/// Subclasses typically:
	/// <list type="number">
	/// <item><description>Provide the shader name via <see cref="ShaderName"/>.</description></item>
	/// <item><description>Expose serialized fields that represent shader parameters.</description></item>
	/// <item><description>Override <see cref="SetMaterialProperties(Material)"/> to apply those parameters.</description></item>
	/// </list>
	/// </remarks>
	[Serializable]
	public abstract class ShaderProperties
	{
		[Tooltip(ToolTipStrings.AutomaticallySet)]
		[ReadOnly]
		[SerializeField] private Shader shader;

		/// <summary>
		/// Gets the name of the shader used by this property set.
		/// </summary>
		/// <remarks>
		/// This name is passed to the internal shader lookup system when the properties are enabled.
		/// It should match the full shader path as defined in the shader file.
		/// </remarks>
		public abstract string ShaderName { get; }

		/// <summary>
		/// Gets the resolved shader instance.
		/// </summary>
		/// <value>
		/// The loaded <see cref="UnityEngine.Shader"/>, or <c>null</c> if it could not be found.
		/// </value>
		public Shader Shader => shader;
		
		/// <summary>
		/// Indicates whether this shader requires access to the camera depth texture.
		/// </summary>
		/// <remarks>
		/// Override and return <c>true</c> if the shader samples depth information.
		/// This is used by higher-level systems to enable the correct camera flags.
		/// </remarks>
		public virtual bool RequiresDepthTexture => false;
		
		/// <summary>
		/// Indicates whether this shader requires access to the camera normals texture.
		/// </summary>
		/// <remarks>
		/// Override and return <c>true</c> if the shader samples scene normals.
		/// This is used by higher-level systems to ensure normals are generated when needed.
		/// </remarks>
		public virtual bool RequiresNormalsTexture => false;

		/// <summary>
		/// Applies all shader-related properties to the given material.
		/// </summary>
		/// <param name="material">
		/// The material created for the post process effect, using the shader defined by <see cref="ShaderName"/>.
		/// </param>
		/// <remarks>
		/// Subclasses should override this method and set all relevant shader uniforms
		/// (for example, floats, vectors, colors, textures, and keywords).
		/// </remarks>
		public virtual void SetMaterialProperties(Material material)
		{// Do nothing by default
		}

		/// <summary>
		/// Called when serialized values are changed in the inspector.
		/// </summary>
		/// <remarks>
		/// Override this method to clamp values, enforce invariants, or perform lightweight validation
		/// in response to inspector edits.
		/// </remarks>
		public virtual void OnValidate()
		{// Do nothing by default
		}

		/// <summary>
		/// Called when the owning component or asset is enabled.
		/// </summary>
		/// <remarks>
		/// The default implementation resolves the shader using <see cref="ShaderName"/>.
		/// Subclasses overriding this method should normally call <c>base.OnEnable()</c>.
		/// </remarks>
		public virtual void OnEnable()
		{
			shader = ShaderNames.GetShader(ShaderName);
		}
	}
}
