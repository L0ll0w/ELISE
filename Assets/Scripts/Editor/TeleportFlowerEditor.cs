using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TeleportFlower))]
[CanEditMultipleObjects]
public class TeleportFlowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TeleportFlower flower = (TeleportFlower)target;

        serializedObject.Update();

        // 1. Statut de connexion
        EditorGUILayout.Space(5);
        if (flower.ConnectedFlower == null)
        {
            EditorGUILayout.HelpBox("⚠️ Aucune fleur de destination connectée !\nSélectionnez une fleur connectée ci-dessous ou cliquez sur le bouton de détection automatique.", MessageType.Warning);

            if (GUILayout.Button("🔍 Lier à la fleur de TP la plus proche", GUILayout.Height(30)))
            {
                TeleportFlower[] allFlowers = Object.FindObjectsByType<TeleportFlower>(FindObjectsSortMode.None);
                TeleportFlower closest = null;
                float closestDist = float.MaxValue;

                foreach (var other in allFlowers)
                {
                    if (other != flower)
                    {
                        float dist = Vector3.Distance(flower.transform.position, other.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = other;
                        }
                    }
                }

                if (closest != null)
                {
                    Undo.RecordObject(flower, "Connect Teleport Flower");
                    flower.ConnectedFlower = closest;
                    EditorUtility.SetDirty(flower);

                    Undo.RecordObject(closest, "Connect Teleport Flower Bidirectional");
                    closest.ConnectedFlower = flower;
                    EditorUtility.SetDirty(closest);

                    Debug.Log($"[TeleportFlower] 🔗 '{flower.name}' et '{closest.name}' ont été reliées bidirectionnellement ! (Distance : {closestDist:F1}m)");
                }
                else
                {
                    Debug.LogWarning("[TeleportFlower] Aucune autre TeleportFlower trouvée dans la scène !");
                }
            }
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"🔗 Connectée à : {flower.ConnectedFlower.name}", EditorStyles.boldLabel);
            
            if (flower.ConnectedFlower.ConnectedFlower != flower)
            {
                if (GUILayout.Button("🔁 Forcer la connexion bidirectionnelle (Aller-Retour)", GUILayout.Height(24)))
                {
                    Undo.RecordObject(flower.ConnectedFlower, "Set Bidirectional Teleport Flower");
                    flower.ConnectedFlower.ConnectedFlower = flower;
                    EditorUtility.SetDirty(flower.ConnectedFlower);
                    Debug.Log($"[TeleportFlower] '{flower.ConnectedFlower.name}' a été reliée en retour à '{flower.name}' !");
                }
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);

        // 2. Outils de placement des points
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📍 Outils de Positionnement", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Créer Enfant CenterPoint", GUILayout.Height(24)))
        {
            Transform existing = flower.transform.Find("CenterPoint");
            if (existing == null)
            {
                GameObject centerObj = new GameObject("CenterPoint");
                centerObj.transform.SetParent(flower.transform, false);
                centerObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                Undo.RegisterCreatedObjectUndo(centerObj, "Create CenterPoint");
                
                SerializedProperty centerProp = serializedObject.FindProperty("centerPoint");
                centerProp.objectReferenceValue = centerObj.transform;
            }
            else
            {
                SerializedProperty centerProp = serializedObject.FindProperty("centerPoint");
                centerProp.objectReferenceValue = existing;
            }
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Créer Enfant ExitPoint", GUILayout.Height(24)))
        {
            Transform existing = flower.transform.Find("ExitPoint");
            if (existing == null)
            {
                GameObject exitObj = new GameObject("ExitPoint");
                exitObj.transform.SetParent(flower.transform, false);
                exitObj.transform.localPosition = flower.transform.forward * 2.0f;
                Undo.RegisterCreatedObjectUndo(exitObj, "Create ExitPoint");
                
                SerializedProperty exitProp = serializedObject.FindProperty("exitPoint");
                exitProp.objectReferenceValue = exitObj.transform;
            }
            else
            {
                SerializedProperty exitProp = serializedObject.FindProperty("exitPoint");
                exitProp.objectReferenceValue = existing;
            }
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 3. Réglage Caméra Arrivée
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📷 Caméra à l'Arrivée sur cette Fleur", EditorStyles.boldLabel);
        
        TeleportFlower.ArrivalCameraMode effectiveMode = flower.GetEffectiveCameraMode();
        string infoMsg = effectiveMode switch
        {
            TeleportFlower.ArrivalCameraMode.LookAtTarget => $"☀️ Cette fleur cadrera le SOLEIL ({flower.GetEffectiveFocusTarget()?.name ?? "Recherche auto"}) pendant le fondu noir.",
            TeleportFlower.ArrivalCameraMode.RestoreOriginal => "🌊 Cette fleur restaurera la caméra d'origine (CASCADE) pendant le fondu noir.",
            _ => "⏸️ Aucun changement d'orientation caméra (conserve le cadrage actuel)."
        };
        EditorGUILayout.HelpBox(infoMsg, effectiveMode == TeleportFlower.ArrivalCameraMode.Unchanged ? MessageType.None : MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("☀️ Cadrer le Soleil", GUILayout.Height(24)))
        {
            Undo.RecordObject(flower, "Set Sun Camera Arrival");
            flower.ArrivalCamMode = TeleportFlower.ArrivalCameraMode.LookAtTarget;
            flower.ArrivalFocusTarget = flower.GetEffectiveFocusTarget();
            EditorUtility.SetDirty(flower);
        }
        if (GUILayout.Button("🌊 Restaurer Cascade", GUILayout.Height(24)))
        {
            Undo.RecordObject(flower, "Set Restore Camera Arrival");
            flower.ArrivalCamMode = TeleportFlower.ArrivalCameraMode.RestoreOriginal;
            EditorUtility.SetDirty(flower);
        }
        if (GUILayout.Button("🔄 Défaut", GUILayout.Height(24)))
        {
            Undo.RecordObject(flower, "Reset Camera Arrival");
            flower.ArrivalCamMode = TeleportFlower.ArrivalCameraMode.Unchanged;
            EditorUtility.SetDirty(flower);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 4. Test en mode Play
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎮 Test en Jeu (Play Mode)", EditorStyles.boldLabel);

            GUI.enabled = !flower.IsTeleporting && flower.ConnectedFlower != null;
            if (GUILayout.Button("🚀 Déclencher la Téléportation", GUILayout.Height(30)))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    // Placer le joueur à proximité pour tester
                    player.transform.position = flower.transform.position + Vector3.back * 1.5f;
                }
                flower.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            }
            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // Dessiner le reste de l'inspecteur par défaut
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}
