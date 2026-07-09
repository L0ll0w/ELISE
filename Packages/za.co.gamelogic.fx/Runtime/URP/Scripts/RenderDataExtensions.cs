using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.BuiltIn.Scripts
{
	/// <summary>
	/// Extension methods for <see cref="ScriptableRendererData"/>.
	/// </summary>
	public static class RenderDataExtensions
	{
		/// <summary>
		/// Returns the first renderer feature of type <typeparamref name="TRendererFeature"/>,
		/// or throws if none is found.
		/// </summary>
		/// <typeparam name="TRendererFeature">The type of renderer feature to find.</typeparam>
		/// <param name="renderData">The renderer data to search.</param>
		/// <returns>The first matching renderer feature.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no renderer feature of type <typeparamref name="TRendererFeature"/> exists in <paramref name="renderData"/>.
		/// </exception>
		public static TRendererFeature FindRequiredFeature<TRendererFeature>(this ScriptableRendererData renderData)
			where TRendererFeature : ScriptableRendererFeature
		{
			var feature = renderData.rendererFeatures.Find(f => f is TRendererFeature) as TRendererFeature;

			return feature == null
				? throw new InvalidOperationException($"Required renderer feature of type {typeof(TRendererFeature).Name} not found in renderer data {renderData.name}.")
				: feature;
		}

		/// <summary>
		/// Returns the first renderer feature of type <typeparamref name="TRendererFeature"/>,
		/// or <see langword="null"/> if none is found.
		/// </summary>
		/// <typeparam name="TRendererFeature">The type of renderer feature to find.</typeparam>
		/// <param name="renderData">The renderer data to search.</param>
		/// <returns>The first matching renderer feature, or <see langword="null"/>.</returns>
		public static TRendererFeature FindFeature<TRendererFeature>(this ScriptableRendererData renderData)
			where TRendererFeature : ScriptableRendererFeature
		{
			return renderData.rendererFeatures.Find(f => f is TRendererFeature) as TRendererFeature;
		}

		/// <summary>
		/// Returns all renderer features of type <typeparamref name="TRendererFeature"/>.
		/// </summary>
		/// <typeparam name="TRendererFeature">The type of renderer feature to find.</typeparam>
		/// <param name="renderData">The renderer data to search.</param>
		/// <returns>
		/// All renderer features of type <typeparamref name="TRendererFeature"/>,
		/// or an empty sequence if none are found.
		/// </returns>
		public static IEnumerable<TRendererFeature> FindFeatures<TRendererFeature>(this ScriptableRendererData renderData)
			where TRendererFeature : ScriptableRendererFeature
		{
			return renderData.rendererFeatures.FindAll(f => f is TRendererFeature) as IEnumerable<TRendererFeature>;
		}
	}
}
