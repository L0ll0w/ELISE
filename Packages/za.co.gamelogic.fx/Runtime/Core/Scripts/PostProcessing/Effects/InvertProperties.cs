using System;
using Gamelogic.Fx.Internal;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the invert post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class InvertProperties : ShaderProperties
	{
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Invert;
	}
}
