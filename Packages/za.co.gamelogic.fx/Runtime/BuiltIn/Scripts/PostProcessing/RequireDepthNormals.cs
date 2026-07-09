using Gamelogic.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	/// <summary>
	/// Add this script to a camera to ensure that it generates a depth normals texture.
	/// Only relevant when using the Built-in Render Pipeline.
	/// </summary>
	/// <remarks>
	/// This texture is used by some effects, such as
	/// <see cref="Gamelogic.Fx.BuiltIn.PostProcessing.Effects.SimpleOutlinePostProcess"/>.
	/// </remarks>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public sealed class RequireDepthNormals : MonoBehaviour
	{
		[ReadOnly, UsedImplicitly] // Debug aid
		[SerializeField] private DepthTextureMode currentDepthTextureMode;
		
		public void OnEnable()
		{
			var cam = GetComponent<Camera>();
			cam.depthTextureMode |= DepthTextureMode.DepthNormals;
			currentDepthTextureMode = cam.depthTextureMode;
		}
	}
}
