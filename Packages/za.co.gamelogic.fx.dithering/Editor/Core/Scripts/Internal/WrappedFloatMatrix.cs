using UnityEngine;
using Gamelogic.Extensions;

namespace Gamelogic.Fx.Dithering.Editor
{
	/// <summary>
	/// A wrapped matrix that makes it easier to draw in a window.
	/// </summary>
	internal sealed class WrappedFloatMatrix : ScriptableObject
	{
		public const string MatrixFieldName = nameof(matrix);

		[Presets(nameof(DitherMatrixPresets), nameof(DitherMatrixPresets))] 
		public FloatMatrix matrix = DitherMatrixPresets.Checker.Clone();
	}
}
