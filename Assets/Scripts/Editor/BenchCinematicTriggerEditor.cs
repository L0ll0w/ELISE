using UnityEngine;
using UnityEditor;

/// <summary>
/// Éditeur personnalisé pour BenchCinematicTrigger :
/// Propose des boutons d'automatisation pour générer les points d'ancrage (Seat, Exit, Passant),
/// des aides visuelles et la validation des références.
/// </summary>
[CustomEditor(typeof(BenchCinematicTrigger))]
public class BenchCinematicTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BenchCinematicTrigger trigger = (BenchCinematicTrigger)target;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🪑 Configuration du Banc & Séquence Cinématique", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox(
            "Cette cinématique gère l'interaction avec le banc :\n" +
            "1. Le joueur saute sur le banc pour s'asseoir.\n" +
            "2. Cadrage cinématique du banc.\n" +
            "3. Premier dialogue.\n" +
            "4. Arrivée d'un passant depuis la gauche.\n" +
            "5. Le passant parle et change de sprite.\n" +
            "6. La fleur assise sur le banc despawn.\n" +
            "7. Le passant reprend son idle et redit un dialogue.\n" +
            "8. Le passant repart vers la droite.\n" +
            "9. Le joueur saute du banc et reprend le contrôle.",
            MessageType.Info
        );

        if (GUILayout.Button("✨ Générer / Assigner les Points d'Ancrage Automatiquement", GUILayout.Height(30)))
        {
            GenerateAnchorPoints(trigger);
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(4);
            bool inRange = trigger.IsPlayerInRangeForDebug;
            EditorGUILayout.LabelField($"Statut : {(inRange ? "🟢 Joueur à portée (Touche E active)" : "⚪ Joueur hors de portée")}", EditorStyles.boldLabel);

            if (GUILayout.Button("🎬 Déclencher la Cinématique Maintenant (Test)", GUILayout.Height(28)))
            {
                trigger.TriggerCinematicManually();
            }

            if (GUILayout.Button("🍎 Faire Tomber la Pomme Maintenant (Test)", GUILayout.Height(28)))
            {
                trigger.ResetAndTriggerAppleFall();
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        DrawDefaultInspector();
    }

    private void GenerateAnchorPoints(BenchCinematicTrigger trigger)
    {
        Undo.RecordObject(trigger, "Generate Bench Anchor Points");

        Transform benchTransform = trigger.transform;

        // 1. Seat Point (Sur le banc)
        Transform seat = benchTransform.Find("SeatPoint");
        if (seat == null)
        {
            GameObject seatGo = new GameObject("SeatPoint");
            seatGo.transform.SetParent(benchTransform);
            seatGo.transform.localPosition = new Vector3(-0.4f, 0.45f, 0f);
            seat = seatGo.transform;
            Undo.RegisterCreatedObjectUndo(seatGo, "Create SeatPoint");
        }

        // 2. Exit Seat Point (Au sol devant le banc)
        Transform exitSeat = benchTransform.Find("ExitSeatPoint");
        if (exitSeat == null)
        {
            GameObject exitGo = new GameObject("ExitSeatPoint");
            exitGo.transform.SetParent(benchTransform);
            exitGo.transform.localPosition = new Vector3(-0.4f, 0f, -0.9f);
            exitSeat = exitGo.transform;
            Undo.RegisterCreatedObjectUndo(exitGo, "Create ExitSeatPoint");
        }

        // 3. Passerby Stop Point (Au sol devant le banc, au niveau de la fleur)
        Transform passerbyStop = benchTransform.Find("PasserbyStopPoint");
        if (passerbyStop == null)
        {
            GameObject stopGo = new GameObject("PasserbyStopPoint");
            stopGo.transform.SetParent(benchTransform);
            stopGo.transform.localPosition = new Vector3(0.3f, 0f, -1.2f);
            passerbyStop = stopGo.transform;
            Undo.RegisterCreatedObjectUndo(stopGo, "Create PasserbyStopPoint");
        }

        // 4. Passerby Exit Point (Vers la droite)
        Transform passerbyExit = benchTransform.Find("PasserbyExitPoint");
        if (passerbyExit == null)
        {
            GameObject exitGo = new GameObject("PasserbyExitPoint");
            exitGo.transform.SetParent(benchTransform);
            exitGo.transform.localPosition = new Vector3(10f, 0f, -1.2f);
            passerbyExit = exitGo.transform;
            Undo.RegisterCreatedObjectUndo(exitGo, "Create PasserbyExitPoint");
        }

        // 5. Bench Center Point (Au milieu exact du banc pour cadrer la caméra)
        Transform centerPoint = benchTransform.Find("BenchCenterPoint") ?? benchTransform.Find("BenchCenter");
        if (centerPoint == null)
        {
            GameObject centerGo = new GameObject("BenchCenterPoint");
            centerGo.transform.SetParent(benchTransform);
            centerGo.transform.position = trigger.GetBenchCenter();
            centerPoint = centerGo.transform;
            Undo.RegisterCreatedObjectUndo(centerGo, "Create BenchCenterPoint");
        }

        // Assigner via SerializedObject pour la sérialisation propre
        SerializedObject so = serializedObject;
        so.Update();

        SerializedProperty seatProp = so.FindProperty("seatPoint");
        if (seatProp.objectReferenceValue == null) seatProp.objectReferenceValue = seat;

        SerializedProperty exitSeatProp = so.FindProperty("exitSeatPoint");
        if (exitSeatProp.objectReferenceValue == null) exitSeatProp.objectReferenceValue = exitSeat;

        SerializedProperty passerbyStopProp = so.FindProperty("passerbyStopPoint");
        if (passerbyStopProp.objectReferenceValue == null) passerbyStopProp.objectReferenceValue = passerbyStop;

        SerializedProperty passerbyExitProp = so.FindProperty("passerbyExitPoint");
        if (passerbyExitProp.objectReferenceValue == null) passerbyExitProp.objectReferenceValue = passerbyExit;

        SerializedProperty camFocusProp = so.FindProperty("cameraFocusTarget");
        if (camFocusProp.objectReferenceValue == null) camFocusProp.objectReferenceValue = centerPoint;

        // 6. Pomme suspendue dans le ciel
        SerializedProperty appleProp = so.FindProperty("fallingApple");
        if (appleProp != null && appleProp.objectReferenceValue == null)
        {
            GameObject apple = GameObject.Find("Apple") ?? GameObject.Find("Pomme");
            if (apple != null)
            {
                appleProp.objectReferenceValue = apple;
            }
        }

        so.ApplyModifiedProperties();

        Debug.Log("[BenchCinematicTriggerEditor] Points d'ancrage générés et assignés avec succès !");
    }
}
