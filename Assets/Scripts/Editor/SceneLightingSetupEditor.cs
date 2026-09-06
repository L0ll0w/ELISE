using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SceneLightingSetup))]
public class SceneLightingSetupEditor : Editor
{
    [MenuItem("GameObject/2.5D RPG/Créer Éclairage Scène (Scene Lighting Setup)", false, 10)]
    public static void CreateLightingSetupGameObject(MenuCommand menuCommand)
    {
        GameObject lightingObj = new GameObject("SceneLightingSetup");
        SceneLightingSetup setup = lightingObj.AddComponent<SceneLightingSetup>();
        
        GameObjectUtility.SetParentAndAlign(lightingObj, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(lightingObj, "Create SceneLightingSetup");
        Selection.activeObject = lightingObj;

        setup.PresetDreamcoreFairy();
        Debug.Log("[SceneLightingSetup] Studio d'éclairage créé avec succès et appliqué à la scène !");
    }

    public override void OnInspectorGUI()
    {
        SceneLightingSetup setup = (SceneLightingSetup)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("💡 Studio d'Éclairage & Atmosphère 2.5D", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Presets 1-Clic pour Faire Ressortir la Scène", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("✨ Rêve Féerique (Recommandé)", GUILayout.Height(32)))
        {
            Undo.RecordObject(target, "Preset Dreamcore Fairy");
            setup.PresetDreamcoreFairy();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("🌅 Crépuscule Magique", GUILayout.Height(32)))
        {
            Undo.RecordObject(target, "Preset Magical Twilight");
            setup.PresetMagicalTwilight();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌙 Clair de Lune Nuit", GUILayout.Height(32)))
        {
            Undo.RecordObject(target, "Preset Moonlight Night");
            setup.PresetMoonlightNight();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("⚡ Re-synchroniser", GUILayout.Height(32)))
        {
            Undo.RecordObject(target, "Apply Lighting Setup");
            setup.ApplyLightingSetup();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            setup.ApplyLightingSetup();
            SceneView.RepaintAll();
        }
    }
}
