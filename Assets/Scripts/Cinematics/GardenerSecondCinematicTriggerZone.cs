using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

/// <summary>
/// Script de cinématique spécialisé pour la deuxième rencontre avec le Jardinier.
/// Gère le zoom sur le jardinier, un premier dialogue, l'animation de claquement de doigts (snap),
/// l'apparition d'un monstre au point défini, un second dialogue, et l'appel de l'événement de combat.
/// </summary>
[AddComponentMenu("2.5D RPG/Gardener Second Cinematic Trigger Zone")]
public class GardenerSecondCinematicTriggerZone : CinematicTriggerZone
{
    [Header("Story Progression Conditions")]
    [Tooltip("Le flag requis dans le StoryStateManager pour que cette cinématique puisse se déclencher.")]
    [SerializeField] private string requiredFlag = "gardener_intro_completed";

    [Tooltip("La valeur attendue pour ce flag.")]
    [SerializeField] private bool expectedValue = true;

    [Header("Séquence Spécifique Jardinier 2")]
    [Tooltip("Le deuxième dialogue à lancer après l'apparition du monstre.")]
    [SerializeField] private DialogueData secondDialogueData;

    [Tooltip("L'Animator du Jardinier pour jouer l'animation de claquement de doigts. Si vide, sera recherché sur le focusTarget.")]
    [SerializeField] private Animator gardenerAnimator;

    [Tooltip("Nom de l'état d'animation à jouer pour le claquement de doigts (ex: snap).")]
    [SerializeField] private string snapAnimationStateName = "snap";

    [Tooltip("Nom de l'état d'animation de repos (ex: idle ou levitate) à rejouer après le claquement de doigts.")]
    [SerializeField] private string idleAnimationStateName = "idle";

    [Tooltip("Délai (en secondes) après le début de l'animation de claquement de doigts avant l'apparition du monstre.")]
    [SerializeField] private float delayBeforeSpawn = 0.5f;

    [Header("Apparition du Monstre")]
    [Tooltip("Le Prefab du monstre à faire apparaître. Si renseigné, il sera instancié au Spawn Point.")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("Le point d'apparition (Spawn Point) du monstre.")]
    [SerializeField] private Transform monsterSpawnPoint;

    [Tooltip("Un GameObject du monstre déjà présent dans la scène mais inactif. Sera activé s'il est renseigné.")]
    [SerializeField] private GameObject monsterSceneObject;

    [Tooltip("Effet visuel (ex: particules) à instancier au point d'apparition du monstre au moment du spawn.")]
    [SerializeField] private GameObject spawnEffectPrefab;

    [Tooltip("Délai (en secondes) après l'apparition du monstre (et la fin du déplacement de la caméra) avant de lancer le second dialogue.")]
    [SerializeField] private float delayAfterSpawn = 1.2f;

    [Tooltip("Durée de la transition de la caméra pour glisser du Jardinier vers le monstre.")]
    [SerializeField] private float transitionToMonsterDuration = 1.5f;

    [Header("Transition Combat Rythmique")]
    [Tooltip("Données de combat rythmique spécifiques (EnemyCombatData) pour le monstre apparu. Si renseigné, sera assigné au monstre.")]
    [SerializeField] private EnemyCombatData monsterCombatData;

    [Header("Dialogues Tutoriel Jardinier (Optionnel)")]
    [Tooltip("Si vrai, le combat de cette cinématique sera configuré comme le combat tutoriel du Jardinier.")]
    [SerializeField] private bool isGardenerTutorialCombat = true;

    [Tooltip("Dialogue du Jardinier au tout début du combat.")]
    [SerializeField] private DialogueData startTutorialDialogue;

    [Tooltip("Dialogue du Jardinier après la première esquive.")]
    [SerializeField] private DialogueData afterFirstDodgeDialogue;

    [Tooltip("Dialogue du Jardinier après la deuxième esquive.")]
    [SerializeField] private DialogueData afterSecondDodgeDialogue;

    [Tooltip("Dialogue du Jardinier après avoir battu le boss.")]
    [SerializeField] private DialogueData victoryTutorialDialogue;

    [Tooltip("Si vrai, ramène la caméra sur le joueur et réactive ses contrôles à la fin de la cinématique. Si faux, laisse le joueur gelé et la caméra sur place pour le combat.")]
    [SerializeField] private bool endCinematicNormally = false;

    [Tooltip("Événement déclenché à la fin pour lancer le combat au tour par tour.")]
    public UnityEvent onCombatTriggered;

    protected override IEnumerator ExecuteCinematicRoutine()
    {
        // Vérifier le flag requis dans StoryStateManager
        if (StoryStateManager.Instance != null && !string.IsNullOrEmpty(requiredFlag))
        {
            bool flagValue = StoryStateManager.Instance.GetFlag(requiredFlag);
            if (flagValue != expectedValue)
            {
                // Si la condition n'est pas remplie, on réinitialise le trigger pour un passage futur
                alreadyTriggered = false;
                yield break;
            }
        }

        Debug.Log($"[GardenerSecondCinematicTriggerZone] Déclenchement de la deuxième cinématique sur '{gameObject.name}'");

        // 1. Récupération des références caméra et joueur
        EnsureReferences();

        if (virtualCamera == null)
        {
            Debug.LogError("[GardenerSecondCinematicTriggerZone] Aucune CinemachineCamera trouvée dans la scène !");
            yield break;
        }

        if (cameraHelper != null)
        {
            cameraHelper.SaveOriginalSettings();
            cameraHelper.enabled = false;
        }

        // Geler le joueur
        LockPlayer();

        // Attendre avant de commencer le déplacement de la caméra (le joueur est gelé pendant ce temps)
        if (delayBeforeCameraMove > 0f)
        {
            yield return new WaitForSeconds(delayBeforeCameraMove);
        }

        // 2. Transition de la caméra vers la cible de focus (le Jardinier)
        // Résolution automatique du Jardinier si focusTarget n'est pas configuré
        if (focusTarget == null)
        {
            GameObject gardenerObj = GameObject.Find("Gardener");
            if (gardenerObj != null)
            {
                focusTarget = gardenerObj.transform;
            }
            else
            {
                // Rechercher par tag ou tout autre objet contenant "Gardener" dans le nom
                foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (obj.name.Contains("Gardener") && obj.activeInHierarchy)
                    {
                        focusTarget = obj.transform;
                        break;
                    }
                }
            }
        }

        Transform target = focusTarget != null ? focusTarget : (playerMovement != null ? playerMovement.transform : null);
        
        if (target != null)
        {
            yield return StartCoroutine(TransitionCameraToTarget(target));
        }

        // Temporisation avant le premier dialogue
        if (delayBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(delayBeforeDialogue);
        }

        // 3. Déclenchement du premier dialogue (champ dialogueData de la classe de base)
        if (dialogueData != null)
        {
            yield return StartCoroutine(RunDialogue(dialogueData));
        }

        // 4. ANIMATION : Claquement de doigts (snap)
        if (gardenerAnimator == null && focusTarget != null)
        {
            gardenerAnimator = focusTarget.GetComponent<Animator>();
            if (gardenerAnimator == null)
            {
                gardenerAnimator = focusTarget.GetComponentInChildren<Animator>();
            }
        }

        if (gardenerAnimator != null && !string.IsNullOrEmpty(snapAnimationStateName))
        {
            Debug.Log($"[GardenerSecondCinematicTriggerZone] Lecture de l'animation de claquement de doigts '{snapAnimationStateName}' sur le Jardinier.");
            gardenerAnimator.Play(snapAnimationStateName);
        }

        // Attendre le délai avant le spawn du monstre
        if (delayBeforeSpawn > 0f)
        {
            yield return new WaitForSeconds(delayBeforeSpawn);
        }

        // 5. APPARITION DU MONSTRE
        Vector3 spawnPos = monsterSpawnPoint != null ? monsterSpawnPoint.position : transform.position;
        Quaternion spawnRot = monsterSpawnPoint != null ? monsterSpawnPoint.rotation : Quaternion.identity;

        // Instancier l'effet de spawn (particules, fumée...)
        if (spawnEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(spawnEffectPrefab, spawnPos, spawnRot);
            Destroy(effectInstance, 4f); // Nettoyer après 4 secondes
        }
        else
        {
            // Effet de fumée magique procédural par défaut
            GameObject proceduralEffect = new GameObject("ProceduralSmokeSpawnEffect");
            proceduralEffect.transform.position = spawnPos;
            proceduralEffect.transform.rotation = spawnRot;
            proceduralEffect.AddComponent<ProceduralSmokeEffect>();
            // Note: ProceduralSmokeEffect s'autodétruira de la scène de lui-même
        }

        // Instanciation ou activation du monstre
        GameObject spawnedMonster = null;
        if (monsterPrefab != null)
        {
            spawnedMonster = Instantiate(monsterPrefab, spawnPos, spawnRot);
            spawnedMonster.name = monsterPrefab.name;
            Debug.Log($"[GardenerSecondCinematicTriggerZone] Monstre instancié avec succès : {spawnedMonster.name}");
        }

        if (monsterSceneObject != null)
        {
            monsterSceneObject.transform.position = spawnPos;
            monsterSceneObject.transform.rotation = spawnRot;
            monsterSceneObject.SetActive(true);
            Debug.Log($"[GardenerSecondCinematicTriggerZone] Monstre de scène activé avec succès : {monsterSceneObject.name}");
        }

        // Retourner sur l'animation Idle après le claquement de doigts et le spawn
        if (gardenerAnimator != null && !string.IsNullOrEmpty(idleAnimationStateName))
        {
            gardenerAnimator.Play(idleAnimationStateName);
        }

        // Faire glisser la caméra vers le monstre
        float originalTransitionIn = transitionInDuration;
        transitionInDuration = transitionToMonsterDuration;

        Transform monsterFocus = spawnedMonster != null ? spawnedMonster.transform : (monsterSceneObject != null ? monsterSceneObject.transform : monsterSpawnPoint);
        if (monsterFocus != null)
        {
            yield return StartCoroutine(TransitionCameraToTarget(monsterFocus));
        }

        transitionInDuration = originalTransitionIn;

        // Temporisation après le spawn avant le deuxième dialogue
        if (delayAfterSpawn > 0f)
        {
            yield return new WaitForSeconds(delayAfterSpawn);
        }

        // 6. Déclenchement du second dialogue
        if (secondDialogueData != null)
        {
            yield return StartCoroutine(RunDialogue(secondDialogueData));
        }

        // Temporisation après le dialogue
        if (delayAfterDialogue > 0f)
        {
            yield return new WaitForSeconds(delayAfterDialogue);
        }

        // 7. Retour caméra ou transition vers combat
        if (endCinematicNormally)
        {
            yield return StartCoroutine(TransitionCameraBack());
            
            // Réactiver le joueur
            UnlockPlayer();
        }
        else
        {
            // Conserver le joueur et la caméra verrouillés sur place pour le combat
            Debug.Log("[GardenerSecondCinematicTriggerZone] Séquence terminée. Contrôles et caméra figés pour le début du combat.");
        }

        // 8. Lancement du combat rythmique radial (RhythmCombatManager)
        if (onCombatTriggered != null)
        {
            Debug.Log("[GardenerSecondCinematicTriggerZone] Appel de l'événement onCombatTriggered.");
            onCombatTriggered.Invoke();
        }

        GameObject enemyToFight = spawnedMonster != null ? spawnedMonster : (monsterSceneObject != null ? monsterSceneObject : null);
        if (enemyToFight != null)
        {
            // Associer les données de combat rythmique si configurées dans l'inspecteur
            if (monsterCombatData != null)
            {
                if (isGardenerTutorialCombat)
                {
                    monsterCombatData.IsGardenerTutorial = true;
                    if (startTutorialDialogue != null) monsterCombatData.StartTutorialDialogue = startTutorialDialogue;
                    if (afterFirstDodgeDialogue != null) monsterCombatData.AfterFirstDodgeDialogue = afterFirstDodgeDialogue;
                    if (afterSecondDodgeDialogue != null) monsterCombatData.AfterSecondDodgeDialogue = afterSecondDodgeDialogue;
                    if (victoryTutorialDialogue != null) monsterCombatData.VictoryTutorialDialogue = victoryTutorialDialogue;
                }

                EnemyCombatDataHolder dataHolder = enemyToFight.GetComponent<EnemyCombatDataHolder>();
                if (dataHolder == null) dataHolder = enemyToFight.AddComponent<EnemyCombatDataHolder>();
                dataHolder.CombatData = monsterCombatData;
            }

            RhythmCombatManager rhythmManager = RhythmCombatManager.Instance;
            if (rhythmManager == null) rhythmManager = FindFirstObjectByType<RhythmCombatManager>();

            if (rhythmManager != null)
            {
                Debug.Log($"[GardenerSecondCinematicTriggerZone] Lancement du combat rythmique sur '{enemyToFight.name}' via RhythmCombatManager !");
                rhythmManager.StartCombat(enemyToFight);
            }
            else if (CombatManager.Instance != null)
            {
                Debug.Log($"[GardenerSecondCinematicTriggerZone] Lancement du combat via CombatManager classique sur '{enemyToFight.name}'.");
                CombatManager.Instance.StartCombat(enemyToFight);
            }
            else
            {
                Debug.LogWarning("[GardenerSecondCinematicTriggerZone] Aucun RhythmCombatManager ni CombatManager trouvé dans la scène.");
            }
        }
        else
        {
            Debug.LogWarning("[GardenerSecondCinematicTriggerZone] Impossible de lancer le combat car aucun monstre n'a été instancié ou activé.");
        }
    }
}
