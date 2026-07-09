using System;
using Gamelogic.Fx.Internal;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the max-filter post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class MaxProperties : SeparableShaderProperties
	{
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Max;
	}
}
