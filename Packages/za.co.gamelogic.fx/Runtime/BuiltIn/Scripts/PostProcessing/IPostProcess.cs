using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	/*
		The common suffix is to distinguish from RendererFeatures.
	*/
	/// <summary>
	/// ⭐ Interface for post process effects.
	/// </summary>
	/// <remarks>
	/// Intended for the built-in render pipeline.
	///
	/// Any class that implements this interface can be used as a post process with <see cref="PostProcessRunner"/>.
	/// 
	/// Usually <see cref="PostProcess"/> is a suitable base class for custom post effects, but in cases you want to
	/// have the implementation complete, you can use this interface. 
	/// </remarks>
	public interface IPostProcess
	{
		/// <summary>
		/// Whether the post process is enabled.
		/// </summary>
		/// <remarks>The <see cref="PostProcessRunner"/> ignores it if disabled.</remarks>
		// ReSharper disable once InconsistentNaming
		// The name is so that Unity components work with it. 
		bool enabled { get; }

		/// <summary>
		/// The function that performs the post process effect.
		/// </summary>
		/// <param name="sourceTexture">The full screen source texture.</param>
		/// <param name="destTexture">The render target for the post process.</param>
		/// <remarks>
		/// A typical implementation uses
		/// <see cref="Graphics.Blit(UnityEngine.Texture,UnityEngine.RenderTexture,UnityEngine.Material)"/>
		/// to perform the post-process effect.
		/// </remarks>
		void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture);
	}
}
