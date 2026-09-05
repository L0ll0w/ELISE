using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum ArrowDirection { Up, Down, Left, Right }

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

    [Header("Configuration Caméra Attaque / QTE")]
    [Tooltip("Distance de la caméra lors de la phase d'attaque QTE.")]
    [SerializeField] private float attackCameraDistance = 6f;
    [Tooltip("Hauteur de la caméra lors de la phase d'attaque QTE.")]
    [SerializeField] private float attackCameraHeight = 1.8f;
    [Tooltip("Angle d'inclinaison Z (tilt) de la caméra lors de la phase d'attaque QTE.")]
    [SerializeField] private float attackCameraTiltZ = 2f;

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

    [Header("Positionnement et Matériaux des PV Joueurs")]
    [Tooltip("Anchor Min X/Y des barres de PV Joueur (Middle Center compact : 0.37, 0.47).")]
    [SerializeField] private Vector2 playerHPAnchorMin = new Vector2(0.37f, 0.47f);
    [Tooltip("Anchor Max X/Y des barres de PV Joueur (Middle Center compact : 0.63, 0.55).")]
    [SerializeField] private Vector2 playerHPAnchorMax = new Vector2(0.63f, 0.55f);
    [Tooltip("Matériau personnalisé pour le fond de la barre de vie des joueurs.")]
    [SerializeField] private Material playerHPBackgroundMaterial;
    [Tooltip("Matériau personnalisé pour le remplissage (Fill) de la barre de vie des joueurs.")]
    [SerializeField] private Material playerHPFillMaterial;
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

    [Header("Helldivers 2 QTE Attaque")]
    [Tooltip("Délai de sécurité (en secondes) à l'apparition du menu de combat pendant lequel les clics / validations sont ignorés (évite l'attaque accidentelle).")]
    [SerializeField] private float menuInputSecurityDelay = 0.40f;
    private float menuEnableTime = 0f;
    [Tooltip("Nom de l'état d'animation de préparation de tir sur l'Animator du joueur.")]
    [SerializeField] private string shootAnimationStateName = "shoot";

    [Tooltip("Nom de l'état d'animation de tir joué à la réussite du QTE avant le départ du projectile (ex: shoot 2).")]
    [SerializeField] private string qteSuccessShootAnimationStateName = "shoot 2";

    [Tooltip("Délai (en secondes) de l'animation shoot 2 avant que le projectile ne parte.")]
    [SerializeField] private float qteSuccessShootDelay = 0.5f;

    [Tooltip("Nom de l'état d'animation de danse/repos sur l'Animator du joueur après l'attaque.")]
    [SerializeField] private string danceAnimationStateName = "dance";

    [Tooltip("Nom de l'état d'animation de célébration/victoire et de discussion du joueur (ex: facedance).")]
    [SerializeField] private string faceDanceAnimationStateName = "facedance";

    [Tooltip("Nom de l'état ou du Trigger d'animation de coup reçu (Hit) sur l'ennemi.")]
    [SerializeField] private string enemyHitAnimationName = "hit";

    [Tooltip("Délai (en secondes) de l'animation de tir avant l'apparition du QTE.")]
    [SerializeField] private float shootAnimationDelay = 0.45f;

    [Tooltip("Inverser horizontalement le sprite (Flip X) pendant l'animation de tir (cocher si le sprite tire vers la gauche par défaut).")]
    [SerializeField] private bool flipPlayerDuringShoot = false;

    [Tooltip("Temps limite (en secondes) pour réaliser la combinaison directionnelle.")]
    [SerializeField] private float qteTimeLimit = 4.0f;

    [Tooltip("Séquence de flèches pour réaliser l'attaque.")]
    [SerializeField] private List<ArrowDirection> qteComboSequence = new List<ArrowDirection>()
    {
        ArrowDirection.Right,
        ArrowDirection.Down,
        ArrowDirection.Left,
        ArrowDirection.Right,
        ArrowDirection.Right
    };

    [Tooltip("Prefab optionnel du projectile d'encre.")]
    [SerializeField] private GameObject inkProjectilePrefab;

    [Tooltip("Point d'origine du tir (bout du doigt du joueur). Si non renseigné, sera calculé automatiquement.")]
    [SerializeField] private Transform playerFingerTip;

    [Header("Champs QTE Personnalisés (UI Attaque)")]
    [Tooltip("Le panel (RectTransform) de QTE personnalisé.")]
    [SerializeField] private RectTransform customQtePanel;
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
    private Vector3 originalMainCamPos;
    private Quaternion originalMainCamRot = Quaternion.identity;
    private float originalMainCamFOV = 40f;
    private int dodgeBeatsCount = 0;
    private bool hasPlayedFirstDodgeTutorial = false;
    private bool hasPlayedSecondDodgeTutorial = false;
    private bool hasPlayedVictoryTutorial = false;
    private int tutorialPlayerTurnCount = 0;
    private bool isGardenerInterventionActive = false;
    private int currentTalkDialogueStep = 0;

    // Références pour la sauvegarde et restauration de la musique de fond d'origine
    private AudioSource previousAudioSource;
    private AudioClip previousMusicClip;
    private float previousMusicTime;
    private float previousMusicVolume;

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

    // Helldivers 2 Arrow Combo QTE
    private int qteCurrentIndex = 0;
    private float qteStartTime = 0f;
    private bool qteUIWaitingForAnim = false;
    private List<Image> qteArrowCardFills = new List<Image>();
    private List<TextMeshProUGUI> qteArrowTexts = new List<TextMeshProUGUI>();

    // Système d'Attaque Ennemie (Telegraphs)
    private Dictionary<GridCell, int> activeTelegraphs = new Dictionary<GridCell, int>(); // Clé: GridCell(ring, sector), Valeur: beat à laquelle l'attaque frappe
    private HashSet<GridCell> groundOnlyTelegraphs = new HashSet<GridCell>(); // Clés des attaques au sol esquivables par le saut

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
        hasPlayedFirstDodgeTutorial = false;
        hasPlayedSecondDodgeTutorial = false;
        hasPlayedVictoryTutorial = false;
        tutorialPlayerTurnCount = 0;
        currentTalkDialogueStep = 0;
        originalCameraDistance = cameraDistance;
        originalCameraHeight = cameraHeight;

        if (Camera.main != null)
        {
            originalMainCamPos = Camera.main.transform.position;
            originalMainCamRot = Camera.main.transform.rotation;
            originalMainCamFOV = Camera.main.fieldOfView;
        }

        // 0. Détecter et effectuer un fondu de sortie sur la musique de fond actuelle
        previousAudioSource = null;
        previousMusicClip = null;
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        AudioSource beatManagerSource = BeatManager.Instance != null ? BeatManager.Instance.GetComponent<AudioSource>() : null;

        foreach (var source in allAudioSources)
        {
            if (source != null && source.isPlaying && source.clip != null && source != beatManagerSource)
            {
                previousAudioSource = source;
                previousMusicClip = source.clip;
                previousMusicTime = source.time;
                previousMusicVolume = source.volume;
                StartCoroutine(FadeOutAudioSource(source, 0.8f));
                break;
            }
        }

        Debug.Log("[RhythmCombatManager] Initialisation du combat rythmique radial...");

        // 1. Fondu au noir
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(1f));

        // 2. Geler le joueur et désactiver le suivi de groupe
        PlayerLockManager.SetPlayerLocked(true, hideFollowers: true);

        Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
        if (leader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
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
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
            if (cameraHelper == null) cameraHelper = virtualCamera.GetComponentInChildren<CinemachineHelper>();
            if (cameraHelper != null)
            {
                cameraHelper.SaveOriginalSettings();
            }
            virtualCamera.enabled = false;
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

        // 4. Recherche de Zone Libre et Repositionnement synchrone Ennemi + Grille + Joueur
        Vector3 initialCenter = activeEnemy.transform.position;
        Vector3 combatCenter = FindSafeCombatCenter(initialCenter, 4.5f);
        combatCenter = SnapToGround(combatCenter);

        // Déplacer l'ennemi au centre de la zone sécurisée
        activeEnemy.transform.position = combatCenter;

        if (radialGrid == null)
        {
            radialGrid = FindFirstObjectByType<RadialCombatGrid>();
        }

        if (radialGrid == null)
        {
            GameObject gridObj = new GameObject("RadialCombatGrid");
            radialGrid = gridObj.AddComponent<RadialCombatGrid>();
        }

        // Déplacer la grille au centre de la zone sécurisée
        radialGrid.transform.position = combatCenter;
        radialGrid.SetGridActive(true);

        // 5. Calculer le secteur de départ du joueur et le placer directement sur la nouvelle grille
        Vector3 dirToPlayer = (leader.position - initialCenter).normalized;
        if (dirToPlayer.sqrMagnitude < 0.001f) dirToPlayer = Vector3.forward;
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

        // Repositionner physiquement le joueur sur sa case de départ sur la grille réalignée
        Vector3 playerCellPos = radialGrid.GetCellPosition(0, startSector);
        leader.position = SnapToGround(playerCellPos);

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
        BeatManager.Instance.StartMusic();
        StartCoroutine(FadeInBeatManager(combatMusicVolume, 0.8f));

        // S'abonner aux battements
        BeatManager.Instance.OnBeat += ProcessEnemyAttackBeat;

        // Attendre la stabilisation
        yield return new WaitForSeconds(0.2f);

        // 8. Fondu de retour (Fade In)
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(0f));

        // 9. Si c'est le combat tutoriel du Jardinier, lancer le dialogue du tout début
        if (activeCombatData != null && activeCombatData.IsGardenerTutorial)
        {
            DialogueData startDiag = GetTutorialDialogueOrDefault(
                activeCombatData.StartTutorialDialogue,
                "Jardinier",
                "Attention ! Le combat commence. Restez concentré et esquivez les attaques en rythme !"
            );
            yield return StartCoroutine(MoveGardenerToPlayerAndRunDialogue(startDiag));
        }

        isGardenerInterventionActive = false;
        currentState = CombatState.Active;
        if (playerController != null) playerController.SetInputEnabled(true);
        logText.text = "ESQUIVEZ EN RYTHME ! Évitez les attaques de l'ennemi !";
    }

    #endregion

    #region Boucle de Combat & Attaques de l'Ennemi (Dodge Phase)

    private void ProcessEnemyAttackBeat(int beatIndex)
    {
        if (currentState != CombatState.Active) return;

        if (currentPhase != CombatPhase.DodgePhase || isGardenerInterventionActive) return;

        // A. Évaluer et appliquer les dégâts des alertes qui devaient frapper à ce beat
        ApplyTelegraphDamage(beatIndex);

        // B. Gérer la durée de la phase d'esquive
        dodgeBeatsCount++;
        int duration = activeCombatData != null ? activeCombatData.DodgePhaseDuration : 16;
        if (dodgeBeatsCount >= duration)
        {
            if (activeCombatData != null && activeCombatData.IsGardenerTutorial)
            {
                if (!hasPlayedFirstDodgeTutorial)
                {
                    hasPlayedFirstDodgeTutorial = true;
                    DialogueData d = GetTutorialDialogueOrDefault(
                        activeCombatData.AfterFirstDodgeDialogue,
                        "Jardinier",
                        "Bien esquivé ! Pour l'instant, utilisez la commande PARLER pour essayer de communiquer."
                    );
                    StartCoroutine(RunDodgeTutorialAndTransition(d));
                    return;
                }
                else if (!hasPlayedSecondDodgeTutorial)
                {
                    hasPlayedSecondDodgeTutorial = true;
                    DialogueData d = GetTutorialDialogueOrDefault(
                        activeCombatData.AfterSecondDodgeDialogue,
                        "Jardinier",
                        "Parfait ! Il est temps de riposter ! Utilisez la commande ATTAQUER pour lancer une frappe."
                    );
                    StartCoroutine(RunDodgeTutorialAndTransition(d));
                    return;
                }
            }

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
        List<GridCell> resolvedKeys = new List<GridCell>();

        foreach (var pair in activeTelegraphs)
        {
            if (pair.Value <= currentBeat)
            {
                resolvedKeys.Add(pair.Key);
                
                int ring = pair.Key.Ring;
                int sector = pair.Key.Sector;

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

    public void TelegraphCell(GridCell cell, int impactBeat, bool isGroundOnly = false)
    {
        TelegraphCell(cell.Ring, cell.Sector, impactBeat, isGroundOnly);
    }

    public void TelegraphCell(int ring, int sector, int impactBeat, bool isGroundOnly = false)
    {
        GridCell cell = new GridCell(ring, sector);
        if (!activeTelegraphs.ContainsKey(cell))
        {
            activeTelegraphs.Add(cell, impactBeat);
            
            if (isGroundOnly)
            {
                groundOnlyTelegraphs.Add(cell);
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

            // Pop-up de dégâts gribouillé sur l'ennemi (police custom du jeu)
            if (activeEnemy != null)
            {
                DamageNumberPopup.Create(activeEnemy.transform.position + Vector3.up * 1.2f, damage, isPlayerDamage: false, fontAsset: customCombatFont);
            }

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

        // Pop-up de dégâts gribouillé sur le joueur (police custom du jeu)
        if (playerController != null)
        {
            DamageNumberPopup.Create(playerController.transform.position + Vector3.up * 1.2f, dmg, isPlayerDamage: true, fontAsset: customCombatFont);
        }

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

        // Jouer l'animation facedance sur le joueur dès le début du Jugement
        if (!string.IsNullOrEmpty(faceDanceAnimationStateName))
        {
            PlayPlayerAnimation(faceDanceAnimationStateName);
        }

        // Dialogue tutoriel de victoire du Jardinier si actif
        if (activeCombatData != null && activeCombatData.IsGardenerTutorial && !hasPlayedVictoryTutorial)
        {
            hasPlayedVictoryTutorial = true;
            DialogueData vicDiag = GetTutorialDialogueOrDefault(
                activeCombatData.VictoryTutorialDialogue,
                "Jardinier",
                "Beau travail ! Vous avez vaincu le monstre."
            );
            yield return StartCoroutine(MoveGardenerToPlayerAndRunDialogue(vicDiag));
        }

        // Focus caméra serré sur l'ennemi qui attend son jugement
        if (activeEnemy != null)
        {
            cameraDistance = 3.2f;
            cameraHeight = 1.8f;
        }

        if (logText != null) logText.text = "L'ennemi est à votre merci... Quel est votre jugement ?";

        // 1. Fondu d'assombrissement du décor et apparition du halo lumineux sur l'ennemi
        Vector3 haloPos = activeEnemy != null ? activeEnemy.transform.position : transform.position;
        yield return StartCoroutine(DimSceneLightingAndSpawnHaloRoutine(haloPos));

        // 2. Affichage et choix dans le Menu de Verdict (Balance de la Justice)
        int verdictChoice = -1; // 0 = GRACIER (Gauche), 1 = CONDAMNER (Droite)
        yield return StartCoroutine(RunVerdictChoiceRoutine(res => verdictChoice = res));

        // 3. Traitement selon la sentence choisie
        if (verdictChoice == 1) // CONDAMNER
        {
            if (logText != null) logText.text = "Vous choisissez de CONDAMNER l'ennemi !";

            DialogueData condDiag = activeCombatData != null ? activeCombatData.CondemnedDialogue : null;
            if (condDiag != null)
            {
                yield return StartCoroutine(RunDirectDialogue(condDiag));
            }

            if (activeEnemy != null)
            {
                yield return StartCoroutine(AnimateEnemyDisintegration(activeEnemy));
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
        else // GRACIER (0)
        {
            if (logText != null) logText.text = "Vous choisissez de GRACIER l'ennemi !";

            DialogueData sparedDiag = activeCombatData != null ? activeCombatData.SparedDialogue : null;
            if (sparedDiag != null)
            {
                yield return StartCoroutine(RunDirectDialogue(sparedDiag));
            }

            if (activeEnemy != null)
            {
                yield return StartCoroutine(AnimateEnemyAscension(activeEnemy));
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }

        // 4. Restauration de l'éclairage de la scène
        yield return StartCoroutine(RestoreSceneLightingRoutine());

        // 5. Fin du combat avec victoire
        yield return StartCoroutine(EndCombatRoutine(true));
    }

    private IEnumerator RunDirectDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null) yield break;

        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);

        bool dialogueFinished = false;
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueData, () => dialogueFinished = true);
            while (!dialogueFinished) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }
    }

    #region Système de Verdict (Balance de la Justice : Condamner vs Gracier)

    private GameObject verdictHaloObj;
    private Light verdictSpotlight;
    private Color savedAmbientLight;
    private List<System.Tuple<Light, float>> savedLightIntensities = new List<System.Tuple<Light, float>>();

    private IEnumerator DimSceneLightingAndSpawnHaloRoutine(Vector3 targetPos)
    {
        // 1. Sauvegarder la couleur de lumière ambiante originale
        savedAmbientLight = RenderSettings.ambientLight;
        savedLightIntensities.Clear();

        // 2. Récupérer toutes les lumières de la scène et sauvegarder leurs intensités
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l != null && l.enabled)
            {
                savedLightIntensities.Add(new System.Tuple<Light, float>(l, l.intensity));
            }
        }

        // 3. Fondu d'assombrissement (0.8s)
        float elapsed = 0f;
        float fadeDur = 0.8f;
        Color targetAmbient = new Color(0.04f, 0.04f, 0.08f, 1f);

        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDur);

            RenderSettings.ambientLight = Color.Lerp(savedAmbientLight, targetAmbient, t);

            foreach (var item in savedLightIntensities)
            {
                if (item.Item1 != null)
                {
                    item.Item1.intensity = Mathf.Lerp(item.Item2, item.Item2 * 0.15f, t);
                }
            }
            yield return null;
        }

        // 4. Instancier le Halo de lumière céleste au-dessus de l'ennemi
        if (verdictHaloObj != null) Destroy(verdictHaloObj);

        verdictHaloObj = new GameObject("Verdict_HaloLightBeam");
        verdictHaloObj.transform.position = targetPos;

        // Spotlight 3D dirigé vers le sol
        GameObject spotObj = new GameObject("Verdict_Spotlight");
        spotObj.transform.SetParent(verdictHaloObj.transform, false);
        spotObj.transform.position = targetPos + Vector3.up * 7f;
        spotObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        verdictSpotlight = spotObj.AddComponent<Light>();
        verdictSpotlight.type = LightType.Spot;
        verdictSpotlight.range = 14f;
        verdictSpotlight.spotAngle = 36f;
        verdictSpotlight.intensity = 8f;
        verdictSpotlight.color = new Color(1f, 0.95f, 0.75f, 1f);

        // Faisceau visuel vertical (procédural)
        GameObject beamObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beamObj.name = "Verdict_BeamVisual";
        beamObj.transform.SetParent(verdictHaloObj.transform, false);
        beamObj.transform.position = targetPos + Vector3.up * 3.5f;
        beamObj.transform.localScale = new Vector3(2.4f, 3.5f, 2.4f);

        Collider c = beamObj.GetComponent<Collider>();
        if (c != null) Destroy(c);

        MeshRenderer mr = beamObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = new Color(1f, 0.92f, 0.65f, 0.25f);
        }
    }

    private IEnumerator RestoreSceneLightingRoutine()
    {
        // Nettoyer le halo
        if (verdictHaloObj != null)
        {
            Destroy(verdictHaloObj);
            verdictHaloObj = null;
        }

        // Fondu de restauration de l'éclairage de la scène
        float elapsed = 0f;
        float fadeDur = 0.8f;
        Color currentAmbient = RenderSettings.ambientLight;

        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDur);

            RenderSettings.ambientLight = Color.Lerp(currentAmbient, savedAmbientLight, t);

            foreach (var item in savedLightIntensities)
            {
                if (item.Item1 != null)
                {
                    item.Item1.intensity = Mathf.Lerp(item.Item1.intensity, item.Item2, t);
                }
            }
            yield return null;
        }

        RenderSettings.ambientLight = savedAmbientLight;
        foreach (var item in savedLightIntensities)
        {
            if (item.Item1 != null)
            {
                item.Item1.intensity = item.Item2;
            }
        }
    }

    private IEnumerator RunVerdictChoiceRoutine(System.Action<int> callback)
    {
        // 1. Déterminer le parent UI Canvas
        Transform parentTransform = (runtimeUIContainer != null) ? runtimeUIContainer.transform : (combatCanvas != null ? combatCanvas.transform : transform);

        // 2. Créer le conteneur principal du Verdict
        GameObject vPanel = new GameObject("VerdictPanel");
        vPanel.transform.SetParent(parentTransform, false);

        RectTransform vRect = vPanel.AddComponent<RectTransform>();
        vRect.anchorMin = Vector2.zero;
        vRect.anchorMax = Vector2.one;
        vRect.sizeDelta = Vector2.zero;

        // Titre de jugement
        GameObject titleObj = new GameObject("VerdictTitle");
        titleObj.transform.SetParent(vPanel.transform, false);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        if (customCombatFont != null) titleTxt.font = customCombatFont;
        titleTxt.text = "BALANCE DE LA JUSTICE\n<size=22>QUEL EST VOTRE JUGEMENT ?</size>";
        titleTxt.fontSize = 32f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(1f, 0.95f, 0.8f, 1f);

        RectTransform tRect = titleTxt.rectTransform;
        tRect.anchorMin = new Vector2(0.2f, 0.76f);
        tRect.anchorMax = new Vector2(0.8f, 0.94f);
        tRect.sizeDelta = Vector2.zero;

        // --- GRAPHISME DE LA BALANCE DE LA JUSTICE ---
        GameObject balanceRoot = new GameObject("JusticeScale_Root");
        balanceRoot.transform.SetParent(vPanel.transform, false);
        RectTransform scaleRootRect = balanceRoot.AddComponent<RectTransform>();
        scaleRootRect.anchorMin = new Vector2(0.42f, 0.35f);
        scaleRootRect.anchorMax = new Vector2(0.58f, 0.72f);
        scaleRootRect.sizeDelta = Vector2.zero;

        // Base/Pied vertical de la balance
        GameObject baseObj = new GameObject("Scale_Base");
        baseObj.transform.SetParent(balanceRoot.transform, false);
        Image baseImg = baseObj.AddComponent<Image>();
        baseImg.sprite = uiFillSprite;
        baseImg.color = new Color(0.18f, 0.16f, 0.14f, 0.95f);
        RectTransform bRect = baseImg.rectTransform;
        bRect.anchorMin = new Vector2(0.46f, 0.05f);
        bRect.anchorMax = new Vector2(0.54f, 0.85f);
        bRect.sizeDelta = Vector2.zero;

        // Socle horizontal du bas
        GameObject socleObj = new GameObject("Scale_Socle");
        socleObj.transform.SetParent(balanceRoot.transform, false);
        Image socleImg = socleObj.AddComponent<Image>();
        socleImg.sprite = uiFillSprite;
        socleImg.color = new Color(0.85f, 0.75f, 0.45f, 1f); // Or doré
        RectTransform sRect = socleImg.rectTransform;
        sRect.anchorMin = new Vector2(0.15f, 0.0f);
        sRect.anchorMax = new Vector2(0.85f, 0.06f);
        sRect.sizeDelta = Vector2.zero;

        // Barre pivotante (Beam) de la balance
        GameObject beamObj = new GameObject("Scale_Beam");
        beamObj.transform.SetParent(balanceRoot.transform, false);
        RectTransform beamRect = beamObj.AddComponent<RectTransform>();
        beamRect.anchorMin = new Vector2(0.5f, 0.8f);
        beamRect.anchorMax = new Vector2(0.5f, 0.8f);
        beamRect.sizeDelta = new Vector2(260f, 14f);
        beamRect.anchoredPosition = Vector2.zero;

        Image beamImg = beamObj.AddComponent<Image>();
        beamImg.sprite = uiFillSprite;
        beamImg.color = new Color(0.9f, 0.8f, 0.45f, 1f); // Doré brillant

        // Plateau Gauche (Gracier)
        GameObject leftPan = new GameObject("Pan_Left");
        leftPan.transform.SetParent(beamObj.transform, false);
        RectTransform leftPanRect = leftPan.AddComponent<RectTransform>();
        leftPanRect.anchoredPosition = new Vector2(-120f, -40f);
        leftPanRect.sizeDelta = new Vector2(70f, 12f);
        Image lpImg = leftPan.AddComponent<Image>();
        lpImg.sprite = uiFillSprite;
        lpImg.color = new Color(0.2f, 0.8f, 0.35f, 0.9f); // Vert clémence

        // Filin Gauche
        GameObject leftString = new GameObject("String_Left");
        leftString.transform.SetParent(beamObj.transform, false);
        RectTransform lsRect = leftString.AddComponent<RectTransform>();
        lsRect.anchoredPosition = new Vector2(-120f, -20f);
        lsRect.sizeDelta = new Vector2(3f, 40f);
        Image lsImg = leftString.AddComponent<Image>();
        lsImg.sprite = uiFillSprite;
        lsImg.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);

        // Plateau Droit (Condamner)
        GameObject rightPan = new GameObject("Pan_Right");
        rightPan.transform.SetParent(beamObj.transform, false);
        RectTransform rightPanRect = rightPan.AddComponent<RectTransform>();
        rightPanRect.anchoredPosition = new Vector2(120f, -40f);
        rightPanRect.sizeDelta = new Vector2(70f, 12f);
        Image rpImg = rightPan.AddComponent<Image>();
        rpImg.sprite = uiFillSprite;
        rpImg.color = new Color(0.9f, 0.15f, 0.15f, 0.9f); // Rouge condamnation

        // Filin Droit
        GameObject rightString = new GameObject("String_Right");
        rightString.transform.SetParent(beamObj.transform, false);
        RectTransform rsRect = rightString.AddComponent<RectTransform>();
        rsRect.anchoredPosition = new Vector2(120f, -20f);
        rsRect.sizeDelta = new Vector2(3f, 40f);
        Image rsImg = rightString.AddComponent<Image>();
        rsImg.sprite = uiFillSprite;
        rsImg.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);

        // --- BOUTONS GRACIER (GAUCHE) ET CONDAMNER (DROITE) ---
        int selectedIndex = 0; // 0 = GRACIER (Gauche), 1 = CONDAMNER (Droite)
        bool chosen = false;

        Button gracierBtn = CreateRPGButton("GracierBtn", "GRACIER\n<size=14>(Grâce Céleste)</size>", vPanel.transform, 
            new Vector2(0.12f, 0.35f), new Vector2(0.38f, 0.52f), 
            () => { selectedIndex = 0; chosen = true; }, -3f, 0f);

        Button condamnerBtn = CreateRPGButton("CondamnerBtn", "CONDAMNER\n<size=14>(Sentence Suprême)</size>", vPanel.transform, 
            new Vector2(0.62f, 0.35f), new Vector2(0.88f, 0.52f), 
            () => { selectedIndex = 1; chosen = true; }, 3f, 0f);

        RepositionRPGButton(condamnerBtn, new Vector2(0.62f, 0.35f), new Vector2(0.88f, 0.52f));

        if (EventSystem.current != null && gracierBtn != null)
        {
            EventSystem.current.SetSelectedGameObject(gracierBtn.gameObject);
        }

        // Boucle d'attente d'entrée joueur avec animation de la balance
        float enterTime = Time.time;
        while (!chosen)
        {
            // Entrées Clavier / Gamepad pour switcher entre Gauche et Droite
            bool leftPressed = false;
            bool rightPressed = false;
            bool submitPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) leftPressed = true;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) rightPressed = true;
                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) submitPressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame) leftPressed = true;
                if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame) rightPressed = true;
                if (gp.buttonSouth.wasPressedThisFrame) submitPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q)) leftPressed = true;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) rightPressed = true;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)) submitPressed = true;
#endif

            if (leftPressed)
            {
                selectedIndex = 0;
                if (EventSystem.current != null && gracierBtn != null) EventSystem.current.SetSelectedGameObject(gracierBtn.gameObject);
            }
            else if (rightPressed)
            {
                selectedIndex = 1;
                if (EventSystem.current != null && condamnerBtn != null) EventSystem.current.SetSelectedGameObject(condamnerBtn.gameObject);
            }

            // Détection du survol de souris
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                if (EventSystem.current.currentSelectedGameObject == condamnerBtn.gameObject) selectedIndex = 1;
                else if (EventSystem.current.currentSelectedGameObject == gracierBtn.gameObject) selectedIndex = 0;
            }

            if (submitPressed && Time.time - enterTime > 0.3f)
            {
                chosen = true;
            }

            // --- ANIMATION D'INCLINAISON DE LA BALANCE DE LA JUSTICE ---
            // Gracier (Gauche) penche la balance vers la gauche (-18 degrés), Condamner (Droite) vers la droite (+18 degrés)
            float targetBeamAngle = (selectedIndex == 0) ? -18f : 18f;

            // Oscillations légères comme une vraie balance
            targetBeamAngle += Mathf.Sin(Time.time * 3f) * 1.5f;

            beamRect.localRotation = Quaternion.Slerp(beamRect.localRotation, Quaternion.Euler(0f, 0f, targetBeamAngle), Time.deltaTime * 7f);

            // Conserver les plateaux de la balance toujours verticaux
            leftPanRect.localRotation = Quaternion.Euler(0f, 0f, -beamRect.localRotation.eulerAngles.z);
            rightPanRect.localRotation = Quaternion.Euler(0f, 0f, -beamRect.localRotation.eulerAngles.z);

            yield return null;
        }

        // Nettoyer l'UI du Verdict
        Destroy(vPanel);

        callback?.Invoke(selectedIndex);
    }

    private IEnumerator AnimateEnemyAscension(GameObject enemyObj)
    {
        if (enemyObj == null) yield break;

        // Déclencher animation d'idle ou lévitation si disponible
        Animator enemyAnim = enemyObj.GetComponent<Animator>();
        if (enemyAnim == null) enemyAnim = enemyObj.GetComponentInChildren<Animator>();
        if (enemyAnim != null)
        {
            enemyAnim.Play("idle");
        }

        SpriteRenderer[] sprites = enemyObj.GetComponentsInChildren<SpriteRenderer>();
        Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>();

        Vector3 startPos = enemyObj.transform.position;
        Vector3 targetAscentPos = startPos + Vector3.up * 8f; // Monter de 8m dans le faisceau lumineux

        // Créer un nuage d'étincelles célestes qui s'élèvent
        GameObject sparkCloud = new GameObject("Ascension_SparkCloud");
        sparkCloud.transform.position = startPos + Vector3.up * 0.5f;

        for (int i = 0; i < 30; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.transform.SetParent(sparkCloud.transform, false);
            spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);

            Collider c = spark.GetComponent<Collider>();
            if (c != null) Destroy(c);

            MeshRenderer mr = spark.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = new Color(1f, 0.95f, 0.7f, 0.9f); // Doré lumineux
            }

            spark.transform.localPosition = Random.insideUnitSphere * 0.8f;
            StartCoroutine(AnimateAshParticle(spark.transform, mr));
        }

        float elapsed = 0f;
        float duration = 2.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Mouvement vertical vers le haut à travers le halo
            float easeUp = Mathf.Pow(t, 2f); // Accélération vers le haut
            enemyObj.transform.position = Vector3.Lerp(startPos, targetAscentPos, easeUp);

            // Teinte dorée & Fondu de transparence céleste (Alpha 1.0 -> 0.0)
            float alpha = Mathf.Lerp(1.0f, 0.0f, t);
            Color goldFadeColor = new Color(1f, 0.96f, 0.8f, alpha);

            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = goldFadeColor;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = goldFadeColor;
                }
            }

            yield return null;
        }

        Destroy(sparkCloud);
        Destroy(enemyObj);
    }

    #endregion

    private IEnumerator DefeatRoutine()
    {
        currentState = CombatState.Defeat;
        logText.text = "Tout le groupe a succombé...";

        yield return new WaitForSeconds(2.5f);
        yield return StartCoroutine(EndCombatRoutine(false));
    }

    #region Tutoriel Jardinier & Déplacements

    private DialogueData GetTutorialDialogueOrDefault(DialogueData data, string defaultSpeaker, string defaultText)
    {
        if (data != null && data.nodes != null && data.nodes.Length > 0) return data;

        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
        DialogueNode node = new DialogueNode();
        node.nodeID = "0";
        node.characterName = defaultSpeaker;
        node.sentence = defaultText;
        temp.nodes = new DialogueNode[] { node };
        return temp;
    }

    private IEnumerator RunDodgeTutorialAndTransition(DialogueData dialogueData)
    {
        isGardenerInterventionActive = true;
        currentPhase = CombatPhase.DodgePhase;

        // 1. Conserver la caméra à la distance d'esquive d'origine (AVANT tout changement de plan)
        cameraDistance = originalCameraDistance;
        cameraHeight = originalCameraHeight;

        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);

        // 2. Attendre la fin complète du dernier battement d'esquive et l'impact des projectiles
        float beatDurationSeconds = 60f / (activeCombatData != null ? activeCombatData.Bpm : musicBpm);
        yield return new WaitForSeconds(beatDurationSeconds * 1.2f);

        // 3. Nettoyer les alertes restantes
        ClearAllTelegraphs();

        // 4. Déplacer le Jardinier vers le joueur et lancer son dialogue (AVANT le changement de plan caméra et avant le menu)
        if (dialogueData != null)
        {
            yield return StartCoroutine(MoveGardenerToPlayerAndRunDialogue(dialogueData));
        }

        // 5. Passer la main au tour du joueur, changer le plan caméra et ouvrir le menu de combat !
        isGardenerInterventionActive = false;
        TransitionToPlayerTurn();
    }

    /// <summary>
    /// Déplace le Jardinier présent dans la scène vers le joueur pour lui parler, puis le ramène à sa position initiale.
    /// </summary>
    private IEnumerator MoveGardenerToPlayerAndRunDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null) yield break;

        isGardenerInterventionActive = true;
        currentPhase = CombatPhase.DodgePhase;

        // Bloquer les mouvements du joueur sur la grille pendant tout le déplacement et le dialogue du Jardinier
        if (playerController != null) playerController.SetInputEnabled(false);

        // Masquer le menu RPG et maintenir la caméra en vue d'esquive large pendant toute l'intervention du Jardinier
        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);
        cameraDistance = originalCameraDistance;
        cameraHeight = originalCameraHeight;

        // Rechercher le Jardinier dans la scène
        Transform gardenerTransform = null;
        GameObject gardenerObj = GameObject.Find("Gardener");
        if (gardenerObj != null)
        {
            gardenerTransform = gardenerObj.transform;
        }
        else
        {
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.Contains("Gardener") && go.activeInHierarchy)
                {
                    gardenerTransform = go.transform;
                    break;
                }
            }
        }

        if (gardenerTransform == null)
        {
            Debug.LogWarning("[RhythmCombatManager] Jardinier introuvable dans la scène pour le dialogue tutoriel. Ouverture directe du dialogue.");
            if (DialogueManager.Instance != null)
            {
                bool finished = false;
                DialogueManager.Instance.StartDialogue(dialogueData, () => finished = true);
                while (!finished) yield return null;
            }
            isGardenerInterventionActive = false;
            yield break;
        }

        SpriteRenderer gardenerSprite = gardenerTransform.GetComponent<SpriteRenderer>();
        if (gardenerSprite == null) gardenerSprite = gardenerTransform.GetComponentInChildren<SpriteRenderer>();

        Animator gardenerAnimator = gardenerTransform.GetComponent<Animator>();
        if (gardenerAnimator == null) gardenerAnimator = gardenerTransform.GetComponentInChildren<Animator>();

        Vector3 originalPos = gardenerTransform.position;
        Quaternion originalRot = gardenerTransform.rotation;

        Transform playerTransform = playerController != null ? playerController.transform : null;
        if (playerTransform == null && GroupManager.Instance != null) playerTransform = GroupManager.Instance.Leader;

        Vector3 targetPos = originalPos;
        if (playerTransform != null)
        {
            float targetY = Mathf.Max(originalPos.y, playerTransform.position.y + 0.8f);
            targetPos = new Vector3(
                playerTransform.position.x + 2.2f,
                targetY,
                playerTransform.position.z + 0.8f
            );
        }

        // 1. Déplacement vers le joueur
        if (gardenerAnimator != null) gardenerAnimator.Play("levitate");
        if (gardenerSprite != null) gardenerSprite.flipX = targetPos.x > gardenerTransform.position.x;

        float moveSpeed = 4.5f;
        float dist = Vector3.Distance(gardenerTransform.position, targetPos);
        if (dist > 0.1f)
        {
            float duration = dist / moveSpeed;
            float elapsed = 0f;
            Vector3 startPos = gardenerTransform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += Mathf.Sin(elapsed * 8f) * 0.15f; // Lévitation
                gardenerTransform.position = current;
                yield return null;
            }
        }
        gardenerTransform.position = targetPos;

        if (gardenerAnimator != null) gardenerAnimator.Play("idle");
        if (gardenerSprite != null && playerTransform != null)
        {
            gardenerSprite.flipX = playerTransform.position.x > gardenerTransform.position.x;
        }

        // 2. Lancement du dialogue
        bool dialogueFinished = false;
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueData, () => dialogueFinished = true);
            while (!dialogueFinished) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // 3. Retour à la position d'origine
        if (gardenerAnimator != null) gardenerAnimator.Play("levitate");
        if (gardenerSprite != null) gardenerSprite.flipX = originalPos.x > gardenerTransform.position.x;

        dist = Vector3.Distance(gardenerTransform.position, originalPos);
        if (dist > 0.1f)
        {
            float duration = dist / moveSpeed;
            float elapsed = 0f;
            Vector3 startPos = gardenerTransform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 current = Vector3.Lerp(startPos, originalPos, t);
                current.y += Mathf.Sin(elapsed * 8f) * 0.15f;
                gardenerTransform.position = current;
                yield return null;
            }
        }

        gardenerTransform.position = originalPos;
        gardenerTransform.rotation = originalRot;
        if (gardenerAnimator != null) gardenerAnimator.Play("idle");
        if (gardenerSprite != null)
        {
            // Se retourner pour refaire face au joueur/centre de la scène après être revenu à sa place
            if (playerTransform != null)
            {
                gardenerSprite.flipX = playerTransform.position.x > gardenerTransform.position.x;
            }
            else
            {
                gardenerSprite.flipX = false;
            }
        }
        isGardenerInterventionActive = false;
    }

    #endregion

    private IEnumerator EndCombatRoutine(bool victory)
    {
        currentState = CombatState.Transitioning;

        // Fondu de sortie fluide de la musique de combat
        yield return StartCoroutine(FadeOutBeatManager(0.8f));

        // Restauration fluide de la musique d'exploration originale
        if (previousAudioSource != null && previousMusicClip != null)
        {
            previousAudioSource.clip = previousMusicClip;
            previousAudioSource.time = previousMusicTime;
            previousAudioSource.gameObject.SetActive(true);
            previousAudioSource.enabled = true;
            StartCoroutine(FadeInAudioSource(previousAudioSource, previousMusicVolume, 0.8f));
        }

        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(1f));

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

        // 5. Restauration complète de la caméra d'origine (distance, hauteur, orientation et Cinemachine)
        cameraDistance = originalCameraDistance;
        cameraHeight = originalCameraHeight;

        if (Camera.main != null)
        {
            Camera.main.transform.position = originalMainCamPos;
            Camera.main.transform.rotation = originalMainCamRot;
            Camera.main.fieldOfView = originalMainCamFOV;
        }

        Transform targetLeader = GroupManager.Instance != null && GroupManager.Instance.Leader != null 
            ? GroupManager.Instance.Leader 
            : null;

        if (targetLeader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) targetLeader = pm.transform;
        }

        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (virtualCamera != null)
        {
            virtualCamera.enabled = true;

            if (targetLeader != null)
            {
                virtualCamera.Follow = targetLeader;
            }

            if (cameraHelper == null)
            {
                cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
                if (cameraHelper == null) cameraHelper = virtualCamera.GetComponentInChildren<CinemachineHelper>();
            }

            if (cameraHelper != null)
            {
                cameraHelper.enabled = true;
                if (targetLeader != null) cameraHelper.SetTargetPlayer(targetLeader);
                cameraHelper.UpdateCameraSettings(false); // Restaure l'offset, la hauteur, la rotation et le FOV d'origine
            }
        }

        if (brain != null)
        {
            brain.enabled = true;
        }

        // 6. Réactiver les compagnons et le mouvement normal
        PlayerLockManager.SetPlayerLocked(false);

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(0f));

        currentState = CombatState.Transitioning;
        activeEnemy = null;
        Debug.Log("[RhythmCombatManager] Combat terminé !");
    }

    #endregion

    #region Algorithme de Recherche et Positionnement Dégagé

    private Vector3 FindSafeCombatCenter(Vector3 initialCenter, float arenaRadius = 4.5f)
    {
        LayerMask obstacleLayers = LayerMask.GetMask("Default", "Environment", "Obstacle", "Solid", "Wall");
        // Si aucun obstacle n'est détecté ET que la position repose sur du sol ferme hors du vide
        if (!Physics.CheckSphere(initialCenter, arenaRadius, obstacleLayers) && IsGroundValidForArena(initialCenter, arenaRadius))
        {
            return initialCenter;
        }

        // Recherche en cercles concentriques extérieurs d'une zone dégagée et sécurisée
        int steps = 12;
        float stepDistance = 1.5f;
        int maxRings = 6;

        for (int ring = 1; ring <= maxRings; ring++)
        {
            float radius = ring * stepDistance;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * (2f * Mathf.PI / steps);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 candidate = initialCenter + offset;
                candidate = SnapToGround(candidate);

                if (!Physics.CheckSphere(candidate, arenaRadius, obstacleLayers) && IsGroundValidForArena(candidate, arenaRadius))
                {
                    Debug.Log($"[RhythmCombatManager] Zone de combat dégagée et sécurisée trouvée à {candidate} (décalage de {radius}m).");
                    return candidate;
                }
            }
        }

        Debug.LogWarning("[RhythmCombatManager] Impossible de trouver une zone de combat 100% dégagée et hors du vide. Utilisation du centre initial.");
        return initialCenter;
    }

    /// <summary>
    /// Vérifie que le centre et la circonférence de la grille reposent sur du sol solide (pas au-dessus du vide).
    /// </summary>
    private bool IsGroundValidForArena(Vector3 center, float checkRadius)
    {
        LayerMask groundLayers = ~0 & ~(1 << LayerMask.NameToLayer("Ignore Raycast"));

        // 1. Vérifier le centre
        Vector3 centerRayOrigin = center + Vector3.up * 5f;
        if (!Physics.Raycast(centerRayOrigin, Vector3.down, 15f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // 2. Vérifier 8 points sur le périmètre
        int samplePoints = 8;
        float sampleRadius = checkRadius * 0.8f;
        for (int i = 0; i < samplePoints; i++)
        {
            float angle = i * (2f * Mathf.PI / samplePoints);
            Vector3 samplePos = center + new Vector3(Mathf.Cos(angle) * sampleRadius, 5f, Mathf.Sin(angle) * sampleRadius);
            if (!Physics.Raycast(samplePos, Vector3.down, 15f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                return false; // Un point de l'arène tombe dans le vide !
            }
        }

        return true;
    }

    #endregion

    #region Helpers Fondu Audio (Crossfade)

    private IEnumerator FadeOutAudioSource(AudioSource source, float duration)
    {
        if (source == null) yield break;
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        if (source != null)
        {
            source.Pause();
            source.volume = startVol;
        }
    }

    private IEnumerator FadeInAudioSource(AudioSource source, float targetVol, float duration)
    {
        if (source == null) yield break;
        source.volume = 0f;
        if (!source.isPlaying) source.Play();
        float elapsed = 0f;
        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }
        if (source != null) source.volume = targetVol;
    }

    private IEnumerator FadeInBeatManager(float targetVol, float duration)
    {
        if (BeatManager.Instance == null) yield break;
        BeatManager.Instance.Volume = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            BeatManager.Instance.Volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }
        BeatManager.Instance.Volume = targetVol;
    }

    private IEnumerator FadeOutBeatManager(float duration)
    {
        if (BeatManager.Instance == null) yield break;
        float startVol = BeatManager.Instance.Volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            BeatManager.Instance.Volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        BeatManager.Instance.StopMusic();
        BeatManager.Instance.Volume = startVol;
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
            targetCamPos = playerPos - dirToCenter * playerTurnCameraDistance - leftShoulderDir * playerTurnCameraLeftOffset + Vector3.up * playerTurnCameraHeight;
            targetCamRot = Quaternion.LookRotation((center + Vector3.up * 1.5f) - targetCamPos);
            // Angle néerlandais (Z-tilt) stylisé et paramétré
            targetCamRot = targetCamRot * Quaternion.Euler(0f, 0f, playerTurnCameraTiltZ);
        }
        else if (currentPhase == CombatPhase.QTEActive)
        {
            // 2. QTE Actif (Attaque) : Vue de profil cinématique (midpoint face-à-face)
            Vector3 profileDir = Vector3.Cross(Vector3.up, dirToCenter).normalized;
            Vector3 midPoint = (playerPos + center) * 0.5f;
            targetCamPos = midPoint + profileDir * attackCameraDistance + Vector3.up * attackCameraHeight;
            targetCamRot = Quaternion.LookRotation(midPoint - targetCamPos);
            // Z-tilt stylisé et paramétré
            targetCamRot = targetCamRot * Quaternion.Euler(0f, 0f, attackCameraTiltZ);
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

            // Configurer le CanvasScaler pour la résolution indépendante (1920x1080, Match 0.5)
            CanvasScaler customScaler = combatCanvas.GetComponent<CanvasScaler>();
            if (customScaler == null) customScaler = combatCanvas.gameObject.AddComponent<CanvasScaler>();
            customScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            customScaler.referenceResolution = new Vector2(1920, 1080);
            customScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            customScaler.matchWidthOrHeight = 0.5f;

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
        
        // Valeurs d'ancrage configurables dans l'Inspecteur (par défaut Middle Center : 0.32, 0.47 à 0.68, 0.55)
        groupRect.anchorMin = playerHPAnchorMin;
        groupRect.anchorMax = playerHPAnchorMax;
        groupRect.sizeDelta = Vector2.zero;
        groupRect.anchoredPosition = Vector2.zero;
        
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
            if (playerHPBackgroundMaterial != null) allyBg.material = playerHPBackgroundMaterial;

            // 4. Remplissage PV (Crayon de couleur rouge ou gris)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(allyPanel.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = uiFillSprite;
            fillImg.color = i == activeAllyIndex ? new Color(0.85f, 0.08f, 0.14f, 0.9f) : new Color(0.5f, 0.5f, 0.52f, 0.4f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (playerHPFillMaterial != null) fillImg.material = playerHPFillMaterial;
            else if (playerHPBackgroundMaterial != null) fillImg.material = playerHPBackgroundMaterial;

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

            // Forcer le Canvas de la QTE au tout premier plan (devant tout le reste)
            Canvas customQteCanvas = customQtePanel.GetComponent<Canvas>();
            if (customQteCanvas == null) customQteCanvas = customQtePanel.gameObject.AddComponent<Canvas>();
            customQteCanvas.overrideSorting = true;
            customQteCanvas.sortingOrder = 99999;
            customQteCanvas.planeDistance = 0.1f;

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

            if (customQteInstructionText != null) qteInstructionText = customQteInstructionText;
            if (customQteFeedbackText != null) qteFeedbackText = customQteFeedbackText;

            customQtePanel.gameObject.SetActive(false); // Masqué au début
        }
        else
        {
            // --- QTE PANEL (Helldivers 2 - Arrow Only UI - Foreground Layer & Monochrome Black/White/Gray) ---
            GameObject qteBorder = new GameObject("QteBorder");
            qteBorder.transform.SetParent(parent, false);
            
            // Forcer l'affichage au tout premier plan (devant le décor 3D et autres UI)
            Canvas qteCanvas = qteBorder.AddComponent<Canvas>();
            qteCanvas.overrideSorting = true;
            qteCanvas.sortingOrder = 99999;
            qteCanvas.planeDistance = 0.1f;
            qteBorder.AddComponent<GraphicRaycaster>();

            RectTransform qteBRect = qteBorder.GetComponent<RectTransform>();
            qteBRect.anchorMin = new Vector2(0.38f, 0.28f); // Rapproché et centré
            qteBRect.anchorMax = new Vector2(0.62f, 0.40f);
            qteBRect.sizeDelta = Vector2.zero;

            qteArrowCardFills.Clear();
            qteArrowTexts.Clear();

            int count = qteComboSequence != null && qteComboSequence.Count > 0 ? qteComboSequence.Count : 5;
            float cardWidthPct = 1f / count;

            for (int i = 0; i < count; i++)
            {
                float minX = i * cardWidthPct;
                float maxX = (i + 1) * cardWidthPct;

                GameObject arrowTxtObj = new GameObject($"ArrowText_{i}");
                arrowTxtObj.transform.SetParent(qteBorder.transform, false);
                TextMeshProUGUI arrowTxt = arrowTxtObj.AddComponent<TextMeshProUGUI>();
                if (customCombatFont != null) arrowTxt.font = customCombatFont;
                ArrowDirection dir = (qteComboSequence != null && i < qteComboSequence.Count) ? qteComboSequence[i] : ArrowDirection.Right;
                arrowTxt.text = GetArrowSymbol(dir);
                arrowTxt.fontSize = 62f; // Plus grand et plus visible
                arrowTxt.fontStyle = FontStyles.Bold;
                arrowTxt.color = new Color(0.25f, 0.25f, 0.28f, 0.65f); // Gris charbon translucide au départ
                arrowTxt.alignment = TextAlignmentOptions.Center;

                // Epais contour noir très net pour visibilité maximale devant n'importe quel décor 3D
                arrowTxt.outlineWidth = 0.38f;
                arrowTxt.outlineColor = new Color(0.04f, 0.04f, 0.06f, 1f);

                RectTransform atRect = arrowTxt.rectTransform;
                atRect.anchorMin = new Vector2(minX, 0f);
                atRect.anchorMax = new Vector2(maxX, 1f);
                atRect.sizeDelta = Vector2.zero;
                qteArrowTexts.Add(arrowTxt);
            }

            // Feedback Text discret
            GameObject qteFeedObj = new GameObject("QteFeedbackText");
            qteFeedObj.transform.SetParent(qteBorder.transform, false);
            qteFeedbackText = qteFeedObj.AddComponent<TextMeshProUGUI>();
            if (customCombatFont != null) qteFeedbackText.font = customCombatFont;
            qteFeedbackText.text = "";
            qteFeedbackText.fontSize = 20f;
            qteFeedbackText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            qteFeedbackText.alignment = TextAlignmentOptions.Center;
            qteFeedbackText.color = new Color(0.95f, 0.95f, 1f, 1f); // Blanc argenté
            qteFeedbackText.outlineWidth = 0.35f;
            qteFeedbackText.outlineColor = new Color(0.04f, 0.04f, 0.06f, 1f);

            RectTransform feedRect = qteFeedbackText.rectTransform;
            feedRect.anchorMin = new Vector2(0.05f, -0.4f);
            feedRect.anchorMax = new Vector2(0.95f, -0.05f);
            feedRect.sizeDelta = Vector2.zero;

            qteBorder.SetActive(false); // Masqué au début
            qtePanel = qteBorder;
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

        Canvas diaCanvas = diaBorder.AddComponent<Canvas>();
        diaCanvas.overrideSorting = true;
        diaCanvas.sortingOrder = 9999;
        diaBorder.AddComponent<GraphicRaycaster>();

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
        diaTextObj.AddComponent<TypewriterEffects>();
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
        if (Time.time < menuEnableTime) return;

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
        tutorialPlayerTurnCount++;
        menuEnableTime = Time.time + menuInputSecurityDelay;

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

        // Afficher l'UI du menu RPG et la barre de vie
        if (rpgMenuPanel != null)
        {
            CanvasGroup cg = rpgMenuPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = rpgMenuPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            rpgMenuPanel.SetActive(true);

            // Désactiver temporairement tous les boutons pendant l'animation d'entrée et la sécurité d'input
            SetCombatMenuButtonsInteractable(false);

            // Forcer le recalcul immédiat du layout
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rpgMenuPanel.GetComponent<RectTransform>());

            if (customMenuPanel == null)
            {
                RepositionRPGButton(attackButton, new Vector2(0.02f, 0.15f), new Vector2(0.24f, 0.85f));
                RepositionRPGButton(talkButton, new Vector2(0.26f, 0.15f), new Vector2(0.48f, 0.85f));
                RepositionRPGButton(companionsButton, new Vector2(0.50f, 0.15f), new Vector2(0.72f, 0.85f));
                RepositionRPGButton(fleeButton, new Vector2(0.74f, 0.15f), new Vector2(0.96f, 0.85f));
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

        // Attendre la fin du délai de sécurité d'input avant de réactiver l'interactivité
        yield return new WaitForSeconds(menuInputSecurityDelay);

        SetCombatMenuButtonsInteractable(true);

        if (EventSystem.current != null)
        {
            Button defaultBtn = attackButton != null ? attackButton : customFightButton;
            if (defaultBtn != null) EventSystem.current.SetSelectedGameObject(defaultBtn.gameObject);
        }
    }

    private void TransitionToDodgePhase()
    {
        isGardenerInterventionActive = false;
        currentPhase = CombatPhase.DodgePhase;
        dodgeBeatsCount = 0;

        // Remettre l'orientation normale et la danse du joueur
        RestorePlayerOrientation();
        if (!string.IsNullOrEmpty(danceAnimationStateName))
        {
            PlayPlayerAnimation(danceAnimationStateName);
        }

        // Restaurer la caméra aux distances initiales de combat
        cameraDistance = originalCameraDistance;
        cameraHeight = originalCameraHeight;

        if (combatCanvas != null && combatCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            combatCanvas.planeDistance = customMenuPlaneDistance;
        }

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
            radialGrid.ClearAllWarnings();
        }
        activeTelegraphs.Clear();
        groundOnlyTelegraphs.Clear();
    }

    private string GetArrowSymbol(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up: return "▲";
            case ArrowDirection.Down: return "▼";
            case ArrowDirection.Left: return "◄";
            case ArrowDirection.Right: return "►";
            default: return "►";
        }
    }

    private void PlayPlayerAnimation(string animState)
    {
        if (string.IsNullOrEmpty(animState)) return;

        Animator playerAnim = null;
        if (playerController != null)
        {
            playerAnim = playerController.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = playerController.GetComponentInChildren<Animator>();
        }
        if (playerAnim == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerAnim = pm.GetComponent<Animator>();
                if (playerAnim == null) playerAnim = pm.GetComponentInChildren<Animator>();
            }
        }

        if (playerAnim != null)
        {
            playerAnim.Play(animState);
        }
    }

    private void SetPlayerShootingState(bool shooting)
    {
        if (playerController != null)
        {
            playerController.IsShootingAnimation = shooting;
        }
        else
        {
            RhythmPlayerController rpc = FindFirstObjectByType<RhythmPlayerController>();
            if (rpc != null) rpc.IsShootingAnimation = shooting;
        }
    }

    private void OrientPlayerTowardsEnemy()
    {
        SetPlayerShootingState(true);

        Transform playerTrans = (activeAllyIndex >= 0 && activeAllyIndex < allies.Count && allies[activeAllyIndex] != null) 
            ? allies[activeAllyIndex] 
            : null;

        if (playerTrans == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerTrans = pm.transform;
        }

        if (playerTrans != null)
        {
            SpriteRenderer sr = playerTrans.GetComponent<SpriteRenderer>();
            if (sr == null) sr = playerTrans.GetComponentInChildren<SpriteRenderer>();
            
            if (sr != null)
            {
                Vector3 enemyPos = activeEnemy != null ? activeEnemy.transform.position : (radialGrid != null ? radialGrid.transform.position : transform.position);
                Camera mainCam = Camera.main;
                bool enemyOnScreenLeft = mainCam != null ? mainCam.WorldToScreenPoint(enemyPos).x < mainCam.WorldToScreenPoint(playerTrans.position).x : enemyPos.x < playerTrans.position.x;
                
                sr.flipX = enemyOnScreenLeft;
            }
        }
    }

    private void RestorePlayerOrientation()
    {
        SetPlayerShootingState(false);

        Transform playerTrans = (activeAllyIndex >= 0 && activeAllyIndex < allies.Count && allies[activeAllyIndex] != null) 
            ? allies[activeAllyIndex] 
            : null;

        if (playerTrans == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerTrans = pm.transform;
        }

        if (playerTrans != null)
        {
            SpriteRenderer sr = playerTrans.GetComponent<SpriteRenderer>();
            if (sr == null) sr = playerTrans.GetComponentInChildren<SpriteRenderer>();
            
            if (sr != null)
            {
                Vector3 enemyPos = activeEnemy != null ? activeEnemy.transform.position : (radialGrid != null ? radialGrid.transform.position : transform.position);
                Camera mainCam = Camera.main;
                bool enemyOnScreenLeft = mainCam != null ? mainCam.WorldToScreenPoint(enemyPos).x < mainCam.WorldToScreenPoint(playerTrans.position).x : enemyPos.x < playerTrans.position.x;
                
                // Pour l'animation "dance" (qui pointe vers la droite par défaut),
                // si l'ennemi est à gauche à l'écran, flipX = true.
                sr.flipX = enemyOnScreenLeft;
            }
        }
    }

    private void SetCombatMenuButtonsInteractable(bool interactable)
    {
        bool otherAlive = false;
        for (int i = 0; i < allies.Count; i++)
        {
            if (i != activeAllyIndex && allyHP[i] > 0)
            {
                otherAlive = true;
                break;
            }
        }

        bool isTutorial = activeCombatData != null && activeCombatData.IsGardenerTutorial;

        bool canFight;
        bool canTalk;
        bool canCompanions;
        bool canFlee;

        if (isTutorial)
        {
            canFlee = false; // Escape est TOUJOURS désactivé durant ce combat tutoriel

            if (tutorialPlayerTurnCount == 1)
            {
                // Après le 2ème dialogue (1ère esquive) : UNIQUEMENT Talk (Parler)
                canFight = false;
                canTalk = interactable;
                canCompanions = false;
            }
            else if (tutorialPlayerTurnCount == 2)
            {
                // Après le 3ème dialogue (2ème esquive) : UNIQUEMENT Fight (Attaquer)
                canFight = interactable;
                canTalk = false;
                canCompanions = false;
            }
            else
            {
                // Tours 3+ : Combat normal (Fight, Talk, Companions), sauf Escape qui reste désactivé
                canFight = interactable;
                canTalk = interactable;
                canCompanions = interactable && otherAlive;
            }
        }
        else
        {
            canFight = interactable;
            canTalk = interactable;
            canCompanions = interactable && otherAlive;
            canFlee = interactable;
        }

        if (attackButton != null) attackButton.interactable = canFight;
        if (talkButton != null) talkButton.interactable = canTalk;
        if (companionsButton != null) companionsButton.interactable = canCompanions;
        if (fleeButton != null) fleeButton.interactable = canFlee;

        if (customFightButton != null) customFightButton.interactable = canFight;
        if (customTalkButton != null) customTalkButton.interactable = canTalk;
        if (customCompanionButton != null) customCompanionButton.interactable = canCompanions;
        if (customEscapeButton != null) customEscapeButton.interactable = canFlee;
    }

    private void StartQTE()
    {
        if (Time.time < menuEnableTime) return;

        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);

        // Masquer le panneau QTE au début pendant l'animation de shoot
        if (qtePanel != null) qtePanel.SetActive(false);

        qteResolved = false;
        qteCurrentIndex = 0;
        qteUIWaitingForAnim = true;
        currentPhase = CombatPhase.QTEActive;

        if (qteFeedbackText != null) qteFeedbackText.text = "";

        // Réinitialiser la couleur et la taille des flèches
        for (int i = 0; i < qteArrowTexts.Count; i++)
        {
            if (qteArrowTexts[i] != null)
            {
                qteArrowTexts[i].color = new Color(0.2f, 0.2f, 0.25f, 0.6f);
                qteArrowTexts[i].rectTransform.localScale = Vector3.one;
            }
        }

        // Orienter dynamiquement le tir du joueur vers l'ennemi (peu importe le secteur de l'arène)
        OrientPlayerTowardsEnemy();

        // Déclencher l'animation shoot du joueur
        Animator playerAnim = null;
        if (playerController != null)
        {
            playerAnim = playerController.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = playerController.GetComponentInChildren<Animator>();
        }
        if (playerAnim == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerAnim = pm.GetComponent<Animator>();
                if (playerAnim == null) playerAnim = pm.GetComponentInChildren<Animator>();
            }
        }

        if (playerAnim != null && !string.IsNullOrEmpty(shootAnimationStateName))
        {
            playerAnim.Play(shootAnimationStateName);
        }

        // 3. Faire apparaître l'UI QTE à la fin de l'animation de tir
        StartCoroutine(ShowQTEAfterShootRoutine());
    }

    private IEnumerator ShowQTEAfterShootRoutine()
    {
        yield return new WaitForSeconds(shootAnimationDelay);

        qteUIWaitingForAnim = false;
        qteStartTime = Time.time;
        qteStartFrame = Time.frameCount;

        if (qtePanel != null) qtePanel.SetActive(true);
    }

    private void UpdateQTE()
    {
        if (qteResolved || qteUIWaitingForAnim) return;

        // Vérifier le temps écoule
        float elapsed = Time.time - qteStartTime;
        if (elapsed >= qteTimeLimit)
        {
            StartCoroutine(ResolveHelldiversQTERoutine(false));
            return;
        }

        // Anti-flash input frame 1
        if (Time.frameCount == qteStartFrame || elapsed < 0.1f) return;

        ArrowDirection? inputDir = GetCurrentDirectionalInput();
        if (inputDir.HasValue)
        {
            ArrowDirection targetDir = (qteComboSequence != null && qteCurrentIndex < qteComboSequence.Count) 
                ? qteComboSequence[qteCurrentIndex] 
                : ArrowDirection.Right;

            if (inputDir.Value == targetDir)
            {
                OnCorrectArrowInput();
            }
            else
            {
                StartCoroutine(ResolveHelldiversQTERoutine(false));
            }
        }
    }

    private ArrowDirection? GetCurrentDirectionalInput()
    {
        bool up = false, down = false, left = false, right = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame) up = true;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) down = true;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) left = true;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) right = true;
        }

        var gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame) up = true;
            if (gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame) down = true;
            if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame) left = true;
            if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame) right = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Z)) up = true;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) down = true;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q)) left = true;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) right = true;
#endif

        if (up) return ArrowDirection.Up;
        if (down) return ArrowDirection.Down;
        if (left) return ArrowDirection.Left;
        if (right) return ArrowDirection.Right;

        return null;
    }

    private void OnCorrectArrowInput()
    {
        if (qteCurrentIndex < qteArrowTexts.Count && qteArrowTexts[qteCurrentIndex] != null)
        {
            qteArrowTexts[qteCurrentIndex].color = new Color(1.0f, 1.0f, 1.0f, 1f); // Blanc brillant monochrome
            qteArrowTexts[qteCurrentIndex].outlineColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            qteArrowTexts[qteCurrentIndex].outlineWidth = 0.45f;
            StartCoroutine(AnimateCardPulse(qteArrowTexts[qteCurrentIndex].rectTransform));
        }

        qteCurrentIndex++;

        int totalCount = qteComboSequence != null ? qteComboSequence.Count : 5;
        if (qteCurrentIndex >= totalCount)
        {
            StartCoroutine(ResolveHelldiversQTERoutine(true));
        }
    }

    private IEnumerator AnimateCardPulse(RectTransform cardRect)
    {
        if (cardRect == null) yield break;
        Vector3 origScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.28f, 1.28f, 1.28f);
        float elapsed = 0f;
        float duration = 0.08f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardRect.localScale = Vector3.Lerp(origScale, targetScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardRect.localScale = Vector3.Lerp(targetScale, origScale, elapsed / duration);
            yield return null;
        }

        cardRect.localScale = origScale;
    }

    private IEnumerator ResolveHelldiversQTERoutine(bool success)
    {
        qteResolved = true;

        if (success)
        {
            if (qteFeedbackText != null)
            {
                qteFeedbackText.text = "COMBINAISON REUSSIE !";
                qteFeedbackText.color = new Color(0.95f, 0.95f, 1.0f, 1f); // Blanc argenté monochrome
            }

            logText.text = "Combinaison réussie ! Préparation du tir...";

            // Masquer l'UI de QTE dès la réussite
            if (qtePanel != null) qtePanel.SetActive(false);

            // 1. Déclencher l'animation shoot 2 du joueur
            if (!string.IsNullOrEmpty(qteSuccessShootAnimationStateName))
            {
                PlayPlayerAnimation(qteSuccessShootAnimationStateName);
            }

            // 2. Attendre la fin de l'animation shoot 2 avant que le projectile ne parte
            yield return new WaitForSeconds(qteSuccessShootDelay);

            logText.text = "Tir d'encre parti !";

            // Calcul du point de tir du doigt
            Vector3 spawnPos;
            if (playerFingerTip != null)
            {
                spawnPos = playerFingerTip.position;
            }
            else
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                Transform pTrans = pm != null ? pm.transform : transform;
                spawnPos = pTrans.position + Vector3.up * 1.2f + pTrans.forward * 0.5f;
            }

            // Instancier le projectile d'encre
            GameObject projObj;
            if (inkProjectilePrefab != null)
            {
                projObj = Instantiate(inkProjectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projObj = new GameObject("InkProjectile_Instance");
                projObj.transform.position = spawnPos;
                projObj.AddComponent<InkProjectile>();
            }

            InkProjectile inkScript = projObj.GetComponent<InkProjectile>();
            if (inkScript == null) inkScript = projObj.AddComponent<InkProjectile>();

            bool impactDone = false;
            Transform targetTransform = activeEnemy != null ? activeEnemy.transform : transform;

            inkScript.Launch(targetTransform, () =>
            {
                impactDone = true;
            });

            yield return new WaitUntil(() => impactDone);

            // Effet d'impact complet sur l'ennemi (Flash rouge/clignotement comme le joueur + animation Hit)
            if (activeEnemy != null)
            {
                StartCoroutine(AnimateEnemyHitEffect(activeEnemy));
            }

            // Tremblement de la caméra d'impact
            ShakeCamera(0.25f, 0.3f);

            // Appliquer les dégâts
            enemyHP = Mathf.Max(0, enemyHP - 50);
            UpdateUI();

            // Pop-up de dégâts gribouillé sur l'ennemi (police custom du jeu)
            if (activeEnemy != null)
            {
                DamageNumberPopup.Create(activeEnemy.transform.position + Vector3.up * 1.2f, 50, isPlayerDamage: false, fontAsset: customCombatFont);
            }

            if (attackSuccessParticles != null && activeEnemy != null)
            {
                ParticleSystem ps = Instantiate(attackSuccessParticles, activeEnemy.transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 1.0f);
            }

            logText.text = "Le projectile d'encre touche l'ennemi ! Dégâts : 50";

            yield return new WaitForSeconds(0.8f);

            // Remettre l'orientation normale et relancer l'animation dance
            RestorePlayerOrientation();
            PlayPlayerAnimation(danceAnimationStateName);

            if (enemyHP <= 0)
            {
                StartCoroutine(VictoryRoutine());
            }
            else
            {
                TransitionToDodgePhase();
            }
        }
        else
        {
            if (qteFeedbackText != null)
            {
                qteFeedbackText.text = "RATE !";
                qteFeedbackText.color = new Color(0.85f, 0.12f, 0.14f, 1f);
            }

            // Flasher les flèches en rouge
            for (int i = qteCurrentIndex; i < qteArrowTexts.Count; i++)
            {
                if (qteArrowTexts[i] != null)
                {
                    qteArrowTexts[i].color = new Color(0.85f, 0.12f, 0.14f, 1f);
                }
            }

            logText.text = "Combinaison ratée ! Attaque annulée.";

            if (qtePanel != null)
            {
                StartCoroutine(ShakeTransform(qtePanel.transform, 0.35f, 8f));
            }

            yield return new WaitForSeconds(0.8f);

            if (qtePanel != null) qtePanel.SetActive(false);

            // Remettre l'orientation normale et relancer l'animation dance
            RestorePlayerOrientation();
            PlayPlayerAnimation(danceAnimationStateName);

            TransitionToDodgePhase();
        }
    }

    private IEnumerator ShakeTransform(Transform trans, float duration, float strength)
    {
        Vector3 origPos = trans.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);
            trans.localPosition = origPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }
        trans.localPosition = origPos;
    }

    public void ShakeCamera(float duration = 0.25f, float intensity = 0.3f)
    {
        StartCoroutine(ShakeCameraRoutine(duration, intensity));
    }

    private IEnumerator ShakeCameraRoutine(float duration, float intensity)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        Vector3 origPos = mainCam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-intensity, intensity);
            float offsetY = Random.Range(-intensity, intensity);
            mainCam.transform.localPosition = origPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        mainCam.transform.localPosition = origPos;
    }

    private IEnumerator AnimateEnemyHitEffect(GameObject enemyObj)
    {
        if (enemyObj == null) yield break;

        // 1. Déclencher l'animation Hit
        Animator enemyAnim = enemyObj.GetComponent<Animator>();
        if (enemyAnim == null) enemyAnim = enemyObj.GetComponentInChildren<Animator>();
        if (enemyAnim != null && !string.IsNullOrEmpty(enemyHitAnimationName))
        {
            enemyAnim.SetTrigger(enemyHitAnimationName);
            enemyAnim.Play(enemyHitAnimationName);
        }

        // 2. Clignotement / flash rouge sur le sprite de l'ennemi (identique au joueur)
        SpriteRenderer[] sprites = enemyObj.GetComponentsInChildren<SpriteRenderer>();
        Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>();

        Color redFlash = new Color(1f, 0.2f, 0.2f, 1f);
        Vector3 origPos = enemyObj.transform.localPosition;

        float elapsed = 0f;
        float duration = 0.45f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float blink = Mathf.PingPong(elapsed * 25f, 1f);
            Color currentColor = blink > 0.5f ? redFlash : Color.white;

            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = currentColor;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = currentColor;
                }
            }

            // Secousse d'impact sur l'ennemi
            float offsetX = Random.Range(-0.12f, 0.12f);
            float offsetY = Random.Range(-0.12f, 0.12f);
            enemyObj.transform.localPosition = origPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Restaurer la couleur et position d'origine
        enemyObj.transform.localPosition = origPos;
        foreach (SpriteRenderer sr in sprites)
        {
            if (sr != null) sr.color = Color.white;
        }
        foreach (Renderer r in renderers)
        {
            if (r != null && r.material.HasProperty("_Color"))
            {
                r.material.color = Color.white;
            }
        }
    }

    private void StartDialogue()
    {
        if (Time.time < menuEnableTime) return;

        if (rpgMenuPanel != null) rpgMenuPanel.SetActive(false);
        if (groupContainerObj != null) groupContainerObj.SetActive(false);

        // Jouer l'animation facedance du joueur pendant la discussion
        if (!string.IsNullOrEmpty(faceDanceAnimationStateName))
        {
            PlayPlayerAnimation(faceDanceAnimationStateName);
        }

        // Positionner le plan du Canvas tout près de l'objectif de la caméra (0.5m) pour passer devant tout objet 3D
        if (combatCanvas != null && combatCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            combatCanvas.planeDistance = 0.5f;
        }

        if (DialogueManager.Instance != null && activeCombatData != null && activeCombatData.TalkDialogues != null && activeCombatData.TalkDialogues.Count > 0)
        {
            currentPhase = CombatPhase.DialogueActive;

            // Déterminer l'index de la réplique (1 réplique par action PARLER, puis boucle sur la dernière)
            int lineIndex = Mathf.Min(currentTalkDialogueStep, activeCombatData.TalkDialogues.Count - 1);
            currentTalkDialogueStep++;

            // Créer un DialogueData d'une seule réplique
            DialogueData tempDialogue = ScriptableObject.CreateInstance<DialogueData>();
            DialogueNode node = new DialogueNode();
            node.nodeID = "0";
            node.characterName = activeCombatData.EnemyName;
            node.portrait = null;
            node.sentence = activeCombatData.TalkDialogues[lineIndex];
            node.nextNodeID = null; // Une seule ligne par commande PARLER
            node.choices = null;
            tempDialogue.nodes = new DialogueNode[] { node };

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (logText != null) logText.text = "Discussion engagée avec " + activeCombatData.EnemyName;

            DialogueManager.Instance.StartDialogue(tempDialogue, () =>
            {
                TransitionToDodgePhase();
            });
        }
        else if (activeCombatData != null && activeCombatData.TalkDialogues != null && activeCombatData.TalkDialogues.Count > 0)
        {
            // Fallback si DialogueManager n'est pas présent dans la scène
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            currentPhase = CombatPhase.DialogueActive;
            dialogueEnterTime = Time.time;

            int lineIndex = Mathf.Min(currentTalkDialogueStep, activeCombatData.TalkDialogues.Count - 1);
            currentTalkDialogueStep++;

            currentDialogueIndex = 9999; // Se ferme au prochain clic
            if (logText != null) logText.text = "Discussion engagée avec " + activeCombatData.EnemyName;

            TypewriterEffects typewriter = dialogueText != null ? dialogueText.GetComponent<TypewriterEffects>() : null;
            if (typewriter == null && dialogueText != null)
            {
                typewriter = dialogueText.gameObject.AddComponent<TypewriterEffects>();
            }

            if (typewriter != null)
            {
                typewriter.StartTyping(activeCombatData.TalkDialogues[lineIndex]);
            }
            else if (dialogueText != null)
            {
                dialogueText.text = activeCombatData.TalkDialogues[lineIndex];
            }
        }
        else
        {
            // Fallback si aucun dialogue n'est configuré
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            currentPhase = CombatPhase.DialogueActive;
            dialogueEnterTime = Time.time;
            currentDialogueIndex = 9999;

            string msg = "Vous tentez de parler, mais l'ennemi ne semble pas disposé à discuter...";
            if (logText != null) logText.text = "Aucun dialogue disponible.";

            TypewriterEffects typewriter = dialogueText != null ? dialogueText.GetComponent<TypewriterEffects>() : null;
            if (typewriter == null && dialogueText != null)
            {
                typewriter = dialogueText.gameObject.AddComponent<TypewriterEffects>();
            }

            if (typewriter != null)
            {
                typewriter.StartTyping(msg);
            }
            else if (dialogueText != null)
            {
                dialogueText.text = msg;
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
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)) advancePressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) advancePressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) advancePressed = true;

        if (advancePressed)
        {
            TypewriterEffects typewriter = dialogueText != null ? dialogueText.GetComponent<TypewriterEffects>() : null;
            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.Skip();
                return;
            }

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
                    string nextSentence = activeCombatData.TalkDialogues[currentDialogueIndex];
                    dialogueEnterTime = Time.time; // réinitialiser le cooldown
                    if (typewriter != null)
                    {
                        typewriter.StartTyping(nextSentence);
                    }
                    else
                    {
                        dialogueText.text = nextSentence;
                    }
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

    private IEnumerator AnimateEnemyDisintegration(GameObject enemyObj)
    {
        if (enemyObj == null) yield break;

        // Déclencher animation de mort/hit si disponible
        Animator enemyAnim = enemyObj.GetComponent<Animator>();
        if (enemyAnim == null) enemyAnim = enemyObj.GetComponentInChildren<Animator>();
        if (enemyAnim != null)
        {
            enemyAnim.SetTrigger("Die");
        }

        SpriteRenderer[] sprites = enemyObj.GetComponentsInChildren<SpriteRenderer>();
        Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>();

        Vector3 origScale = enemyObj.transform.localScale;
        Vector3 origPos = enemyObj.transform.localPosition;

        // Générer des particules de désintégration (cendres / encre qui s'élèvent)
        GameObject ashCloudObj = new GameObject("DisintegrationAshCloud");
        ashCloudObj.transform.position = enemyObj.transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < 28; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.SetParent(ashCloudObj.transform, false);
            float scale = Random.Range(0.06f, 0.18f);
            particle.transform.localScale = Vector3.one * scale;

            Collider c = particle.GetComponent<Collider>();
            if (c != null) Destroy(c);

            MeshRenderer mr = particle.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            }

            Vector3 randomOffset = Random.insideUnitSphere * 0.6f;
            particle.transform.localPosition = randomOffset;
            StartCoroutine(AnimateAshParticle(particle.transform, mr));
        }

        // Animation de dissolution / écrasement / déformation de désintégration
        float elapsed = 0f;
        float duration = 1.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Translucidité (Alpha 1.0 -> 0.0)
            float alpha = Mathf.Lerp(1.0f, 0.0f, t);
            Color fadeColor = new Color(0.1f, 0.1f, 0.12f, alpha);

            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = fadeColor;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = fadeColor;
                }
            }

            // Écrasement / étirement chaotique de désintégration
            float scaleX = Mathf.Lerp(origScale.x, origScale.x * 1.5f, t);
            float scaleY = Mathf.Lerp(origScale.y, 0.02f, t);
            float scaleZ = Mathf.Lerp(origScale.z, origScale.z * 1.5f, t);
            enemyObj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            // Tremblement de désintégration
            float shakeX = Random.Range(-0.1f, 0.1f) * (1f - t);
            float shakeY = Random.Range(-0.1f, 0.1f) * (1f - t);
            enemyObj.transform.localPosition = origPos + new Vector3(shakeX, shakeY, 0f);

            yield return null;
        }

        Destroy(ashCloudObj, 1.5f);
        Destroy(enemyObj);
    }

    private IEnumerator AnimateAshParticle(Transform particleTrans, MeshRenderer mr)
    {
        Vector3 startPos = particleTrans.localPosition;
        Vector3 floatDir = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(1.2f, 2.5f), Random.Range(-0.8f, 0.8f));
        float elapsed = 0f;
        float duration = Random.Range(0.8f, 1.4f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (particleTrans != null)
            {
                particleTrans.localPosition = startPos + floatDir * t;
                particleTrans.localScale = Vector3.Lerp(particleTrans.localScale, Vector3.zero, t);
            }

            if (mr != null && mr.material.HasProperty("_Color"))
            {
                Color c = mr.material.color;
                c.a = Mathf.Lerp(0.9f, 0f, t);
                mr.material.color = c;
            }

            yield return null;
        }
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

        // Toujours forcer la couleur de texte initiale sur blanc (lisible sur fond sombre)
        originalTextColor = Color.white;
        targetTextColor = Color.white;
        if (buttonText != null)
        {
            buttonText.color = Color.white;
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
        targetTextColor = Color.white; // Toujours écriture blanche très lisible quand l'option n'est PAS sélectionnée !

        if (shadowImage != null)
        {
            shadowImage.color = new Color(0.9f, 0.9f, 0.92f, 0.6f);
        }
    }
}
