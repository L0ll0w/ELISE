using System;
using Gamelogic.Fx.Internal;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the box blur post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class BoxBlurProperties : SeparableShaderProperties
	{
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.BoxBlur;
	}
}
