#if GAMELOGIC_INTERNAL

using Gamelogic.Fx.Editor.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if GAMELOGIC_HAS_URP
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;
#endif

#if GL_HAS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Gamelogic.Fx.Editor.Internal
{

	internal class RenderPipelineSwitcherWindow : EditorWindow
	{
#if GAMELOGIC_HAS_URP
		private const string URPAssetPath = "Assets/RenderPipelines/URP/URP 3D.asset";
#endif

#if GL_HAS_HDRP
	private const string HDRPAssetPath = "Assets/RenderPipelines/HDRP/HDRP.asset";
#endif

		[MenuItem(Constants.ToolsRoot + "Render Pipeline Switcher")]
		
		public static void ShowWindow()
		{
			GetWindow<RenderPipelineSwitcherWindow>("Render Pipeline Switcher");
		}

		private void OnGUI()
		{
			GUILayout.Label("Select Render Pipeline", EditorStyles.boldLabel);

			if (GUILayout.Button("Built-in RP"))
			{
				ConfirmAndSetRenderPipeline(asset: null, renderPipelineName: "Built-in RP");
			}

#if GAMELOGIC_HAS_URP

			if (GUILayout.Button("URP"))
			{
				var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);

				if (urp == null)
				{
					Directory.CreateDirectory("Assets/RenderPipelines/URP");

					var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
					var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();

					AssetDatabase.CreateAsset(rendererData, "Assets/RenderPipelines/URP/ForwardRenderer.asset");

					// Link renderer data (needed for URP 7–13)
					var serializedURP = new SerializedObject(urpAsset);
					var rendererDataList = serializedURP.FindProperty("m_RendererDataList");
					rendererDataList.arraySize = 1;
					rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
					serializedURP.FindProperty("m_DefaultRendererIndex").intValue = 0;
					serializedURP.ApplyModifiedProperties();

					AssetDatabase.CreateAsset(urpAsset, URPAssetPath);
					AssetDatabase.SaveAssets();
					Debug.Log("Created new URP asset and linked renderer data.");
				}

				ConfirmAndSetRenderPipeline(urp, renderPipelineName: "URP");
			}
#endif

#if GL_HAS_HDRP
		if (GUILayout.Button("HDRP"))
		{
			var hdrp = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(HDRPAssetPath);

			if (hdrp == null)
			{
				Directory.CreateDirectory("Assets/RenderPipelines/HDRP");
				hdrp = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
				AssetDatabase.CreateAsset(hdrp, HDRPAssetPath);
				Debug.Log("Created new HDRP asset at " + HDRPAssetPath);
			}	
			PlayerSettings.colorSpace = ColorSpace.Linear;
			ConfirmAndSetRenderPipeline(hdrp, name: "HDRP");
		}
#endif
		}

		private void ConfirmAndSetRenderPipeline(RenderPipelineAsset asset, string renderPipelineName)
		{
			bool confirmed = EditorUtility.DisplayDialog(
				title: "Confirm Render Pipeline Switch",
				message:
				$"This action could take several minutes.\nAre you sure you want to switch to {renderPipelineName}?",
				ok: "Yes, switch",
				cancel: "Cancel"
			);

			if (!confirmed)
			{
				return;
			}

			SetRenderPipeline(asset, renderPipelineName);
		}

		private void SetRenderPipeline(RenderPipelineAsset asset, string renderPipelineName)
		{
			GraphicsSettings.defaultRenderPipeline = asset;
			QualitySettings.renderPipeline = asset;

			Debug.Log($"Successfully switched to {renderPipelineName}");
		}
	}
}
#endif
