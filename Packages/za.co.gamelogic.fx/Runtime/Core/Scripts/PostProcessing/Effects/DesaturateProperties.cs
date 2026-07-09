using System;
using Gamelogic.Fx.Internal;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the desaturate post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class DesaturateProperties : ShaderProperties
	{
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Desaturate;
	}
}
