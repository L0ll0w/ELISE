using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Provides extension methods for configuring materials used by Gamelogic Fx effects.
	/// These helpers simplify setting keyword collections, texture tiling, color lists,
	/// and kernel parameters in a consistent and centralized way.
	/// </summary>
	public static class MaterialExtensions
	{
		private const string TilingSuffix = "_ST";
		
		/// <summary>
		/// Enables or disables shader keywords on the material according to the values
		/// in the provided keyword collection.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="keywords">The set of keywords to apply.</param>
		public static void Set(this Material material, IEnumerable<Keyword> keywords)
		{
			foreach (var keyword in keywords)
			{
				if (keyword.enabled)
				{
					material.EnableKeyword(keyword.name);
				}
				else
				{
					material.DisableKeyword(keyword.name);
				}
			}
		}

		/// <summary>
		/// Sets a texture and its associated tiling parameters on the material.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="textureName">The shader property name of the texture.</param>
		/// <param name="textureTiling">The tiling information and texture asset.</param>
		public static void SetTextureTiling(this Material material, string textureName, TextureTiling textureTiling)
		{
			material.SetTexture(textureName, textureTiling.Texture);
			material.SetVector(textureName + TilingSuffix, textureTiling.CalculatedTiling);
		}

		/// <summary>
		/// Sets an integer shader property on the material using the appropriate API
		/// depending on the Unity version.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="shaderPropertyId">The integer ID of the shader property.</param>
		/// <param name="value">The integer value to assign to the shader property.</param>
		public static void SetIntegerUniversal(this Material material, int shaderPropertyId, int value)
		{
			#if UNITY_2021_2_OR_NEWER
				material.SetInteger(shaderPropertyId, value);
			#else
				material.SetInt(shaderPropertyId, value);
			#endif
		}
		
		/// <summary>
		/// Sets an integer shader property on the material using its property name.
		/// Automatically resolves the property ID and routes the call to the universal setter.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="propertyName">The name of the shader property.</param>
		/// <param name="value">The integer value to assign to the shader property.</param>
		public static void SetIntegerUniversal(this Material material, string propertyName, int value)
		{
			material.SetIntegerUniversal(Shader.PropertyToID(propertyName), value);
		}
		
		/// <summary>
		/// Applies all color properties defined in the given <see cref="ColorShaderPropertyList"/>
		/// to the material.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="list">The list of color shader properties to set.</param>
		internal static void SetColors(this Material material, ColorShaderPropertyList list) => list.SetOn(material);
		
		/// <summary>
		/// Applies the kernel parameters to the material for use in sampling-based effects.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="kernel">The kernel description containing offset, size, and spacing.</param>
		internal static void SetKernel(this Material material, KernelInfo kernel)
		{
			material.SetFloat(KernelInfo.OffsetID, kernel.offset);
#if UNITY_2021_2_OR_NEWER
			material.SetInteger(KernelInfo.SizeID, kernel.size);
#else
			material.SetInt(KernelInfo.SizeID, kernel.size);
#endif
			material.SetFloat(KernelInfo.JumpSizeID, kernel.jumpSize);
		}
		
		/// <summary>
		/// Sets multiple shader properties on the material.
		/// </summary>
		/// <param name="material">The material to set the properties on.</param>
		/// <param name="shaderProperties">The shader properties to set. If <see langword="null"/> this method does nothing.</param>
		internal static void Set(this Material material, ShaderProperty[] shaderProperties)
		{
			if (shaderProperties == null)
			{
				return;
			}

			foreach (var shaderProperty in shaderProperties)
			{
				material.Set(shaderProperty);
			}
		}

		/// <summary>
		/// Sets a shader property on the material.
		/// </summary>
		/// <param name="material">The material to set the property on.</param>
		/// <param name="shaderProperty">The shader property to set.</param>
		internal static void Set(this Material material, ShaderProperty shaderProperty) => shaderProperty.SetOn(material);

		/// <summary>
		/// Sets a <see cref="LockableVector2"/> as a shader vector property on the material.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="propertyId">The integer ID of the shader property.</param>
		/// <param name="value">The lockable vector whose underlying value is applied.</param>
		internal static void SetVector(this Material material, int propertyId, LockableVector2 value)
		{
			material.SetVector(propertyId, value.vector);
		}

		/// <summary>
		/// Sets a <see cref="LockableVector3"/> as a shader vector property on the material.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="propertyId">The integer ID of the shader property.</param>
		/// <param name="value">The lockable vector whose underlying value is applied.</param>
		internal static void SetVector(this Material material, int propertyId, LockableVector3 value)
		{
			material.SetVector(propertyId, value.vector);
		}
		
		/// <summary>
		/// Sets the specified keyword as enabled on the material, while disabling all other keywords in the same group.
		/// </summary>
		/// <param name="material">The material to modify.</param>
		/// <param name="enabledKeyword">The keyword to enable on the material.</param>
		/// <param name="keywordGroup">The group of keywords that are mutually exclusive, where only one can be enabled
		/// at a time. The enabledKeyword must be part of this group.</param>
		public static void SetKeywordOfGroup(this Material material, string enabledKeyword, string[] keywordGroup)
		{
			Assert.IsTrue(keywordGroup.Contains(enabledKeyword));
				
			foreach (var keyword in keywordGroup)
			{
				material.DisableKeyword(keyword);
			}

			material.EnableKeyword(enabledKeyword);
		}
	}
}
