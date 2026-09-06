using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CinematicSequence))]
public class CinematicSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CinematicSequence sequence = (CinematicSequence)target;

        serializedObject.Update();

        SerializedProperty indexProp = serializedObject.FindProperty("previewShotIndex");
        SerializedProperty livePreviewProp = serializedObject.FindProperty("enableLivePreview");
        SerializedProperty shotsProp = serializedObject.FindProperty("shots");

        // Barre d'outils de prévisualisation dans l'Inspector
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎬 Prévisualisation en Direct", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.PropertyField(livePreviewProp, new GUIContent("Activer le Live Preview"));

        if (shotsProp.arraySize > 0)
        {
            // Menu déroulant de sélection du plan à prévisualiser
            string[] shotNames = new string[shotsProp.arraySize];
            for (int i = 0; i < shotsProp.arraySize; i++)
            {
                SerializedProperty shotElement = shotsProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = shotElement.FindPropertyRelative("shotName");
                string name = string.IsNullOrEmpty(nameProp.stringValue) ? $"Plan {i + 1}" : nameProp.stringValue;
                shotNames[i] = $"{i + 1}. {name}";
            }

            int currentIndex = Mathf.Clamp(indexProp.intValue, 0, shotsProp.arraySize - 1);
            int newIndex = EditorGUILayout.Popup("Plan Prévisualisé", currentIndex, shotNames);

            if (newIndex != currentIndex)
            {
                indexProp.intValue = newIndex;
                serializedObject.ApplyModifiedProperties();
                sequence.PreviewCurrentShot();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("◀ Plan Précédent", GUILayout.Height(26)))
            {
                if (indexProp.intValue > 0)
                {
                    indexProp.intValue--;
                    serializedObject.ApplyModifiedProperties();
                    sequence.PreviewCurrentShot();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Plan Suivant ▶", GUILayout.Height(26)))
            {
                if (indexProp.intValue < shotsProp.arraySize - 1)
                {
                    indexProp.intValue++;
                    serializedObject.ApplyModifiedProperties();
                    sequence.PreviewCurrentShot();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("🎥 Rafraîchir la Vue", GUILayout.Height(26)))
            {
                serializedObject.ApplyModifiedProperties();
                sequence.PreviewCurrentShot();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Aucun plan dans la liste des shots. Ajoutez un plan ci-dessous.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // Détection des modifications des propriétés dans l'inspecteur par drag ou frappe
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (livePreviewProp.boolValue)
            {
                sequence.PreviewCurrentShot();
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}
