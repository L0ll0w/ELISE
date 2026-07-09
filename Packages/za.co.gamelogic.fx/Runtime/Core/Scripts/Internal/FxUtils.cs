using System.Collections.Generic;
using Gamelogic.Extensions;
using UnityEngine;
using Gamelogic.Fx.Mapping;
using UnityEngine.Rendering;
using UnityObject = UnityEngine.Object;
namespace Gamelogic.Fx.Internal
{
	/*	Small methods used in more than one place but too specific for API exposure.
		
		Why not simply Utils? There are too many other classes like it. 
		Why not Utilities? Inconsistent with Extensions and Unity. 
	*/
	internal static class FxUtils
	{
		internal static void LogGamelogicSystemMessage(string message)
		{
			Debug.Log($"[Gamelogic Fx] {message}");
		}
		
		internal static void UpdateMaterial(ref Material materialToUpdate, Shader shader)
		{
			if (shader == null)
			{
				materialToUpdate = null;
				return;
			}
			
			if (materialToUpdate == null)
			{
				materialToUpdate = CreateMaterial (shader);
				return;
			}
			
			if (materialToUpdate.shader != shader)
			{
				DestroyMaterial(materialToUpdate);
				materialToUpdate = CreateMaterial(shader);
			}
		}
		
		/// <summary>
		/// Gets a mesh from a renderer, if possible.
		/// </summary>
		/// <param name="renderer">The renderer to get the mesh from.</param>
		internal static Mesh GetMesh(Renderer renderer)
		{
			// This can happen, for example, when a new scene is loaded. 
			if(renderer == null)
			{
				return null;
			}
			
			switch (renderer)
			{
				case MeshRenderer meshRenderer:
				{
					var meshFilter = meshRenderer.GetComponent<MeshFilter>();
					return meshFilter ? meshFilter.sharedMesh : null;
				}

				case SkinnedMeshRenderer skinnedMeshRenderer:
					return skinnedMeshRenderer.sharedMesh;

				default:
					return null;
			}
		}
		
		internal static void SetTilingFromMaterialToBlock(
			Material sourceMaterial,
			string sourceMaterialTexturePropertyName,
			MaterialPropertyBlock destinationBlock,
			string destinationBlockTilingPropertyName)
		{
			// Use the first material for tiling, which is the most typical case
			if (sourceMaterial == null || !sourceMaterial.HasTexture(sourceMaterialTexturePropertyName))
			{
				return;
			}
			
			var scale = sourceMaterial.GetTextureScale(sourceMaterialTexturePropertyName);
			var offset = sourceMaterial.GetTextureOffset(sourceMaterialTexturePropertyName);
			var tiling = new Vector4(scale.x, scale.y, offset.x, offset.y);
			
			destinationBlock.SetVector(destinationBlockTilingPropertyName, tiling);
		}
		
		/* TODO: This method may be called multiple times by different effects
			We should maintain the list  centrally so it is shared among those who need it.
		*/
		internal static void RefreshRendererList(List<Renderer> renderers)
		{
			renderers.Clear();
			renderers.AddRange(
				UnityObject.FindObjectsByType<Renderer>(
					FindObjectsInactive.Exclude,
					FindObjectsSortMode.None
				)
			);
		}
		
		internal static void DrawBackground(
			CommandBuffer buffer,
			Material backgroundMaterial, 
			MapProperties mapProperties)
		{
			buffer.ClearRenderTarget(true, true, mapProperties.BackgroundColor);
			
			if (backgroundMaterial == null)
			{
				return;
			}
			
			mapProperties.SetBackgroundMaterialProperties(backgroundMaterial);
			buffer.DrawProcedural(
				Matrix4x4.identity,
				backgroundMaterial,
				0,
				MeshTopology.Triangles,
				3
			);
		}

#if GAMELOGIC_HAS_URP 
		private static Material CreateMaterial(Shader shader) => CoreUtils.CreateEngineMaterial (shader); 
#else 
		private static Material CreateMaterial(Shader shader) => new Material(shader); 
#endif
		
#if GAMELOGIC_HAS_URP
		private static void DestroyMaterial(Material material) => CoreUtils.Destroy(material);
#else
		private static void DestroyMaterial(Material material) => GLMonoBehaviour.DestroyUniversal(material);
#endif

	}
}
