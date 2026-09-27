using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère le combat-énigme du Tournesol au Xylophone en utilisant le SYSTÈME DE COMBAT OFFICIEL DU JEU (RhythmCombatManager).
/// Lance le combat avec le système de combat polaire, la caméra de combat, le cadrage, l'orientation vers le boss et le snapping au sol.
/// Maintient le combat en phase d'esquive permanente sans afficher le menu d'actions.
/// </summary>
[AddComponentMenu("2.5D RPG/World/Sunflower Xylophone Puzzle")]
public class SunflowerXylophonePuzzle : MonoBehaviour
{
    public static SunflowerXylophonePuzzle Instance { get; private set; }

    [Header("Références Principales")]
    [Tooltip("Référence au composant SunflowerXylophone")]
    [SerializeField] private SunflowerXylophone xylophone;

    [Tooltip("GameObject de l'ennemi/Tournesol à combattre. Si vide, utilise le GameObject du Xylophone.")]
    [SerializeField] private GameObject sunflowerEnemyObject;

    [Header("Configuration de l'Énigme")]
    [Tooltip("Flag d'histoire dans StoryStateManager qui déclenche le combat énigme (ex: 'start_xylo_fight')")]
    [SerializeField] private string triggerFlag = "start_xylo_fight";

    [Tooltip("Flag d'histoire défini à True dans StoryStateManager une fois l'énigme réussie (ex: 'xylo_puzzle_cleared')")]
    [SerializeField] private string victoryFlag = "xylo_puzzle_cleared";

    [Header("Configuration de la Grille Radiale")]
    [Tooltip("Angle d'orientation du centre de la grille en degrés (270° = vers le bas/écran, 90° = vers le haut, 0° = droite, 180° = gauche).")]
    [SerializeField] private float arcCenterAngle = 270f;

    [Tooltip("Angle d'ouverture total de l'arc à 8 pales en degrés (ex: 160°).")]
    [SerializeField] private float arcAngleDegrees = 160f;

    [Tooltip("Séquence des cases (0 à 7) à exécuter dans l'ordre pour réussir le puzzle (ex: 0, 2, 4, 5, 4, 2, 0)")]
    [SerializeField] private int[] targetSequence = new int[] { 0, 2, 4, 5, 4, 2, 0 };

    [Tooltip("Dialogue optionnel à lancer immédiatement après la victoire")]
    [SerializeField] private DialogueData victoryDialogue;

    [Header("Feedback Visuel & Sonore")]
    [Tooltip("Effet sonore joué lorsqu'une note correcte de la séquence est sautée")]
    [SerializeField] private AudioClip correctStepSound;

    [Tooltip("Effet sonore joué en cas de victoire")]
    [SerializeField] private AudioClip victorySound;

    [Header("Configuration Audio du Combat")]
    [Tooltip("Si vrai, désactive la musique de fond du combat pendant le puzzle du Tournesol pour n'entendre que les notes jouées.")]
    [SerializeField] private bool disableCombatMusic = true;

    [Range(0.1f, 5.0f)]
    [Tooltip("Volume de lecture des notes audio des pales (1.0 = volume normal, 2.0 = deux fois plus fort, 3.0 = trois fois plus fort, etc.).")]
    [SerializeField] private float keyNotesVolume = 1.5f;

    [Header("Animation d'Émergence du Sol")]
    [Tooltip("Si vrai, joue une animation d'émergence des pales hors du sol au début du combat.")]
    [SerializeField] private bool animateEmergingFromGround = true;

    [Tooltip("Profondeur sous le sol (en mètres) d'où émergent les pales au départ.")]
    [SerializeField] private float undergroundDepth = 1.2f;

    [Tooltip("Durée de l'animation d'émergence pour chaque pale en secondes.")]
    [SerializeField] private float riseDuration = 0.45f;

    [Tooltip("Délai de décalage (en secondes) entre la sortie de chaque pale (effet de cascade).")]
    [SerializeField] private float staggerDelay = 0.05f;

    [Tooltip("Effet sonore optionnel joué lorsque les pales émergent du sol.")]
    [SerializeField] private AudioClip keyEmergingSound;

    [Header("Positionnement du Joueur sur les Pales")]
    [Tooltip("Aligner automatiquement la position du joueur sur le GameObject de la pale correspondante.")]
    [SerializeField] private bool alignPlayerOnKeys = true;

    [Tooltip("Hauteur verticale (offset Y) pour poser les pieds du joueur sur la pale (0.47m par défaut).")]
    [SerializeField] private float playerYOffsetOnKey = 0.47f;

    [Tooltip("Décalage offset 3D optionnel (X, Y, Z) pour affiner le placement du joueur sur la pale.")]
    [SerializeField] private Vector3 playerPositionOffsetOnKey = Vector3.zero;

    [Header("Pales Placées Manuellement (Cases 0 à 7)")]
    [Tooltip("GameObjects des 8 pales placées manuellement dans la scène pour chaque case (0 à 7).")]
    [SerializeField] private GameObject[] placedKeys = new GameObject[8];

    [Tooltip("Clips audio de la note attribuée à chaque case (0 à 7). joué lors du saut sur la case.")]
    [SerializeField] private AudioClip[] keyAudioClips = new AudioClip[8];

    [Tooltip("Masquer automatiquement les pales au démarrage du jeu jusqu'au début du combat.")]
    [SerializeField] private bool autoHideKeysOnStart = true;

    [Tooltip("Préfabriqués optionnels (obsolète si placedKeys est renseigné).")]
    [SerializeField] private GameObject[] keyPrefabs = new GameObject[8];

    // État interne
    private bool isPuzzleActive = false;
    private bool isEmergingAnimationActive = false;
    private int currentSequenceIndex = 0;
    private int lastRecordedSector = -1;
    private Vector3[] originalKeyLocalPositions;
    private float activePlayerYOffset = 0.47f;
    private RhythmPlayerController subscribedPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        if (xylophone == null) xylophone = GetComponent<SunflowerXylophone>();
        if (sunflowerEnemyObject == null && xylophone != null) sunflowerEnemyObject = xylophone.gameObject;
        if (sunflowerEnemyObject == null) sunflowerEnemyObject = gameObject;

        CacheOriginalKeyPositions();
    }

    private void OnDisable()
    {
        UnregisterPlayerEvents();
    }

    private void OnDestroy()
    {
        UnregisterPlayerEvents();
    }

    private void RegisterPlayerEvents(RhythmPlayerController rPlayer)
    {
        UnregisterPlayerEvents();
        if (rPlayer != null)
        {
            subscribedPlayer = rPlayer;
            subscribedPlayer.OnLanded += HandlePlayerLanded;
        }
    }

    private void UnregisterPlayerEvents()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnLanded -= HandlePlayerLanded;
            subscribedPlayer = null;
        }
    }

    private void HandlePlayerLanded()
    {
        if (!isPuzzleActive) return;
        if (isEmergingAnimationActive) return;

        if (RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive)
        {
            RhythmPlayerController rPlayer = RhythmCombatManager.Instance.PlayerController;
            if (rPlayer != null && rPlayer.IsInputEnabled)
            {
                int currentSector = rPlayer.CurrentSector;
                if (currentSector >= 0)
                {
                    lastRecordedSector = currentSector;
                    OnPlayerSteppedOnSector(currentSector);
                }
            }
        }
    }

    private void Start()
    {
        CacheOriginalKeyPositions();

        // Masquer les pales placées au sol avant le déclenchement du combat
        if (autoHideKeysOnStart && !isPuzzleActive)
        {
            SetKeysActive(false);
        }
    }

    private void Update()
    {
        // 1. Écouter si le flag de déclenchement du dialogue a été activé
        if (!isPuzzleActive && StoryStateManager.Instance != null && !string.IsNullOrEmpty(triggerFlag))
        {
            if (StoryStateManager.Instance.GetFlag(triggerFlag))
            {
                StoryStateManager.Instance.SetFlag(triggerFlag, false);
                StartPuzzleCombat();
            }
        }

        // 2. Si l'énigme est active, surveiller le déplacement du joueur sur la grille de combat officielle
        if (isPuzzleActive && RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive)
        {
            RhythmPlayerController rPlayer = RhythmCombatManager.Instance.PlayerController;
            if (rPlayer != null)
            {
                if (subscribedPlayer != rPlayer)
                {
                    RegisterPlayerEvents(rPlayer);
                }

                int currentSector = rPlayer.CurrentSector;
                if (currentSector != -1)
                {
                    // Aligner le joueur sur la pale correspondante si activé
                    if (alignPlayerOnKeys)
                    {
                        Vector3 targetPlayerPos;
                        if (isEmergingAnimationActive && RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.RadialGrid != null)
                        {
                            // Pendant la sortie des pales du sol, ancrer le joueur au niveau du sol de la grille (jamais sous le sol)
                            Vector3 gridCellPos = RhythmCombatManager.Instance.RadialGrid.GetCellPosition(0, currentSector);
                            targetPlayerPos = gridCellPos + Vector3.up * activePlayerYOffset + playerPositionOffsetOnKey;
                        }
                        else
                        {
                            // Une fois les pales sorties, aligner le joueur sur la position finale de la pale
                            Vector3 keyPos = GetKeyFinalWorldPosition(currentSector);
                            targetPlayerPos = new Vector3(keyPos.x, keyPos.y + activePlayerYOffset, keyPos.z) + playerPositionOffsetOnKey;
                        }

                        rPlayer.OverrideTargetPosition(targetPlayerPos);
                    }

                    if (currentSector != lastRecordedSector)
                    {
                        lastRecordedSector = currentSector;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Lance le combat rythmique officiel via RhythmCombatManager en mode déplacement continu sans menu UI.
    /// </summary>
    public void StartPuzzleCombat()
    {
        if (isPuzzleActive) return;

        Debug.Log("[SunflowerXylophonePuzzle] ⚔️ Lancement du combat rythmique officiel du Tournesol !");

        isPuzzleActive = true;
        currentSequenceIndex = 0;
        lastRecordedSector = -1;

        // Récupérer le RhythmCombatManager
        RhythmCombatManager rhythmManager = RhythmCombatManager.Instance;
        if (rhythmManager == null) rhythmManager = FindFirstObjectByType<RhythmCombatManager>();
        if (rhythmManager == null)
        {
            GameObject rObj = new GameObject("RhythmCombatManager");
            rhythmManager = rObj.AddComponent<RhythmCombatManager>();
        }

        // Configurer le mode déplacement continu sans menu et la coupure de la musique de combat
        rhythmManager.IsEndlessMovementPhase = true;
        rhythmManager.HideCombatMenuUI = true;
        rhythmManager.MuteCombatMusic = disableCombatMusic;

        // Récupérer la grille radiale unique officielle du RhythmCombatManager
        RadialCombatGrid radialGrid = rhythmManager.RadialGrid;
        if (radialGrid != null)
        {
            // Configurer la grille en arc frontal à 8 secteurs (1 rangée) selon les paramètres de l'Inspecteur
            radialGrid.Configure(RadialCombatGrid.GridShapeType.PartialArc, 8, 1, arcAngleDegrees, arcCenterAngle);
        }

        // Lancer la coroutine d'initialisation du combat
        StartCoroutine(StartPuzzleCombatRoutine(rhythmManager));
    }

    private IEnumerator StartPuzzleCombatRoutine(RhythmCombatManager rhythmManager)
    {
        // Lancer le combat rythmique officiel
        rhythmManager.StartCombat(sunflowerEnemyObject, xylophone != null ? xylophone.transform : transform);

        // Attendre que le fondu au noir soit effectif (écran totalement noir)
        yield return new WaitForSeconds(0.6f);

        // Masquer le xylophone de base pendant que l'écran est au noir
        if (xylophone != null)
        {
            xylophone.SetXylophoneVisualsAndAudioActive(false);
        }

        // Attendre que le fondu de retour soit terminé et que le combat soit actif
        while (rhythmManager.CurrentState != RhythmCombatManager.CombatState.Active)
        {
            yield return null;
        }

        RhythmPlayerController rPlayer = rhythmManager.PlayerController;

        // Hauteur Y au niveau du sol (0.47m par défaut) au départ avant l'émergence
        activePlayerYOffset = 0.47f;

        // Désactiver les entrées/déplacements du joueur pendant l'apparition des pales
        if (rPlayer != null)
        {
            rPlayer.SetInputEnabled(false);
        }

        // Déclencher l'animation d'émergence des pales hors du sol
        if (animateEmergingFromGround)
        {
            yield return StartCoroutine(AnimateKeysEmergingFromGroundRoutine(rPlayer));
        }
        else
        {
            SetKeysActive(true);
        }

        // Une fois les pales émergées et le saut terminé, la hauteur du joueur prend la valeur choisie dans l'Inspecteur
        activePlayerYOffset = playerYOffsetOnKey;

        // Réactiver les contrôles du joueur
        if (rPlayer != null)
        {
            rPlayer.SetInputEnabled(true);
        }
    }

    private Vector3 GetKeyFinalWorldPosition(int sectorIndex)
    {
        GameObject[] keys = GetActiveKeyObjects();
        if (keys != null && sectorIndex >= 0 && sectorIndex < keys.Length && keys[sectorIndex] != null)
        {
            if (originalKeyLocalPositions != null && sectorIndex < originalKeyLocalPositions.Length)
            {
                Transform parent = keys[sectorIndex].transform.parent;
                if (parent != null)
                {
                    return parent.TransformPoint(originalKeyLocalPositions[sectorIndex]);
                }
            }
            return keys[sectorIndex].transform.position;
        }
        return transform.position;
    }

    private void CacheOriginalKeyPositions()
    {
        GameObject[] keys = GetActiveKeyObjects();
        if (keys == null) return;

        if (originalKeyLocalPositions == null || originalKeyLocalPositions.Length != keys.Length)
        {
            originalKeyLocalPositions = new Vector3[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] != null)
                {
                    originalKeyLocalPositions[i] = keys[i].transform.localPosition;
                }
            }
        }
    }

    private void RestoreOriginalKeyPositions()
    {
        GameObject[] keys = GetActiveKeyObjects();
        if (keys == null || originalKeyLocalPositions == null) return;

        for (int i = 0; i < keys.Length && i < originalKeyLocalPositions.Length; i++)
        {
            if (keys[i] != null)
            {
                keys[i].transform.localPosition = originalKeyLocalPositions[i];
            }
        }
    }

    private void SetKeyCollidersEnabled(bool enabled)
    {
        GameObject[] keys = GetActiveKeyObjects();
        if (keys == null) return;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null)
            {
                Collider[] colliders = keys[i].GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    if (col != null) col.enabled = enabled;
                }
            }
        }
    }

    private IEnumerator AnimateKeysEmergingFromGroundRoutine(RhythmPlayerController rPlayer)
    {
        isEmergingAnimationActive = true;
        SetKeyCollidersEnabled(false);
        CacheOriginalKeyPositions();
        GameObject[] keys = GetActiveKeyObjects();
        if (keys == null)
        {
            SetKeyCollidersEnabled(true);
            isEmergingAnimationActive = false;
            yield break;
        }

        int playerSector = (rPlayer != null) ? rPlayer.CurrentSector : 0;
        bool hasPlayerJumped = false;

        // Positionner toutes les pales sous le sol et les activer
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null && i < originalKeyLocalPositions.Length)
            {
                Vector3 targetPos = originalKeyLocalPositions[i];
                keys[i].transform.localPosition = targetPos - new Vector3(0f, undergroundDepth, 0f);
                keys[i].SetActive(true);
            }
        }

        if (keyEmergingSound != null)
        {
            AudioSource.PlayClipAtPoint(keyEmergingSound, transform.position);
        }

        // Déclencher la montée en cascade de chaque pale
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null && i < originalKeyLocalPositions.Length)
            {
                Vector3 startPos = keys[i].transform.localPosition;
                Vector3 targetPos = originalKeyLocalPositions[i];
                StartCoroutine(AnimateSingleKeyRise(keys[i].transform, startPos, targetPos, riseDuration));

                // Lorsque la pale où se trouve le joueur commence à émerger, basculer la hauteur cible sur la pale et déclencher le saut !
                if (i == playerSector && !hasPlayerJumped && rPlayer != null)
                {
                    hasPlayerJumped = true;
                    activePlayerYOffset = playerYOffsetOnKey;
                    rPlayer.Jump();
                }

                yield return new WaitForSeconds(staggerDelay);
            }
        }

        // Si le joueur n'a pas encore sauté, déclencher le saut et basculer la hauteur
        if (!hasPlayerJumped && rPlayer != null)
        {
            hasPlayerJumped = true;
            activePlayerYOffset = playerYOffsetOnKey;
            rPlayer.Jump();
        }

        // Attendre la fin complète de l'émergence et de la retombée du saut
        yield return new WaitForSeconds(Mathf.Max(riseDuration, 0.45f));
        SetKeyCollidersEnabled(true);
        isEmergingAnimationActive = false;
    }

    private IEnumerator AnimateSingleKeyRise(Transform keyTransform, Vector3 startPos, Vector3 targetPos, float duration)
    {
        if (keyTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (keyTransform == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Courbe EaseOutBack avec léger rebond élastique en haut
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easeBack = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);

            keyTransform.localPosition = Vector3.LerpUnclamped(startPos, targetPos, easeBack);
            yield return null;
        }

        if (keyTransform != null)
        {
            keyTransform.localPosition = targetPos;
        }
    }

    /// <summary>
    /// Active ou désactive toutes les pales renseignées (placedKeys ou keyPrefabs).
    /// </summary>
    private void SetKeysActive(bool active)
    {
        GameObject[] targetKeys = GetActiveKeyObjects();
        if (targetKeys == null) return;

        for (int i = 0; i < targetKeys.Length; i++)
        {
            if (targetKeys[i] != null)
            {
                targetKeys[i].SetActive(active);
            }
        }
    }

    private GameObject[] GetActiveKeyObjects()
    {
        if (placedKeys != null && placedKeys.Length > 0 && HasAnyValidObject(placedKeys))
        {
            return placedKeys;
        }
        return keyPrefabs;
    }

    private bool HasAnyValidObject(GameObject[] array)
    {
        if (array == null) return false;
        foreach (var obj in array)
        {
            if (obj != null) return true;
        }
        return false;
    }

    private Transform GetKeyTransform(int sectorIndex)
    {
        GameObject[] targetKeys = GetActiveKeyObjects();
        if (targetKeys != null && sectorIndex >= 0 && sectorIndex < targetKeys.Length && targetKeys[sectorIndex] != null)
        {
            return targetKeys[sectorIndex].transform;
        }
        return null;
    }

    /// <summary>
    /// Joue un clip audio à une position donnée avec gestion du multiplicateur de volume (supporte les volumes > 1.0).
    /// </summary>
    private void PlayNoteSoundAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        float remainingVol = keyNotesVolume;
        while (remainingVol > 0f)
        {
            float volChunk = Mathf.Min(remainingVol, 1.0f);
            AudioSource.PlayClipAtPoint(clip, position, volChunk);
            remainingVol -= 1.0f;
        }
    }

    /// <summary>
    /// Appelé chaque fois que le joueur se déplace sur un nouveau secteur de la grille de combat officielle.
    /// </summary>
    private void OnPlayerSteppedOnSector(int sectorIndex)
    {
        Debug.Log($"[SunflowerXylophonePuzzle] 🎵 Le joueur a marché sur le secteur {sectorIndex}");

        // 1. Jouer la note audio attribuée à cette case
        Vector3 cellPos = transform.position;
        if (RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.RadialGrid != null)
        {
            cellPos = RhythmCombatManager.Instance.RadialGrid.GetCellPosition(0, sectorIndex);
        }

        if (keyAudioClips != null && sectorIndex < keyAudioClips.Length && keyAudioClips[sectorIndex] != null)
        {
            PlayNoteSoundAtPosition(keyAudioClips[sectorIndex], cellPos);
        }
        else if (xylophone != null)
        {
            xylophone.PlayNote(sectorIndex);
        }

        // 2. Animer le rebond visuel de la pale sur la case
        Transform keyTransform = GetKeyTransform(sectorIndex);
        if (keyTransform != null)
        {
            StartCoroutine(AnimateKeyHitEffect(keyTransform));
        }

        // 3. Vérification de la séquence
        if (targetSequence != null && targetSequence.Length > 0)
        {
            int expectedSector = targetSequence[currentSequenceIndex];

            if (sectorIndex == expectedSector)
            {
                currentSequenceIndex++;
                Debug.Log($"[SunflowerXylophonePuzzle] 🍏 Note correcte ({currentSequenceIndex}/{targetSequence.Length})");

                if (correctStepSound != null)
                {
                    AudioSource.PlayClipAtPoint(correctStepSound, cellPos);
                }

                if (currentSequenceIndex >= targetSequence.Length)
                {
                    OnPuzzleSolved();
                }
            }
            else
            {
                Debug.Log("[SunflowerXylophonePuzzle] ❌ Mauvaise note ! Réinitialisation.");
                currentSequenceIndex = (sectorIndex == targetSequence[0]) ? 1 : 0;
            }
        }
    }

    private IEnumerator AnimateKeyHitEffect(Transform keyTransform)
    {
        if (keyTransform == null) yield break;

        Vector3 origPos = keyTransform.localPosition;
        Vector3 sunkenPos = origPos - new Vector3(0, 0.08f, 0);
        Vector3 origScale = keyTransform.localScale;
        Vector3 expandedScale = origScale * 1.15f;

        float elapsed = 0f;
        float duration = 0.08f;

        while (elapsed < duration)
        {
            if (keyTransform == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            keyTransform.localPosition = Vector3.Lerp(origPos, sunkenPos, t);
            keyTransform.localScale = Vector3.Lerp(origScale, expandedScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            if (keyTransform == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            keyTransform.localPosition = Vector3.Lerp(sunkenPos, origPos, t);
            keyTransform.localScale = Vector3.Lerp(expandedScale, origScale, t);
            yield return null;
        }

        if (keyTransform != null)
        {
            keyTransform.localPosition = origPos;
            keyTransform.localScale = origScale;
        }
    }

    /// <summary>
    /// Déclenché lorsque l'énigme est résolue.
    /// </summary>
    private void OnPuzzleSolved()
    {
        Debug.Log("[SunflowerXylophonePuzzle] 🎉 ÉNIGME RÉUSSIE ! Fin du combat !");

        isPuzzleActive = false;
        UnregisterPlayerEvents();

        // Désactiver les pales du décor à la fin du combat et réinitialiser leurs positions
        RestoreOriginalKeyPositions();
        SetKeysActive(false);

        if (victorySound != null)
        {
            AudioSource.PlayClipAtPoint(victorySound, transform.position);
        }

        // Mettre fin au combat rythmique officiel (restaure la caméra, le joueur et l'environnement)
        if (RhythmCombatManager.Instance != null)
        {
            RhythmCombatManager.Instance.IsEndlessMovementPhase = false;
            RhythmCombatManager.Instance.HideCombatMenuUI = false;
            RhythmCombatManager.Instance.MuteCombatMusic = false;
            RhythmCombatManager.Instance.EndCombat(true);
        }

        // Définir le flag de victoire dans StoryStateManager
        if (StoryStateManager.Instance != null && !string.IsNullOrEmpty(victoryFlag))
        {
            StoryStateManager.Instance.SetFlag(victoryFlag, true);
        }

        // Réafficher le xylophone de base et relancer sa musique
        if (xylophone != null)
        {
            xylophone.SetXylophoneVisualsAndAudioActive(true);
        }

        // Lancer le dialogue de victoire s'il existe
        if (victoryDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(victoryDialogue);
        }
    }
}
