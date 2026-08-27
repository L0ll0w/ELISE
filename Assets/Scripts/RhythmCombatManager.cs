using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Gestionnaire principal du système de combat rythmique radial.
/// Gère la boucle de combat, la génération des attaques ennemies en rythme,
/// la détection des dégâts, les attaques du joueur sur le beat, et le système de Tag-Team.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Rhythm Combat Manager")]
public class RhythmCombatManager : MonoBehaviour
{
    public static RhythmCombatManager Instance { get; private set; }

    /// <summary>
    /// Indique si le combat rythmique est actuellement actif.
    /// </summary>
    public bool IsCombatActive => activeEnemy != null;

    public enum CombatState { Transitioning, Active, Victory, Defeat }

    [Header("Configuration Rythmique")]
    [Tooltip("Musique de combat (Clip audio).")]
    [SerializeField] private AudioClip combatMusicClip;
    [Tooltip("BPM de la musique de combat.")]
    [SerializeField] private float musicBpm = 120f;

    [Header("Configuration de la Grille")]
    [Tooltip("Prefab ou composant de la grille circulaire.")]
    [SerializeField] private RadialCombatGrid radialGrid;

    [Header("Configuration Ennemi par Défaut (Fallback)")]
    [SerializeField] private EnemyCombatData defaultCombatData;

    [Header("Paramètres du Joueur")]
    [Tooltip("Prefab de particules lors d'une attaque réussie sur le beat.")]
    [SerializeField] private ParticleSystem attackSuccessParticles;
    [Tooltip("Prefab de particules quand le joueur se fait toucher.")]
    [SerializeField] private ParticleSystem hitParticles;

    [Header("Animations du Joueur")]
    [Tooltip("Le nom de l'état d'animation à jouer sur l'Animator du joueur pendant le combat rythmique (ex: dance).")]
    [SerializeField] private string combatAnimationStateName = "dance";

    [Header("Configuration Caméra")]
    [Tooltip("Distance de la caméra par rapport au boss.")]
    [SerializeField] private float cameraDistance = 10f;
    [Tooltip("Hauteur de la caméra au-dessus de la grille.")]
    [SerializeField] private float cameraHeight = 6f;
    [Tooltip("Inclinaison (Pitch) de la caméra.")]
    [SerializeField] private float cameraPitch = 30f;

    [Header("Configuration Caméra Tour Joueur")]
    [Tooltip("Distance de la caméra par rapport au joueur lors de son tour.")]
    [SerializeField] private float playerTurnCameraDistance = 4f;
    [Tooltip("Hauteur de la caméra lors du tour du joueur.")]
    [SerializeField] private float playerTurnCameraHeight = 1.5f;
    [Tooltip("Décalage latéral gauche (offset) de la caméra par rapport au joueur.")]
    [SerializeField] private float playerTurnCameraLeftOffset = 0.8f;
    [Tooltip("Angle d'inclinaison Z (Dutch angle) lors du tour du joueur.")]
    [SerializeField] private float playerTurnCameraTiltZ = -4f;

    [Header("Configuration Caméra Tour Dialogue")]
    [Tooltip("Distance de la caméra par rapport au boss/ennemi lors du dialogue.")]
    [SerializeField] private float talkPhaseCameraDistance = 3.5f;
    [Tooltip("Hauteur de la caméra lors du dialogue.")]
    [SerializeField] private float talkPhaseCameraHeight = 2.0f;
    [Tooltip("Décalage latéral gauche de la caméra lors du dialogue.")]
    [SerializeField] private float talkPhaseCameraLeftOffset = 0.5f;
    [Tooltip("Angle d'inclinaison Z lors du dialogue.")]
    [SerializeField] private float talkPhaseCameraTiltZ = 3.0f;

    [Header("Positionnement 3D du Dialogue")]
    [Tooltip("Rotation X/Y/Z pour l'effet 3D de la boîte de dialogue (en degrés).")]
    [SerializeField] private Vector3 customDialogueRotation = Vector3.zero;
    [Tooltip("Position offset X/Y/Z (décalage) de la boîte de dialogue.")]
    [SerializeField] private Vector3 customDialoguePositionOffset = Vector3.zero;

    [Header("Configuration de la Boîte de Dialogue Fallback")]
    [Tooltip("Anchor Min X/Y de la boîte de dialogue de secours.")]
    [SerializeField] private Vector2 fallbackDialogueAnchorMin = new Vector2(0.3f, 0.05f);
    [Tooltip("Anchor Max X/Y de la boîte de dialogue de secours.")]
    [SerializeField] private Vector2 fallbackDialogueAnchorMax = new Vector2(0.7f, 0.22f);

    [Header("Polices d'écriture")]
    [Tooltip("Police d'écriture personnalisée pour les textes de combat (TMP Font Asset).")]
    [SerializeField] private TMP_FontAsset customCombatFont;

    [Header("Positionnement 3D des PV Joueurs")]
    [Tooltip("Rotation X/Y/Z pour l'effet 3D des barres de vie des joueurs (en degrés).")]
    [SerializeField] private Vector3 customPlayerHPRotation = Vector3.zero;
    [Tooltip("Position offset X/Y/Z (décalage) des barres de vie des joueurs.")]
    [SerializeField] private Vector3 customPlayerHPPositionOffset = Vector3.zero;

    [Header("Champs UI Personnalisés (GameObjects)")]
    [Tooltip("Le canvas personnalisé contenant votre menu.")]
    [SerializeField] private Canvas customCombatCanvas;
    [Tooltip("Le panel (RectTransform) contenant vos boutons de combat à orienter en 3D.")]
    [SerializeField] private RectTransform customMenuPanel;
    [Tooltip("Bouton d'attaque personnalisé.")]
    [SerializeField] private Button customFightButton;
    [Tooltip("Bouton de dialogue (Parler) personnalisé.")]
    [SerializeField] private Button customTalkButton;
    [Tooltip("Bouton de compagnon personnalisé.")]
    [SerializeField] private Button customCompanionButton;
    [Tooltip("Bouton de fuite personnalisé.")]
    [SerializeField] private Button customEscapeButton;

    [Header("Rotation 3D du Menu Personnalisé")]
    [Tooltip("Rotation X (inclinaison verticale) pour l'effet 3D.")]
    [SerializeField] private float customMenuRotationX = 15f;
    [Tooltip("Rotation Y (inclinaison horizontale) pour l'effet 3D.")]
    [SerializeField] private float customMenuRotationY = -25f;
    [Tooltip("Rotation Z (rotation à plat) pour l'effet 3D.")]
    [SerializeField] private float customMenuRotationZ = -5f;
    [Tooltip("Distance du plan (Canvas planeDistance) par rapport à la caméra.")]
    [SerializeField] private float customMenuPlaneDistance = 3f;
    [Tooltip("Position offset X/Y/Z (décalage de position) pour ajuster son placement en direct.")]
    [SerializeField] private Vector3 customMenuPositionOffset = Vector3.zero;

    [Header("Champs QTE Personnalisés (UI Attaque)")]
    [Tooltip("Le panel (RectTransform) de QTE personnalisé.")]
    [SerializeField] private RectTransform customQtePanel;
    [Tooltip("Zone parfaite du QTE personnalisé.")]
    [SerializeField] private RectTransform customQteTargetPerfect;
    [Tooltip("Zone bonne du QTE personnalisé.")]
    [SerializeField] private RectTransform customQteTargetGood;
    [Tooltip("Indicateur de curseur du QTE personnalisé.")]
    [SerializeField] private RectTransform customQteIndicator;
    [Tooltip("Instruction textuelle du QTE personnalisé.")]
    [SerializeField] private TextMeshProUGUI customQteInstructionText;
    [Tooltip("Feedback textuel du QTE personnalisé.")]
    [SerializeField] private TextMeshProUGUI customQteFeedbackText;

    [Header("Rotation 3D de l'Attaque / QTE")]
    [Tooltip("Rotation X (inclinaison verticale) pour l'effet 3D de la QTE.")]
    [SerializeField] private float customQteRotationX = 15f;
    [Tooltip("Rotation Y (inclinaison horizontale) pour l'effet 3D de la QTE.")]
    [SerializeField] private float customQteRotationY = -25f;
    [Tooltip("Rotation Z (rotation à plat) pour l'effet 3D de la QTE.")]
    [SerializeField] private float customQteRotationZ = 5f;
    [Tooltip("Position offset X/Y/Z pour ajuster le placement en direct du panel QTE.")]
    [SerializeField] private Vector3 customQtePositionOffset = Vector3.zero;

    [Header("Volume Audio")]
    [Range(0f, 1f)]
    [Tooltip("Volume de la musique de combat.")]
    [SerializeField] private float combatMusicVolume = 0.5f;

    // État du combat
    private CombatState currentState = CombatState.Transitioning;
    private GameObject activeEnemy;
    private RhythmPlayerController playerController;
    private EnemyCombatData activeCombatData;
    private GameObject activeVisualPrefab;
    private CinemachineBrain brain;

    // Machine à états de phase & variables additionnelles (Combat Séquencé)
    public enum CombatPhase { DodgePhase, PlayerTurn, QTEActive, DialogueActive }
    private CombatPhase currentPhase = CombatPhase.DodgePhase;

    private float originalCameraDistance;
    private float originalCameraHeight;
    private int dodgeBeatsCount = 0;

    // Références UI supplémentaires pour le combat séquencé
    private GameObject runtimeUIContainer;
    private GameObject rpgMenuPanel;
    private Button attackButton;
    private Button talkButton;
    private Button companionsButton;
    private Button fleeButton;

    private GameObject qtePanel;
    private RectTransform qteIndicator;
    private RectTransform qteTargetPerfect;
    private RectTransform qteTargetGood;
    private TextMeshProUGUI qteInstructionText;
    private TextMeshProUGUI qteFeedbackText;
    private float qteStartBeat = 0f;
    private bool qteResolved = false;
    private int qteStartFrame = -1;

    private GameObject dialoguePanel;
    private TextMeshProUGUI dialogueText;
    private int currentDialogueIndex = 0;
    private float dialogueEnterTime = 0f;

    private GameObject companionsSubPanel;
    private List<GameObject> companionButtons = new List<GameObject>();
    private string originalCompanionText = "COMPAGNONS";
    private GameObject groupContainerObj;

    // Gestion du Groupe et PV (Tag-Team)
    private List<Transform> allies = new List<Transform>();
    private List<int> allyHP = new List<int>();
    private List<int> allyMaxHP = new List<int>();
    private List<Sprite> allyOriginalSprites = new List<Sprite>();
    private List<RuntimeAnimatorController> allyOriginalAnimators = new List<RuntimeAnimatorController>();
    private int activeAllyIndex = 0;
    
    private int enemyHP;
    private int enemyMaxHP = 300;

    // Références Caméra
    private CinemachineCamera virtualCamera;
    private CinemachineHelper cameraHelper;

    // Références UI (Gérées dynamiquement)
    private Canvas combatCanvas;
    private CanvasGroup fadeCanvasGroup;
    private TextMeshProUGUI logText;
    private TextMeshProUGUI comboFeedbackText;
    private Image bossHPImage;
    private TextMeshProUGUI bossNameText;
    private List<Image> allyHPImages = new List<Image>();
    private List<TextMeshProUGUI> allyHPTexts = new List<TextMeshProUGUI>();
    private GameObject tagPromptPanel;
    private Sprite uiFillSprite;

    // Système d'Attaque Ennemie (Telegraphs)
    private Dictionary<string, int> activeTelegraphs = new Dictionary<string, int>(); // Clé: ring_sector, Valeur: beat à laquelle l'attaque frappe
    private HashSet<string> groundOnlyTelegraphs = new HashSet<string>(); // Clés des attaques au sol esquivables par le saut

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureEventSystem();
        CreateFadeCanvas();
    }

    private void Update()
    {
        if (currentState != CombatState.Active) return;

        // Suivi de caméra fluide derrière le joueur et orientation du boss
        UpdateCameraView(false);
        OrientBossTowardsPlayer();

        // Positionnement 3D en temps réel du menu choices
        if (rpgMenuPanel != null && rpgMenuPanel.activeSelf)
        {
            RectTransform menuRect = rpgMenuPanel.GetComponent<RectTransform>();
            if (menuRect != null && customMenuPanel != null)
            {
                menuRect.localRotation = Quaternion.Euler(customMenuRotationX, customMenuRotationY, customMenuRotationZ);
                menuRect.anchoredPosition3D = customMenuPositionOffset;
            }
            if (combatCanvas != null && combatCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                combatCanvas.planeDistance = customMenuPlaneDistance;
            }
        }

        // Positionnement 3D en temps réel du panel d'attaque (QTE)
        if (qtePanel != null && qtePanel.activeSelf)
        {
            RectTransform qteRect = qtePanel.GetComponent<RectTransform>();
            if (qteRect != null)
            {
                qteRect.localRotation = Quaternion.Euler(customQteRotationX, customQteRotationY, customQteRotationZ);
                qteRect.anchoredPosition3D = customQtePositionOffset;
            }
        }

        // Positionnement 3D en temps réel de la boîte de dialogue (locale ou globale)
        if (currentPhase == CombatPhase.DialogueActive)
        {
            RectTransform activeDiaRect = null;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                activeDiaRect = DialogueManager.Instance.DialoguePanelRect;
            }
            else if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                activeDiaRect = dialoguePanel.GetComponent<RectTransform>();
            }

            if (activeDiaRect != null)
            {
                if (customDialogueRotation != Vector3.zero)
                {
                    activeDiaRect.localRotation = Quaternion.Euler(customDialogueRotation);
                }
                if (customDialoguePositionOffset != Vector3.zero)
                {
                    activeDiaRect.anchoredPosition3D = customDialoguePositionOffset;
                }
            }
        }

        // Mise à jour du volume de la musique en direct depuis l'inspecteur
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.Volume = combatMusicVolume;
        }

        // Positionnement 3D en temps réel des barres de vie des joueurs
        if (groupContainerObj != null && groupContainerObj.activeSelf)
        {
            RectTransform hpRect = groupContainerObj.GetComponent<RectTransform>();
            if (hpRect != null)
            {
                if (customPlayerHPRotation != Vector3.zero)
                {
                    hpRect.localRotation = Quaternion.Euler(customPlayerHPRotation);
                }
                if (customPlayerHPPositionOffset != Vector3.zero)
                {
                    hpRect.anchoredPosition3D = customPlayerHPPositionOffset;
                }
            }
        }

        switch (currentPhase)
        {
            case CombatPhase.DodgePhase:
                // Dans la phase d'esquive, le joueur contrôle son personnage (géré par RhythmPlayerController)
                // et attend la fin du compte de battements.
                break;

            case CombatPhase.PlayerTurn:
                // Le joueur choisit une option dans le menu RPG.
                break;

            case CombatPhase.QTEActive:
                UpdateQTE();
                break;

            case CombatPhase.DialogueActive:
                UpdateDialogue();
                break;
        }
    }

    #region Lancement du Combat Rythmique

    public void StartCombat(GameObject enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("[RhythmCombatManager] Impossible de lancer le combat car l'ennemi est nul.");
            return;
        }

        activeEnemy = enemy;

        // Récupérer le conteneur de données de combat s'il existe
        EnemyCombatDataHolder holder = enemy.GetComponent<EnemyCombatDataHolder>();
        activeCombatData = holder != null ? holder.CombatData : defaultCombatData;

        StartCoroutine(StartCombatRoutine());
    }

    private IEnumerator StartCombatRoutine()
    {
        EnsureEventSystem();
        currentState = CombatState.Transitioning;
        currentPhase = CombatPhase.DodgePhase;
        dodgeBeatsCount = 0;
        originalCameraDistance = cameraDistance;
        originalCameraHeight = cameraHeight;
        Debug.Log("[RhythmCombatManager] Initialisation du combat rythmique radial...");

        // 1. Fondu au noir
        yield return StartCoroutine(Fade(1f));

        // 2. Geler le joueur et désactiver le suivi de groupe
        Transform leader = null;
        if (GroupManager.Instance != null)
        {
            leader = GroupManager.Instance.Leader;
            GroupManager.Instance.enabled = false;
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null) follower.gameObject.SetActive(false); // Cacher les compagnons
            }
        }
        else
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
        }

        if (leader != null)
        {
            PlayerMovement pm = leader.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }

        // Désactiver Cinemachine (Cerveau de la caméra principale + Caméra virtuelle)
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
        {
            brain.enabled = false; // Bloquer Cinemachine pour pouvoir modifier directement Camera.main
        }

        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.enabled = false;
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
            if (cameraHelper != null)
            {
                cameraHelper.SaveOriginalSettings();
                cameraHelper.enabled = false;
            }
        }

        // 3. Configurer le Groupe (PV et Sauvegarde des Visuels originaux)
        allies.Clear();
        allyHP.Clear();
        allyMaxHP.Clear();
        allyOriginalSprites.Clear();
        allyOriginalAnimators.Clear();

        if (leader != null) allies.Add(leader);
        if (GroupManager.Instance != null)
        {
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null) allies.Add(follower.transform);
            }
        }

        activeAllyIndex = 0;
        for (int i = 0; i < allies.Count; i++)
        {
            allyHP.Add(100);
            allyMaxHP.Add(100);

            // Récupérer et sauvegarder le sprite renderer / controller de chaque allié
            SpriteRenderer sr = allies[i].GetComponentInChildren<SpriteRenderer>();
            Animator anim = allies[i].GetComponentInChildren<Animator>();
            
            allyOriginalSprites.Add(sr != null ? sr.sprite : null);
            allyOriginalAnimators.Add(anim != null ? anim.runtimeAnimatorController : null);
        }

        // Configurer le boss à partir des données ou utiliser les valeurs par défaut
        string enemyName = "Boss";
        int maxHP = 300;
        AudioClip musicClip = combatMusicClip;
        float musicBPM = musicBpm;

        if (activeCombatData != null)
        {
            enemyName = activeCombatData.EnemyName;
            maxHP = activeCombatData.MaxHP;
            musicClip = activeCombatData.MusicTrack != null ? activeCombatData.MusicTrack : combatMusicClip;
            musicBPM = activeCombatData.Bpm;
        }

        enemyMaxHP = maxHP;
        enemyHP = enemyMaxHP;

        // Si un préfabriqué visuel est fourni par les données de l'ennemi, l'instancier
        if (activeCombatData != null && activeCombatData.VisualPrefab != null)
        {
            // Masquer les visuels d'exploration
            SpriteRenderer[] explorationSprites = activeEnemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in explorationSprites)
            {
                sprite.enabled = false;
            }

            // Instancier le visuel de combat au centre
            activeVisualPrefab = Instantiate(activeCombatData.VisualPrefab, activeEnemy.transform);
            activeVisualPrefab.transform.localPosition = Vector3.zero;
            activeVisualPrefab.transform.localRotation = Quaternion.identity;
        }

        // 4. Positionner la grille sous l'ennemi
        Vector3 combatCenter = activeEnemy.transform.position;
        // Aligner l'ennemi au sol au centre
        activeEnemy.transform.position = SnapToGround(combatCenter);

        if (radialGrid == null)
        {
            radialGrid = FindFirstObjectByType<RadialCombatGrid>();
        }

        if (radialGrid == null)
        {
            GameObject gridObj = new GameObject("RadialCombatGrid");
            radialGrid = gridObj.AddComponent<RadialCombatGrid>();
        }
        radialGrid.transform.position = activeEnemy.transform.position;
        radialGrid.SetGridActive(true);

        // 5. Calculer le secteur de départ le plus proche de la position actuelle du joueur face au boss
        Vector3 dirToPlayer = (leader.position - activeEnemy.transform.position).normalized;
        float angleRad = Mathf.Atan2(dirToPlayer.z, dirToPlayer.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        if (angleDeg < 0f) angleDeg += 360f;
        int startSector = Mathf.RoundToInt((angleDeg - 22.5f) / 45f) % 8;
        startSector = (startSector + 8) % 8;

        playerController = leader.gameObject.GetComponent<RhythmPlayerController>();
        if (playerController == null)
        {
            playerController = leader.gameObject.AddComponent<RhythmPlayerController>();
        }
        playerController.Initialize(radialGrid, 0, startSector);
        playerController.SetInputEnabled(true);

        // Jouer l'animation de combat (danse) sur l'Animator du joueur
        if (!string.IsNullOrEmpty(combatAnimationStateName))
        {
            Animator anim = playerController.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                Debug.Log($"[RhythmCombatManager] Lecture de l'état d'animation de combat '{combatAnimationStateName}' sur '{anim.gameObject.name}'.");
                anim.Play(combatAnimationStateName);
            }
        }

        // 6. Orienter la caméra derrière le joueur vers le boss (instantanément au début)
        UpdateCameraView(true);

        // 7. Initialiser l'UI et la musique
        CreateCombatUI();
        UpdateUI();

        if (BeatManager.Instance == null)
        {
            GameObject bmObj = new GameObject("BeatManager");
            bmObj.AddComponent<BeatManager>();
        }
        
        BeatManager.Instance.SetTrack(musicClip, musicBPM);
        BeatManager.Instance.Volume = combatMusicVolume;
        BeatManager.Instance.StartMusic();

        // S'abonner aux battements
        BeatManager.Instance.OnBeat += ProcessEnemyAttackBeat;

        // Attendre la stabilisation
        yield return new WaitForSeconds(0.2f);

        // 8. Fondu de retour (Fade In)
        yield return StartCoroutine(Fade(0f));

        currentState = CombatState.Active;
        logText.text = "ESQUIVEZ EN RYTHME ! Évitez les attaques de l'ennemi !";
    }

    #endregion

    #region Boucle de Combat & Attaques de l'Ennemi (Dodge Phase)

    private void ProcessEnemyAttackBeat(int beatIndex)
    {
        if (currentState != CombatState.Active) return;

        if (currentPhase != CombatPhase.DodgePhase) return;

        // A. Évaluer et appliquer les dégâts des alertes qui devaient frapper à ce beat
        ApplyTelegraphDamage(beatIndex);

        // B. Gérer la durée de la phase d'esquive
        dodgeBeatsCount++;
        int duration = activeCombatData != null ? activeCombatData.DodgePhaseDuration : 16;
        if (dodgeBeatsCount >= duration)
        {
            TransitionToPlayerTurn();
            return;
        }

        // C. Générer de nouveaux patterns d'attaque en déléguant au ScriptableObject du boss
        if (activeCombatData != null && activeCombatData.BeatPattern != null)
        {
            activeCombatData.BeatPattern.ProcessBeat(beatIndex, this, radialGrid, playerController);
        }
        else
        {
            GenerateAttackPattern(beatIndex); // Fallback standard
        }
    }

    private void ApplyTelegraphDamage(int currentBeat)
    {
        List<string> resolvedKeys = new List<string>();

        foreach (var pair in activeTelegraphs)
        {
            if (pair.Value <= currentBeat)
            {
                resolvedKeys.Add(pair.Key);
                
                // Extraire ring et sector
                string[] parts = pair.Key.Split('_');
                int ring = int.Parse(parts[0]);
                int sector = int.Parse(parts[1]);

                // Effacer l'alerte visuelle sur la grille
                radialGrid.SetCellWarning(ring, sector, false);

                bool isGroundOnly = groundOnlyTelegraphs.Contains(pair.Key);

                // Vérifier si le joueur se trouve sur cette case au moment de l'impact
                if (playerController.CurrentRing == ring && playerController.CurrentSector == sector)
                {
                    if (!playerController.IsInvincible)
                    {
                        // Si c'est une attaque au sol, et que le joueur est en train de SAUTER, on ignore les dégâts
                        if (isGroundOnly && playerController.IsJumping)
                        {
                            Debug.Log("[RhythmCombatManager] Le joueur esquive l'attaque au sol grâce au Saut ! Dégâts évités.");
                        }
                        else
                        {
                            TakeDamage(25); // Dégâts standard
                        }
                    }
                }
            }
        }

        // Nettoyer les alertes résolues
        foreach (var key in resolvedKeys)
        {
            activeTelegraphs.Remove(key);
            groundOnlyTelegraphs.Remove(key);
        }
    }

    private void GenerateAttackPattern(int currentBeat)
    {
        // On prévient 1 beat à l'avance (Telegraph à beat N, dégâts à beat N+1)
        int targetImpactBeat = currentBeat + 1;

        // Génération de patterns simples alternés
        int patternIndex = currentBeat % 8;

        switch (patternIndex)
        {
            case 0:
                // 1. Attaque sur tout le cercle intérieur (Ring 0)
                TelegraphEntireRing(0, targetImpactBeat);
                break;
            case 2:
                // 2. Attaque sur tout le cercle extérieur (Ring 1)
                TelegraphEntireRing(1, targetImpactBeat);
                break;
            case 4:
                // 3. Attaque sur les secteurs pairs (0, 2, 4, 6)
                for (int s = 0; s < radialGrid.SectorsCount; s += 2)
                {
                    TelegraphCell(0, s, targetImpactBeat);
                    TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 6:
                // 4. Attaque sur les secteurs impairs (1, 3, 5, 7)
                for (int s = 1; s < radialGrid.SectorsCount; s += 2)
                {
                    TelegraphCell(0, s, targetImpactBeat);
                    TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 7:
                // 5. Attaque ciblée sur le secteur actuel du joueur
                TelegraphCell(0, playerController.CurrentSector, targetImpactBeat);
                TelegraphCell(1, playerController.CurrentSector, targetImpactBeat);
                break;
        }
    }

    public void TelegraphCell(int ring, int sector, int impactBeat, bool isGroundOnly = false)
    {
        string key = $"{ring}_{sector}";
        if (!activeTelegraphs.ContainsKey(key))
        {
            activeTelegraphs.Add(key, impactBeat);
            
            if (isGroundOnly)
            {
                groundOnlyTelegraphs.Add(key);
                // Couleur d'alerte Orange pour les attaques au sol esquivables en sautant
                Color warningOrange = new Color(1.0f, 0.5f, 0.0f, 0.6f);
                radialGrid.SetCellWarning(ring, sector, true, warningOrange);
            }
            else
            {
                radialGrid.SetCellWarning(ring, sector, true);
            }

            // Déclencher le projectile visuel spécifique à cet ennemi
            GameObject projectilePrefab = activeCombatData != null ? activeCombatData.WarningProjectilePrefab : null;
            GameObject impactPrefab = activeCombatData != null ? activeCombatData.ImpactVisualPrefab : null;

            if (projectilePrefab != null && BeatManager.Instance != null)
            {
                float currentBeatDec = BeatManager.Instance.GetCurrentBeatDecimal();
                float beatsRemaining = impactBeat - currentBeatDec;
                // Calculer le temps restant en secondes d'après le BPM de la musique
                float timeToImpact = Mathf.Max(0.05f, beatsRemaining * (60f / musicBpm));

                Vector3 targetPos = radialGrid.GetCellPosition(ring, sector);
                targetPos.y = radialGrid.transform.position.y + 0.02f; // S'aligner sur la hauteur de la grille

                Vector3 startPos = targetPos + Vector3.up * 8f; // Le projectile commence 8 mètres au-dessus de sa cible

                GameObject dropObj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
                dropObj.SetActive(true); // S'assurer que le projectile est actif (au cas où le prefab d'origine était désactivé)
                
                FallingProjectile proj = dropObj.GetComponent<FallingProjectile>();
                if (proj == null) proj = dropObj.AddComponent<FallingProjectile>();
                
                proj.Initialize(startPos, targetPos, timeToImpact, impactPrefab);
            }
            else if (projectilePrefab == null)
            {
                Debug.LogWarning($"[RhythmCombatManager] Le prefab de projectile est null pour l'ennemi {(activeCombatData != null ? activeCombatData.EnemyName : "Inconnu")}. Glissez votre prefab d'encre dans l'EnemyCombatData.");
            }
        }
    }

    public void TelegraphEntireRing(int ringIndex, int impactBeat)
    {
        for (int s = 0; s < radialGrid.SectorsCount; s++)
        {
            TelegraphCell(ringIndex, s, impactBeat);
        }
    }

    #endregion

    #region Action du Joueur : Attaque sur le Beat

    private void EvaluatePlayerAttack()
    {
        float currentBeatDec = BeatManager.Instance.GetCurrentBeatDecimal();
        float closestBeat = Mathf.Round(currentBeatDec);
        float diff = Mathf.Abs(currentBeatDec - closestBeat);

        // Seuil de précision rythmique
        float perfectThreshold = 0.12f;
        float goodThreshold = 0.25f;

        if (diff <= perfectThreshold)
        {
            TriggerAttackCombo("PERFECT !", 25, Color.green);
        }
        else if (diff <= goodThreshold)
        {
            TriggerAttackCombo("BIEN !", 12, Color.yellow);
        }
        else
        {
            TriggerAttackCombo("RATE !", 0, Color.red);
        }
    }

    private void TriggerAttackCombo(string rating, int damage, Color color)
    {
        // Pop-up text
        comboFeedbackText.text = rating;
        comboFeedbackText.color = color;
        StartCoroutine(AnimateComboText());

        if (damage > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - damage);
            UpdateUI();

            // Particules de succès
            if (attackSuccessParticles != null && playerController != null)
            {
                ParticleSystem ps = Instantiate(attackSuccessParticles, activeEnemy.transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 1.0f);
            }

            logText.text = $"Vous touchez le boss en rythme ! Dégâts : {damage}";

            if (enemyHP <= 0)
            {
                StartCoroutine(VictoryRoutine());
            }
        }
        else
        {
            logText.text = "Trop tard ou trop tôt ! Suivez la pulsation.";
        }
    }

    private IEnumerator AnimateComboText()
    {
        comboFeedbackText.transform.localScale = Vector3.one * 1.5f;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            comboFeedbackText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / 0.2f);
            yield return null;
        }

        // Attendre un peu puis vider le texte pour le faire disparaître
        yield return new WaitForSeconds(0.8f);
        comboFeedbackText.text = "";
    }

    #endregion

    #region Système de Tag-Team (Changement d'alliés)

    private void TagNextCharacter()
    {
        if (allies.Count <= 1) return;

        // Trouver le prochain allié vivant
        int nextIndex = (activeAllyIndex + 1) % allies.Count;
        while (nextIndex != activeAllyIndex && allyHP[nextIndex] <= 0)
        {
            nextIndex = (nextIndex + 1) % allies.Count;
        }

        if (nextIndex == activeAllyIndex) return; // Aucun autre personnage vivant

        SwapActiveCharacter(nextIndex);
    }

    private void SwapActiveCharacter(int newIndex)
    {
        activeAllyIndex = newIndex;

        // Mettre à jour l'apparence visuelle du leader pour refléter le personnage sélectionné
        SpriteRenderer playerSR = playerController.GetComponentInChildren<SpriteRenderer>();
        Animator playerAnim = playerController.GetComponentInChildren<Animator>();

        if (playerSR != null && newIndex < allyOriginalSprites.Count)
        {
            playerSR.sprite = allyOriginalSprites[newIndex];
        }

        if (playerAnim != null && newIndex < allyOriginalAnimators.Count)
        {
            // Mettre à jour l'animator original du personnage taggé
            playerAnim.runtimeAnimatorController = allyOriginalAnimators[newIndex];
            playerAnim.Rebind();
            playerAnim.Update(0f);

            // Relancer la danse sur le nouveau personnage s'il possède cet état
            if (!string.IsNullOrEmpty(combatAnimationStateName))
            {
                playerAnim.Play(combatAnimationStateName);
            }
        }

        // Effet de tag
        playerController.TriggerInvincibility();
        if (attackSuccessParticles != null)
        {
            ParticleSystem ps = Instantiate(attackSuccessParticles, playerController.transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }

        logText.text = $"{allies[activeAllyIndex].name} entre dans le combat !";
        UpdateUI();
    }

    #endregion

    #region Gestion des PV et Dégâts

    private void TakeDamage(int dmg)
    {
        if (currentState != CombatState.Active) return;

        // Appliquer les dégâts à l'allié actif
        allyHP[activeAllyIndex] = Mathf.Max(0, allyHP[activeAllyIndex] - dmg);
        playerController.TriggerInvincibility();

        // Particules d'impact
        if (hitParticles != null)
        {
            ParticleSystem ps = Instantiate(hitParticles, playerController.transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }

        // Secouer la caméra (Screenshake simple)
        StartCoroutine(ScreenShake(0.15f, 0.2f));

        UpdateUI();

        if (allyHP[activeAllyIndex] <= 0)
        {
            logText.text = $"{allies[activeAllyIndex].name} est K.O. !";
            
            // Vérifier s'il reste des survivants
            bool anyAlive = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allyHP[i] > 0)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (anyAlive)
            {
                // Tag-team automatique sur le prochain allié vivant
                TagNextCharacter();
            }
            else
            {
                StartCoroutine(DefeatRoutine());
            }
        }
    }

    #endregion

    #region Victoire et Défaite

    private IEnumerator VictoryRoutine()
    {
        currentState = CombatState.Victory;
        logText.text = "Victoire éclatante !";
        BeatManager.Instance.StopMusic();

        yield return new WaitForSeconds(2.0f);
        yield return StartCoroutine(EndCombatRoutine(true));
    }

    private IEnumerator DefeatRoutine()
    {
        currentState = CombatState.Defeat;
        logText.text = "Tout le groupe a succombé...";
        BeatManager.Instance.StopMusic();

        yield return new WaitForSeconds(2.5f);
        yield return StartCoroutine(EndCombatRoutine(false));
    }

    private IEnumerator EndCombatRoutine(bool victory)
    {
        currentState = CombatState.Transitioning;
        yield return StartCoroutine(Fade(1f));

        // 1. Désactiver la grille
        radialGrid.SetGridActive(false);

        // 2. Nettoyer l'UI et les visuels de combat
        if (runtimeUIContainer != null)
        {
            Destroy(runtimeUIContainer);
            runtimeUIContainer = null;
        }

        if (combatCanvas != null)
        {
            if (customCombatCanvas != null)
            {
                // Si c'est un canvas de scène personnalisé, on désactive juste le panel au lieu de détruire le canvas
                if (customMenuPanel != null)
                {
                    customMenuPanel.gameObject.SetActive(false);
                }
            }
            else
            {
                Destroy(combatCanvas.gameObject);
            }
        }
        allyHPImages.Clear();
        allyHPTexts.Clear();
        companionButtons.Clear();
        currentPhase = CombatPhase.DodgePhase;
        dodgeBeatsCount = 0;

        if (activeVisualPrefab != null)
        {
            Destroy(activeVisualPrefab);
        }

        // Restaurer les visuels d'exploration de l'ennemi s'il n'a pas été détruit (ex: défaite)
        if (activeEnemy != null)
        {
            SpriteRenderer[] explorationSprites = activeEnemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in explorationSprites)
            {
                sprite.enabled = true;
            }
        }

        // 3. Restaurer le leader d'origine (supprimer le contrôleur de rythme)
        if (playerController != null)
        {
            // Restaurer le sprite d'origine du leader
            SpriteRenderer sr = playerController.GetComponentInChildren<SpriteRenderer>();
            Animator anim = playerController.GetComponentInChildren<Animator>();
            if (sr != null && allyOriginalSprites.Count > 0) sr.sprite = allyOriginalSprites[0];
            if (anim != null && allyOriginalAnimators.Count > 0)
            {
                anim.runtimeAnimatorController = allyOriginalAnimators[0];
                anim.Rebind();
                anim.Update(0f);
            }

            Destroy(playerController);
        }

        // 4. Si victoire, détruire l'ennemi
        if (victory && activeEnemy != null)
        {
            Destroy(activeEnemy);
        }

        // 5. Réactiver Cinemachine et la caméra
        if (brain != null)
        {
            brain.enabled = true;
        }
        if (virtualCamera != null)
        {
            virtualCamera.enabled = true;
            if (cameraHelper != null)
            {
                cameraHelper.enabled = true;
                cameraHelper.UpdateCameraSettings(false);
            }
            if (GroupManager.Instance != null && GroupManager.Instance.Leader != null)
            {
                virtualCamera.Follow = GroupManager.Instance.Leader;
            }
        }

        // 6. Réactiver les compagnons et le mouvement normal
        if (GroupManager.Instance != null)
        {
            GroupManager.Instance.enabled = true;
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null)
                {
                    follower.gameObject.SetActive(true); // Rendre visible
                    follower.enabled = true;
                }
            }
            GroupManager.Instance.TeleportPartyToLeader();
            GroupManager.Instance.ReapplyAllCollisions();
        }

        Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
        if (leader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
        }

        if (leader != null)
        {
            PlayerMovement pm = leader.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(Fade(0f));

        currentState = CombatState.Transitioning;
        activeEnemy = null;
        Debug.Log("[RhythmCombatManager] Combat terminé !");
    }

    #endregion

    #region Helpers Caméra & Sol

    private void UpdateCameraView(bool instant)
    {
        if (Camera.main == null || playerController == null || radialGrid == null) return;
        
        // Utiliser la TargetPosition (au sol) au lieu de la position physique transform (qui inclut le saut Y)
        Vector3 playerPos = playerController.TargetPosition;
        Vector3 center = radialGrid.transform.position;

        Vector3 dirToCenter = (center - playerPos).normalized;
        dirToCenter.y = 0f;
        dirToCenter.Normalize();

        Vector3 targetCamPos;
        Quaternion targetCamRot;

        if (currentPhase == CombatPhase.PlayerTurn)
        {
            // 1. Zoom Tour du Joueur : Plan héroïque en contre-plongée et décalé sur l'épaule gauche
            Vector3 leftShoulderDir = Vector3.Cross(Vector3.up, dirToCenter).normalized;
            targetCamPos = playerPos - dirToCenter * cameraDistance - leftShoulderDir * playerTurnCameraLeftOffset + Vector3.up * cameraHeight;
            targetCamRot = Quaternion.LookRotation((center + Vector3.up * 1.5f) - targetCamPos);
            // Angle néerlandais (Z-tilt) stylisé et paramétré
            targetCamRot = targetCamRot * Quaternion.Euler(0f, 0f, playerTurnCameraTiltZ);
        }
        else if (currentPhase == CombatPhase.QTEActive)
        {
            // 2. QTE Actif (Attaque) : Vue de profil cinématique (midpoint face-à-face)
            Vector3 profileDir = Vector3.Cross(Vector3.up, dirToCenter).normalized;
            Vector3 midPoint = (playerPos + center) * 0.5f;
            targetCamPos = midPoint + profileDir * 6f + Vector3.up * 1.8f;
            targetCamRot = Quaternion.LookRotation(midPoint - targetCamPos);
            // Z-tilt de 2 degrés
            targetCamRot = targetCamRot * Quaternion.Euler(0f, 0f, 2f);
        }
        else if (currentPhase == CombatPhase.DialogueActive)
        {
            // 3. Dialogue Actif (Parler) : Gros plan dramatique face au boss
            Vector3 leftShoulderDir = Vector3.Cross(Vector3.up, dirToCenter).normalized;
            targetCamPos = center + dirToCenter * talkPhaseCameraDistance - leftShoulderDir * talkPhaseCameraLeftOffset + Vector3.up * talkPhaseCameraHeight;
            targetCamRot = Quaternion.LookRotation((center + Vector3.up * 0.8f) - targetCamPos);
            // Z-tilt stylisé et paramétré
            targetCamRot = targetCamRot * Quaternion.Euler(0f, 0f, talkPhaseCameraTiltZ);
        }
        else
        {
            // 4. Phase d'Esquive neutre : Caméra classique centrée derrière le joueur
            targetCamPos = playerPos - dirToCenter * cameraDistance + Vector3.up * cameraHeight;
            targetCamRot = Quaternion.LookRotation((center + Vector3.up * 1f) - targetCamPos);
        }

        if (instant)
        {
            Camera.main.transform.position = targetCamPos;
            Camera.main.transform.rotation = targetCamRot;
        }
        else
        {
            // Interpolation fluide pour des balayages dynamiques
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCamPos, Time.deltaTime * 6f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, targetCamRot, Time.deltaTime * 6f);
        }
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        // Récupérer et désactiver temporairement les colliders de l'ennemi et du joueur pour éviter l'auto-collision
        Collider[] enemyColliders = activeEnemy != null ? activeEnemy.GetComponentsInChildren<Collider>() : new Collider[0];
        bool[] enemyColStates = new bool[enemyColliders.Length];
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColStates[i] = enemyColliders[i].enabled;
            enemyColliders[i].enabled = false;
        }

        Collider[] playerColliders = playerController != null ? playerController.GetComponentsInChildren<Collider>() : new Collider[0];
        if (playerColliders.Length == 0)
        {
            Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
            if (leader == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) leader = pm.transform;
            }
            if (leader != null)
            {
                playerColliders = leader.GetComponentsInChildren<Collider>();
            }
        }
        bool[] playerColStates = new bool[playerColliders.Length];
        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColStates[i] = playerColliders[i].enabled;
            playerColliders[i].enabled = false;
        }

        RaycastHit hit;
        Vector3 origin = new Vector3(position.x, position.y + 10f, position.z);
        Vector3 finalPos = position;
        
        if (Physics.Raycast(origin, Vector3.down, out hit, 25f))
        {
            finalPos = hit.point;
        }

        // Restaurer les colliders
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColliders[i].enabled = enemyColStates[i];
        }
        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColliders[i].enabled = playerColStates[i];
        }

        return finalPos;
    }

    private void OrientBossTowardsPlayer()
    {
        if (activeEnemy != null && playerController != null)
        {
            SpriteRenderer sr = activeEnemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // Dans la perspective 2.5D, si le joueur est à gauche (axe X) du boss, le boss regarde à gauche (flipX = true)
                sr.flipX = playerController.transform.position.x < activeEnemy.transform.position.x;
            }
        }
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }

    #endregion

    #region Génération de l'UI Dynamique

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem (Spawned)");
            es.AddComponent<EventSystem>();

            #if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
            es.AddComponent<StandaloneInputModule>();
            #endif

            DontDestroyOnLoad(es);
            Debug.Log("EventSystem recréé automatiquement par RhythmCombatManager.");
        }
    }

    private void CreateFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        GameObject canvasObj = new GameObject("RhythmFade_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image img = imageObj.AddComponent<Image>();
        img.color = Color.black;

        RectTransform r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;

        DontDestroyOnLoad(canvasObj);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        CreateFadeCanvas();
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void CreateCombatUI()
    {
        // Créer le Sprite blanc 1x1 s'il n'existe pas encore pour que le remplissage des jauges fonctionne
        if (uiFillSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            uiFillSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        GameObject canvasObj;
        Transform parentTransform;

        if (customCombatCanvas != null)
        {
            canvasObj = customCombatCanvas.gameObject;
            combatCanvas = customCombatCanvas;
            if (Camera.main != null)
            {
                combatCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                combatCanvas.worldCamera = Camera.main;
                combatCanvas.planeDistance = customMenuPlaneDistance;
            }

            // S'assurer de la présence d'un GraphicRaycaster pour la détection des clics
            if (combatCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                combatCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Créer le conteneur runtime temporaire sous forme de RectTransform étiré
            runtimeUIContainer = new GameObject("RhythmCombatRuntimeUI_Container");
            RectTransform containerRect = runtimeUIContainer.AddComponent<RectTransform>();
            runtimeUIContainer.transform.SetParent(combatCanvas.transform, false);
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;
            parentTransform = containerRect;
        }
        else
        {
            canvasObj = new GameObject("RhythmCombatUI_Canvas");
            combatCanvas = canvasObj.AddComponent<Canvas>();
            combatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            combatCanvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            parentTransform = canvasObj.transform;
        }

        // 1. Conteneur Principal (Complètement transparent pour laisser place aux graphismes)
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(parentTransform, false);
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);
        panelImage.raycastTarget = false; // Permettre aux clics de traverser ce panneau invisible

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // 2. Texte de Journal/Log (Flottant au centre inférieur)
        GameObject logObj = new GameObject("LogText");
        logObj.transform.SetParent(mainPanel.transform, false);
        logText = logObj.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 24f;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.Center;
        logText.enableWordWrapping = true;

        RectTransform logRect = logText.rectTransform;
        logRect.anchorMin = new Vector2(0.25f, 0.05f);
        logRect.anchorMax = new Vector2(0.75f, 0.12f);
        logRect.sizeDelta = Vector2.zero;

        // 3. Pop-up de retour de combo (Centre de l'écran)
        GameObject comboObj = new GameObject("ComboFeedbackText");
        comboObj.transform.SetParent(parentTransform, false);
        comboFeedbackText = comboObj.AddComponent<TextMeshProUGUI>();
        comboFeedbackText.fontSize = 55f;
        comboFeedbackText.fontStyle = FontStyles.Bold;
        comboFeedbackText.alignment = TextAlignmentOptions.Center;
        comboFeedbackText.text = "";

        RectTransform comboRect = comboFeedbackText.rectTransform;
        comboRect.anchorMin = new Vector2(0.4f, 0.45f);
        comboRect.anchorMax = new Vector2(0.6f, 0.65f);
        comboRect.sizeDelta = Vector2.zero;

        // 4. Panel d'invitation à Tag (Changement - discret en bas à gauche)
        tagPromptPanel = new GameObject("TagPromptPanel");
        tagPromptPanel.transform.SetParent(mainPanel.transform, false);
        TextMeshProUGUI tagPrompt = tagPromptPanel.AddComponent<TextMeshProUGUI>();
        tagPrompt.text = "[TAB] Changer de héros";
        tagPrompt.fontSize = 15f;
        tagPrompt.color = new Color(0.2f, 0.7f, 1.0f, 0.6f);
        tagPrompt.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform tagPromptRect = tagPrompt.rectTransform;
        tagPromptRect.anchorMin = new Vector2(0.04f, 0.01f);
        tagPromptRect.anchorMax = new Vector2(0.28f, 0.04f);
        tagPromptRect.sizeDelta = Vector2.zero;

        // 6. Barres de PV des Alliés (Empilées verticalement en bas à gauche)
        PopulateAlliesUI(mainPanel.transform);

        // 7. Initialisation des UIs de Combat Séquencé
        CreateSequencedCombatUI(parentTransform);
    }

    private void PopulateAlliesUI(Transform parent)
    {
        allyHPImages.Clear();
        allyHPTexts.Clear();

        // Conteneur vertical en bas à gauche pour les héros (positionnable)
        groupContainerObj = new GameObject("AlliesGroupContainer");
        groupContainerObj.transform.SetParent(parent, false);
        RectTransform groupRect = groupContainerObj.AddComponent<RectTransform>();
        
        // Valeur d'ancrage par défaut en bas à gauche (compact)
        groupRect.anchorMin = new Vector2(0.01f, 0.01f);
        groupRect.anchorMax = new Vector2(0.18f, 0.054f);
        groupRect.sizeDelta = Vector2.zero;
        
        // Inclinaison oblique par défaut (style papier posé de travers)
        groupRect.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
        
        // Masqué par défaut — visible uniquement lors du tour du joueur
        groupContainerObj.SetActive(false);

        float cardHeightPct = 1f / allies.Count;
        float spacing = 0.04f; // Espacement serré

        for (int i = 0; i < allies.Count; i++)
        {
            float minY = 1f - (i + 1) * cardHeightPct + spacing / 2f;
            float maxY = 1f - i * cardHeightPct - spacing / 2f;

            // 1. Plaque d'ombre (papier découpé gris)
            GameObject shadowObj = new GameObject($"AllyShadow_{i}");
            shadowObj.transform.SetParent(groupContainerObj.transform, false);
            RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0f, minY);
            shadowRect.anchorMax = new Vector2(1f, maxY);
            shadowRect.sizeDelta = Vector2.zero;
            shadowRect.anchoredPosition = new Vector2(6f, -6f);
            Image shadowImg = shadowObj.AddComponent<Image>();
            shadowImg.sprite = uiFillSprite;
            shadowImg.color = new Color(0.9f, 0.9f, 0.92f, 0.6f);

            // 2. Bordure de carte (Charbon foncé)
            GameObject borderObj = new GameObject($"AllyBorder_{i}");
            borderObj.transform.SetParent(groupContainerObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, minY);
            borderRect.anchorMax = new Vector2(1f, maxY);
            borderRect.sizeDelta = Vector2.zero;
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = uiFillSprite;
            borderImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            // 3. Fond interne de carte (Blanc papier)
            GameObject allyPanel = new GameObject($"AllyPanel_{i}");
            allyPanel.transform.SetParent(borderObj.transform, false);
            RectTransform allyRect = allyPanel.AddComponent<RectTransform>();
            allyRect.anchorMin = Vector2.zero;
            allyRect.anchorMax = Vector2.one;
            allyRect.offsetMin = new Vector2(2, 3);
            allyRect.offsetMax = new Vector2(-2, -1);
            Image allyBg = allyPanel.AddComponent<Image>();
            allyBg.sprite = uiFillSprite;
            allyBg.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            // 4. Remplissage PV (Crayon de couleur rouge ou gris)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(allyPanel.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = uiFillSprite;
            fillImg.color = i == activeAllyIndex ? new Color(0.85f, 0.08f, 0.14f, 0.9f) : new Color(0.5f, 0.5f, 0.52f, 0.4f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;

            RectTransform fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            allyHPImages.Add(fillImg);

            // 5. Texte PV crayonné (Nom + PV)
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(allyPanel.transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            if (customCombatFont != null) txt.font = customCombatFont;
            txt.text = $"  {allies[i].name.ToUpper()}  |  {allyHP[i]}/{allyMaxHP[i]} PV";
            txt.fontSize = 11f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.black; // Texte noir sur papier blanc
            txt.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform txtRect = txt.rectTransform;
            txtRect.anchorMin = new Vector2(0.04f, 0f);
            txtRect.anchorMax = new Vector2(0.96f, 1f);
            txtRect.sizeDelta = Vector2.zero;
            allyHPTexts.Add(txt);
        }
    }

    private void UpdateUI()
    {
        if (bossHPImage != null)
        {
            bossHPImage.fillAmount = (float)enemyHP / enemyMaxHP;
        }

        for (int i = 0; i < allies.Count; i++)
        {
            if (i < allyHPImages.Count && allyHPImages[i] != null)
            {
                allyHPImages[i].fillAmount = (float)allyHP[i] / allyMaxHP[i];
                // Mettre en surbrillance l'allié actif (Rouge marqueur vs Gris estompé)
                if (i == activeAllyIndex)
                {
                    allyHPImages[i].color = new Color(0.85f, 0.08f, 0.14f, 0.9f);
                }
                else
                {
                    allyHPImages[i].color = new Color(0.5f, 0.5f, 0.52f, 0.4f);
                }
            }

            if (i < allyHPTexts.Count && allyHPTexts[i] != null)
            {
                allyHPTexts[i].text = $"  {allies[i].name.ToUpper()}  |  {allyHP[i]}/{allyMaxHP[i]} PV";
            }
        }

        // Cacher l'invite de tag s'il ne reste qu'un seul personnage vivant
        if (tagPromptPanel != null)
        {
            int aliveCount = 0;
            foreach (var hp in allyHP)
            {
                if (hp > 0) aliveCount++;
            }
            tagPromptPanel.SetActive(aliveCount > 1);
        }
    }

    #endregion

    #region Combat Séquencé (Style Undertale / Juge d'Âmes)

    private void CreateSequencedCombatUI(Transform parent)
    {
        // --- MENU RPG ---
        if (customMenuPanel != null)
        {
            // Auto-détection de sécurité des boutons s'ils ne sont pas assignés dans l'inspecteur
            if (customFightButton == null)
            {
                foreach (Button b in customMenuPanel.GetComponentsInChildren<Button>(true))
                {
                    string n = b.name.ToLower();
                    if (n.Contains("fight") || n.Contains("attack") || n.Contains("attaquer") || n.Contains("combat"))
                    {
                        customFightButton = b;
                        break;
                    }
                }
            }
            if (customTalkButton == null)
            {
                foreach (Button b in customMenuPanel.GetComponentsInChildren<Button>(true))
                {
                    string n = b.name.ToLower();
                    if (n.Contains("talk") || n.Contains("parler") || n.Contains("dialogue"))
                    {
                        customTalkButton = b;
                        break;
                    }
                }
            }
            if (customCompanionButton == null)
            {
                foreach (Button b in customMenuPanel.GetComponentsInChildren<Button>(true))
                {
                    string n = b.name.ToLower();
                    if (n.Contains("companion") || n.Contains("compagnon") || n.Contains("allie"))
                    {
                        customCompanionButton = b;
                        break;
                    }
                }
            }
            if (customEscapeButton == null)
            {
                foreach (Button b in customMenuPanel.GetComponentsInChildren<Button>(true))
                {
                    string n = b.name.ToLower();
                    if (n.Contains("escape") || n.Contains("flee") || n.Contains("fuir") || n.Contains("esquive"))
                    {
                        customEscapeButton = b;
                        break;
                    }
                }
            }

            Debug.Log($"[RhythmCombatManager] Boutons personnalisés détectés - Fight: {customFightButton?.name}, Talk: {customTalkButton?.name}, Companion: {customCompanionButton?.name}, Escape: {customEscapeButton?.name}");

            rpgMenuPanel = customMenuPanel.gameObject;
            // Configurer la rotation 3D sur le panel personnalisé
            customMenuPanel.localRotation = Quaternion.Euler(customMenuRotationX, customMenuRotationY, customMenuRotationZ);

            // Attacher les callbacks et l'animateur aux boutons personnalisés
            if (customFightButton != null)
            {
                customFightButton.onClick.RemoveAllListeners();
                customFightButton.onClick.AddListener(StartQTE);
                attackButton = customFightButton;

                RhythmUIButtonAnimator anim = customFightButton.GetComponent<RhythmUIButtonAnimator>();
                if (anim == null) anim = customFightButton.gameObject.AddComponent<RhythmUIButtonAnimator>();
                anim.Setup(customFightButton.GetComponent<Image>(), null, customFightButton.GetComponentInChildren<TextMeshProUGUI>(), customFightButton.transform.localRotation.eulerAngles.z);
            }
            if (customTalkButton != null)
            {
                customTalkButton.onClick.RemoveAllListeners();
                customTalkButton.onClick.AddListener(StartDialogue);
                talkButton = customTalkButton;

                RhythmUIButtonAnimator anim = customTalkButton.GetComponent<RhythmUIButtonAnimator>();
                if (anim == null) anim = customTalkButton.gameObject.AddComponent<RhythmUIButtonAnimator>();
                anim.Setup(customTalkButton.GetComponent<Image>(), null, customTalkButton.GetComponentInChildren<TextMeshProUGUI>(), customTalkButton.transform.localRotation.eulerAngles.z);
            }
            if (customCompanionButton != null)
            {
                customCompanionButton.onClick.RemoveAllListeners();
                customCompanionButton.onClick.AddListener(ShowCompanionsSubMenu);
                companionsButton = customCompanionButton;

                TextMeshProUGUI txt = companionsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    originalCompanionText = txt.text;
                }

                RhythmUIButtonAnimator anim = customCompanionButton.GetComponent<RhythmUIButtonAnimator>();
                if (anim == null) anim = customCompanionButton.gameObject.AddComponent<RhythmUIButtonAnimator>();
                anim.Setup(customCompanionButton.GetComponent<Image>(), null, customCompanionButton.GetComponentInChildren<TextMeshProUGUI>(), customCompanionButton.transform.localRotation.eulerAngles.z);
            }
            if (customEscapeButton != null)
            {
                customEscapeButton.onClick.RemoveAllListeners();
                customEscapeButton.onClick.AddListener(FleeCombat);
                fleeButton = customEscapeButton;

                RhythmUIButtonAnimator anim = customEscapeButton.GetComponent<RhythmUIButtonAnimator>();
                if (anim == null) anim = customEscapeButton.gameObject.AddComponent<RhythmUIButtonAnimator>();
                anim.Setup(customEscapeButton.GetComponent<Image>(), null, customEscapeButton.GetComponentInChildren<TextMeshProUGUI>(), customEscapeButton.transform.localRotation.eulerAngles.z);
            }
            
            customMenuPanel.gameObject.SetActive(false); // Masqué au début
        }
        else
        {
            GameObject rpgMenuBorder = new GameObject("RPGMenuBorder");
            rpgMenuBorder.transform.SetParent(parent, false);
            RectTransform borderRect = rpgMenuBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.32f, 0.12f);
            borderRect.anchorMax = new Vector2(0.68f, 0.28f);
            borderRect.sizeDelta = Vector2.zero;
            // Angle oblique asymétrique
            borderRect.localRotation = Quaternion.Euler(0f, 0f, -5f);

            Image borderImg = rpgMenuBorder.AddComponent<Image>();
            borderImg.sprite = uiFillSprite;
            // Fond blanc crayonné asymétrique pour le contour
            borderImg.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

            rpgMenuPanel = new GameObject("RPGMenuPanel");
            rpgMenuPanel.transform.SetParent(rpgMenuBorder.transform, false);
            RectTransform menuRect = rpgMenuPanel.AddComponent<RectTransform>();
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            // Légère asymétrie de bordure
            menuRect.offsetMin = new Vector2(4, 3);
            menuRect.offsetMax = new Vector2(-4, -5);
            Image menuImg = rpgMenuPanel.AddComponent<Image>();
            menuImg.sprite = uiFillSprite;
            menuImg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f); // Noir profond

            // Boutons décalés (Staggered Y et rotations alternées)
            attackButton = CreateRPGButton("AttackBtn", "ATTAQUER", rpgMenuPanel.transform, new Vector2(0.02f, 0.15f), new Vector2(0.24f, 0.85f), StartQTE, -2f, -8f);
            talkButton = CreateRPGButton("TalkBtn", "PARLER", rpgMenuPanel.transform, new Vector2(0.26f, 0.15f), new Vector2(0.48f, 0.85f), StartDialogue, 2f, 6f);
            companionsButton = CreateRPGButton("CompanionsBtn", "COMPAGNONS", rpgMenuPanel.transform, new Vector2(0.50f, 0.15f), new Vector2(0.72f, 0.85f), ShowCompanionsSubMenu, -1f, -2f);
            originalCompanionText = "COMPAGNONS";
            fleeButton = CreateRPGButton("FleeBtn", "FUIR", rpgMenuPanel.transform, new Vector2(0.74f, 0.15f), new Vector2(0.96f, 0.85f), FleeCombat, 3f, 8f);

            rpgMenuBorder.SetActive(false); // Masqué au début
            // Sauvegarder la référence du bord dans rpgMenuPanel pour activer/désactiver le tout facilement
            rpgMenuPanel = rpgMenuBorder;
        }

        // --- QTE PANEL ---
        if (customQtePanel != null)
        {
            qtePanel = customQtePanel.gameObject;
            if (customQteIndicator == null)
            {
                foreach (RectTransform r in customQtePanel.GetComponentsInChildren<RectTransform>(true))
                {
                    if (r.name.ToLower().Contains("indicator") || r.name.ToLower().Contains("cursor") || r.name.ToLower().Contains("curseur"))
                    {
                        customQteIndicator = r;
                        break;
                    }
                }
            }
            if (customQteTargetPerfect == null)
            {
                foreach (RectTransform r in customQtePanel.GetComponentsInChildren<RectTransform>(true))
                {
                    if (r.name.ToLower().Contains("perfect") || r.name.ToLower().Contains("parfait"))
                    {
                        customQteTargetPerfect = r;
                        break;
                    }
                }
            }
            if (customQteTargetGood == null)
            {
                foreach (RectTransform r in customQtePanel.GetComponentsInChildren<RectTransform>(true))
                {
                    if (r.name.ToLower().Contains("good") || r.name.ToLower().Contains("bien"))
                    {
                        customQteTargetGood = r;
                        break;
                    }
                }
            }
            if (customQteInstructionText == null)
            {
                customQteInstructionText = customQtePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (customQteFeedbackText == null)
            {
                foreach (TextMeshProUGUI t in customQtePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (t.name.ToLower().Contains("feedback") || t.name.ToLower().Contains("result"))
                    {
                        customQteFeedbackText = t;
                        break;
                    }
                }
            }

            if (customQteIndicator != null) qteIndicator = customQteIndicator;
            if (customQteTargetPerfect != null) qteTargetPerfect = customQteTargetPerfect;
            if (customQteTargetGood != null) qteTargetGood = customQteTargetGood;
            if (customQteInstructionText != null) qteInstructionText = customQteInstructionText;
            if (customQteFeedbackText != null) qteFeedbackText = customQteFeedbackText;

            customQtePanel.gameObject.SetActive(false); // Masqué au début
        }
        else
        {
            // Plaque d'ombre (crayonné papier)
            GameObject qteShadow = new GameObject("QteShadow");
            qteShadow.transform.SetParent(parent, false);
            RectTransform shadowRect = qteShadow.AddComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0.3f, 0.18f);
            shadowRect.anchorMax = new Vector2(0.7f, 0.38f);
            shadowRect.sizeDelta = Vector2.zero;
            shadowRect.anchoredPosition = new Vector2(10f, -10f); // Décalage ombre
            shadowRect.localRotation = Quaternion.Euler(0f, 0f, 3f);
            Image shadowImg = qteShadow.AddComponent<Image>();
            shadowImg.sprite = uiFillSprite;
            shadowImg.color = new Color(0.9f, 0.9f, 0.92f, 0.6f); // Gris crayonné transparent

            GameObject qteBorder = new GameObject("QteBorder");
            qteBorder.transform.SetParent(parent, false);
            RectTransform qteBRect = qteBorder.AddComponent<RectTransform>();
            qteBRect.anchorMin = new Vector2(0.3f, 0.18f);
            qteBRect.anchorMax = new Vector2(0.7f, 0.38f);
            qteBRect.sizeDelta = Vector2.zero;
            // Contre-angle dynamique par rapport au menu principal
            qteBRect.localRotation = Quaternion.Euler(0f, 0f, 3f);

            Image qteBImg = qteBorder.AddComponent<Image>();
            qteBImg.sprite = uiFillSprite;
            qteBImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f); // Bordure charbon foncée

            GameObject qtePanelInner = new GameObject("QtePanelInner");
            qtePanelInner.transform.SetParent(qteBorder.transform, false);
            RectTransform qteIRect = qtePanelInner.AddComponent<RectTransform>();
            qteIRect.anchorMin = Vector2.zero;
            qteIRect.anchorMax = Vector2.one;
            qteIRect.offsetMin = new Vector2(3, 4);
            qteIRect.offsetMax = new Vector2(-3, -2);
            Image qteIImg = qtePanelInner.AddComponent<Image>();
            qteIImg.sprite = uiFillSprite;
            qteIImg.color = new Color(0.95f, 0.95f, 0.95f, 1.0f); // Fond blanc papier sketch!

            // Instruction
            GameObject qteInstObj = new GameObject("QteInstruction");
            qteInstObj.transform.SetParent(qtePanelInner.transform, false);
            qteInstructionText = qteInstObj.AddComponent<TextMeshProUGUI>();
            if (customCombatFont != null) qteInstructionText.font = customCombatFont;
            qteInstructionText.text = "APPUYEZ SUR ESPACE AU BON MOMENT !";
            qteInstructionText.fontSize = 18f;
            qteInstructionText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            qteInstructionText.color = Color.black; // Texte noir sur papier blanc!
            qteInstructionText.alignment = TextAlignmentOptions.Center;
            RectTransform qteInstRect = qteInstructionText.rectTransform;
            qteInstRect.anchorMin = new Vector2(0.05f, 0.72f);
            qteInstRect.anchorMax = new Vector2(0.95f, 0.95f);
            qteInstRect.sizeDelta = Vector2.zero;

            // Rail de QTE
            GameObject qteRailObj = new GameObject("QteRail");
            qteRailObj.transform.SetParent(qtePanelInner.transform, false);
            Image railImg = qteRailObj.AddComponent<Image>();
            railImg.sprite = uiFillSprite;
            railImg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f); // Noir charbon pour le rail
            RectTransform railRect = railImg.rectTransform;
            railRect.anchorMin = new Vector2(0.08f, 0.22f);
            railRect.anchorMax = new Vector2(0.92f, 0.42f);
            railRect.sizeDelta = Vector2.zero;

            // Zone Jaune (BIEN)
            GameObject yellowZoneObj = new GameObject("YellowZone");
            yellowZoneObj.transform.SetParent(qteRailObj.transform, false);
            Image yellowImg = yellowZoneObj.AddComponent<Image>();
            yellowImg.sprite = uiFillSprite;
            yellowImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f); // Gris crayonné (Bien)
            RectTransform yellowRect = yellowImg.rectTransform;
            yellowRect.anchorMin = new Vector2(0.35f, 0f);
            yellowRect.anchorMax = new Vector2(0.65f, 1f);
            yellowRect.sizeDelta = Vector2.zero;
            qteTargetGood = yellowRect;

            // Zone Verte (PARFAIT)
            GameObject greenZoneObj = new GameObject("GreenZone");
            greenZoneObj.transform.SetParent(qteRailObj.transform, false);
            Image greenImg = greenZoneObj.AddComponent<Image>();
            greenImg.sprite = uiFillSprite;
            greenImg.color = Color.white; // Blanc pur (Parfait)
            RectTransform greenRect = greenImg.rectTransform;
            greenRect.anchorMin = new Vector2(0.45f, 0f);
            greenRect.anchorMax = new Vector2(0.55f, 1f);
            greenRect.sizeDelta = Vector2.zero;
            qteTargetPerfect = greenRect;

            // Indicateur (Curseur)
            GameObject indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(qteRailObj.transform, false);
            Image indImg = indicatorObj.AddComponent<Image>();
            indImg.sprite = uiFillSprite;
            indImg.color = new Color(0.85f, 0.08f, 0.14f, 1f); // Crayon de couleur rouge pour l'indicateur!
            qteIndicator = indImg.rectTransform;
            qteIndicator.anchorMin = new Vector2(0f, -0.2f);
            qteIndicator.anchorMax = new Vector2(0.015f, 1.2f);
            qteIndicator.sizeDelta = Vector2.zero;

            // Feedback Text
            GameObject qteFeedObj = new GameObject("QteFeedbackText");
            qteFeedObj.transform.SetParent(qtePanelInner.transform, false);
            qteFeedbackText = qteFeedObj.AddComponent<TextMeshProUGUI>();
            if (customCombatFont != null) qteFeedbackText.font = customCombatFont;
            qteFeedbackText.text = "";
            qteFeedbackText.fontSize = 24f;
            qteFeedbackText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            qteFeedbackText.alignment = TextAlignmentOptions.Center;
            qteFeedbackText.color = Color.black;
            RectTransform feedRect = qteFeedbackText.rectTransform;
            feedRect.anchorMin = new Vector2(0.1f, 0.48f);
            feedRect.anchorMax = new Vector2(0.9f, 0.68f);
            feedRect.sizeDelta = Vector2.zero;

            qteBorder.SetActive(false); // Masqué au début
            qtePanel = qteBorder;

            // Lier l'ombre comme enfant de la bordure pour simplifier l'activation
            qteShadow.transform.SetParent(qteBorder.transform, true);
            qteShadow.transform.SetAsFirstSibling(); // A l'arrière
        }

        // --- DIALOGUE PANEL ---
        GameObject diaBorder = new GameObject("DialogueBorder");
        diaBorder.transform.SetParent(parent, false);
        RectTransform diaBRect = diaBorder.AddComponent<RectTransform>();
        diaBRect.anchorMin = fallbackDialogueAnchorMin;
        diaBRect.anchorMax = fallbackDialogueAnchorMax;
        diaBRect.sizeDelta = Vector2.zero;
        // Légère inclinaison oblique assortie
        diaBRect.localRotation = Quaternion.Euler(0f, 0f, -2f);

        Image diaBImg = diaBorder.AddComponent<Image>();
        diaBImg.sprite = uiFillSprite;
        diaBImg.color = new Color(0.9f, 0.9f, 0.9f, 0.95f); // Bordure blanc crayonné

        GameObject diaPanelInner = new GameObject("DialoguePanelInner");
        diaPanelInner.transform.SetParent(diaBorder.transform, false);
        RectTransform diaIRect = diaPanelInner.AddComponent<RectTransform>();
        diaIRect.anchorMin = Vector2.zero;
        diaIRect.anchorMax = Vector2.one;
        diaIRect.offsetMin = new Vector2(3, 4);
        diaIRect.offsetMax = new Vector2(-3, -2);
        Image diaIImg = diaPanelInner.AddComponent<Image>();
        diaIImg.sprite = uiFillSprite;
        diaIImg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        GameObject diaTextObj = new GameObject("DialogueText");
        diaTextObj.transform.SetParent(diaPanelInner.transform, false);
        dialogueText = diaTextObj.AddComponent<TextMeshProUGUI>();
        if (customCombatFont != null) dialogueText.font = customCombatFont;
        dialogueText.text = "...";
        dialogueText.fontSize = 20f;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
        RectTransform diaTextRect = dialogueText.rectTransform;
        diaTextRect.anchorMin = new Vector2(0.05f, 0.15f);
        diaTextRect.anchorMax = new Vector2(0.95f, 0.85f);
        diaTextRect.sizeDelta = Vector2.zero;

        // Prompt Suivant
        GameObject promptObj = new GameObject("DialoguePrompt");
        promptObj.transform.SetParent(diaPanelInner.transform, false);
        TextMeshProUGUI promptText = promptObj.AddComponent<TextMeshProUGUI>();
        if (customCombatFont != null) promptText.font = customCombatFont;
        promptText.text = "[ESPACE / CLIC] Suivant";
        promptText.fontSize = 12f;
        promptText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        promptText.alignment = TextAlignmentOptions.BottomRight;
        RectTransform promptRect = promptText.rectTransform;
        promptRect.anchorMin = new Vector2(0.7f, 0.02f);
        promptRect.anchorMax = new Vector2(0.98f, 0.15f);
        promptRect.sizeDelta = Vector2.zero;

        diaBorder.SetActive(false); // Masqué au début
        dialoguePanel = diaBorder;

        // --- COMPANIONS SUB-PANEL ---
        GameObject compBorder = new GameObject("CompanionsBorder");
        compBorder.transform.SetParent(parent, false);
        RectTransform compBRect = compBorder.AddComponent<RectTransform>();
        compBRect.anchorMin = new Vector2(0.35f, 0.32f);
        compBRect.anchorMax = new Vector2(0.65f, 0.58f);
        compBRect.sizeDelta = Vector2.zero;
        compBRect.localRotation = Quaternion.Euler(0f, 0f, -3f);
        Image compBImg = compBorder.AddComponent<Image>();
        compBImg.sprite = uiFillSprite;
        compBImg.color = new Color(1f, 1f, 1f, 0.2f);

        companionsSubPanel = new GameObject("CompanionsPanelInner");
        companionsSubPanel.transform.SetParent(compBorder.transform, false);
        RectTransform compIRect = companionsSubPanel.AddComponent<RectTransform>();
        compIRect.anchorMin = Vector2.zero;
        compIRect.anchorMax = Vector2.one;
        compIRect.offsetMin = new Vector2(2, 2);
        compIRect.offsetMax = new Vector2(-2, -2);
        Image compIImg = companionsSubPanel.AddComponent<Image>();
        compIImg.sprite = uiFillSprite;
        compIImg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        GameObject compTitleObj = new GameObject("Title");
        compTitleObj.transform.SetParent(companionsSubPanel.transform, false);
        TextMeshProUGUI compTitle = compTitleObj.AddComponent<TextMeshProUGUI>();
        compTitle.text = "CHOISISSEZ UN COMPAGNON";
        compTitle.fontSize = 16f;
        compTitle.fontStyle = FontStyles.Bold;
        compTitle.color = Color.white;
        compTitle.alignment = TextAlignmentOptions.Center;
        RectTransform compTitleRect = compTitle.rectTransform;
        compTitleRect.anchorMin = new Vector2(0.05f, 0.82f);
        compTitleRect.anchorMax = new Vector2(0.95f, 0.98f);
        compTitleRect.sizeDelta = Vector2.zero;

        compBorder.SetActive(false); // Masqué au début
        // Conserver le border comme référence générale pour SetActive
        companionsSubPanel = compBorder;
    }

    private Button CreateRPGButton(string name, string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClickAction, float baseRotation = -4f, float staggerY = 0f)
    {
        // 1. Plaque d'ombre (découpée rouge cramoisi)
        GameObject shadowObj = new GameObject(name + "_Shadow");
        shadowObj.transform.SetParent(parent, false);
        RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
        shadowRect.anchorMin = anchorMin;
        shadowRect.anchorMax = anchorMax;
        shadowRect.sizeDelta = Vector2.zero;
        shadowRect.anchoredPosition = new Vector2(8f, -8f + staggerY); // Décalage de l'ombre + décalage vertical
        shadowRect.localRotation = Quaternion.Euler(0f, 0f, baseRotation);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = uiFillSprite;
        shadowImg.color = new Color(0.9f, 0.9f, 0.92f, 0.6f); // Blanc crayonné transparent

        // 2. Bouton principal (Noir charbon)
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = new Vector2(0f, staggerY); // Décalage vertical pour l'effet en escalier
        rect.localRotation = Quaternion.Euler(0f, 0f, baseRotation);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = uiFillSprite;
        img.color = new Color(0.06f, 0.06f, 0.08f, 0.95f); // Noir profond

        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None; // Désactiver les transitions par défaut (notre script gère)

        // 3. Texte (oblique)
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        if (customCombatFont != null) txt.font = customCombatFont;
        txt.text = label;
        txt.fontSize = 20f;
        txt.fontStyle = FontStyles.Bold | FontStyles.Italic; // Texte penché + gras
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform txtRect = txt.rectTransform;
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        // Ajouter l'animateur personnalisé
        RhythmUIButtonAnimator animator = btnObj.AddComponent<RhythmUIButtonAnimator>();
        animator.Setup(img, shadowImg, txt, baseRotation);

        btn.onClick.AddListener(() => onClickAction?.Invoke());
        return btn;
    }

    private void RepositionRPGButton(Button button, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (button == null) return;
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            float currentY = rect.anchoredPosition.y;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = new Vector2(0f, currentY);
        }

        // Trouver et repositionner l'ombre correspondante
        if (button.transform.parent != null)
        {
            Transform shadowTrans = button.transform.parent.Find(button.name + "_Shadow");
            if (shadowTrans != null)
            {
                RectTransform shadowRect = shadowTrans.GetComponent<RectTransform>();
                if (shadowRect != null)
                {
                    float currentShadowY = shadowRect.anchoredPosition.y;
                    shadowRect.anchorMin = anchorMin;
                    shadowRect.anchorMax = anchorMax;
                    shadowRect.sizeDelta = Vector2.zero;
                    shadowRect.anchoredPosition = new Vector2(8f, currentShadowY);
                }
            }
        }
    }

    private void ShowCompanionsSubMenu()
    {
        // Masquer le menu principal
        rpgMenuPanel.SetActive(false);
        companionsSubPanel.SetActive(true);

        // Nettoyer les anciens boutons
        foreach (var btn in companionButtons)
        {
            if (btn != null) Destroy(btn);
        }
        companionButtons.Clear();

        // Récupérer le conteneur interne pour ajouter les boutons
        Transform container = companionsSubPanel.transform.Find("CompanionsPanelInner");
        if (container == null) container = companionsSubPanel.transform;

        // Trouver les compagnons et créer des boutons verticalement
        int buttonIndex = 0;
        float startY = 0.65f;
        float spacing = 0.15f;

        GameObject firstCompanionBtn = null;

        for (int i = 0; i < allies.Count; i++)
        {
            if (i == activeAllyIndex) continue; // Pas le héros actif
            if (allyHP[i] <= 0) continue;       // Seulement s'il est en vie

            int indexToSwap = i;
            float topY = startY - buttonIndex * spacing;
            float bottomY = topY - 0.12f;

            Button btn = CreateRPGButton($"CompBtn_{i}", $"{allies[i].name.ToUpper()} ({allyHP[i]}/{allyMaxHP[i]} PV)", container, 
                new Vector2(0.1f, bottomY), new Vector2(0.9f, topY), 
                () => {
                    SwapActiveCharacter(indexToSwap);
                    companionsSubPanel.SetActive(false);
                    TransitionToDodgePhase();
                }
            );

            if (firstCompanionBtn == null)
            {
                firstCompanionBtn = btn.gameObject;
            }

            companionButtons.Add(btn.gameObject);
            buttonIndex++;
        }

        // Ajouter un bouton de Retour tout en bas
        float backTopY = 0.12f;
        float backBottomY = 0.02f;
        Button backBtn = CreateRPGButton("BackBtn", "RETOUR", container,
            new Vector2(0.2f, backBottomY), new Vector2(0.8f, backTopY),
            () => {
                companionsSubPanel.SetActive(false);
                rpgMenuPanel.SetActive(true);
                if (groupContainerObj != null) groupContainerObj.SetActive(true);
                
                // Rétablir le focus sur le bouton Compagnons
                if (EventSystem.current != null && companionsButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(companionsButton.gameObject);
                }
            }
        );
        companionButtons.Add(backBtn.gameObject);

        // Mettre le focus de navigation sur le premier compagnon ou le bouton retour
        if (EventSystem.current != null)
        {
            if (firstCompanionBtn != null)
            {
                EventSystem.current.SetSelectedGameObject(firstCompanionBtn);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(backBtn.gameObject);
            }
        }
    }

    private void TransitionToPlayerTurn()
    {
        currentPhase = CombatPhase.PlayerTurn;

        // Désactiver les contrôles du joueur sur la grille
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        // Nettoyer tous les telegraphs / alertes sur la grille
        ClearAllTelegraphs();

        // Zoom caméra : ajuster la distance et la hauteur pour un plan serré
        cameraDistance = playerTurnCameraDistance;
        cameraHeight = playerTurnCameraHeight;

        // Afficher l'UI du menu RPG et la barre de vie (les animations sont gérées par PlayerTurnEntranceAnimation)
        if (rpgMenuPanel != null)
        {
            // alpha=0 avant SetActive pour éviter le flash — le slide est géré dans le coroutine
            CanvasGroup cg = rpgMenuPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = rpgMenuPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            rpgMenuPanel.SetActive(true);

            // Vérifier s'il reste d'autres compagnons en vie
            bool otherAlive = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (i != activeAllyIndex && allyHP[i] > 0)
                {
                    otherAlive = true;
                    break;
                }
            }
            // Mettre à jour l'affichage du bouton Compagnons en style gribouillé/grésillé (barré) au lieu de le désactiver
            if (companionsButton != null)
            {
                companionsButton.gameObject.SetActive(true); // Conserver le bouton actif pour ne pas casser le layout
                companionsButton.interactable = otherAlive;

                TextMeshProUGUI txt = companionsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    if (otherAlive)
                    {
                        txt.text = originalCompanionText;
                        txt.color = Color.white;
                    }
                    else
                    {
                        txt.text = $"<s>{originalCompanionText}</s>"; // Texte barré / crayonné
                        txt.color = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Gris atténué crayonné
                    }
                }

                // Désactiver le comportement d'animation de survol s'il n'est pas interactif
                RhythmUIButtonAnimator anim = companionsButton.GetComponent<RhythmUIButtonAnimator>();
                if (anim != null)
                {
                    if (!otherAlive)
                    {
                        anim.DeselectButton(); // S'assurer qu'il ne reste pas surélevé ou blanc
                    }
                }

                // Assombrir le bouton compagnon pour accentuer l'effet barré
                Image img = companionsButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = otherAlive ? new Color(0.06f, 0.06f, 0.08f, 0.95f) : new Color(0.02f, 0.02f, 0.02f, 0.3f);
                }

                // Masquer ou atténuer l'ombre crayonné correspondante
                if (companionsButton.transform.parent != null)
                {
                    Transform shadowTrans = companionsButton.transform.parent.Find(companionsButton.name + "_Shadow");
                    if (shadowTrans != null)
                    {
                        Image shadowImg = shadowTrans.GetComponent<Image>();
                        if (shadowImg != null)
                        {
                            shadowImg.color = otherAlive ? new Color(0.9f, 0.9f, 0.92f, 0.6f) : new Color(0f, 0f, 0f, 0f);
                        }
                    }
                }

                Debug.Log($"[RhythmCombatManager] Bouton compagnon mis à jour (Interactable: {otherAlive})");
            }
            else
            {
                Debug.LogWarning("[RhythmCombatManager] Impossible de mettre à jour le bouton compagnon car companionsButton est NULL.");
            }

            // Forcer le recalcul immédiate du layout (notamment pour les GridLayoutGroup)
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rpgMenuPanel.GetComponent<RectTransform>());

            // Si on utilise le menu généré dynamiquement, s'assurer de leur positionnement standard (4 boutons)
            if (customMenuPanel == null)
            {
                RepositionRPGButton(attackButton, new Vector2(0.02f, 0.15f), new Vector2(0.24f, 0.85f));
                RepositionRPGButton(talkButton, new Vector2(0.26f, 0.15f), new Vector2(0.48f, 0.85f));
                RepositionRPGButton(companionsButton, new Vector2(0.50f, 0.15f), new Vector2(0.72f, 0.85f));
                RepositionRPGButton(fleeButton, new Vector2(0.74f, 0.15f), new Vector2(0.96f, 0.85f));
            }

            // Sélectionner le bouton d'attaque par défaut pour la navigation manette/clavier
            if (EventSystem.current != null && attackButton != null)
            {
                EventSystem.current.SetSelectedGameObject(attackButton.gameObject);
            }
        }

        logText.text = "À votre tour ! Choisissez une action.";

        // Lancer l'animation d'entrée des boutons et de la barre de vie
        StartCoroutine(PlayerTurnEntranceAnimation());
    }

    private IEnumerator PlayerTurnEntranceAnimation()
    {
        // === 1. Barre de vie : pop-scale élastique ===
        if (groupContainerObj != null)
        {
            groupContainerObj.SetActive(true);
            groupContainerObj.transform.localScale = Vector3.zero;

            float t = 0f, dur = 0.3f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                // 0→1.12 en 70% du temps, puis 1.12→1.0 en 30%
                float s = p < 0.7f
                    ? Mathf.Lerp(0f, 1.12f, p / 0.7f)
                    : Mathf.Lerp(1.12f, 1f, (p - 0.7f) / 0.3f);
                groupContainerObj.transform.localScale = Vector3.one * s;
                yield return null;
            }
            groupContainerObj.transform.localScale = Vector3.one;
        }

        // === 2. Menu boutons : slide entier depuis la gauche via localPosition ===
        if (rpgMenuPanel != null && rpgMenuPanel.activeSelf)
        {
            // S'assurer qu'un CanvasGroup existe pour le fade
            CanvasGroup cg = rpgMenuPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = rpgMenuPanel.AddComponent<CanvasGroup>();

            // La position courante est propre (ForceRebuildLayout peut l'avoir réinitialisée)
            // On applique l'offset maintenant pendant que alpha=0 (invisible)
            Vector3 origPos  = rpgMenuPanel.transform.localPosition;
            Vector3 startPos = origPos + new Vector3(-1400f, 0f, 0f);
            rpgMenuPanel.transform.localPosition = startPos;

            float t = 0f, dur = 0.38f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                // Ease-out cubique
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                rpgMenuPanel.transform.localPosition = Vector3.Lerp(startPos, origPos, ease);
                cg.alpha = ease;
                yield return null;
            }

            rpgMenuPanel.transform.localPosition = origPos;
            cg.alpha = 1f;
        }
    }

    private void TransitionToDodgePhase()
    {
        currentPhase = CombatPhase.DodgePhase;
        dodgeBeatsCount = 0;

        // Restaurer la caméra aux distances initiales de combat
        cameraDistance = originalCameraDistance;
        cameraHeight = originalCameraHeight;

        // Réactiver les contrôles du joueur sur la grille
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }

        // Masquer tous les menus et panneaux
        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);
        if (qtePanel != null) qtePanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (companionsSubPanel != null) companionsSubPanel.SetActive(false);

        logText.text = "ESQUIVEZ EN RYTHME !";
    }

    private void ClearAllTelegraphs()
    {
        if (radialGrid != null)
        {
            foreach (var key in new List<string>(activeTelegraphs.Keys))
            {
                string[] parts = key.Split('_');
                if (parts.Length == 2)
                {
                    int ring = int.Parse(parts[0]);
                    int sector = int.Parse(parts[1]);
                    radialGrid.SetCellWarning(ring, sector, false);
                }
            }
        }
        activeTelegraphs.Clear();
        groundOnlyTelegraphs.Clear();
    }

    private void StartQTE()
    {
        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);
        if (qtePanel != null) qtePanel.SetActive(true);

        qteResolved = false;
        qteStartBeat = BeatManager.Instance.GetCurrentBeatDecimal();
        qteStartFrame = Time.frameCount; // Enregistrer la frame de départ
        currentPhase = CombatPhase.QTEActive;

        if (qteFeedbackText != null) qteFeedbackText.text = "";
        if (qteIndicator != null)
        {
            qteIndicator.anchorMin = new Vector2(0f, -0.2f);
            qteIndicator.anchorMax = new Vector2(0.015f, 1.2f);
        }
    }

    private void UpdateQTE()
    {
        if (qteResolved) return;

        float elapsedBeats = BeatManager.Instance.GetCurrentBeatDecimal() - qteStartBeat;
        float progress = elapsedBeats / 2.0f; // Le QTE dure 2 battements

        // Mettre à jour la position de l'indicateur (curseur)
        if (qteIndicator != null)
        {
            qteIndicator.anchorMin = new Vector2(Mathf.Clamp01(progress), -0.2f);
            qteIndicator.anchorMax = new Vector2(Mathf.Clamp01(progress + 0.015f), 1.2f);
        }

        // Si le temps est dépassé
        if (progress >= 1.05f)
        {
            StartCoroutine(ResolveQTERoutine(1.05f));
            return;
        }

        // Empêcher de consommer l'input de validation du menu RPG (cooldown)
        if (Time.frameCount == qteStartFrame || elapsedBeats < 0.15f) return;

        // Détection d'inputs
        bool inputPressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) inputPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) inputPressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) inputPressed = true;

        if (inputPressed)
        {
            StartCoroutine(ResolveQTERoutine(progress));
        }
    }

    private IEnumerator ResolveQTERoutine(float progress)
    {
        qteResolved = true;

        float distance = Mathf.Abs(progress - 0.5f);
        string feedback = "";
        Color color = Color.white;
        int damage = 0;

        if (distance <= 0.05f) // Zone Verte (Parfait)
        {
            feedback = "PARFAIT !";
            color = Color.green;
            damage = 50;
        }
        else if (distance <= 0.15f) // Zone Jaune (Bien)
        {
            feedback = "BIEN !";
            color = Color.yellow;
            damage = 25;
        }
        else // Hors zone (Raté)
        {
            feedback = "RATE !";
            color = Color.red;
            damage = 0;
        }

        if (qteFeedbackText != null)
        {
            qteFeedbackText.text = feedback;
            qteFeedbackText.color = color;
        }

        // Utiliser aussi le combo feedback text au milieu de l'écran
        if (comboFeedbackText != null)
        {
            comboFeedbackText.text = feedback;
            comboFeedbackText.color = color;
            StartCoroutine(AnimateComboText());
        }

        if (damage > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - damage);
            UpdateUI();

            // Particules de succès
            if (attackSuccessParticles != null && activeEnemy != null)
            {
                ParticleSystem ps = Instantiate(attackSuccessParticles, activeEnemy.transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 1.0f);
            }

            logText.text = $"Vous touchez le boss en rythme ! Dégâts : {damage}";
        }
        else
        {
            logText.text = "Trop tard ou trop tôt ! Suivez le tempo.";
        }

        yield return new WaitForSeconds(1.2f);

        if (enemyHP <= 0)
        {
            StartCoroutine(VictoryRoutine());
        }
        else
        {
            TransitionToDodgePhase();
        }
    }

    private void StartDialogue()
    {
        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);

        if (DialogueManager.Instance != null && activeCombatData != null && activeCombatData.TalkDialogues != null && activeCombatData.TalkDialogues.Count > 0)
        {
            currentPhase = CombatPhase.DialogueActive;

            // Créer un DialogueData temporaire à la volée
            DialogueData tempDialogue = ScriptableObject.CreateInstance<DialogueData>();
            List<string> lines = activeCombatData.TalkDialogues;
            DialogueNode[] nodes = new DialogueNode[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                nodes[i] = new DialogueNode();
                nodes[i].nodeID = i.ToString();
                nodes[i].characterName = activeCombatData.EnemyName;
                nodes[i].portrait = null; // Pas de portrait !
                nodes[i].sentence = lines[i];
                nodes[i].nextNodeID = (i + 1 < lines.Count) ? (i + 1).ToString() : null;
                nodes[i].choices = null;
            }
            tempDialogue.nodes = nodes;

            // Masquer notre propre panel de combat de dialogue s'il y en a un
            if (dialoguePanel != null) dialoguePanel.SetActive(false);

            logText.text = "Discussion engagée avec " + activeCombatData.EnemyName;

            DialogueManager.Instance.StartDialogue(tempDialogue, () =>
            {
                TransitionToDodgePhase();
            });
        }
        else
        {
            // Fallback si DialogueManager n'est pas présent dans la scène
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            currentPhase = CombatPhase.DialogueActive;
            dialogueEnterTime = Time.time;

            if (activeCombatData != null && activeCombatData.TalkDialogues != null && activeCombatData.TalkDialogues.Count > 0)
            {
                currentDialogueIndex = 0;
                dialogueText.text = activeCombatData.TalkDialogues[0];
                logText.text = "Discussion engagée avec " + activeCombatData.EnemyName;
            }
            else
            {
                currentDialogueIndex = 9999;
                dialogueText.text = "Vous tentez de parler, mais l'ennemi ne semble pas disposé à discuter...";
                logText.text = "Aucun dialogue disponible.";
            }
        }
    }

    private void UpdateDialogue()
    {
        // Si le gestionnaire de dialogue global est actif, il gère les inputs de son côté
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        if (Time.time - dialogueEnterTime < 0.2f) return;

        bool advancePressed = false;
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) advancePressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) advancePressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) advancePressed = true;

        if (advancePressed)
        {
            if (currentDialogueIndex == 9999) // Mode fallback
            {
                dialoguePanel.SetActive(false);
                TransitionToDodgePhase();
            }
            else
            {
                currentDialogueIndex++;
                if (activeCombatData != null && currentDialogueIndex < activeCombatData.TalkDialogues.Count)
                {
                    dialogueText.text = activeCombatData.TalkDialogues[currentDialogueIndex];
                    dialogueEnterTime = Time.time; // réinitialiser le cooldown
                }
                else
                {
                    dialoguePanel.SetActive(false);
                    TransitionToDodgePhase();
                }
            }
        }
    }

    private void FleeCombat()
    {
        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);
        logText.text = "Vous fuyez le combat !";
        StartCoroutine(FleeRoutine());
    }

    private IEnumerator FleeRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(EndCombatRoutine(false));
    }

    #endregion

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= ProcessEnemyAttackBeat;
        }
    }
}

/// <summary>
/// Composant d'animation de survol et sélection pour le menu de combat stylisé.
/// </summary>
public class RhythmUIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private RectTransform rectTransform;
    private Image buttonImage;
    private TextMeshProUGUI buttonText;
    private Image shadowImage;
    private Button parentButton;

    private Vector3 targetScale = Vector3.one;
    private Vector2 originalPosition;
    private Vector2 targetPositionOffset = Vector2.zero;

    private float originalRotationZ = 0f;
    private float targetRotationZ = 0f;

    private Color originalBgColor;
    private Color targetBgColor;

    private Color originalTextColor;
    private Color targetTextColor;

    private float animationSpeed = 10f;
    private Vector2 currentOffset = Vector2.zero;
    private float currentRotationZ = 0f;

    public void Setup(Image mainImg, Image shadowImg, TextMeshProUGUI text, float baseRotation)
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = mainImg;
        shadowImage = shadowImg;
        buttonText = text;
        parentButton = GetComponent<Button>();

        originalRotationZ = baseRotation;
        targetRotationZ = baseRotation;
        currentRotationZ = baseRotation;

        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }

        if (buttonImage != null)
        {
            originalBgColor = buttonImage.color;
            targetBgColor = originalBgColor;
        }
        else
        {
            originalBgColor = new Color(0.06f, 0.06f, 0.08f, 0.95f);
            targetBgColor = originalBgColor;
        }

        if (buttonText != null)
        {
            originalTextColor = buttonText.color;
            targetTextColor = originalTextColor;
        }
        else
        {
            originalTextColor = Color.white;
            targetTextColor = originalTextColor;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Interpolation d'échelle
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // Interpolation du décalage de position
        currentOffset = Vector2.Lerp(currentOffset, targetPositionOffset, Time.deltaTime * animationSpeed);
        rectTransform.anchoredPosition = originalPosition + currentOffset;

        // Interpolation de la rotation Z
        currentRotationZ = Mathf.Lerp(currentRotationZ, targetRotationZ, Time.deltaTime * animationSpeed);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotationZ);

        // Interpolation des couleurs
        if (buttonImage != null)
        {
            buttonImage.color = Color.Lerp(buttonImage.color, targetBgColor, Time.deltaTime * animationSpeed);
        }
        if (buttonText != null)
        {
            buttonText.color = Color.Lerp(buttonText.color, targetTextColor, Time.deltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => SelectButton();
    public void OnPointerExit(PointerEventData eventData) => DeselectButton();
    public void OnSelect(BaseEventData eventData) => SelectButton();
    public void OnDeselect(BaseEventData eventData) => DeselectButton();

    public void SelectButton()
    {
        if (parentButton != null && !parentButton.interactable) return;

        targetScale = Vector3.one * 1.15f;
        targetPositionOffset = new Vector2(15f, 10f); // Décalage vers le haut/droite (effet 3D "papier découpé")
        targetRotationZ = originalRotationZ - 4f;     // Rotation dynamique additionnelle

        // Inversion de couleur stylisée (Fond blanc éclatant, texte noir)
        targetBgColor = Color.white; 
        targetTextColor = Color.black;

        // Ombre foncée contrastée pour faire ressortir le relief crayonné
        if (shadowImage != null)
        {
            shadowImage.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        }
    }

    public void DeselectButton()
    {
        targetScale = Vector3.one;
        targetPositionOffset = Vector2.zero;
        targetRotationZ = originalRotationZ;

        targetBgColor = originalBgColor;
        targetTextColor = originalTextColor;

        if (shadowImage != null)
        {
            shadowImage.color = new Color(0.9f, 0.9f, 0.92f, 0.6f);
        }
    }
}
