using Gamelogic.Fx.URP.Internal;
using UnityEditor;

using Gamelogic.Extensions.Editor.Internal;

namespace Gamelogic.Fx.Editor.URP.Internal
{
	/// <summary>
	/// Initializes <see cref="PipelineDefiner"/> in the editor context on domain reload.
	/// </summary>
	[InitializeOnLoad]
	internal static class PipelineDefinerEditor
	{
		static PipelineDefinerEditor()
		{
			PipelineDefiner.Init();
			SetContextFeatures();
		}

		private static void SetContextFeatures()
		{
#if GAMELOGIC_HAS_URP
			AssetConfig.ContextFeatures["Has URP"] = true;
			AssetConfig.ContextFeatures["Using URP"] = PipelineDefiner.UsingURP();
#else
			AssetConfig.ContextFeatures["Has URP"] = false;
			AssetConfig.ContextFeatures["Using URP"] = false;
#endif
			
#if GAMELOGIC_HAS_URP_RENDER_GRAPH
			AssetConfig.ContextFeatures["Has RenderGraph"] = true;
			AssetConfig.ContextFeatures["Using RenderGraph"] = PipelineDefiner.UsingRenderGraph();
#else
			AssetConfig.ContextFeatures["Has RenderGraph"] = false;
			AssetConfig.ContextFeatures["Using RenderGraph"] = false;
#endif
		}
			
	}
	

}
