using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CinematicSequence))]
public class CinematicSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CinematicSequence sequence = (CinematicSequence)target;

        // Boutons de contrôle rapides dans l'Inspector
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎬 Prévisualisation en Direct", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("◀ Plan Précédent", GUILayout.Height(28)))
        {
            SerializedProperty indexProp = serializedObject.FindProperty("previewShotIndex");
            if (indexProp.intValue > 0)
            {
                indexProp.intValue--;
                serializedObject.ApplyModifiedProperties();
                sequence.PreviewCurrentShot();
                SceneView.RepaintAll();
            }
        }

        if (GUILayout.Button("Plan Suivant ▶", GUILayout.Height(28)))
        {
            SerializedProperty indexProp = serializedObject.FindProperty("previewShotIndex");
            SerializedProperty shotsProp = serializedObject.FindProperty("shots");
            if (indexProp.intValue < shotsProp.arraySize - 1)
            {
                indexProp.intValue++;
                serializedObject.ApplyModifiedProperties();
                sequence.PreviewCurrentShot();
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        DrawDefaultInspector();
    }
}
