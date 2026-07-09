using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	internal static class MixboxFeatures
	{
		private const string HasMixboxKeyword = "GAMELOGIC_HAS_MIXBOX";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		internal static void Init()
		{
			#if GAMELOGIC_HAS_MIXBOX
				Shader.EnableKeyword(HasMixboxKeyword);
				FxUtils.LogGamelogicSystemMessage("Mixbox package found. Enabled global shader keyword: " + HasMixboxKeyword);
			#else
				Shader.DisableKeyword(HasMixboxKeyword);
				FxUtils.LogGamelogicSystemMessage("Mixbox package not found. Disabled global shader keyword: " + HasMixboxKeyword);
			#endif
			
			#if GAMELOGIC_DEBUG_SHADERS
				Shader.EnableKeyword("GAMELOGIC_DEBUG_SHADERS");
				FxUtils.LogGamelogicSystemMessage("GAMELOGIC_DEBUG_SHADERS is enabled.");
			#else
				Shader.DisableKeyword("GAMELOGIC_DEBUG_SHADERS");
				FxUtils.LogGamelogicSystemMessage("GAMELOGIC_DEBUG_SHADERS is disabled.");
			#endif
		}
		
		internal static bool IsMixboxPresent()
		{
#if GAMELOGIC_HAS_MIXBOX
			return true;
#else
			return false;
#endif
		}
	}
}
