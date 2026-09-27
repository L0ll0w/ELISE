using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SunflowerXylophone))]
[InitializeOnLoad]
public class SunflowerXylophoneEditor : Editor
{
    // Buffer statique qui persiste en mémoire d'Éditeur entre le mode Play et le mode Édition
    public static List<SunflowerXylophone.MelodyNote> recordedNotesBuffer = new List<SunflowerXylophone.MelodyNote>();
    public static bool isRecordingActive = false;
    private string quickSequenceText = "0, 2, 4, 5, 4, 2, 0";
    private float defaultBeatDuration = 1.0f;

    static SunflowerXylophoneEditor()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Lorsqu'on quitte le mode Play et qu'on revient en mode Éditeur
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (recordedNotesBuffer != null && recordedNotesBuffer.Count > 0)
            {
                SunflowerXylophone targetInScene = Object.FindObjectOfType<SunflowerXylophone>();
                if (targetInScene != null)
                {
                    Undo.RecordObject(targetInScene, "Save Recorded Xylophone Melody");
                    targetInScene.melody = new List<SunflowerXylophone.MelodyNote>(recordedNotesBuffer);
                    EditorUtility.SetDirty(targetInScene);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetInScene.gameObject.scene);
                    Debug.Log($"<color=#00FF88>[SunflowerXylophone] 💾 SUCCÈS ! {recordedNotesBuffer.Count} notes enregistrées durant le Play Mode ont été conservées et sauvegardées sur {targetInScene.name} !</color>");
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        SunflowerXylophone script = (SunflowerXylophone)target;

        serializedObject.Update();

        // 1. Enregistrement & Contrôles
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎵 Lecture Audio WAV & Contrôles", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Jouer le Morceau", GUILayout.Height(32)))
            {
                script.PlayMelody();
            }
            if (GUILayout.Button("⏹ Arrêter", GUILayout.Height(32)))
            {
                script.StopMelody();
                isRecordingActive = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (script.useFullWavAudio && script.audioSource != null && script.audioSource.isPlaying)
            {
                EditorGUILayout.HelpBox($"⏱ Audio en cours : {script.audioSource.time:F2}s / {script.fullMelodyWavClip?.length:F2}s\n" +
                    $"🔴 Enregistrement : {(isRecordingActive ? "ACTIF" : "Inactif")}\n" +
                    $"Cliquez sur les numéros de lames ci-dessous pendant la musique pour frapper et enregistrer les notes en direct.", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = isRecordingActive ? Color.red : Color.white;
                if (GUILayout.Button(isRecordingActive ? "🔴 ENREGISTREMENT ACTIF (Cliquez pour arrêter)" : "⚪ Démarrer l'Enregistrement", GUILayout.Height(30)))
                {
                    isRecordingActive = !isRecordingActive;
                    if (isRecordingActive && (recordedNotesBuffer == null || recordedNotesBuffer.Count == 0))
                    {
                        recordedNotesBuffer = new List<SunflowerXylophone.MelodyNote>();
                    }
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("🗑 Vider les Notes", GUILayout.Height(30)))
                {
                    Undo.RecordObject(script, "Clear Melody Notes");
                    script.melody.Clear();
                    recordedNotesBuffer.Clear();
                    EditorUtility.SetDirty(script);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Lames de Xylophone (Frappe / Enregistrement) :");
            EditorGUILayout.BeginHorizontal();
            int keyCount = script.keys != null ? script.keys.Length : 8;
            for (int i = 0; i < keyCount; i++)
            {
                if (GUILayout.Button($"{i}", GUILayout.Height(32)))
                {
                    script.PlayNote(i);

                    if (isRecordingActive && script.audioSource != null && script.audioSource.isPlaying)
                    {
                        float currentTimestamp = script.audioSource.time;
                        SunflowerXylophone.MelodyNote note = new SunflowerXylophone.MelodyNote(i, currentTimestamp);

                        // Ajouter à l'objet actuel et au buffer statique d'Éditeur
                        script.melody.Add(note);
                        recordedNotesBuffer.Add(note);

                        Debug.Log($"[SunflowerXylophone] 🎙 Note {i} enregistrée à {currentTimestamp:F2}s (Total: {recordedNotesBuffer.Count} notes)");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // Mode Éditeur (Hors Play Mode)
            EditorGUILayout.HelpBox("Passez en mode Play pour écouter le WAV et enregistrer les notes en direct.", MessageType.Info);

            if (recordedNotesBuffer != null && recordedNotesBuffer.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"💾 Buffer d'enregistrement : {recordedNotesBuffer.Count} notes prêtes");
                if (GUILayout.Button("Ré-appliquer les Notes Enregistrées", GUILayout.Height(24)))
                {
                    Undo.RecordObject(script, "Apply Recorded Notes");
                    script.melody = new List<SunflowerXylophone.MelodyNote>(recordedNotesBuffer);
                    EditorUtility.SetDirty(script);
                    Debug.Log($"[SunflowerXylophone] {recordedNotesBuffer.Count} notes appliquées à la partition !");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // 2. Générateur / Importation Rapide
        EditorGUILayout.LabelField("⚡ Partition & Importation Rapide", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        quickSequenceText = EditorGUILayout.TextField("Suite d'index (ex: 0, 2, 4, 5)", quickSequenceText);
        defaultBeatDuration = EditorGUILayout.FloatField("Intervalle (sec)", defaultBeatDuration);

        if (GUILayout.Button("Générer la Liste des Notes", GUILayout.Height(26)))
        {
            string[] parts = quickSequenceText.Split(new char[] { ',', ' ', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            List<SunflowerXylophone.MelodyNote> newMelody = new List<SunflowerXylophone.MelodyNote>();

            float currentTime = 0f;
            foreach (string p in parts)
            {
                if (int.TryParse(p.Trim(), out int val))
                {
                    newMelody.Add(new SunflowerXylophone.MelodyNote(val, currentTime, defaultBeatDuration));
                    currentTime += defaultBeatDuration;
                }
            }

            if (newMelody.Count > 0)
            {
                Undo.RecordObject(script, "Generate Xylophone Melody");
                script.melody = newMelody;
                recordedNotesBuffer = new List<SunflowerXylophone.MelodyNote>(newMelody);
                EditorUtility.SetDirty(script);
                Debug.Log($"[SunflowerXylophone] {newMelody.Count} notes générées !");
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}
