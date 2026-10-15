using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinématique d'interaction avec un banc :
/// 1. Le joueur interagit avec le banc (touche E / manette).
/// 2. Le joueur saute sur le banc pour s'asseoir (arc parabolique fluide).
/// 3. La caméra effectue un plan cinématique centré sur le banc.
/// 4. Un premier dialogue se déclenche (DialogueData ou bulle de texte).
/// 5. Un personnage situé à gauche s'avance vers la droite et s'arrête devant le banc / la fleur.
/// 6. Ce personnage parle tout en changeant de sprite.
/// 7. La fleur assise sur le banc joue son animation de despawn (DevoveoDespawn).
/// 8. Le personnage reprend son idle, puis dit un second dialogue.
/// 9. Le personnage reprend sa marche vers la droite et quitte la scène.
/// 10. Le joueur ressaute du banc au sol et le jeu reprend son cours normal.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Bench Cinematic Trigger")]
public class BenchCinematicTrigger : Interactable
{
    [Header("Configuration Déclencheur")]
    [Tooltip("La cinématique ne peut-elle être jouée qu'une seule fois ?")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("Flag optionnel à activer dans StoryStateManager une fois la cinématique terminée.")]
    [SerializeField] private string flagToSetOnComplete = "";

    [Header("Dialogue de Choix Préalable (S'asseoir ?)")]
    [Tooltip("Afficher un dialogue avec choix avant de s'asseoir sur le banc.")]
    [SerializeField] private bool askBeforeSitting = true;

    [Tooltip("Asset DialogueData personnalisé contenant la question et les choix. Si vide, génère automatiquement un dialogue 'Voulez-vous vous asseoir ?' avec les choix Oui / Non.")]
    [SerializeField] private DialogueData promptDialogueData;

    [Tooltip("Texte de la question affichée si promptDialogueData n'est pas renseigné.")]
    [SerializeField] private string promptQuestionText = "Voulez-vous vous asseoir sur le banc ?";

    [Tooltip("Texte du choix d'acceptation (ex: 'Oui').")]
    [SerializeField] private string confirmChoiceText = "Oui";

    [Tooltip("Texte du choix de refus (ex: 'Non').")]
    [SerializeField] private string cancelChoiceText = "Non";

    [Header("Saut du Joueur sur le Banc")]
    [Tooltip("Point précis sur le banc où le joueur doit atterrir pour s'asseoir.")]
    [SerializeField] private Transform seatPoint;

    [Tooltip("Point au sol où le joueur atterrit en ressortant du banc (si vide, utilise la position initiale du joueur).")]
    [SerializeField] private Transform exitSeatPoint;

    [Tooltip("Durée du saut vers le banc (en secondes).")]
    [SerializeField] private float jumpToSeatDuration = 0.6f;

    [Tooltip("Hauteur maximale de l'arc parabolique lors du saut vers le banc (en mètres).")]
    [SerializeField] private float jumpToSeatHeight = 0.8f;

    [Tooltip("Durée du saut pour descendre du banc (en secondes).")]
    [SerializeField] private float jumpOffBenchDuration = 0.5f;

    [Tooltip("Hauteur maximale de l'arc parabolique lors de la descente (en mètres).")]
    [SerializeField] private float jumpOffBenchHeight = 0.7f;

    [Tooltip("Inverser l'orientation horizontale (flipX) du joueur une fois assis.")]
    [SerializeField] private bool flipPlayerOnSeat = false;

    [Tooltip("Sprite optionnel du joueur assis (laisser vide pour conserver le sprite actuel).")]
    [SerializeField] private Sprite playerSitSprite;

    [Tooltip("Nom de l'animation ou du trigger à jouer quand le joueur est assis (ex: 'sit' ou 'idle').")]
    [SerializeField] private string playerSitAnimName = "";

    [Tooltip("Clip audio de saut personnalisé (si vide, utilise le JumpSFX du PlayerMovement).")]
    [SerializeField] private AudioClip customJumpSFX;

    [Header("Cadrage Caméra Cinématique (Plan Large & Mouvement Très Lent)")]
    [Tooltip("CinemachineCamera dédiée au plan du banc (Optionnel : si assignée, sa priorité sera augmentée pour déclencher un blend fluide).")]
    [SerializeField] private CinemachineCamera benchVirtualCamera;

    [Tooltip("Cible Transform manuelle sur laquelle centrer la caméra (si vide, centre automatiquement au milieu géométrique du banc).")]
    [SerializeField] private Transform cameraFocusTarget;

    [Tooltip("Décalage vertical du point ciblé au milieu du banc (ex: 0.3m pour bien cadrer la scène).")]
    [SerializeField] private float focusHeightOffset = 0.3f;

    [Tooltip("Décalage horizontal (X relatif à la caméra) pour ajuster le centrage gauche/droite si besoin.")]
    [SerializeField] private float focusHorizontalOffset = 0f;

    [Tooltip("Distance de recul de la caméra par rapport au banc (plus la valeur est grande, plus la caméra dézoome. Ex: 14m pour un beau dézoom d'ensemble).")]
    [SerializeField] private float cinematicDistance = 14.0f;

    [Tooltip("Conserver strictement l'angle actuel de la caméra (recommandé : aucun changement d'inclinaison ni de rotation).")]
    [SerializeField] private bool keepCameraAngle = true;

    [Tooltip("Field Of View (FOV) cible lors du rapprochement (-1 pour conserver le FOV naturel actuel de la caméra).")]
    [SerializeField] private float targetFOV = -1f;

    [Tooltip("Distance de recul (Z) de secours si keepCameraAngle est décoché.")]
    [SerializeField] private float cameraDistance = 14f;

    [Tooltip("Hauteur (Y) de secours par rapport au banc si keepCameraAngle est décoché.")]
    [SerializeField] private float cameraHeight = 5f;

    [Tooltip("Inclinaison verticale de secours (X Axis) en degrés si keepCameraAngle est décoché.")]
    [Range(0f, 85f)]
    [SerializeField] private float cameraPitch = 22f;

    [Tooltip("Orientation horizontale de secours (Y Axis) en degrés (-1 pour conserver l'orientation actuelle).")]
    [SerializeField] private float cameraYaw = -1f;

    [Tooltip("Field Of View (FOV) de secours si keepCameraAngle est décoché.")]
    [Range(10f, 100f)]
    [SerializeField] private float cameraFOV = 36f;

    [Tooltip("Durée de la transition de dézoom très lent vers le banc (en secondes).")]
    [SerializeField] private float cameraTransitionInDuration = 4.5f;

    [Tooltip("Durée du retour très lent vers le joueur (en secondes).")]
    [SerializeField] private float cameraTransitionOutDuration = 4.0f;

    [Tooltip("Courbe de lissage pour une approche très douce et cinématographique.")]
    [SerializeField] private AnimationCurve cameraCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Premier Dialogue (Assis sur le banc)")]
    [Tooltip("Temps de pause une fois assis sur le banc avant que la caméra ne commence sa transition très lente (en secondes).")]
    [SerializeField] private float delayAfterSitBeforeCamera = 0.8f;

    [Tooltip("Délai d'attente après le cadrage de la caméra avant de déclencher le premier dialogue (en secondes).")]
    [SerializeField] private float delayBeforeFirstDialogue = 1.5f;

    [Tooltip("Premier dialogue à lancer (Asset ScriptableObject DialogueData).")]
    [SerializeField] private DialogueData firstDialogueData;

    [Tooltip("Texte alternatif pour bulle en direct au-dessus du joueur si DialogueData n'est pas utilisé.")]
    [SerializeField] private string firstDialogueBubbleText = "";

    [Tooltip("Temps d'attente après le premier dialogue avant l'arrivée du personnage (en secondes).")]
    [SerializeField] private float delayAfterFirstDialogue = 1.0f;

    [Header("Personnage Passant (Marche depuis la gauche)")]
    [Tooltip("Le GameObject du personnage qui vient de la gauche.")]
    [SerializeField] private GameObject passerbyCharacter;

    [Tooltip("Point de départ optionnel où placer le personnage au début (si vide, conserve sa position actuelle dans la scène).")]
    [SerializeField] private Transform passerbyStartPoint;

    [Tooltip("Point d'arrêt devant le banc où le personnage s'arrête au milieu.")]
    [SerializeField] private Transform passerbyStopPoint;

    [Tooltip("Vitesse de marche du personnage (unités/seconde).")]
    [SerializeField] private float passerbyWalkSpeed = 2.5f;

    [Tooltip("Composant Animator du personnage (recherché automatiquement si vide).")]
    [SerializeField] private Animator passerbyAnimator;

    [Tooltip("Nom de l'état ou du paramètre booléen de marche dans l'Animator (ex: 'Walk' ou 'isWalking').")]
    [SerializeField] private string passerbyWalkAnimName = "Walk";

    [Tooltip("Composant SpriteRenderer du personnage (recherché automatiquement si vide).")]
    [SerializeField] private SpriteRenderer passerbySpriteRenderer;

    [Tooltip("Le sprite de base du personnage regarde-t-il vers la DROITE ?")]
    [SerializeField] private bool passerbyFacesRightByDefault = true;

    [Header("Dialogue du Passant & Changement de Sprite")]
    [Tooltip("Sprite à afficher quand le personnage parle devant le banc.")]
    [SerializeField] private Sprite passerbyTalkSprite;

    [Tooltip("Nom d'animation optionnel quand le personnage parle (ex: 'Talk').")]
    [SerializeField] private string passerbyTalkAnimName = "";

    [Tooltip("Dialogue du personnage lorsqu'il parle avec son nouveau sprite.")]
    [SerializeField] private DialogueData passerbyFirstDialogueData;

    [Tooltip("Texte alternatif en bulle en direct pour le premier dialogue du personnage.")]
    [SerializeField] private string passerbyFirstDialogueBubbleText = "";

    [Tooltip("Délai après la prise de parole du personnage avant le despawn de la fleur (en secondes).")]
    [SerializeField] private float delayAfterPasserbyDialogue = 1.2f;

    [Header("Fleur sur le Banc (Despawn)")]
    [Tooltip("Le GameObject ou la racine du personnage fleur assis sur le banc.")]
    [SerializeField] private GameObject flowerCharacter;

    [Tooltip("L'Animator de la fleur (recherché automatiquement sur flowerCharacter si vide).")]
    [SerializeField] private Animator flowerAnimator;

    [Tooltip("Nom de l'animation de disparition de la fleur ('DevoveoDespawn' par défaut).")]
    [SerializeField] private string flowerDespawnAnimName = "DevoveoDespawn";

    [Tooltip("Durée d'attente de l'animation de la fleur en secondes.")]
    [SerializeField] private float flowerDespawnDuration = 2.0f;

    [Tooltip("Désactiver le GameObject de la fleur une fois son animation terminée ?")]
    [SerializeField] private bool deactivateFlowerOnDespawn = true;

    [Header("Passant : Retour en Idle & Second Dialogue")]
    [Tooltip("Sprite de repos (Idle) du personnage (si vide, réutilise le sprite qu'il avait avant de parler).")]
    [SerializeField] private Sprite passerbyIdleSprite;

    [Tooltip("Nom de l'animation Idle à rejouer sur le personnage (ex: 'Idle').")]
    [SerializeField] private string passerbyIdleAnimName = "Idle";

    [Tooltip("Délai d'attente avant le second dialogue du personnage (en secondes).")]
    [SerializeField] private float delayBeforePasserbySecondDialogue = 0.8f;

    [Tooltip("Second dialogue du personnage avant de repartir.")]
    [SerializeField] private DialogueData passerbySecondDialogueData;

    [Tooltip("Texte alternatif en bulle en direct pour le second dialogue.")]
    [SerializeField] private string passerbySecondDialogueBubbleText = "";

    [Tooltip("Temps d'attente après le second dialogue avant de reprendre sa route (en secondes).")]
    [SerializeField] private float delayAfterPasserbySecondDialogue = 1.0f;

    [Header("Passant : Départ vers la Droite")]
    [Tooltip("Point de destination vers la droite où le personnage se retire.")]
    [SerializeField] private Transform passerbyExitPoint;

    [Tooltip("Vitesse de départ du personnage vers la droite (plus rapide que sa vitesse d'arrivée, ex: 4.2m/s).")]
    [SerializeField] private float passerbyExitSpeed = 4.2f;

    [Tooltip("Nom de l'animation à jouer au moment du départ (si vide, reprend automatiquement l'animation qu'il avait en arrivant, ex: 'levitateP2').")]
    [SerializeField] private string passerbyExitAnimName = "";

    [Tooltip("Distance vers la droite parcourue si passerbyExitPoint n'est pas renseigné.")]
    [SerializeField] private float passerbyExitDistance = 14f;

    [Tooltip("Désactiver le personnage passant une fois arrivé à destination ?")]
    [SerializeField] private bool deactivatePasserbyOnExit = true;

    [Tooltip("Délai d'attente pendant la marche du personnage avant que le joueur ne descende du banc.")]
    [SerializeField] private float waitBeforePlayerJumpsOff = 0.9f;

    [Header("Chute de la Pomme du Ciel")]
    [Tooltip("Objet pomme situé dans le ciel à faire tomber lors du départ du personnage (si non assigné, recherche automatiquement 'Apple' ou 'Pomme' dans la scène).")]
    [SerializeField] private GameObject fallingApple;

    [Tooltip("Point de chute / atterrissage au sol (optionnel : si non assigné, calcule automatiquement le sol directement en dessous de la pomme par Raycast).")]
    [SerializeField] private Transform appleLandingPoint;

    [Tooltip("Figer la pomme dans le ciel au lancement du jeu pour l'empêcher de tomber avant la cinématique.")]
    [SerializeField] private bool holdAppleInSkyAtStart = true;

    [Tooltip("Accélération gravitationnelle de la chute (en m/s², défaut : 12 pour une chute plus douce et majestueuse).")]
    [SerializeField] private float appleGravity = 12f;

    [Tooltip("Vitesse maximale de chute (terminal velocity en m/s, ex: 8 pour une descente fluide).")]
    [SerializeField] private float appleMaxFallSpeed = 8f;

    [Tooltip("Vitesse verticale initiale lors du décrochage de la pomme (m/s).")]
    [SerializeField] private float appleInitialVelocity = 0f;

    [Tooltip("Délai d'attente après la descente du joueur du banc avant de déclencher la chute de la pomme (en secondes).")]
    [SerializeField] private float delayAfterPlayerLeavesBeforeAppleFall = 0.3f;

    [Tooltip("Attendre la fin complète de l'atterrissage de la pomme avant de relancer les commandes du joueur.")]
    [SerializeField] private bool waitForAppleToLand = false;

    [Tooltip("Effet sonore optionnel joué à l'impact de la pomme sur le sol (si vide, utilise le son de l'objet CarriableItem s'il existe).")]
    [SerializeField] private AudioClip appleLandSound;

    [Tooltip("Effet de particules optionnel joué à l'impact de la pomme.")]
    [SerializeField] private ParticleSystem appleLandParticles;

    [Tooltip("Activer un léger rebond / squash style jeu de plateforme lors de l'impact au sol.")]
    [SerializeField] private bool enableAppleImpactSquash = true;

    [Tooltip("Hauteur de chute du ciel minimale (en mètres) assurée même si la pomme était placée au niveau du sol au démarrage.")]
    [SerializeField] private float skyDropHeightFallback = 10f;

    [Header("Zone d'Interaction & Proximité")]
    [Tooltip("Rayon de détection (en mètres) autour du banc (ou du seatPoint) pour afficher l'icône E et permettre l'interaction.")]
    [SerializeField] private float interactionDistance = 3.5f;

    [Tooltip("Activer la détection automatique de proximité par distance en plus du trigger (indispensable pour les modèles 3D avec pivot décentré).")]
    [SerializeField] private bool useDistanceDetection = true;

    private bool sequenceRunning = false;
    private bool cinematicCompleted = false;
    private bool isSittingOnBench = false;
    private Vector3 currentSeatPos;
    private bool previousKinematic = false;
    private bool previousUseGravity = true;

    private bool isAppleFalling = false;
    private bool appleHasFallen = false;

    public GameObject FallingApple
    {
        get => fallingApple;
        set => fallingApple = value;
    }

    public Transform AppleLandingPoint
    {
        get => appleLandingPoint;
        set => appleLandingPoint = value;
    }

    public bool IsAppleFalling => isAppleFalling;
    public bool AppleHasFallen => appleHasFallen;

    private PlayerMovement playerMovement;
    private CinemachineCamera mainVirtualCamera;
    private CinemachineHelper mainCameraHelper;
    private Sprite savedPlayerSprite;
    private bool savedPlayerFlipX;
    private Sprite savedPasserbySprite;
    private string savedPasserbyArrivalAnim = "";

    /// <summary>
    /// Centre réel d'interaction du banc (priorise le seatPoint pour s'aligner sur la position assise et non sur le pivot du modèle).
    /// </summary>
    public override Vector3 InteractionCenter
    {
        get
        {
            if (seatPoint != null) return seatPoint.position;
            return base.InteractionCenter;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Si une ancienne valeur de zoom trop proche était sauvegardée (< 8m), appliquer le dézoom cinématique
        if (cinematicDistance < 8f)
        {
            cinematicDistance = 14.0f;
        }

        // Si les durées étaient encore sur l'ancien réglage rapide, appliquer la vitesse très lente
        if (cameraTransitionInDuration < 3.0f)
        {
            cameraTransitionInDuration = 4.5f;
        }

        if (cameraTransitionOutDuration < 3.0f)
        {
            cameraTransitionOutDuration = 4.0f;
        }

        // Vitesse de départ du passant plus rapide que sa vitesse d'arrivée
        if (passerbyExitSpeed <= passerbyWalkSpeed)
        {
            passerbyExitSpeed = Mathf.Max(4.2f, passerbyWalkSpeed * 1.6f);
        }

        // Si le flag d'histoire a déjà été validé, désactiver définitivement l'interaction dès le chargement
        if (!string.IsNullOrEmpty(flagToSetOnComplete) && StoryStateManager.Instance != null && StoryStateManager.Instance.GetFlag(flagToSetOnComplete))
        {
            cinematicCompleted = true;
            isPlayerInRange = false;
            HideIndicator();
        }

        ResolveReferences();

        // Si l'ancienne valeur rapide était enregistrée, basculer sur la chute plus douce
        if (appleGravity > 20f)
        {
            appleGravity = 12f;
        }

        if (holdAppleInSkyAtStart && fallingApple != null)
        {
            HoldAppleInSky();
        }
    }

    protected override void Update()
    {
        // Une fois la cinématique jouée, le banc devient totalement inactif
        if (cinematicCompleted)
        {
            if (isPlayerInRange)
            {
                isPlayerInRange = false;
                HideIndicator();
            }
            return;
        }
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        // Détection de proximité dynamique par distance
        if (useDistanceDetection && !sequenceRunning && playerMovement != null)
        {
            float dist = Vector3.Distance(InteractionCenter, playerMovement.transform.position);
            if (dist <= interactionDistance && CanInteract())
            {
                isPlayerInRange = true;
            }
            else if (dist > interactionDistance + 0.6f)
            {
                isPlayerInRange = false;
            }
        }

        base.Update();
    }

    private void LateUpdate()
    {
        // Ancrage strict sur le banc tant que le joueur est assis : neutralise la gravité et fige la position
        if (isSittingOnBench && playerMovement != null)
        {
            playerMovement.transform.position = currentSeatPos;
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                rb.useGravity = false;
            }
        }
    }

    protected virtual void OnDisable()
    {
        if (isSittingOnBench && playerMovement != null)
        {
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = previousKinematic;
                rb.useGravity = previousUseGravity;
            }
            isSittingOnBench = false;
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (cinematicCompleted) return;
        base.OnTriggerEnter(other);
    }

    protected override void OnTriggerStay(Collider other)
    {
        if (cinematicCompleted) return;
        base.OnTriggerStay(other);
    }

    public bool IsPlayerInRangeForDebug => isPlayerInRange && !cinematicCompleted;

    /// <summary>
    /// Permet de déclencher la cinématique manuellement depuis l'inspecteur pour tester.
    /// </summary>
    public void TriggerCinematicManually()
    {
        if (sequenceRunning || cinematicCompleted) return;
        Interact();
    }

    protected override bool CanInteract()
    {
        if (cinematicCompleted) return false;
        if (sequenceRunning) return false;
        if (oneShot && (hasInteracted || cinematicCompleted)) return false;
        return base.CanInteract();
    }

    protected override void Interact()
    {
        if (sequenceRunning) return;
        StartCoroutine(StartInteractionFlowRoutine());
    }

    private IEnumerator StartInteractionFlowRoutine()
    {
        sequenceRunning = true;
        ResolveReferences();

        // 1. Dialogue préalable avec choix (Oui / Non)
        if (askBeforeSitting)
        {
            HideIndicator();

            DialogueData promptData = promptDialogueData;
            bool createdRuntimeData = false;
            if (promptData == null)
            {
                promptData = CreateRuntimePromptDialogue();
                createdRuntimeData = true;
            }

            bool dialogueCompleted = false;
            bool userAccepted = false;

            System.Action<ChoiceData> choiceHandler = (choice) =>
            {
                string text = choice.text != null ? choice.text.Trim() : "";
                string nextId = choice.nextNodeID != null ? choice.nextNodeID.Trim() : "";

                if (text.Equals(confirmChoiceText, System.StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("Oui", System.StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("Yes", System.StringComparison.OrdinalIgnoreCase) ||
                    nextId.Equals("SIT_YES", System.StringComparison.OrdinalIgnoreCase) ||
                    (DialogueManager.Instance != null && DialogueManager.Instance.LastSelectedChoiceIndex == 0))
                {
                    userAccepted = true;
                }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnChoiceSelected += choiceHandler;
                DialogueManager.Instance.StartDialogue(promptData, () =>
                {
                    dialogueCompleted = true;
                });

                yield return new WaitUntil(() => dialogueCompleted);
                DialogueManager.Instance.OnChoiceSelected -= choiceHandler;
            }
            else
            {
                userAccepted = true; // Fallback si le DialogueManager n'est pas présent
            }

            if (createdRuntimeData && promptData != null)
            {
                Destroy(promptData);
            }

            if (!userAccepted)
            {
                // L'utilisateur a choisi "Non" : on annule et on lui redonne les commandes
                sequenceRunning = false;
                PlayerLockManager.SetPlayerLocked(false);
                yield break;
            }
        }

        // 2. L'utilisateur a répondu Oui : on lance la cinématique où il s'assoit sur le banc
        yield return StartCoroutine(ExecuteBenchSequenceRoutine());
    }

    private DialogueData CreateRuntimePromptDialogue()
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.nodes = new DialogueNode[1];
        data.nodes[0] = new DialogueNode
        {
            nodeID = "PROMPT_SIT",
            characterName = "",
            sentence = !string.IsNullOrEmpty(promptQuestionText) ? promptQuestionText : "Voulez-vous vous asseoir sur le banc ?",
            choices = new ChoiceData[]
            {
                new ChoiceData { text = !string.IsNullOrEmpty(confirmChoiceText) ? confirmChoiceText : "Oui", nextNodeID = "SIT_YES" },
                new ChoiceData { text = !string.IsNullOrEmpty(cancelChoiceText) ? cancelChoiceText : "Non", nextNodeID = "SIT_NO" }
            },
            nextNodeID = ""
        };
        return data;
    }

    private void ResolveReferences()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (mainVirtualCamera == null)
        {
            mainVirtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (mainVirtualCamera != null && mainCameraHelper == null)
        {
            mainCameraHelper = mainVirtualCamera.GetComponent<CinemachineHelper>() ?? mainVirtualCamera.GetComponentInChildren<CinemachineHelper>();
        }

        if (flowerCharacter != null && flowerAnimator == null)
        {
            flowerAnimator = flowerCharacter.GetComponent<Animator>() ?? flowerCharacter.GetComponentInChildren<Animator>();
        }

        if (passerbyCharacter != null)
        {
            if (passerbyAnimator == null)
            {
                passerbyAnimator = passerbyCharacter.GetComponent<Animator>() ?? passerbyCharacter.GetComponentInChildren<Animator>();
            }
            if (passerbySpriteRenderer == null)
            {
                passerbySpriteRenderer = passerbyCharacter.GetComponent<SpriteRenderer>() ?? passerbyCharacter.GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (fallingApple == null)
        {
            fallingApple = GameObject.Find("Apple") ?? GameObject.Find("Pomme");
            if (fallingApple == null)
            {
                CarriableItem[] items = FindObjectsByType<CarriableItem>(FindObjectsSortMode.None);
                foreach (var item in items)
                {
                    if (item != null && (item.gameObject.name.IndexOf("apple", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         item.gameObject.name.IndexOf("pomme", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        fallingApple = item.gameObject;
                        break;
                    }
                }
            }
        }
    }

    private IEnumerator ExecuteBenchSequenceRoutine()
    {
        sequenceRunning = true;
        ResolveReferences();

        if (playerMovement == null)
        {
            Debug.LogError("[BenchCinematicTrigger] Impossible de lancer la cinématique : PlayerMovement introuvable !");
            sequenceRunning = false;
            yield break;
        }

        Debug.Log($"[BenchCinematicTrigger] Début de la cinématique du banc sur '{gameObject.name}'");

        // 1. Verrouiller le joueur
        PlayerLockManager.SetPlayerLocked(true);
        HideIndicator();

        Rigidbody playerRb = playerMovement.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            previousKinematic = playerRb.isKinematic;
            previousUseGravity = playerRb.useGravity;
            playerRb.isKinematic = true;
            playerRb.useGravity = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        Vector3 groundOriginPos = playerMovement.transform.position;
        Vector3 targetSeatPos = (seatPoint != null) ? seatPoint.position : transform.position + Vector3.up * 0.5f;
        currentSeatPos = targetSeatPos;

        // 2. Saut parabolique du joueur sur le banc
        PlayJumpSound();
        yield return StartCoroutine(ParabolicJumpRoutine(playerMovement.transform, groundOriginPos, targetSeatPos, jumpToSeatHeight, jumpToSeatDuration));

        // Ancrage assis : désactiver la gravité et figer la position pour ne jamais traverser le banc
        isSittingOnBench = true;
        playerMovement.transform.position = targetSeatPos;
        if (playerRb != null)
        {
            playerRb.useGravity = false;
            playerRb.isKinematic = true;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Pose assise
        SpriteRenderer playerSR = playerMovement.GetComponent<SpriteRenderer>();
        if (playerSR != null)
        {
            savedPlayerSprite = playerSR.sprite;
            savedPlayerFlipX = playerSR.flipX;

            if (flipPlayerOnSeat)
            {
                playerSR.flipX = true;
            }
            if (playerSitSprite != null)
            {
                playerSR.sprite = playerSitSprite;
            }
        }

        Animator playerAnim = playerMovement.GetComponent<Animator>() ?? playerMovement.GetComponentInChildren<Animator>();
        if (playerAnim != null && !string.IsNullOrEmpty(playerSitAnimName))
        {
            PlayAnimationOn(playerAnim, playerSitAnimName);
        }

        // Pause après être assis avant le cadrage caméra
        if (delayAfterSitBeforeCamera > 0f)
        {
            yield return new WaitForSeconds(delayAfterSitBeforeCamera);
        }

        // 3. Plan cinématique de la caméra sur le banc
        Coroutine cameraInRoutine = StartCoroutine(TransitionCameraIn());
        yield return cameraInRoutine;

        // 4. Premier dialogue assis sur le banc : attendre un petit moment avant de le déclencher
        float preDialogueWait = Mathf.Max(0.5f, delayBeforeFirstDialogue);
        yield return new WaitForSeconds(preDialogueWait);

        yield return StartCoroutine(TriggerDialogueStep(firstDialogueData, firstDialogueBubbleText, playerMovement.transform));

        if (delayAfterFirstDialogue > 0f)
        {
            yield return new WaitForSeconds(delayAfterFirstDialogue);
        }

        // 5. Arrivée du personnage passant depuis la gauche vers le centre
        if (passerbyCharacter != null)
        {
            passerbyCharacter.SetActive(true);

            if (passerbyStartPoint != null)
            {
                passerbyCharacter.transform.position = passerbyStartPoint.position;
            }

            Vector3 passerbyTargetPos;
            if (passerbyStopPoint != null)
            {
                passerbyTargetPos = passerbyStopPoint.position;
            }
            else
            {
                // Par défaut, s'arrête légèrement devant le banc
                passerbyTargetPos = transform.position + new Vector3(0f, 0f, -1.2f);
            }

            if (passerbySpriteRenderer != null && savedPasserbySprite == null)
            {
                savedPasserbySprite = passerbySpriteRenderer.sprite;
            }

            savedPasserbyArrivalAnim = ResolvePasserbyMoveAnim(passerbyAnimator, passerbyWalkAnimName);
            yield return StartCoroutine(MovePasserbyTo(passerbyTargetPos, passerbyWalkSpeed, savedPasserbyArrivalAnim));

            // 6. Le personnage parle en changeant de sprite
            if (passerbySpriteRenderer != null)
            {
                savedPasserbySprite = passerbySpriteRenderer.sprite;
                if (passerbyTalkSprite != null)
                {
                    passerbySpriteRenderer.sprite = passerbyTalkSprite;
                }
            }

            if (passerbyAnimator != null && !string.IsNullOrEmpty(passerbyTalkAnimName))
            {
                PlayAnimationOn(passerbyAnimator, passerbyTalkAnimName);
            }

            yield return StartCoroutine(TriggerDialogueStep(passerbyFirstDialogueData, passerbyFirstDialogueBubbleText, passerbyCharacter.transform));

            if (delayAfterPasserbyDialogue > 0f)
            {
                yield return new WaitForSeconds(delayAfterPasserbyDialogue);
            }
        }

        // 7. Animation de despawn de la fleur
        if (flowerCharacter != null)
        {
            Debug.Log($"[BenchCinematicTrigger] Lancement de l'animation '{flowerDespawnAnimName}' sur la fleur.");
            if (flowerAnimator != null && !string.IsNullOrEmpty(flowerDespawnAnimName))
            {
                PlayAnimationOn(flowerAnimator, flowerDespawnAnimName);
            }

            float despawnWait = flowerDespawnDuration;
            if (flowerAnimator != null && flowerAnimator.runtimeAnimatorController != null)
            {
                foreach (var clip in flowerAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == flowerDespawnAnimName && clip.length > 0f)
                    {
                        despawnWait = clip.length;
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(despawnWait);

            if (deactivateFlowerOnDespawn)
            {
                flowerCharacter.SetActive(false);
            }
        }

        // 8. Le personnage reprend son Idle, puis dit son second dialogue
        if (passerbyCharacter != null)
        {
            if (passerbySpriteRenderer != null)
            {
                if (passerbyIdleSprite != null)
                {
                    passerbySpriteRenderer.sprite = passerbyIdleSprite;
                }
                else if (savedPasserbySprite != null)
                {
                    passerbySpriteRenderer.sprite = savedPasserbySprite;
                }
            }

            if (passerbyAnimator != null && !string.IsNullOrEmpty(passerbyIdleAnimName))
            {
                PlayAnimationOn(passerbyAnimator, passerbyIdleAnimName);
            }

            if (delayBeforePasserbySecondDialogue > 0f)
            {
                yield return new WaitForSeconds(delayBeforePasserbySecondDialogue);
            }

            yield return StartCoroutine(TriggerDialogueStep(passerbySecondDialogueData, passerbySecondDialogueBubbleText, passerbyCharacter.transform));

            if (delayAfterPasserbySecondDialogue > 0f)
            {
                yield return new WaitForSeconds(delayAfterPasserbySecondDialogue);
            }

            // 9. Le personnage reprend sa route vers la droite
            Vector3 exitPos;
            if (passerbyExitPoint != null)
            {
                exitPos = passerbyExitPoint.position;
            }
            else
            {
                exitPos = passerbyCharacter.transform.position + Vector3.right * passerbyExitDistance;
            }

            // Rétablir son sprite et son animation où il était quand il est arrivé (ex: levitateP2)
            if (passerbySpriteRenderer != null && savedPasserbySprite != null)
            {
                passerbySpriteRenderer.sprite = savedPasserbySprite;
            }

            string exitAnim = !string.IsNullOrEmpty(passerbyExitAnimName)
                ? passerbyExitAnimName
                : (!string.IsNullOrEmpty(savedPasserbyArrivalAnim) ? savedPasserbyArrivalAnim : passerbyWalkAnimName);
            exitAnim = ResolvePasserbyMoveAnim(passerbyAnimator, exitAnim);

            float exitSpeed = (passerbyExitSpeed > 0f) ? passerbyExitSpeed : (passerbyWalkSpeed * 1.6f);

            // Lancer le départ du passant plus rapidement en arrière-plan pendant que la séquence enchaîne
            StartCoroutine(PasserbyExitRoutine(exitPos, exitSpeed, exitAnim));

            if (waitBeforePlayerJumpsOff > 0f)
            {
                yield return new WaitForSeconds(waitBeforePlayerJumpsOff);
            }
        }

        // 10. Le joueur ressaute du banc au sol et le jeu reprend
        isSittingOnBench = false;
        Vector3 groundLandingPos = (exitSeatPoint != null) ? exitSeatPoint.position : groundOriginPos;
        PlayJumpSound();
        yield return StartCoroutine(ParabolicJumpRoutine(playerMovement.transform, targetSeatPos, groundLandingPos, jumpOffBenchHeight, jumpOffBenchDuration));

        // Rétablir les paramètres du joueur
        if (playerSR != null)
        {
            if (savedPlayerSprite != null) playerSR.sprite = savedPlayerSprite;
            playerSR.flipX = savedPlayerFlipX;
        }

        if (playerAnim != null)
        {
            PlayAnimationOn(playerAnim, "idle");
        }

        if (playerRb != null)
        {
            playerRb.isKinematic = previousKinematic;
            playerRb.useGravity = previousUseGravity;
            playerRb.linearVelocity = Vector3.zero;
        }

        // 11. Déclencher la chute de la pomme APRÈS que le joueur a quitté le banc
        if (delayAfterPlayerLeavesBeforeAppleFall > 0f)
        {
            yield return new WaitForSeconds(delayAfterPlayerLeavesBeforeAppleFall);
        }
        TriggerAppleFall();

        if (waitForAppleToLand)
        {
            yield return new WaitUntil(() => appleHasFallen);
        }

        // Rétablir la caméra vers le joueur
        yield return StartCoroutine(TransitionCameraOut());

        // Déverrouiller le joueur
        PlayerLockManager.SetPlayerLocked(false);

        // Définir le flag d'histoire si renseigné
        if (StoryStateManager.Instance != null && !string.IsNullOrEmpty(flagToSetOnComplete))
        {
            StoryStateManager.Instance.SetFlag(flagToSetOnComplete, true);
        }

        // Désactivation définitive de toute interaction future avec le banc
        cinematicCompleted = true;
        hasInteracted = true;
        sequenceRunning = false;
        isPlayerInRange = false;
        HideIndicator();
        CleanupIndicator();
        Debug.Log($"[BenchCinematicTrigger] Fin de la cinématique du banc. Interaction définitivement désactivée.");
    }

    private IEnumerator PasserbyExitRoutine(Vector3 exitPos, float speed, string exitAnim)
    {
        yield return StartCoroutine(MovePasserbyTo(exitPos, speed, exitAnim, setIdleOnArrival: false));

        if (deactivatePasserbyOnExit && passerbyCharacter != null)
        {
            passerbyCharacter.SetActive(false);
        }
    }

    /// <summary>
    /// Déclenche la chute de la pomme depuis le ciel vers le sol au même moment où le personnage s'en va.
    /// </summary>
    public void TriggerAppleFall()
    {
        if (fallingApple == null)
        {
            ResolveReferences();
        }

        if (fallingApple != null && !isAppleFalling && !appleHasFallen)
        {
            StartCoroutine(AppleFallRoutine());
        }
    }

    /// <summary>
    /// Réinitialise les drapeaux de chute et relance la chute de la pomme (idéal pour tester plusieurs fois).
    /// </summary>
    public void ResetAndTriggerAppleFall()
    {
        isAppleFalling = false;
        appleHasFallen = false;
        TriggerAppleFall();
    }

    private void HoldAppleInSky()
    {
        if (fallingApple == null) return;

        CarriableItem carriable = fallingApple.GetComponent<CarriableItem>();
        if (carriable != null)
        {
            carriable.enabled = false;
        }

        Rigidbody appleRb = fallingApple.GetComponent<Rigidbody>();
        if (appleRb != null)
        {
            if (!appleRb.isKinematic)
            {
                appleRb.linearVelocity = Vector3.zero;
                appleRb.angularVelocity = Vector3.zero;
                appleRb.isKinematic = true;
            }
            appleRb.useGravity = false;
        }
    }

    private float CalculateGroundYAt(Vector3 worldPoint)
    {
        if (appleLandingPoint != null)
        {
            return appleLandingPoint.position.y;
        }

        Collider[] itemColliders = fallingApple != null ? fallingApple.GetComponentsInChildren<Collider>(true) : null;
        if (itemColliders != null)
        {
            foreach (var col in itemColliders) if (col != null) col.enabled = false;
        }

        RaycastHit hit;
        float groundY;
        Vector3 rayStart = new Vector3(worldPoint.x, worldPoint.y + 2f, worldPoint.z);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1000f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
        }
        else
        {
            groundY = (exitSeatPoint != null) ? exitSeatPoint.position.y : transform.position.y;
        }

        if (itemColliders != null)
        {
            foreach (var col in itemColliders) if (col != null) col.enabled = true;
        }

        return groundY;
    }

    private IEnumerator AppleFallRoutine()
    {
        if (fallingApple == null || isAppleFalling || appleHasFallen) yield break;

        isAppleFalling = true;
        fallingApple.SetActive(true);

        // Désactiver temporairement CarriableItem pour éviter qu'il ne déclenche un respawn prématuré
        CarriableItem carriable = fallingApple.GetComponent<CarriableItem>();
        if (carriable != null)
        {
            carriable.enabled = false;
        }

        Rigidbody appleRb = fallingApple.GetComponent<Rigidbody>();
        if (appleRb != null)
        {
            if (!appleRb.isKinematic)
            {
                appleRb.linearVelocity = Vector3.zero;
                appleRb.angularVelocity = Vector3.zero;
                appleRb.isKinematic = true;
            }
            appleRb.useGravity = false;
        }

        // Localiser le centre visuel réel de la pomme (MeshRenderer)
        Renderer rend = fallingApple.GetComponentInChildren<Renderer>();
        Vector3 visualPos = rend != null ? rend.bounds.center : fallingApple.transform.position;

        // Calculer l'altitude du sol directement sous la pomme par Raycast vertical
        float groundY = CalculateGroundYAt(visualPos);

        // Décalage vertical propre (taille visuelle de la pomme clampée entre 0.05m et 0.35m pour éviter tout collider géant)
        float appleOffset = rend != null ? Mathf.Clamp(rend.bounds.extents.y, 0.05f, 0.35f) : 0.15f;
        float targetVisualY = groundY + appleOffset;

        // Si la pomme était placée au sol au départ, la hisser dans le ciel pour qu'elle puisse chuter
        float currentVisualY = visualPos.y;
        if (currentVisualY < targetVisualY + 1.5f)
        {
            float lift = Mathf.Max(8f, skyDropHeightFallback);
            fallingApple.transform.position += Vector3.up * lift;
            if (appleRb != null) appleRb.position = fallingApple.transform.position;
            currentVisualY += lift;
            Physics.SyncTransforms();
        }

        float totalDistanceToFall = currentVisualY - targetVisualY;
        float fallenDistance = 0f;
        float currentVelocityY = appleInitialVelocity;
        Vector3 originalScale = fallingApple.transform.localScale;
        float elapsed = 0f;

        // Chute avec accélération progressive visible et contrôlée (plus lente)
        while (fallenDistance < totalDistanceToFall && elapsed < 12f)
        {
            elapsed += Time.deltaTime;
            currentVelocityY += appleGravity * Time.deltaTime;
            if (appleMaxFallSpeed > 0f && currentVelocityY > appleMaxFallSpeed)
            {
                currentVelocityY = appleMaxFallSpeed;
            }

            float step = currentVelocityY * Time.deltaTime;

            if (fallenDistance + step > totalDistanceToFall)
            {
                step = totalDistanceToFall - fallenDistance;
            }

            fallenDistance += step;
            fallingApple.transform.position += Vector3.down * step;

            if (appleRb != null)
            {
                appleRb.position = fallingApple.transform.position;
            }

            // Légère rotation sur elle-même pour un effet naturel et organique
            fallingApple.transform.Rotate(Vector3.up, 50f * Time.deltaTime, Space.Self);
            fallingApple.transform.Rotate(Vector3.right, 20f * Time.deltaTime, Space.Self);

            yield return null;
        }

        // Atterrissage au sol
        if (appleRb != null)
        {
            appleRb.position = fallingApple.transform.position;
            appleRb.isKinematic = true;
            appleRb.useGravity = false;
            Physics.SyncTransforms();
        }

        Vector3 landPosition = rend != null ? rend.bounds.center : fallingApple.transform.position;

        // Effet sonore d'impact (optionnel, uniquement si assigné dans l'Inspecteur)
        if (appleLandSound != null)
        {
            AudioSource appleAudio = fallingApple.GetComponent<AudioSource>();
            if (appleAudio != null)
            {
                appleAudio.PlayOneShot(appleLandSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(appleLandSound, landPosition);
            }
        }

        // Particules d'impact
        if (appleLandParticles != null)
        {
            ParticleSystem ps = Instantiate(appleLandParticles, landPosition, Quaternion.identity);
            Destroy(ps.gameObject, 2.5f);
        }

        // Rebond élastique / Squash cartoon à l'impact
        if (enableAppleImpactSquash && originalScale != Vector3.zero)
        {
            Vector3 squashScale = new Vector3(originalScale.x * 1.35f, originalScale.y * 0.5f, originalScale.z * 1.35f);
            fallingApple.transform.localScale = squashScale;

            float squashDuration = 0.14f;
            float squashElapsed = 0f;
            while (squashElapsed < squashDuration)
            {
                squashElapsed += Time.deltaTime;
                float t = squashElapsed / squashDuration;
                fallingApple.transform.localScale = Vector3.Lerp(squashScale, originalScale, t);
                yield return null;
            }

            fallingApple.transform.localScale = originalScale;
        }

        // Réactiver CarriableItem et enregistrer la position au sol pour les interactions
        if (carriable != null)
        {
            carriable.CustomRespawnPosition = landPosition;
            carriable.enabled = true;
        }

        isAppleFalling = false;
        appleHasFallen = true;
        Debug.Log($"[BenchCinematicTrigger] La pomme a atterri avec succès au sol : {landPosition} (distance tombée : {fallenDistance:F2}m)");
    }

    private IEnumerator MovePasserbyTo(Vector3 targetPos, float speed, string animOverride = null, bool setIdleOnArrival = true)
    {
        if (passerbyCharacter == null) yield break;

        string moveAnim = !string.IsNullOrEmpty(animOverride) ? animOverride : passerbyWalkAnimName;
        moveAnim = ResolvePasserbyMoveAnim(passerbyAnimator, moveAnim);

        // Activer l'animation de marche / lévitation
        if (passerbyAnimator != null && !string.IsNullOrEmpty(moveAnim))
        {
            PlayAnimationOn(passerbyAnimator, moveAnim);
            SetBoolIfPresent(passerbyAnimator, "isWalking", true);
        }

        // Orientation du sprite vers la cible
        float dirX = targetPos.x - passerbyCharacter.transform.position.x;
        if (passerbySpriteRenderer != null && Mathf.Abs(dirX) > 0.05f)
        {
            bool movingRight = dirX > 0f;
            passerbySpriteRenderer.flipX = passerbyFacesRightByDefault ? !movingRight : movingRight;
        }

        while (Vector3.Distance(passerbyCharacter.transform.position, targetPos) > 0.05f)
        {
            passerbyCharacter.transform.position = Vector3.MoveTowards(
                passerbyCharacter.transform.position,
                targetPos,
                speed * Time.deltaTime
            );
            yield return null;
        }

        passerbyCharacter.transform.position = targetPos;

        // Arrêt de la marche uniquement si demandé (ex: arrivée devant le banc)
        if (setIdleOnArrival && passerbyAnimator != null)
        {
            SetBoolIfPresent(passerbyAnimator, "isWalking", false);
            if (!string.IsNullOrEmpty(passerbyIdleAnimName))
            {
                PlayAnimationOn(passerbyAnimator, passerbyIdleAnimName);
            }
        }
    }

    private string ResolvePasserbyMoveAnim(Animator anim, string preferredName)
    {
        if (anim == null) return preferredName;

        if (!string.IsNullOrEmpty(preferredName) && anim.HasState(0, Animator.StringToHash(preferredName)))
        {
            return preferredName;
        }

        // Recherche automatique des états de déplacement (ex: levitateP2 pour le Gardener)
        string[] candidates = { "levitateP2", "levitate", "Walk", "walk", "Move", "move", "Run", "run" };
        foreach (string candidate in candidates)
        {
            if (anim.HasState(0, Animator.StringToHash(candidate)))
            {
                return candidate;
            }
        }

        return preferredName;
    }

    private IEnumerator ParabolicJumpRoutine(Transform targetObj, Vector3 startPos, Vector3 endPos, float arcHeight, float duration)
    {
        if (targetObj == null) yield break;

        float elapsed = 0f;
        duration = Mathf.Max(0.05f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Interpolation horizontale / linéaire
            Vector3 currentLinear = Vector3.Lerp(startPos, endPos, t);

            // Arc parabolique : 4 * h * t * (1 - t)
            float parabolicY = 4f * arcHeight * t * (1f - t);
            currentLinear.y += parabolicY;

            targetObj.position = currentLinear;
            yield return null;
        }

        targetObj.position = endPos;
    }

    private void PlayJumpSound()
    {
        if (customJumpSFX != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(customJumpSFX, 1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(customJumpSFX, transform.position, 1f);
            }
        }
        else if (playerMovement != null)
        {
            playerMovement.PlayJumpSFX();
        }
    }

    private IEnumerator TriggerDialogueStep(DialogueData data, string bubbleText, Transform bubbleAnchor)
    {
        if (data != null && DialogueManager.Instance != null)
        {
            bool dialogueFinished = false;
            DialogueManager.Instance.StartDialogue(data, () =>
            {
                dialogueFinished = true;
            });
            yield return new WaitUntil(() => dialogueFinished);

            // Re-verrouiller immédiatement le joueur car DialogueManager.EndDialogue() unpause et réactive le joueur
            if (sequenceRunning)
            {
                PlayerLockManager.SetPlayerLocked(true);
                if (playerMovement != null)
                {
                    playerMovement.enabled = false;
                    if (isSittingOnBench)
                    {
                        playerMovement.transform.position = currentSeatPos;
                    }
                    Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.useGravity = false;
                        rb.isKinematic = true;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(bubbleText) && bubbleAnchor != null)
        {
            LiveSpeechBubble bubble = LiveSpeechBubble.GetOrCreate(bubbleAnchor, new Vector3(0f, 2.2f, 0f));
            if (bubble != null)
            {
                bubble.gameObject.SetActive(true);
                bubble.Speak(bubbleText);
                yield return new WaitWhile(() => bubble.IsTyping);
                yield return new WaitForSeconds(1.8f);
                bubble.Clear();
            }
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }
    }

    #region Transitions Caméra

    private Vector3 savedCamPos;
    private Quaternion savedCamRot;
    private float savedCamFOV;
    private bool hasSavedCam = false;

    private IEnumerator TransitionCameraIn()
    {
        // 1. Si une CinemachineCamera dédiée est assignée, l'activer en priorité élevée
        if (benchVirtualCamera != null)
        {
            benchVirtualCamera.gameObject.SetActive(true);
            benchVirtualCamera.Priority = 20;
            yield return new WaitForSeconds(cameraTransitionInDuration);
            yield break;
        }

        // 2. Sinon, transition fluide sur la caméra existante
        if (mainVirtualCamera == null) yield break;

        if (mainCameraHelper != null)
        {
            mainCameraHelper.SaveOriginalSettings();
            mainCameraHelper.enabled = false;
        }

        Camera mainCam = Camera.main;
        savedCamPos = mainCam != null ? mainCam.transform.position : mainVirtualCamera.transform.position;
        savedCamRot = mainCam != null ? mainCam.transform.rotation : mainVirtualCamera.transform.rotation;
        savedCamFOV = mainVirtualCamera.Lens.FieldOfView;
        hasSavedCam = true;

        // Cadrer au milieu du banc (plutôt que sur le point assis du joueur)
        Vector3 benchCenter = GetBenchCenter();
        Vector3 cameraRight = savedCamRot * Vector3.right;
        Vector3 targetFocusPoint = benchCenter + Vector3.up * focusHeightOffset + cameraRight * focusHorizontalOffset;

        Vector3 targetPos;
        Quaternion targetRot;
        float finalFOV = (targetFOV > 0f) ? targetFOV : savedCamFOV;

        if (keepCameraAngle)
        {
            // Ne modifie pas l'angle de la caméra : utilise la direction exacte de visée de la caméra actuelle
            Vector3 cameraForward = savedCamRot * Vector3.forward;
            // Centre le milieu du banc sur l'écran et dézoome en douceur pour un plan large
            targetPos = targetFocusPoint - cameraForward.normalized * cinematicDistance;
            targetRot = savedCamRot;
        }
        else
        {
            float targetYaw = cameraYaw >= 0f ? cameraYaw : savedCamRot.eulerAngles.y;
            Vector3 localOffset = new Vector3(focusHorizontalOffset, cameraHeight, -cameraDistance);
            Vector3 targetOffset = Quaternion.Euler(0f, targetYaw, 0f) * localOffset;
            targetPos = targetFocusPoint + targetOffset;

            Vector3 targetCenter = targetFocusPoint + Vector3.up * 0.5f;
            Vector3 lookDir = targetCenter - targetPos;
            targetRot = lookDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDir, Vector3.up)
                : Quaternion.Euler(cameraPitch, targetYaw, 0f);
            finalFOV = cameraFOV;
        }

        mainVirtualCamera.Follow = null;

        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, cameraTransitionInDuration);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = (cameraCurve != null) ? cameraCurve.Evaluate(Mathf.Clamp01(elapsed / dur)) : Mathf.SmoothStep(0f, 1f, elapsed / dur);

            mainVirtualCamera.transform.position = Vector3.Lerp(savedCamPos, targetPos, t);
            if (!keepCameraAngle)
            {
                mainVirtualCamera.transform.rotation = Quaternion.Slerp(savedCamRot, targetRot, t);
            }
            else
            {
                mainVirtualCamera.transform.rotation = savedCamRot;
            }

            mainVirtualCamera.Lens.FieldOfView = Mathf.Lerp(savedCamFOV, finalFOV, t);

            yield return null;
        }

        mainVirtualCamera.transform.position = targetPos;
        mainVirtualCamera.transform.rotation = targetRot;
        mainVirtualCamera.Lens.FieldOfView = finalFOV;
    }

    private IEnumerator TransitionCameraOut()
    {
        // 1. Si une CinemachineCamera dédiée est assignée, réinitialiser sa priorité
        if (benchVirtualCamera != null)
        {
            benchVirtualCamera.Priority = 0;
            benchVirtualCamera.gameObject.SetActive(false);
            yield return new WaitForSeconds(cameraTransitionOutDuration);
            yield break;
        }

        // 2. Sinon, transition fluide de retour vers le joueur
        if (mainVirtualCamera == null || !hasSavedCam) yield break;

        Vector3 currentPos = mainVirtualCamera.transform.position;
        Quaternion currentRot = mainVirtualCamera.transform.rotation;
        float currentFOV = mainVirtualCamera.Lens.FieldOfView;

        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, cameraTransitionOutDuration);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = (cameraCurve != null) ? cameraCurve.Evaluate(Mathf.Clamp01(elapsed / dur)) : Mathf.SmoothStep(0f, 1f, elapsed / dur);

            mainVirtualCamera.transform.position = Vector3.Lerp(currentPos, savedCamPos, t);
            if (!keepCameraAngle)
            {
                mainVirtualCamera.transform.rotation = Quaternion.Slerp(currentRot, savedCamRot, t);
            }
            else
            {
                mainVirtualCamera.transform.rotation = savedCamRot;
            }

            mainVirtualCamera.Lens.FieldOfView = Mathf.Lerp(currentFOV, savedCamFOV, t);

            yield return null;
        }

        if (mainCameraHelper != null)
        {
            mainCameraHelper.ResetDynamicState();
            mainCameraHelper.enabled = true;
            mainCameraHelper.SetTargetPlayer(playerMovement != null ? playerMovement.transform : null);
            mainCameraHelper.UpdateCameraSettings(false);
        }
        else if (playerMovement != null)
        {
            mainVirtualCamera.Follow = playerMovement.transform;
        }

        mainVirtualCamera.ForceCameraPosition(savedCamPos, savedCamRot);
        yield return null;
    }

    #endregion

    private void PlayAnimationOn(Animator anim, string animName)
    {
        if (anim == null || string.IsNullOrEmpty(animName)) return;

        anim.Play(animName);
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == animName && param.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(animName);
                break;
            }
        }
    }

    private void SetBoolIfPresent(Animator anim, string boolName, bool val)
    {
        if (anim == null || string.IsNullOrEmpty(boolName)) return;
        foreach (var p in anim.parameters)
        {
            if (p.name == boolName && p.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(boolName, val);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Collider du banc
        // Rayon de détection d'interaction
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
        Gizmos.DrawSphere(InteractionCenter, interactionDistance);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(InteractionCenter, interactionDistance);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // Point pour s'asseoir
        if (seatPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(seatPoint.position, 0.2f);
            Gizmos.DrawWireSphere(seatPoint.position, 0.3f);
        }

        // Point de sortie
        if (exitSeatPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(exitSeatPoint.position, 0.2f);
        }

        // Trajet du passant (Start -> Stop -> Exit)
        if (passerbyStartPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(passerbyStartPoint.position, 0.25f);
        }

        if (passerbyStopPoint != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawSphere(passerbyStopPoint.position, 0.25f);

            if (passerbyStartPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(passerbyStartPoint.position, passerbyStopPoint.position);
            }
        }

        if (passerbyExitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(passerbyExitPoint.position, 0.25f);

            if (passerbyStopPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(passerbyStopPoint.position, passerbyExitPoint.position);
            }
        }

        // Cible caméra manuelle ou centre automatique du banc
        Vector3 bCenter = GetBenchCenter();
        Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.9f);
        Gizmos.DrawWireSphere(bCenter, 0.25f);
        Gizmos.DrawLine(bCenter - Vector3.right * 0.35f, bCenter + Vector3.right * 0.35f);
        Gizmos.DrawLine(bCenter - Vector3.forward * 0.35f, bCenter + Vector3.forward * 0.35f);

        if (cameraFocusTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(cameraFocusTarget.position, 0.3f);
        }
    }

    /// <summary>
    /// Calcule le véritable centre géométrique et visuel du banc pour la caméra (au lieu du point assis du joueur).
    /// </summary>
    public Vector3 GetBenchCenter()
    {
        // 1. Cible manuelle prioritaire si spécifiée dans l'inspecteur
        if (cameraFocusTarget != null)
        {
            return cameraFocusTarget.position;
        }

        // 2. Point enfant "BenchCenterPoint" ou "BenchCenter" s'il a été créé
        Transform centerChild = transform.Find("BenchCenterPoint") ?? transform.Find("BenchCenter");
        if (centerChild != null)
        {
            return centerChild.position;
        }

        // 3. Centre physique du collider principal du banc (BoxCollider du banc)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds.center;
        }

        // 4. Renderers 3D combinés du banc
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            Bounds b = meshRenderers[0].bounds;
            for (int i = 1; i < meshRenderers.Length; i++)
            {
                b.Encapsulate(meshRenderers[i].bounds);
            }
            return b.center;
        }

        // 5. Milieu entre le joueur assis (seatPoint) et la fleur assise
        if (seatPoint != null && flowerCharacter != null)
        {
            return (seatPoint.position + flowerCharacter.transform.position) * 0.5f;
        }

        // 6. Repli sur le seatPoint ou le transform
        if (seatPoint != null)
        {
            return seatPoint.position;
        }

        return transform.position;
    }
}
