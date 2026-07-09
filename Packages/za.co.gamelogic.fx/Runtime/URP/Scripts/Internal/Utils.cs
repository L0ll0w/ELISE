using System;
using UnityEngine.Rendering.Universal;

namespace Gamelogic.Fx.URP.Internal
{
	internal static class Utils
	{
		/// <summary>
		/// Determines whether a render pass should execute for the given camera,
		/// based on the specified <see cref="CameraScope"/>.
		/// </summary>
		/// <param name="cameraData">The camera data for the camera currently being rendered.</param>
		/// <param name="cameraScope">Controls which cameras the pass executes on.</param>
		/// <param name="customCameraScopePredicate">
		/// A predicate invoked when <paramref name="cameraScope"/> is <see cref="CameraScope.Custom"/>.
		/// When <see langword="null"/>, returns <see langword="false"/> for every camera.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the pass should execute for this camera; otherwise <see langword="false"/>.
		/// </returns>
		internal static bool ShouldExecute(
			ref CameraData cameraData,
			CameraScope cameraScope,
			Func<CameraData, bool> customCameraScopePredicate = null)
		{
			switch (cameraScope)
			{
				case CameraScope.Base:
					return cameraData.renderType == CameraRenderType.Base;

				case CameraScope.Final:
					return cameraData.resolveFinalTarget;

				case CameraScope.Custom:
					return customCameraScopePredicate?.Invoke(cameraData) ?? false;

				case CameraScope.All:
					return true;

				default:
					throw new ArgumentException($"Unsupported CameraScope value: {cameraScope}");
			}
		}
	}
}
