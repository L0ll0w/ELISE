using System;
using Gamelogic.Fx.Internal;

namespace Gamelogic.Fx.PostProcessing.Effects
{
	/// <summary>
	/// Shader properties for the min-filter post-processing effect.
	/// </summary>
	[Serializable]
	public sealed class MinProperties : SeparableShaderProperties
	{
		public override string ShaderName => Constants.ShaderNameRoot + ShaderNames.Min;
	}
}
