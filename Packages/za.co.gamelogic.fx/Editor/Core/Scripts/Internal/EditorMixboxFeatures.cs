using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Gamelogic.Extensions;
using Gamelogic.Extensions.Editor.Internal;
using Gamelogic.Fx.Internal;
using UnityEditor;
using UnityEngine;

#if GAMELOGIC_HAS_URP
using UnityEngine.Rendering.Universal;
#endif 

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Does various things to make Mixbox support work, or show warnings in the editor on components 
	/// if it is not present. 
	/// </summary>
	[ReuseCandidate(Note = "Can be made with arbitrary packages.")]
	[InitializeOnLoad]
	internal static class EditorMixboxFeatures
	{
		private const string InstallLink = "https://github.com/scrtwpns/mixbox/tree/master/unity";

		static EditorMixboxFeatures()
		{
			MixboxFeatures.Init();
			UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
			SetContextFeatures();
		}

		private static void SetContextFeatures()
		{
#if GAMELOGIC_HAS_MIXBOX
			AssetConfig.ContextFeatures["Has Mixbox"] = true;
#else
			AssetConfig.ContextFeatures["Has Mixbox"] = false;
#endif
		}

		private static void OnPostHeaderGUI(UnityEditor.Editor editor)
		{
			if(editor.targets.Length != 1)
			{
				return;
			}

			var target = editor.target;

			switch (target)
			{
				case GameObject go:
				{
					if(TryGetMixboxComponentFromGameObject(go, out var componentType))
					{
						DrawMixboxStatus(componentType.Name);
					}

					return;
				}
				
#if GAMELOGIC_HAS_URP // This case is for RendererFeatures, which are ScriptableObjects in URP.
				case ScriptableObject so:
				{
					if(TryGetMixboxRendererFeatureFromScriptableObject(so, out var featureType))
					{
						DrawMixboxStatus(featureType.Name);
					}

					break;
				}
#endif
			}
		}

		private static bool TryGetMixboxComponentFromGameObject(GameObject go, out Type componentType)
		{
			componentType = go
				.GetComponents<Component>()
				.Select(c => c?.GetType())
				.FirstOrDefault(RequiresMixbox);

			return componentType != null;
		}

#if GAMELOGIC_HAS_URP
		private static bool TryGetMixboxRendererFeatureFromScriptableObject(
			ScriptableObject so,
			out Type featureType)
		{
			featureType = null;
			
			if (so is not ScriptableRendererData rendererData)
			{
				return false;
			}

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
			
			var featuresField = typeof(ScriptableRendererData).GetField("m_RendererFeatures", bindingFlags);

			if(featuresField == null)
			{
				return false;
			}

			if(featuresField.GetValue(rendererData) is not IEnumerable features)
			{
				return false;
			}

			foreach(object feature in features)
			{
				if(feature == null)
				{
					continue;
				}

				var type = feature.GetType();

				if (!RequiresMixbox(type))
				{
					continue;
				}
				
				featureType = type;
				return true;
			}

			return false;
		}
#endif
		
		private static bool RequiresMixbox(Type type) => type != null && Attribute.IsDefined(type, typeof(RequiresMixboxAttribute), inherit: true);

		private static void DrawMixboxStatus(string componentName)
		{
			bool present = MixboxFeatures.IsMixboxPresent();

			if(present)
			{
				EditorGUILayout.HelpBox(
					$"The {componentName} needs Mixbox.\n✔ Mixbox found.",
					MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox(
					$"The {componentName} needs Mixbox.",
					MessageType.Warning);

				if(GUILayout.Button("⚠ See how to install it.", EditorStyles.linkLabel))
				{
					Application.OpenURL(InstallLink);
				}
			}
		}
	}
}
