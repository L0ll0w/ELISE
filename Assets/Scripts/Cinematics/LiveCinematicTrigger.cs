using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Structure définissant une fleur à couper et son sprite une fois coupée.
/// </summary>
[System.Serializable]
public class FlowerCutData
{
    [Tooltip("SpriteRenderer de la fleur à couper (Optionnel, sera recherché automatiquement à côté du waypoint si vide).")]
    public SpriteRenderer flowerSpriteRenderer;

    [Tooltip("Sprite de la fleur une fois qu'elle est coupée.")]
    public Sprite cutSprite;

    [Tooltip("Préfab de particules d'impact optionnel.")]
    public GameObject cutParticlePrefab;
}

/// <summary>
/// Déclencheur de cinématique en direct (Live / Temps Réel).
/// Séquence exécutée à chaque point de passage (Waypoint) :
/// 1. Attente & Dialogue écrit lettre par lettre au-dessus du PNJ (en noir).
/// 2. Lancement de l'animation ShearClose (regarde à GAUCHE).
/// 3. Joue le son de coupe (0.3s après le début de ShearClose).
/// 4. Fondu ROUGE sur la plante à côté, puis passage au sprite coupé une fois cutAnimDuration terminée.
/// 5. Attente après l'animation.
/// 6. Effacement du texte, animation Idle Share, orientation à DROITE et déplacement vers le point suivant.
/// 7. À l'arrivée au dernier point : arrêt à GAUCHE puis 1s après, animation DevoveoSpawn sur l'objet à côté.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Live Cinematic Trigger")]
public class LiveCinematicTrigger : MonoBehaviour
{
    [Header("Configuration Déclencheur")]
    [Tooltip("La cinématique ne se déclenche-t-elle qu'une seule fois ?")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("Tag du joueur pour la détection.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Cible & Personnage")]
    [Tooltip("Le personnage (Transform) qui effectuera les mouvements et animations.")]
    [SerializeField] private Transform targetCharacter;

    [Tooltip("Composant Animator du personnage (recherché automatiquement sur targetCharacter si non assigné).")]
    [SerializeField] private Animator targetAnimator;

    [Tooltip("Composant SpriteRenderer du personnage (recherché automatiquement pour le retournement du sprite).")]
    [SerializeField] private SpriteRenderer targetSprite;

    [Header("Points de Passage (Waypoints)")]
    [Tooltip("Liste des points de destination (Point 1, Point 2, Point 3, etc.).")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Effets Sonores (SFX)")]
    [Tooltip("Clip audio du son de coupe (ex: ciseaux / coupe de fleur).")]
    [SerializeField] private AudioClip cutSoundClip;

    [Tooltip("Délai d'attente après le début de l'animation ShearClose pour jouer le son de coupe (en secondes).")]
    [SerializeField] private float cutSoundDelay = 0.3f;

    [Tooltip("Volume du son de coupe (0.0 à 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float cutSoundVolume = 1.0f;

    [Header("Fleurs Coupées & Fondu Rouge")]
    [Tooltip("Délai d'attente après le début de l'animation ShearClose avant le démarrage du fondu rouge (en secondes).")]
    [SerializeField] private float delayBeforeRedFade = 0.3f;

    [Tooltip("Durée du fondu rouge (en secondes). La fleur passe en sprite coupé à la fin de cette durée.")]
    [SerializeField] private float cutAnimDuration = 0.5f;

    [Tooltip("Couleur du fondu (Rouge par défaut).")]
    [SerializeField] private Color redFadeColor = Color.red;

    [Tooltip("Liste des Sprites des fleurs UNE FOIS COUPÉES (Élément 0 = Point 1, Élément 1 = Point 2, etc.).")]
    [SerializeField] private List<Sprite> cutSprites = new List<Sprite>();

    [Tooltip("Liste directe des SpriteRenderer des plantes/fleurs dans la scène à couper (Optionnel, détecté automatiquement par proximité si vide).")]
    [SerializeField] private List<SpriteRenderer> flowerObjects = new List<SpriteRenderer>();

    [Tooltip("Rayon de recherche automatique (en mètres) autour du waypoint pour trouver la plante à côté si non assignée.")]
    [SerializeField] private float autoSearchRadius = 3.5f;

    [Tooltip("Données avancées des fleurs (Alternative structurée).")]
    [SerializeField] private List<FlowerCutData> cutFlowers = new List<FlowerCutData>();

    [Header("Étape Finale (Arrivée au dernier point)")]
    [Tooltip("Activer le déclenchement d'une animation sur l'objet à côté du dernier point ?")]
    [SerializeField] private bool enableFinalObjectAnimation = true;

    [Tooltip("L'Animator de l'objet situé à côté du dernier point (Optionnel, recherché automatiquement près du dernier point si vide).")]
    [SerializeField] private Animator finalObjectAnimator;

    [Tooltip("Nom de l'état d'animation à jouer sur l'objet du dernier point.")]
    [SerializeField] private string finalAnimationName = "DevoveoSpawn";

    [Tooltip("Délai d'attente (en secondes) après l'arrivée au dernier point avant de déclencher l'animation de l'objet.")]
    [SerializeField] private float finalAnimationDelay = 1.0f;

    [Tooltip("Temps d'attente supplémentaire (en secondes) APRÈS la fin de l'animation de l'objet (ex: DevoveoSpawn) avant le départ vers la table.")]
    [SerializeField] private float extraWaitAfterSpawnAnim = 1.0f;

    [Tooltip("Durée de secours (en secondes) pour l'animation de l'objet si sa durée réelle ne peut pas être lue dans l'Animator.")]
    [SerializeField] private float fallbackSpawnAnimDuration = 1.5f;

    [Header("Séquence de la Table & Arrosage Final")]
    [Tooltip("Activer la séquence complète (Table -> Retour -> Élévation/Arrosage -> Anim Fleur Inversée -> Retour Table -> Envol) ?")]
    [SerializeField] private bool enableTableSequence = true;

    [Tooltip("Point de passage (Waypoint) vers la table.")]
    [SerializeField] private Transform tableWaypoint;

    [Tooltip("Objet sur la table à ACTIVATION (ex: l'arrosoir).")]
    [SerializeField] private GameObject objectToActivateOnTable;

    [Tooltip("Objet sur la table à DÉSACTIVATION.")]
    [SerializeField] private GameObject objectToDeactivateOnTable;

    [Tooltip("Composant HandWateringCan de l'arrosoir (Optionnel, recherché automatiquement sur objectToActivateOnTable si vide).")]
    [SerializeField] private HandWateringCan wateringCan;

    [Tooltip("Nom de l'animation à jouer sur le jardinier lors du déplacement.")]
    [SerializeField] private string wateringTravelAnimName = "GardenerWatheringTravel";

    [Tooltip("Point de retour final où l'arrosage s'effectue (Optionnel, si vide utilise la position initiale du jardinier).")]
    [SerializeField] private Transform returnWaypoint;

    [Tooltip("Délai d'attente à la table avant de repartir vers le point de retour (en secondes).")]
    [SerializeField] private float waitAtTableDuration = 0.5f;

    [Header("Élévation & Arrosage (Point de Retour)")]
    [Tooltip("Point/Transform où l'eau doit atterrir (Optionnel, si vide utilise automatiquement le dernier point de passage/la fleur).")]
    [SerializeField] private Transform wateringTargetPoint;

    [Tooltip("Temps d'attente (en secondes) après l'arrivée au point de retour avant de s'élever.")]
    [SerializeField] private float waitBeforeLevitate = 1.0f;

    [Tooltip("Hauteur d'élévation du jardinier pour arroser (en mètres).")]
    [SerializeField] private float levitateHeight = 0.6f;

    [Tooltip("Vitesse d'élévation et de descente du jardinier.")]
    [SerializeField] private float levitateSpeed = 2.0f;

    [Tooltip("Durée de l'écoulement de l'eau (en secondes).")]
    [SerializeField] private float wateringDuration = 3.0f;

    [Tooltip("Nom de l'animation à jouer sur la fleur une fois le jardinier reposé au sol ('DevoveoDespawn' par défaut).")]
    [SerializeField] private string flowerDespawnAnimName = "DevoveoDespawn";

    [Tooltip("Temps d'attente (en secondes) après le retour au sol du jardinier avant que la fleur ne joue son animation.")]
    [SerializeField] private float waitBeforeFlowerDespawn = 1.0f;

    [Header("Séquence de Fin (Réactivation & Envol)")]
    [Tooltip("Nom de l'animation Idle à jouer sur le jardinier lors de la réactivation de l'objet sur la table ('Idle P1' par défaut).")]
    [SerializeField] private string returnIdleAnimName = "Idle P1";

    [Tooltip("Temps d'attente (en secondes) après la fin de l'animation inversée de la fleur avant de retourner à la table.")]
    [SerializeField] private float waitBeforeReturnToTable = 1.0f;

    [Tooltip("Temps d'attente (en secondes) après la réactivation de l'objet sur la table avant de s'envoler.")]
    [SerializeField] private float waitBeforeFlyUp = 1.0f;

    [Tooltip("Vitesse à laquelle le jardinier s'envole vers le haut.")]
    [SerializeField] private float flyUpSpeed = 8.0f;

    [Tooltip("Hauteur à atteindre vers le haut hors-champ avant d'être détruit.")]
    [SerializeField] private float flyUpHeight = 15.0f;

    [Tooltip("Détruire le GameObject du jardinier une fois hors-champ ?")]
    [SerializeField] private bool destroyGardenerOnExit = true;

    [Header("Dialogue en Direct (Texte au-dessus du PNJ)")]
    [Tooltip("Activer l'affichage d'un texte écrit lettre par lettre au-dessus du personnage avant ShearClose ?")]
    [SerializeField] private bool enableLiveDialogue = true;

    [Tooltip("Textes prononcés à chaque point de passage (Ex: [0] = 1er point, [1] = 2nd point, [2] = 3ème point).")]
    [SerializeField] private List<string> dialogueTexts = new List<string>()
    {
        "Hop, une petite coupe !",
        "Et de deux !",
        "Et voilà !"
    };

    [Tooltip("Police de caractère personnalisée pour le texte du dialogue (TMP_FontAsset). Si vide, utilise la police par défaut.")]
    [SerializeField] private TMP_FontAsset dialogueFont;

    [Tooltip("Couleur du texte (Noir par défaut).")]
    [SerializeField] private Color dialogueColor = Color.black;

    [Tooltip("Hauteur (décalage Y) du texte au-dessus de la tête du personnage (en mètres).")]
    [SerializeField] private float dialogueHeightOffset = 2.2f;

    [Tooltip("Vitesse d'apparition des lettres (en secondes par caractère).")]
    [SerializeField] private float dialogueTypingSpeed = 0.04f;

    [Tooltip("Taille de la police du texte.")]
    [SerializeField] private float dialogueFontSize = 4.2f;

    [Tooltip("Nombre maximum de mots par ligne avant retour à la ligne (ex: 3 mots ou 2 mots longs).")]
    [SerializeField] private int dialogueMaxWordsPerLine = 3;

    [Header("Paramètres d'Animation & Délais")]
    [Tooltip("Délai d'attente initial avant le premier cycle (en secondes).")]
    [SerializeField] private float initialDelay = 0.0f;

    [Tooltip("Temps d'attente à chaque point avant de jouer l'animation ShearClose (en secondes).")]
    [SerializeField] private float waitBeforeShear = 2.0f;

    [Tooltip("Nom de l'état ou du clip d'animation ShearClose dans l'Animator.")]
    [SerializeField] private string shearCloseAnimName = "ShearClose";

    [Tooltip("Temps d'attente après ShearClose avant de repasser en Idle Share et de bouger (en secondes).")]
    [SerializeField] private float waitAfterShear = 1.0f;

    [Tooltip("Nom de l'état ou du clip d'animation Idle Share dans l'Animator.")]
    [SerializeField] private string idleShareAnimName = "Idle Share";

    [Header("Paramètres de Déplacement")]
    [Tooltip("Vitesse de déplacement du personnage vers les points (unités / seconde).")]
    [SerializeField] private float moveSpeed = 3.0f;

    [Tooltip("Distance minimale d'arrivée au point.")]
    [SerializeField] private float stopDistance = 0.05f;

    [Header("Orientation du Sprite")]
    [Tooltip("Inverser le flip si le sprite de base est orienté à l'envers.")]
    [SerializeField] private bool invertFlip = false;

    private bool alreadyTriggered = false;
    private Coroutine sequenceCoroutine;
    private CharacterController characterController;
    private Rigidbody rb;
    private LiveSpeechBubble activeSpeechBubble;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (targetCharacter == null)
        {
            targetAnimator = GetComponentInChildren<Animator>();
            if (targetAnimator != null)
            {
                targetCharacter = targetAnimator.transform;
            }
        }

        if (targetCharacter != null)
        {
            if (targetAnimator == null)
            {
                targetAnimator = targetCharacter.GetComponent<Animator>() ?? targetCharacter.GetComponentInChildren<Animator>();
            }
            if (targetSprite == null)
            {
                targetSprite = targetCharacter.GetComponent<SpriteRenderer>() ?? targetCharacter.GetComponentInChildren<SpriteRenderer>();
            }

            characterController = targetCharacter.GetComponent<CharacterController>();
            rb = targetCharacter.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered) return;

        if (other.CompareTag(playerTag) || other.GetComponent<PlayerMovement>() != null)
        {
            if (oneShot)
            {
                alreadyTriggered = true;
            }

            ResolveReferences();

            if (targetCharacter == null)
            {
                Debug.LogError($"[LiveCinematicTrigger] Aucun personnage cible n'est assigné sur {gameObject.name} !", this);
                return;
            }

            if (waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning($"[LiveCinematicTrigger] Aucun point de passage (waypoint) n'a été renseigné sur {gameObject.name} !", this);
                return;
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }
            sequenceCoroutine = StartCoroutine(ExecuteLiveCinematicRoutine());
        }
    }

    /// <summary>
    /// Routine principale exécutant la cinématique en direct avec dialogue, fondu rouge sur la plante et son de coupe.
    /// </summary>
    private IEnumerator ExecuteLiveCinematicRoutine()
    {
        Debug.Log($"[LiveCinematicTrigger] Cinématique en direct démarrée sur '{gameObject.name}'. Le joueur conserve le contrôle !");

        // Sauvegarder la position d'origine du jardinier pour le retour
        Vector3 startCharacterPosition = targetCharacter != null ? targetCharacter.position : transform.position;

        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // 0. Au départ, orientation de base (pour couper)
        SetFacing(false);

        // Boucle à travers chaque point de passage renseigné
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform currentWaypoint = waypoints[i];
            if (currentWaypoint == null)
            {
                Debug.LogWarning($"[LiveCinematicTrigger] Le point de passage #{i + 1} est null sur {gameObject.name}. Étape ignorée.");
                continue;
            }

            Debug.Log($"[LiveCinematicTrigger] Étape {i + 1}/{waypoints.Count} : Début attente & dialogue...");

            // 1. Orientation de base (pour la coupe)
            SetFacing(false);

            // 2. Déclencher le dialogue en direct au-dessus du PNJ
            string textToSpeak = (dialogueTexts != null && i < dialogueTexts.Count) ? dialogueTexts[i] : "";
            if (enableLiveDialogue && !string.IsNullOrEmpty(textToSpeak) && targetCharacter != null)
            {
                activeSpeechBubble = LiveSpeechBubble.GetOrCreate(targetCharacter, Vector3.up * dialogueHeightOffset);
                if (activeSpeechBubble != null)
                {
                    activeSpeechBubble.Speak(textToSpeak, dialogueTypingSpeed, dialogueColor, dialogueFontSize, dialogueMaxWordsPerLine, dialogueFont);
                }
            }

            // Attendre la durée spécifiée avant ShearClose
            if (waitBeforeShear > 0f)
            {
                yield return new WaitForSeconds(waitBeforeShear);
            }

            // 3. Jouer l'animation ShearClose (orientation de base)
            SetFacing(false);
            if (targetAnimator != null && !string.IsNullOrEmpty(shearCloseAnimName))
            {
                Debug.Log($"[LiveCinematicTrigger] Étape {i + 1}/{waypoints.Count} : Exécution de l'animation '{shearCloseAnimName}'");
                PlayAnimation(shearCloseAnimName);
            }

            // Déclencher la lecture du son de coupe (0.3s après le début de ShearClose)
            StartCoroutine(PlayCutSoundRoutine(cutSoundDelay));

            // Déclencher le fondu rouge sur la plante puis le passage au sprite coupé à la fin de cutAnimDuration
            TriggerFlowerCut(i);

            // Attendre après l'animation ShearClose
            if (waitAfterShear > 0f)
            {
                yield return new WaitForSeconds(waitAfterShear);
            }

            // Effacer le texte de dialogue lors du départ
            if (activeSpeechBubble != null)
            {
                activeSpeechBubble.Clear();
            }

            // 4. Passer en animation Idle Share
            if (targetAnimator != null && !string.IsNullOrEmpty(idleShareAnimName))
            {
                Debug.Log($"[LiveCinematicTrigger] Étape {i + 1}/{waypoints.Count} : Transition vers '{idleShareAnimName}'");
                PlayAnimation(idleShareAnimName);
            }

            // 5. Se déplacer vers le point cible (Flippera automatiquement pendant le mouvement)
            Debug.Log($"[LiveCinematicTrigger] Étape {i + 1}/{waypoints.Count} : Déplacement vers '{currentWaypoint.name}'");
            yield return StartCoroutine(MoveCharacterToPosition(currentWaypoint.position));

            // 6. À l'arrivée au point, il s'arrête et reprend son orientation de base pour couper
            SetFacing(false);
            Debug.Log($"[LiveCinematicTrigger] Arrivé au point #{i + 1} ('{currentWaypoint.name}') - Reprend l'orientation de base.");
        }

        // Fin de la séquence : s'assurer que le texte est effacé et que le PNJ est en orientation de base
        if (activeSpeechBubble != null)
        {
            activeSpeechBubble.Clear();
        }

        SetFacing(false);
        if (targetAnimator != null && !string.IsNullOrEmpty(idleShareAnimName))
        {
            PlayAnimation(idleShareAnimName);
        }

        // --- ÉTAPE FINALE 1 : Animation DevoveoSpawn sur l'objet situé à côté du dernier point ---
        if (enableFinalObjectAnimation)
        {
            if (finalAnimationDelay > 0f)
            {
                yield return new WaitForSeconds(finalAnimationDelay);
            }

            // Déclenche l'animation de spawn, attend sa fin complète + 1s supplémentaire
            yield return StartCoroutine(PlayAndWaitForFinalObjectAnimationRoutine());
        }

        // --- ÉTAPE FINALE 2 : Séquence de la Table & Arrosage Final ---
        if (enableTableSequence)
        {
            // Recherche automatique du HandWateringCan si non assigné dans l'Inspecteur
            if (wateringCan == null && objectToActivateOnTable != null)
            {
                wateringCan = objectToActivateOnTable.GetComponent<HandWateringCan>() ?? objectToActivateOnTable.GetComponentInChildren<HandWateringCan>();
            }

            // 1. Déplacement du jardinier vers la table (Flip l'objet entier pendant le mouvement vers la table)
            if (tableWaypoint != null)
            {
                Debug.Log($"[LiveCinematicTrigger] Déplacement du jardinier vers la table ('{tableWaypoint.name}')...");
                yield return StartCoroutine(MoveCharacterToPosition(tableWaypoint.position, true, flipGameObject: true));
            }

            // Arrêté à la table : orientation de l'objet entier flipped
            SetFacing(true, flipGameObject: true);

            // 2. Activation de l'arrosoir et désactivation de l'autre objet sur la table
            if (objectToActivateOnTable != null)
            {
                objectToActivateOnTable.SetActive(true);
                Debug.Log($"[LiveCinematicTrigger] Objet sur la table '{objectToActivateOnTable.name}' ACTIVÉ.");
                StopAllWateringParticles(objectToActivateOnTable);
            }

            if (objectToDeactivateOnTable != null)
            {
                objectToDeactivateOnTable.SetActive(false);
                Debug.Log($"[LiveCinematicTrigger] Objet sur la table '{objectToDeactivateOnTable.name}' DÉSACTIVÉ.");
            }

            StopAllWateringParticles(objectToActivateOnTable);

            // Passage en animation de portage d'arrosoir / déplacement
            if (targetAnimator != null && !string.IsNullOrEmpty(wateringTravelAnimName))
            {
                Debug.Log($"[LiveCinematicTrigger] Passage en animation '{wateringTravelAnimName}' sur le jardinier.");
                PlayAnimation(wateringTravelAnimName);
            }

            if (waitAtTableDuration > 0f)
            {
                yield return new WaitForSeconds(waitAtTableDuration);
            }

            // 3. Retour au point de retour (Orientation de base sans flip du GameObject)
            Vector3 originPos = (returnWaypoint != null) ? returnWaypoint.position : startCharacterPosition;
            Debug.Log($"[LiveCinematicTrigger] Retour du jardinier vers son point de retour ({originPos})...");
            StopAllWateringParticles(objectToActivateOnTable);
            yield return StartCoroutine(MoveCharacterToPosition(originPos, false, flipGameObject: false));

            // Arrivée au point de retour : orientation de base (ne flip pas une fois arrivé au point)
            SetFacing(false, flipGameObject: false);
            StopAllWateringParticles(objectToActivateOnTable);

            // 4. Attente de 1 seconde avant de s'élever
            if (waitBeforeLevitate > 0f)
            {
                yield return new WaitForSeconds(waitBeforeLevitate);
            }

            StopAllWateringParticles(objectToActivateOnTable);

            // 5. Le jardinier s'élève un peu vers le haut
            Vector3 groundPos = targetCharacter.position;
            Vector3 targetLevitatePos = groundPos + Vector3.up * levitateHeight;

            Debug.Log($"[LiveCinematicTrigger] Le jardinier s'élève de {levitateHeight}m...");
            yield return StartCoroutine(LerpCharacterPositionRoutine(targetLevitatePos, levitateSpeed));

            // 6. ARRIVÉE EN HAUT DE SA LÉVITATION : L'eau commence SEULEMENT MAINTENANT à couler pendant wateringDuration (3 secondes par défaut)
            if (wateringCan == null && objectToActivateOnTable != null)
            {
                wateringCan = objectToActivateOnTable.GetComponent<HandWateringCan>() ?? objectToActivateOnTable.GetComponentInChildren<HandWateringCan>();
            }

            if (wateringCan != null)
            {
                Transform targetToUse = wateringTargetPoint;
                if (targetToUse == null && waypoints != null && waypoints.Count > 0)
                {
                    targetToUse = waypoints[waypoints.Count - 1];
                }
                if (targetToUse != null)
                {
                    wateringCan.SetTargetPoint(targetToUse);
                }

                Debug.Log($"[LiveCinematicTrigger] Jardinier arrivé en haut de sa lévitation : Début de l'arrosage vers '{(targetToUse != null ? targetToUse.name : "cible")}' pendant {wateringDuration}s...");
                wateringCan.StartWatering();
            }

            if (wateringDuration > 0f)
            {
                yield return new WaitForSeconds(wateringDuration);
            }

            // 7. L'eau s'arrête et le jardinier redescend au sol
            if (wateringCan != null)
            {
                Debug.Log($"[LiveCinematicTrigger] Arrêt de l'arrosage.");
                wateringCan.StopWatering();
            }
            StopAllWateringParticles(objectToActivateOnTable);

            Debug.Log($"[LiveCinematicTrigger] Le jardinier redescend au sol...");
            yield return StartCoroutine(LerpCharacterPositionRoutine(groundPos, levitateSpeed));
            StopAllWateringParticles(objectToActivateOnTable);

            // Attente de 1 seconde après le retour au sol avant le début de l'animation de la fleur
            if (waitBeforeFlowerDespawn > 0f)
            {
                Debug.Log($"[LiveCinematicTrigger] Jardinier reposé au sol : Attente de {waitBeforeFlowerDespawn}s avant l'animation de la fleur...");
                yield return new WaitForSeconds(waitBeforeFlowerDespawn);
            }

            // 8. La fleur joue son animation DevoveoDespawn
            Debug.Log($"[LiveCinematicTrigger] Déclenchement de l'animation '{flowerDespawnAnimName}' sur la fleur...");
            yield return StartCoroutine(PlayFinalObjectAnimationRoutine(flowerDespawnAnimName));

            // 9. Ce n'est qu'1 seconde APRÈS la fin de l'animation qu'il se dirige à nouveau vers la table
            if (waitBeforeReturnToTable > 0f)
            {
                Debug.Log($"[LiveCinematicTrigger] Attente de {waitBeforeReturnToTable}s après l'animation de la fleur...");
                yield return new WaitForSeconds(waitBeforeReturnToTable);
            }

            if (tableWaypoint != null)
            {
                Debug.Log($"[LiveCinematicTrigger] Le jardinier se dirige à nouveau vers la table ('{tableWaypoint.name}')...");
                yield return StartCoroutine(MoveCharacterToPosition(tableWaypoint.position, true, flipGameObject: true));
            }

            SetFacing(true, flipGameObject: true);

            // Réactiver l'objet désactivé plus tôt sur la table (laisse le deuxième objet activé aussi)
            if (objectToDeactivateOnTable != null)
            {
                objectToDeactivateOnTable.SetActive(true);
                Debug.Log($"[LiveCinematicTrigger] Objet sur la table '{objectToDeactivateOnTable.name}' RÉACTIVÉ.");
            }

            StopAllWateringParticles(objectToActivateOnTable);

            // Passage en animation Idle P1 lors de la réactivation de l'objet sur la table
            if (targetAnimator != null && !string.IsNullOrEmpty(returnIdleAnimName))
            {
                Debug.Log($"[LiveCinematicTrigger] Passage en animation '{returnIdleAnimName}' sur le jardinier à la table.");
                PlayAnimation(returnIdleAnimName);
            }

            // 10. Après 1 seconde, il s'envole vers le haut et est détruit quand il est hors champ
            if (waitBeforeFlyUp > 0f)
            {
                yield return new WaitForSeconds(waitBeforeFlyUp);
            }

            Debug.Log($"[LiveCinematicTrigger] Le jardinier s'envole vers le haut...");
            Vector3 flyTargetPos = targetCharacter.position + Vector3.up * flyUpHeight;
            yield return StartCoroutine(LerpCharacterPositionRoutine(flyTargetPos, flyUpSpeed));

            if (destroyGardenerOnExit && targetCharacter != null)
            {
                Debug.Log($"[LiveCinematicTrigger] Jardinier hors-champ : destruction de {targetCharacter.gameObject.name}.");
                Destroy(targetCharacter.gameObject);
            }
        }

        Debug.Log($"[LiveCinematicTrigger] Séquence cinématique en direct terminée avec succès sur '{gameObject.name}'.");
    }

    /// <summary>
    /// Déclenche l'animation de spawn sur l'objet du dernier point, attend sa fin complète,
    /// puis attend extraWaitAfterSpawnAnim secondes (1s par défaut) avant d'enchaîner.
    /// </summary>
    private IEnumerator PlayAndWaitForFinalObjectAnimationRoutine()
    {
        Animator targetAnim = finalObjectAnimator;

        // Si non renseigné dans l'Inspecteur, rechercher l'Animator à côté du dernier waypoint
        if (targetAnim == null && waypoints != null && waypoints.Count > 0)
        {
            Transform lastWaypoint = waypoints[waypoints.Count - 1];
            if (lastWaypoint != null)
            {
                // Rechercher un Animator sur le waypoint ou parmi ses proches enfants
                targetAnim = lastWaypoint.GetComponent<Animator>() ?? lastWaypoint.GetComponentInChildren<Animator>();

                if (targetAnim == null)
                {
                    // Recherche d'un Animator d'objet à proximité (excluant le PNJ Jardinier et le trigger)
                    Collider[] colliders = Physics.OverlapSphere(lastWaypoint.position, autoSearchRadius);
                    foreach (var col in colliders)
                    {
                        if (col == null || col.gameObject == gameObject) continue;
                        if (targetCharacter != null && (col.transform == targetCharacter || col.transform.IsChildOf(targetCharacter))) continue;

                        Animator anim = col.GetComponent<Animator>() ?? col.GetComponentInChildren<Animator>();
                        if (anim != null && anim != targetAnimator)
                        {
                            targetAnim = anim;
                            break;
                        }
                    }

                    // Recherche secondaire par type si toujours non trouvé
                    if (targetAnim == null)
                    {
                        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
                        float minDistSqr = autoSearchRadius * autoSearchRadius;
                        foreach (var anim in allAnimators)
                        {
                            if (anim == null || anim == targetAnimator) continue;
                            if (targetCharacter != null && anim.transform.IsChildOf(targetCharacter)) continue;
                            if (anim.transform.IsChildOf(transform)) continue;

                            float distSqr = (anim.transform.position - lastWaypoint.position).sqrMagnitude;
                            if (distSqr < minDistSqr)
                            {
                                minDistSqr = distSqr;
                                targetAnim = anim;
                            }
                        }
                    }
                }
            }
        }

        if (targetAnim != null && !string.IsNullOrEmpty(finalAnimationName))
        {
            Debug.Log($"[LiveCinematicTrigger] Déclenchement de l'animation finale '{finalAnimationName}' sur l'objet '{targetAnim.gameObject.name}'");
            
            targetAnim.enabled = true;
            targetAnim.Play(finalAnimationName);

            foreach (AnimatorControllerParameter param in targetAnim.parameters)
            {
                if (param.name == finalAnimationName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    targetAnim.SetTrigger(finalAnimationName);
                    break;
                }
            }

            yield return null;

            float clipLength = fallbackSpawnAnimDuration;
            if (targetAnim.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in targetAnim.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == finalAnimationName)
                    {
                        clipLength = clip.length;
                        break;
                    }
                }
            }

            AnimatorStateInfo stateInfo = targetAnim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0f && !float.IsInfinity(stateInfo.length) && !float.IsNaN(stateInfo.length) && stateInfo.length < 30f)
            {
                clipLength = stateInfo.length;
            }

            if (float.IsInfinity(clipLength) || float.IsNaN(clipLength) || clipLength <= 0f || clipLength > 30f)
            {
                clipLength = fallbackSpawnAnimDuration;
            }

            Debug.Log($"[LiveCinematicTrigger] Attente de la fin de l'animation '{finalAnimationName}' ({clipLength}s)...");
            yield return new WaitForSeconds(clipLength);
        }
        else
        {
            Debug.LogWarning($"[LiveCinematicTrigger] Impossible de jouer l'animation finale '{finalAnimationName}' : aucun Animator cible n'a été trouvé près du dernier point.");
        }

        // Attente supplémentaire de 1s (ou extraWaitAfterSpawnAnim) après la fin complète de l'animation
        if (extraWaitAfterSpawnAnim > 0f)
        {
            Debug.Log($"[LiveCinematicTrigger] Attente supplémentaire de {extraWaitAfterSpawnAnim}s après l'animation avant de partir vers la table.");
            yield return new WaitForSeconds(extraWaitAfterSpawnAnim);
        }
    }

    /// <summary>
    /// Joue le son de coupe (cutSoundClip) après le délai spécifié (0.3s par défaut) à partir du début de l'animation ShearClose.
    /// </summary>
    private IEnumerator PlayCutSoundRoutine(float delaySeconds)
    {
        if (cutSoundClip == null) yield break;

        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        Vector3 soundPos = targetCharacter != null ? targetCharacter.position : transform.position;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(cutSoundClip, cutSoundVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(cutSoundClip, soundPos, cutSoundVolume);
        }

        Debug.Log($"[LiveCinematicTrigger] Son de coupe '{cutSoundClip.name}' joué avec succès.");
    }

    /// <summary>
    /// Déclenche le fondu rouge sur la plante à côté puis change son sprite pour le sprite coupé à la fin de cutAnimDuration.
    /// </summary>
    private void TriggerFlowerCut(int index)
    {
        SpriteRenderer targetFlower = null;
        Sprite cutSprite = null;

        // 1. Chercher dans flowerObjects
        if (flowerObjects != null && index >= 0 && index < flowerObjects.Count && flowerObjects[index] != null)
        {
            targetFlower = flowerObjects[index];
        }

        // 2. Chercher dans cutFlowers
        if (targetFlower == null && cutFlowers != null && index >= 0 && index < cutFlowers.Count && cutFlowers[index] != null)
        {
            targetFlower = cutFlowers[index].flowerSpriteRenderer;
            if (cutSprite == null) cutSprite = cutFlowers[index].cutSprite;
        }

        // 3. Chercher le sprite coupé dans la liste cutSprites
        if (cutSprite == null && cutSprites != null && index >= 0 && index < cutSprites.Count)
        {
            cutSprite = cutSprites[index];
        }

        // 4. Si aucune plante n'a été spécifiée manuellement, recherche automatique du SpriteRenderer le plus proche du Waypoint
        if (targetFlower == null)
        {
            Vector3 searchPos = (waypoints != null && index >= 0 && index < waypoints.Count && waypoints[index] != null)
                ? waypoints[index].position
                : (targetCharacter != null ? targetCharacter.position : transform.position);

            targetFlower = FindNearestFlower(searchPos);
        }

        if (targetFlower != null)
        {
            Debug.Log($"[LiveCinematicTrigger] Lancement du fondu rouge sur la plante '{targetFlower.gameObject.name}' (Étape #{index + 1})");
            StartCoroutine(RedFadeCutRoutine(targetFlower, cutSprite, delayBeforeRedFade));
        }
        else
        {
            Debug.LogWarning($"[LiveCinematicTrigger] Aucune plante à proximité du waypoint #{index + 1} n'a été trouvée dans un rayon de {autoSearchRadius}m.");
        }
    }

    /// <summary>
    /// Recherche dans la scène le SpriteRenderer de la plante à côté la plus proche du Waypoint.
    /// </summary>
    private SpriteRenderer FindNearestFlower(Vector3 searchPosition)
    {
        SpriteRenderer nearestSprite = null;
        float minDistanceSqr = autoSearchRadius * autoSearchRadius;

        SpriteRenderer[] allSprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in allSprites)
        {
            if (sr == null || sr == targetSprite) continue;
            if (targetCharacter != null && (sr.transform == targetCharacter || sr.transform.IsChildOf(targetCharacter))) continue;
            if (sr.transform == transform || sr.transform.IsChildOf(transform)) continue;

            float distSqr = (sr.transform.position - searchPosition).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestSprite = sr;
            }
        }

        return nearestSprite;
    }

    /// <summary>
    /// Coroutine effectuant :
    /// 1. Une attente du délai spécifié (delayBeforeRedFade) après l'animation ShearClose.
    /// 2. Un fondu progressif de la couleur de la plante vers le ROUGE pendant cutAnimDuration.
    /// 3. Dès que cutAnimDuration est terminée : désactivation de l'Animator et bascule instantanée vers le sprite coupé.
    /// </summary>
    private IEnumerator RedFadeCutRoutine(SpriteRenderer flowerRenderer, Sprite newCutSprite, float delaySeconds)
    {
        if (flowerRenderer == null) yield break;

        GameObject flowerObj = flowerRenderer.gameObject;
        Transform flowerTransform = flowerRenderer.transform;
        Color originalColor = flowerRenderer.color;

        // 1. Attendre le délai après l'exécution de l'animation ShearClose
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        // 2. Fondu progressif de la couleur d'origine vers la couleur rouge (redFadeColor) pendant cutAnimDuration
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, cutAnimDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Translucidité / Fondu fluide vers le rouge
            flowerRenderer.color = Color.Lerp(originalColor, redFadeColor, t);

            yield return null;
        }

        flowerRenderer.color = redFadeColor;

        // 3. Une fois cutAnimDuration terminée : Désactiver l'Animator pour éviter qu'il n'écrase le sprite
        Animator flowerAnim = flowerObj.GetComponent<Animator>() ?? flowerObj.GetComponentInChildren<Animator>();
        if (flowerAnim != null)
        {
            flowerAnim.enabled = false;
        }

        // Réinitialiser la couleur et appliquer le sprite coupé
        flowerRenderer.color = originalColor;

        if (newCutSprite != null)
        {
            flowerRenderer.sprite = newCutSprite;
            Debug.Log($"[LiveCinematicTrigger] Plante '{flowerObj.name}' basculée avec succès sur le sprite coupé '{newCutSprite.name}' !");
        }
        else
        {
            Debug.LogWarning($"[LiveCinematicTrigger] Aucun sprite coupé (cutSprite) n'a été spécifié pour '{flowerObj.name}'. Veuillez remplir la liste Cut Sprites dans l'inspecteur.");
        }
    }

    private float initialAbsScaleX = 1f;
    private bool hasSavedInitialScale = false;

    private void SaveInitialScale()
    {
        if (!hasSavedInitialScale && targetCharacter != null)
        {
            initialAbsScaleX = Mathf.Abs(targetCharacter.localScale.x);
            if (Mathf.Approximately(initialAbsScaleX, 0f)) initialAbsScaleX = 1f;
            hasSavedInitialScale = true;
        }
    }

    /// <summary>
    /// Déplace le personnage de manière fluide jusqu'à la position cible.
    /// Ajuste l'orientation pendant le déplacement selon flipDuringMove et flipGameObject.
    /// </summary>
    private IEnumerator MoveCharacterToPosition(Vector3 destination, bool flipDuringMove = true, bool flipGameObject = false)
    {
        if (targetCharacter == null) yield break;

        // Définir l'orientation pendant le déplacement
        SetFacing(flipDuringMove, flipGameObject);

        float sqrStopDistance = stopDistance * stopDistance;
        float elapsed = 0f;
        float maxTimeout = 15f;

        while (elapsed < maxTimeout)
        {
            Vector3 currentPos = targetCharacter.position;
            Vector3 direction = destination - currentPos;

            float distanceSqr = direction.sqrMagnitude;

            if (distanceSqr <= sqrStopDistance)
            {
                break;
            }

            Vector3 moveDirection = direction.normalized;
            float step = moveSpeed * Time.deltaTime;

            if (step * step > distanceSqr)
            {
                step = Mathf.Sqrt(distanceSqr);
            }

            Vector3 movement = moveDirection * step;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement);
            }
            else if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(currentPos + movement);
            }
            else
            {
                targetCharacter.position = currentPos + movement;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetCharacter.position = destination;
    }

    /// <summary>
    /// Déplace le personnage de manière fluide jusqu'à la position cible spécifiée à une vitesse donnée.
    /// Utile pour l'élévation, la descente et l'envol.
    /// </summary>
    private IEnumerator LerpCharacterPositionRoutine(Vector3 destination, float speed)
    {
        if (targetCharacter == null) yield break;

        float effectiveSpeed = Mathf.Max(0.1f, speed);
        float sqrStopDist = stopDistance * stopDistance;
        float elapsed = 0f;
        float maxTimeout = 15f;

        while (elapsed < maxTimeout)
        {
            Vector3 currentPos = targetCharacter.position;
            Vector3 direction = destination - currentPos;
            float distSqr = direction.sqrMagnitude;

            if (distSqr <= sqrStopDist) break;

            Vector3 moveDir = direction.normalized;
            float step = effectiveSpeed * Time.deltaTime;
            if (step * step > distSqr)
            {
                step = Mathf.Sqrt(distSqr);
            }

            Vector3 movement = moveDir * step;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement);
            }
            else if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(currentPos + movement);
            }
            else
            {
                targetCharacter.position = currentPos + movement;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetCharacter.position = destination;
    }

    /// <summary>
    /// Déclenche l'animation spécifiée (ex: DevoveoDespawn) sur l'objet du dernier point,
    /// et attend sa fin complète avant de continuer.
    /// </summary>
    private IEnumerator PlayFinalObjectAnimationRoutine(string animNameOverride = null)
    {
        string animToPlay = !string.IsNullOrEmpty(animNameOverride) ? animNameOverride : finalAnimationName;
        Animator targetAnim = finalObjectAnimator;

        // Si non renseigné dans l'Inspecteur, rechercher l'Animator à côté du dernier waypoint
        if (targetAnim == null && waypoints != null && waypoints.Count > 0)
        {
            Transform lastWaypoint = waypoints[waypoints.Count - 1];
            if (lastWaypoint != null)
            {
                targetAnim = lastWaypoint.GetComponent<Animator>() ?? lastWaypoint.GetComponentInChildren<Animator>();

                if (targetAnim == null)
                {
                    Collider[] colliders = Physics.OverlapSphere(lastWaypoint.position, autoSearchRadius);
                    foreach (var col in colliders)
                    {
                        if (col == null || col.gameObject == gameObject) continue;
                        if (targetCharacter != null && (col.transform == targetCharacter || col.transform.IsChildOf(targetCharacter))) continue;

                        Animator anim = col.GetComponent<Animator>() ?? col.GetComponentInChildren<Animator>();
                        if (anim != null && anim != targetAnimator)
                        {
                            targetAnim = anim;
                            break;
                        }
                    }

                    if (targetAnim == null)
                    {
                        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
                        float minDistSqr = autoSearchRadius * autoSearchRadius;
                        foreach (var anim in allAnimators)
                        {
                            if (anim == null || anim == targetAnimator) continue;
                            if (targetCharacter != null && anim.transform.IsChildOf(targetCharacter)) continue;
                            if (anim.transform.IsChildOf(transform)) continue;

                            float distSqr = (anim.transform.position - lastWaypoint.position).sqrMagnitude;
                            if (distSqr < minDistSqr)
                            {
                                minDistSqr = distSqr;
                                targetAnim = anim;
                            }
                        }
                    }
                }
            }
        }

        if (targetAnim != null && !string.IsNullOrEmpty(animToPlay))
        {
            Debug.Log($"[LiveCinematicTrigger] Déclenchement de l'animation '{animToPlay}' sur l'objet '{targetAnim.gameObject.name}'");

            targetAnim.enabled = true;
            targetAnim.speed = 1f;
            targetAnim.Play(animToPlay, 0, 0f);

            foreach (AnimatorControllerParameter param in targetAnim.parameters)
            {
                if (param.name == animToPlay && param.type == AnimatorControllerParameterType.Trigger)
                {
                    targetAnim.SetTrigger(animToPlay);
                    break;
                }
            }

            yield return null;

            float clipLength = fallbackSpawnAnimDuration;
            if (targetAnim.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in targetAnim.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == animToPlay)
                    {
                        clipLength = clip.length;
                        break;
                    }
                }
            }

            AnimatorStateInfo stateInfo = targetAnim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0f && !float.IsInfinity(stateInfo.length) && !float.IsNaN(stateInfo.length) && stateInfo.length < 30f)
            {
                clipLength = stateInfo.length;
            }

            if (float.IsInfinity(clipLength) || float.IsNaN(clipLength) || clipLength <= 0f || clipLength > 30f)
            {
                clipLength = fallbackSpawnAnimDuration;
            }

            Debug.Log($"[LiveCinematicTrigger] Attente de la fin de l'animation '{animToPlay}' ({clipLength}s)...");
            yield return new WaitForSeconds(clipLength);
        }
        else
        {
            Debug.LogWarning($"[LiveCinematicTrigger] Impossible de jouer l'animation finale '{animToPlay}' : aucun Animator cible trouvé.");
        }
    }

    /// <summary>
    /// Arrête et efface immédiatement TOUTES les particules d'eau et composants d'arrosage dans un objet.
    /// </summary>
    private void StopAllWateringParticles(GameObject container)
    {
        if (container == null) return;

        HandWateringCan[] cans = container.GetComponentsInChildren<HandWateringCan>(true);
        foreach (var can in cans)
        {
            if (can != null) can.StopWatering();
        }

        ParticleSystem[] particleSystems = container.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.playOnAwake = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }
        }
    }

    /// <summary>
    /// Oriente le jardinier :
    /// isFlipped : true pour retourner, false pour l'orientation de base.
    /// flipGameObject : true pour retourner l'échelle de tout le GameObject (scale.x) au lieu de juste le sprite (nécessaire à la table pour les objets enfants).
    /// </summary>
    private void SetFacing(bool isFlipped, bool flipGameObject = false)
    {
        SaveInitialScale();

        bool shouldFlip = isFlipped;
        if (invertFlip) shouldFlip = !shouldFlip;

        if (flipGameObject && targetCharacter != null)
        {
            // Réinitialiser le flipX des sprites pour éviter une double inversion avec la scale
            if (targetSprite != null) targetSprite.flipX = false;

            SpriteRenderer[] allSprites = targetCharacter.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allSprites)
            {
                if (sr != null && (activeSpeechBubble == null || sr.gameObject != activeSpeechBubble.gameObject))
                {
                    sr.flipX = false;
                }
            }

            // Inverser l'échelle X de l'objet principal pour retourner tous les objets enfants (outils, etc.)
            Vector3 currentScale = targetCharacter.localScale;
            float targetScaleX = shouldFlip ? -initialAbsScaleX : initialAbsScaleX;
            targetCharacter.localScale = new Vector3(targetScaleX, currentScale.y, currentScale.z);
        }
        else
        {
            // Réinitialiser l'échelle X à sa valeur d'origine
            if (targetCharacter != null)
            {
                Vector3 currentScale = targetCharacter.localScale;
                targetCharacter.localScale = new Vector3(initialAbsScaleX, currentScale.y, currentScale.z);
            }

            // Inverser SpriteRenderer.flipX
            if (targetSprite != null)
            {
                targetSprite.flipX = shouldFlip;
            }

            if (targetCharacter != null)
            {
                SpriteRenderer[] allSprites = targetCharacter.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in allSprites)
                {
                    if (sr != null && (activeSpeechBubble == null || sr.gameObject != activeSpeechBubble.gameObject))
                    {
                        sr.flipX = shouldFlip;
                    }
                }
            }
        }
    }

    private void PlayAnimation(string animName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(animName)) return;

        targetAnimator.Play(animName);

        foreach (AnimatorControllerParameter param in targetAnimator.parameters)
        {
            if (param.name == animName && param.type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.SetTrigger(animName);
                break;
            }
        }
    }

    private void OnDisable()
    {
        if (activeSpeechBubble != null)
        {
            activeSpeechBubble.Clear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.3f);
            Gizmos.DrawCube(transform.position, col.bounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }

        Transform startPoint = targetCharacter != null ? targetCharacter : transform;
        Vector3 previousPos = startPoint.position;

        if (waypoints != null && waypoints.Count > 0)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                Transform wp = waypoints[i];
                if (wp == null) continue;

                Color pointColor = Color.HSVToRGB((i * 0.25f) % 1.0f, 0.8f, 1.0f);
                Gizmos.color = pointColor;

                Gizmos.DrawSphere(wp.position, 0.25f);
                Gizmos.DrawWireSphere(wp.position, 0.35f);

                Gizmos.color = new Color(pointColor.r, pointColor.g, pointColor.b, 0.7f);
                Gizmos.DrawLine(previousPos, wp.position);

                previousPos = wp.position;
            }
        }

        if (enableTableSequence && tableWaypoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(tableWaypoint.position, 0.25f);
            Gizmos.DrawWireSphere(tableWaypoint.position, 0.35f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
            Gizmos.DrawLine(previousPos, tableWaypoint.position);

            Vector3 returnTarget = (returnWaypoint != null) ? returnWaypoint.position : startPoint.position;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(returnTarget, 0.25f);
            Gizmos.color = new Color(0f, 1f, 0f, 0.7f);
            Gizmos.DrawLine(tableWaypoint.position, returnTarget);
        }
    }
}
