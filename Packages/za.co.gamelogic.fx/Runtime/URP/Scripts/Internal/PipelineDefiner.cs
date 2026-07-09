using Gamelogic.Fx.Internal;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

#if GAMELOGIC_HAS_URP_RENDER_GRAPH
using JetBrains.Annotations;
using UnityEngine.Assertions;
#endif

#if GAMELOGIC_HAS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Gamelogic.Fx.URP.Internal
{
	/// <summary>
	/// Sets up shader keywords so the correct shader code is called depending
	/// on the environment.
	/// </summary>
	/// <remarks>
	///
	/// Enables or disables the <c>GAMELOGIC_HAS_URP</c> global shader keyword at runtime
	/// based on whether the active render pipeline is URP.
	///
	/// Enables or disables the <c>GAMELOGIC_HAS_URP_RENDER_GRAPH</c> global shader keyword at runtime
	/// based on whether the active URP pipeline asset has render graph enabled.
	/// </remarks>
	/*	The script defines are set up by the asmdef files. Fx packages are divided in core, built-in and URP assemblies
		(and corresponding editor assemblies if required). Only the URP assemblies set up the right defines. 
		
		See as reference Gamelogic.Fx.URP.asmdef.
		
		This class use those script defines, and additional logic to set shader keywords, so the right shader 
		functionality is compiled.
		
		Unfortunately Unity has changes this a lot of versions, so it is difficult to do comprehensive tests.  
	*/
	internal static class PipelineDefiner
	{
		private const string HasUrpKeyword = "GAMELOGIC_HAS_URP";
		private const string HasRenderGraphKeyword = "GAMELOGIC_HAS_URP_RENDER_GRAPH";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		internal static void Init()
		{
#if GAMELOGIC_HAS_URP
			if (UsingURP())
			{
				EnableURPSupport();
			}
			else
			{
				DisableURPSupport();
			}
#else
			DisableURPSupport();
#endif
		}
		
		private static void EnableURPSupport()
		{
			Shader.EnableKeyword(HasUrpKeyword);
			FxUtils.LogGamelogicSystemMessage("URP package found. Enabled global shader keyword: " + HasUrpKeyword);
			
#if GAMELOGIC_HAS_URP_RENDER_GRAPH
			if(UsingRenderGraph())
			{
				EnableRenderGraphSupport();
			}
			else
			{
				DisableRenderGraphSupport();
			}
#else
			DisableRenderGraphSupport();
#endif
		}
		
		/*	Not used in all compiler paths, but left to catch compiler errors for other paths.
		*/
		[UsedImplicitly]  
		private static void EnableRenderGraphSupport()
		{
			Shader.EnableKeyword(HasRenderGraphKeyword);
			FxUtils.LogGamelogicSystemMessage("Unity version supports Render Graph. Enabled global shader keyword: " + HasRenderGraphKeyword);
		}
		
		private static void DisableURPSupport()
		{
			Shader.DisableKeyword(HasUrpKeyword);
			Debug.Log("URP package not found or pipeline asset is not assigned. Disabled global shader keyword: " + HasUrpKeyword);
			
			DisableRenderGraphSupport();
		}
		
		private static void DisableRenderGraphSupport()
		{
			Shader.DisableKeyword(HasRenderGraphKeyword);
			Debug.Log("Unity version does not support Render Graph. Disabled global shader keyword: " + HasRenderGraphKeyword);
		}
		
#if GAMELOGIC_HAS_URP
		internal static bool UsingURP() => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
#endif
		
#if GAMELOGIC_HAS_URP_RENDER_GRAPH
		internal static bool UsingRenderGraph()
		{
			return UsingURP() && !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
		}
#endif
	}
}
