using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôle l'animation et la synchronisation du Tournesol qui joue du Xylophone.
/// Gère la lecture à partir d'un fichier WAV complet OU note par note.
/// Déplace les baguettes (gauche/droite) pour frapper les lames au rythme de la musique.
/// Anime le sinking (enfoncement) et le scale pop (agrandissement) des lames à l'impact.
/// Se met automatiquement en pause lors des dialogues et reprend X secondes après la fin du dialogue.
/// </summary>
[AddComponentMenu("2.5D RPG/World/Sunflower Xylophone")]
public class SunflowerXylophone : MonoBehaviour
{
    [System.Serializable]
    public struct MelodyNote
    {
        [Tooltip("Index de la lame (0 = plus grave / gauche, N = plus aigu / droite)")]
        public int keyIndex;

        [Tooltip("Timestamp exact en secondes depuis le début de la chanson (utilisé avec useFullWavAudio)")]
        public float timestamp;

        [Tooltip("Durée/délai en battements avant la note suivante (utilisé en mode BPM sans WAV)")]
        public float beatDuration;

        [Tooltip("Force l'utilisation de la baguette gauche (true) ou droite (false)")]
        public bool forceCustomMallet;
        public bool useLeftMallet;

        public MelodyNote(int keyIndex, float timestamp, float beatDuration = 1.0f)
        {
            this.keyIndex = keyIndex;
            this.timestamp = timestamp;
            this.beatDuration = beatDuration;
            this.forceCustomMallet = false;
            this.useLeftMallet = false;
        }
    }

    [Header("Musique & Audio")]
    [Tooltip("Clip audio WAV complet de la chanson")]
    public AudioClip fullMelodyWavClip;

    [Tooltip("Si vrai, joue le fichier WAV complet et synchronise les baguettes selon les timestamps")]
    public bool useFullWavAudio = true;

    [Tooltip("Jouer aussi le son de la lame individuelle lors de l'impact (décochez si le son du xylophone est déjà dans le fichier WAV)")]
    public bool playKeySoundsWithWav = false;

    [Header("Atténuation Spatialisée (Son 3D selon la distance)")]
    [Tooltip("Si vrai, le volume du xylophone diminue à mesure que le joueur s'éloigne et devient inaudible à grande distance.")]
    public bool enableDistanceAttenuation = true;

    [Tooltip("Volume maximal quand le joueur est tout proche du xylophone (0.0 à 1.0).")]
    [Range(0f, 1f)]
    public float maxAudioVolume = 1.0f;

    [Tooltip("Distance 3D (en mètres) jusqu'à laquelle le xylophone est entendu au volume maximal (100%).")]
    public float minAudioDistance = 4.0f;

    [Tooltip("Distance 3D (en mètres) à partir de laquelle le xylophone est totalement inaudible (volume = 0%).")]
    public float maxAudioDistance = 16.0f;

    [Tooltip("Distance verticale maximale (en mètres). Si le joueur se trouve sur un autre étage ou niveau (ex: après une téléportation en hauteur), le xylophone devient inaudible.")]
    public float maxVerticalDistance = 7.0f;

    [Tooltip("Intensité de la spatialisation 3D (0.0 = son 2D partout, 1.0 = son 3D spatialisé).")]
    [Range(0f, 1f)]
    public float spatialBlend = 1.0f;

    [Tooltip("Mode d'atténuation Unity (Linear = diminution régulière, Logarithmic = décrément progressif).")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Pause lors du Dialogue")]
    [Tooltip("Met automatiquement en pause le xylophone si un dialogue commence, puis reprend après la fin du dialogue.")]
    public bool pauseDuringDialogue = true;

    [Tooltip("Si vrai, le xylophone ne se met en pause QUE si le dialogue est avec le Tournesol (et continue de jouer pour les autres PNJ).")]
    public bool pauseOnlyForSunflowerDialogue = true;

    [Tooltip("Délai (en secondes) à attendre après la fin du dialogue avant de reprendre le xylophone.")]
    public float resumeDelayAfterDialogue = 1.0f;

    [Header("Lames du Xylophone (Du grave au plus aigu)")]
    [Tooltip("Liste des Transform des lames du xylophone (ordonnées de gauche à droite)")]
    public Transform[] keys;

    [Tooltip("Sons des lames individuelles (si mode note par note ou playKeySoundsWithWav actif)")]
    public AudioClip[] keyAudioClips;

    [Tooltip("Composant AudioSource principal pour jouer le fichier WAV ou les notes")]
    public AudioSource audioSource;

    [Header("Baguettes / Batons")]
    [Tooltip("Baguette de gauche (côté notes graves)")]
    public Transform leftMallet;

    [Tooltip("Baguette de droite (côté notes aiguës)")]
    public Transform rightMallet;

    [Tooltip("Point de repos optionnel pour la baguette gauche quand elle est inactive")]
    public Transform leftRestPoint;

    [Tooltip("Point de repos optionnel pour la baguette droite quand elle est inactive")]
    public Transform rightRestPoint;

    [Header("Répartition des Baguettes")]
    [Tooltip("Index de la lame qui sépare les notes graves (baguette gauche) des aiguës (baguette droite). Exemple: 4 sur 8 lames.")]
    public int highPitchSplitIndex = 4;

    [Tooltip("Inverser les baguettes (si la baguette gauche doit jouer les aiguës et inversement)")]
    public bool invertMallets = false;

    [Header("Paramètres d'Animation des Baguettes")]
    [Tooltip("Hauteur de survol de la baguette au-dessus de la lame (en unités monde)")]
    public float hoverHeight = 0.35f;

    [Tooltip("Temps de déplacement horizontal vers la lame (secondes)")]
    public float moveDuration = 0.12f;

    [Tooltip("Temps d'impact vers le bas pour frapper la lame (secondes)")]
    public float strikeDownDuration = 0.05f;

    [Tooltip("Temps de remontée de la baguette après la frappe (secondes)")]
    public float retractUpDuration = 0.08f;

    [Tooltip("Angle d'inclinaison (rotation X en degrés) lors de la frappe")]
    public float strikeTiltAngle = 20f;

    [Tooltip("Courbe de lissage pour le déplacement")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation de la Lame à l'Impact")]
    [Tooltip("Enfoncement vertical de la lame vers le bas à la frappe")]
    public float keySinkDepth = 0.05f;

    [Tooltip("Multiplicateur d'agrandissement (scale) de la lame lors de la frappe (ex: 1.15 = 15% plus grand)")]
    public float keyHitScaleMultiplier = 1.15f;

    [Tooltip("Durée totale du rebond/agrandissement de la lame (secondes)")]
    public float keyBounceDuration = 0.12f;

    [Header("Réglages de la Mélodie")]
    [Tooltip("Lancer automatiquement au démarrage")]
    public bool playOnStart = true;

    [Tooltip("Boucler la mélodie une fois terminée")]
    public bool loopMelody = true;

    [Tooltip("Délai de pause avant de répéter la mélodie bouclée (en secondes)")]
    public float loopDelay = 1.5f;

    [Tooltip("Tempo en BPM (utilisé si useFullWavAudio est décoché)")]
    public float bpm = 120f;

    [Tooltip("Liste des notes de la partition")]
    public List<MelodyNote> melody = new List<MelodyNote>();

    [System.Serializable]
    public struct ItemAudioSequence
    {
        [Tooltip("Nom/Description de cet objet (ex: Objet 1, Flûte, etc.)")]
        public string itemName;

        [Tooltip("Index de la lame principale associée (0 à 7)")]
        public int keyIndex;

        [Tooltip("Dialogue optionnel joué au Tournesol AVANT de lancer la musique de cet objet.")]
        public DialogueData itemDialogue;

        [Tooltip("Clip audio complet/morceau spécifique joué pour cet objet")]
        public AudioClip audioClip;

        [Tooltip("Couleur de feedback visuel sur le xylophone")]
        public Color itemColor;

        [Tooltip("Liste des frappes de baguettes synchronisées avec l'audio (index de la lame + timestamp en secondes)")]
        public List<MelodyNote> melodyNotes;
    }

    [Header("Délais d'Interaction avec Objets Portés (6 Objets)")]
    [Tooltip("Délai de pause (en secondes) après l'interaction avant de jouer le morceau de l'objet.")]
    public float delayBeforeItemNote = 1.0f;

    [Tooltip("Délai de pause (en secondes) après le morceau avant que le Tournesol ne reprenne son cycle.")]
    public float delayAfterItemNote = 0.5f;

    [Tooltip("Si vrai, reprend la mélodie de fond après le son de l'objet. Si faux (par défaut), le Tournesol s'arrête complètement de jouer quand le son est fini.")]
    public bool resumeMelodyAfterItem = false;

    [Tooltip("Subdivision du tempo BPM pour les frappes des objets (1.0 = chaque beat, 0.5 = croche / 2 frappes par beat).")]
    public float itemTapBeatSubdivision = 0.5f;

    [Tooltip("Configuration facultative des séquences audio et animations de frappes pour chacun des 6 objets.")]
    public ItemAudioSequence[] itemAudioSequences;

    [Header("Jeu Aléatoire Hors Dialogue (6 à 10s)")]
    [Tooltip("Activer le jeu automatique de sons aléatoires dans une liste quand le joueur ne lui parle pas")]
    public bool enableRandomIdleAudios = true;

    [Tooltip("Délai minimum en secondes entre deux morceaux aléatoires (défaut : 6s)")]
    public float minRandomIdleDelay = 6.0f;

    [Tooltip("Délai maximum en secondes entre deux morceaux aléatoires (défaut : 10s)")]
    public float maxRandomIdleDelay = 10.0f;

    [Tooltip("Liste des clips audio parmi lesquels le Tournesol choisit aléatoirement quand le joueur ne lui parle pas")]
    public AudioClip[] randomIdleAudioClips;

    [Header("Concert Spécial Rock Event")]
    [Tooltip("Transform du personnage Tournesol (situé juste derrière le xylophone). Si vide, cherchera le personnage automatiquement.")]
    public Transform sunflowerCharacter;

    [Tooltip("Lumière Spotlight de concert qui éclaire le tournesol en lévitation")]
    public Light concertSpotlight;

    [Tooltip("Intensité maximale du faisceau Spotlight de concert (ex: 30-50 pour un éclairage très intense)")]
    public float concertSpotlightIntensity = 35.0f;

    [Tooltip("Intensité de la lumière directe d'accentuation (Fill Light) collée au Tournesol (ex: 15-30)")]
    public float concertFillLightIntensity = 25.0f;

    [Tooltip("Couleur des lumières de concert illuminant le Tournesol")]
    public Color concertSpotlightColor = new Color(1.0f, 0.98f, 0.88f);

    [Tooltip("Premier emplacement d'apparition des colonnes de feu")]
    public Transform firePoint1;

    [Tooltip("Second emplacement d'apparition des colonnes de feu")]
    public Transform firePoint2;

    [Tooltip("Prefab des colonnes de feu à instancier aux 2 points")]
    public GameObject fireColumnPrefab;

    [Tooltip("Hauteur de lévitation du Tournesol dans les airs (ex: 1.5m)")]
    public float levitateHeight = 1.5f;

    [Tooltip("Durée de lévitation / descente (secondes)")]
    public float levitateDuration = 2.0f;

    [Tooltip("Nom de l'état ou du booléen d'animation Rock (ex: 'DevoveoGuitar' ou 'Rock')")]
    public string rockAnimationState = "DevoveoGuitar";

    [Tooltip("Nom de l'animation Devoveo (animation de base du personnage) jouée juste avant qu'il se repose au sol (ex: 'Devoveo' ou 'Idle').")]
    public string devoveoAnimationState = "Devoveo";

    [Header("Lévitation en Infini (\u221e)")]
    [Tooltip("Amplitude du mouvement en lemniscate (∞) pendant la lévitation (ex: 0.4 = 40cm de balancement)")]
    public float infinityDriftAmplitude = 0.4f;

    [Tooltip("Vitesse de parcours de la trajectoire en infini (ex: 0.6 = un tour de ∞ toutes les ~10 secondes)")]
    public float infinityDriftSpeed = 0.55f;

    [Tooltip("Cascade cylindrique en arrière-plan (si vide, cherchera automatiquement dans la scène)")]
    public CylindricalWaterfall backgroundWaterfall;

    [Tooltip("Durée du fondus d'apparition audio (Fade In) en secondes (ex: 2.0s)")]
    public float audioFadeInDuration = 2.0f;

    [Header("Pulsations de Caméra sur le Beat")]
    [Tooltip("Activer les pulsations/zooms rythmiques de la caméra à chaque temps de la musique (Beat Pulse)")]
    public bool enableCameraBeatPulse = true;

    [Tooltip("Intensité de la pulsation de zoom (FOV Punch en degrés, ex: 3.0)")]
    public float beatPulseAmount = 3.0f;

    [Tooltip("Vitesse de retour de la caméra après chaque pulsation du beat")]
    public float beatPulseReturnSpeed = 14.0f;

    [Tooltip("Dézoom progressif : nombre de degrés de FOV ajoutés au fil de la musique (ex: 14 = +14\u00b0 du début à la fin). Met 0 pour désactiver.")]
    public float progressiveZoomOutAmount = 14.0f;

    [Header("Concert Rock - Effets Avancés")]
    [Tooltip("Intensité du tremblement caméra sur chaque beat (ex: 0.06)")]
    public float beatCameraShakeIntensity = 0.06f;

    [Tooltip("Intensité renforcée du shake caméra après l'apparition des flammes (ex: 0.12)")]
    public float fireBeatCameraShakeIntensity = 0.13f;

    [Tooltip("Décalage (s) du premier temps dans la musique, pour caler le punch pile sur la grosse caisse")]
    public float concertBeatOffset = 0f;

    [Tooltip("Rotation (roll) en degrés du punch caméra sur chaque beat (avant les flammes)")]
    public float beatShakeRollDegrees = 0.8f;

    [Tooltip("Rotation (roll) en degrés du punch caméra sur chaque beat (après les flammes)")]
    public float fireBeatShakeRollDegrees = 2.2f;

    [Tooltip("Vitesse d'amortissement du punch (plus haut = plus sec / claquant)")]
    public float beatShakeDecay = 11f;

    [Tooltip("Fréquence du rebond ressort après l'impact (Hz)")]
    public float beatShakeSpringFrequency = 6f;

    [Tooltip("Multiplicateur d'impact sur le 1er temps de chaque mesure")]
    public float downbeatAccent = 1.6f;

    [Tooltip("Nombre de temps par mesure (pour l'accent du 1er temps)")]
    public int beatsPerBar = 4;

    // Offset de shake appliqué uniquement au moment du rendu (n'interfère pas avec Cinemachine)
    private Camera concertShakeCam;
    private Vector3 concertShakeOffset;
    private float concertShakeRoll;
    private Vector3 concertShakeSavedPos;
    private Quaternion concertShakeSavedRot;
    private bool concertShakeApplied;

    [Tooltip("Activer l'effet de lumières de scène rotatives et colorées (spotlights de concert)")]
    public bool enableStageLights = true;

    [Tooltip("Nombre de projecteurs latéraux de scène générés dynamiquement (ex: 4)")]
    public int stageLightCount = 4;

    [Tooltip("Afficher un titre 'ROCK CONCERT' en style punk au début de la cinématique")]
    public bool showConcertTitleCard = true;

    [Tooltip("Durée d'affichage du titre (ex: 2.5s)")]
    public float concertTitleDuration = 2.5f;

    [Tooltip("Couleur du titre punk (ex: rouge vif)")]
    public Color concertTitleColor = new Color(1f, 0.08f, 0.08f);

    [Tooltip("Intensité du flash blanc au spawn des colonnes de feu (ex: 2.0)")]
    public float fireSpawnFlashIntensity = 2.0f;

    // État interne
    private bool isPlaying = false;
    private bool isPausedForDialogue = false;
    private bool wasDialogueActive = false;
    private Coroutine melodyCoroutine;
    private Coroutine resumeDialogueRoutine;
    private Coroutine itemInteractionRoutine;
    private Coroutine idleRandomRoutine;

    private Vector3 leftInitialPos;
    private Vector3 rightInitialPos;
    private Quaternion leftInitialRot;
    private Quaternion rightInitialRot;

    private Coroutine leftMalletRoutine;
    private Coroutine rightMalletRoutine;

    // Dictionnaire pour conserver les positions et scales de base originaux des lames
    private Dictionary<Transform, Vector3> defaultKeyLocalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> defaultKeyLocalScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Coroutine> activeKeyBounceRoutines = new Dictionary<Transform, Coroutine>();

    private void Awake()
    {
        ConfigureAudioSource();

        if (leftMallet != null)
        {
            leftInitialPos = leftRestPoint != null ? leftRestPoint.position : leftMallet.position;
            leftInitialRot = leftRestPoint != null ? leftRestPoint.rotation : leftMallet.rotation;
        }

        if (rightMallet != null)
        {
            rightInitialPos = rightRestPoint != null ? rightRestPoint.position : rightMallet.position;
            rightInitialRot = rightRestPoint != null ? rightRestPoint.rotation : rightMallet.rotation;
        }

        CacheDefaultKeyTransforms();

        // S'assurer que le modèle de guitare 3D est masqué au repos
        Transform sun = GetSunflowerCharacterTransform();
        if (sun != null)
        {
            Transform[] sunTransforms = sun.GetComponentsInChildren<Transform>(true);
            foreach (var t in sunTransforms)
            {
                if (t != null && t.name == "scene (1)")
                {
                    t.gameObject.SetActive(false);
                }
            }
        }
    }

    private void CacheDefaultKeyTransforms()
    {
        if (keys != null)
        {
            foreach (Transform k in keys)
            {
                if (k != null && !defaultKeyLocalPositions.ContainsKey(k))
                {
                    defaultKeyLocalPositions[k] = k.localPosition;
                    defaultKeyLocalScales[k] = k.localScale;
                }
            }
        }
    }

    private void Start()
    {
        // S'assurer que les distances d'atténuation permettent de ne l'entendre que lorsqu'on est proche
        if (maxAudioDistance > 20f)
        {
            maxAudioDistance = 16f;
        }
        if (minAudioDistance > 6f)
        {
            minAudioDistance = 4f;
        }

        if (enableRandomIdleAudios)
        {
            // Si les audios aléatoires automatiques sont activés, on stoppe la mélodie fixe de fond
            // pour éviter toute interférence/conflit sur l'AudioSource !
            StopMelody();
            StartIdleRandomRoutine();
        }
        else if (playOnStart)
        {
            PlayMelody();
        }
    }

    private Transform cachedPlayerTransform;

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (audioSource != null)
        {
            audioSource.dopplerLevel = 0f;
            audioSource.pitch = 1.0f;

            if (enableDistanceAttenuation)
            {
                audioSource.spatialBlend = spatialBlend;
                audioSource.minDistance = minAudioDistance;
                audioSource.maxDistance = maxAudioDistance;
                audioSource.rolloffMode = rolloffMode;
            }
        }
    }

    private void UpdateDistanceAttenuation()
    {
        if (audioSource == null) return;

        audioSource.dopplerLevel = 0f;
        audioSource.pitch = 1.0f;

        if (!enableDistanceAttenuation)
        {
            audioSource.spatialBlend = 0f;
            return;
        }

        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minAudioDistance;
        audioSource.maxDistance = maxAudioDistance;
        audioSource.rolloffMode = rolloffMode;

        if (!audioSource.isPlaying && !isPlaying) return;

        // Si le joueur est en train d'utiliser une fleur de téléportation, couper immédiatement le son
        if (TeleportFlower.IsAnyFlowerTeleporting)
        {
            audioSource.volume = 0f;
            return;
        }

        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerObj = pm.gameObject;
            }
            if (playerObj != null) cachedPlayerTransform = playerObj.transform;
        }

        if (cachedPlayerTransform != null)
        {
            Vector3 selfPos = transform.position;
            Vector3 playerPos = cachedPlayerTransform.position;

            // Vérification de la distance verticale : si le joueur est sur un autre étage/niveau (ex: téléporté en hauteur ou en fosse)
            float verticalDistance = Mathf.Abs(playerPos.y - selfPos.y);
            if (maxVerticalDistance > 0f && verticalDistance > maxVerticalDistance)
            {
                audioSource.volume = 0f;
                return;
            }

            // Calcul de la distance 3D réelle (prend en compte X, Y et Z)
            float distance = Vector3.Distance(selfPos, playerPos);

            float volumeFactor = 1.0f;
            if (distance <= minAudioDistance)
            {
                volumeFactor = 1.0f;
            }
            else if (distance >= maxAudioDistance)
            {
                volumeFactor = 0.0f;
            }
            else
            {
                float t = (distance - minAudioDistance) / Mathf.Max(0.001f, maxAudioDistance - minAudioDistance);
                if (rolloffMode == AudioRolloffMode.Linear)
                {
                    volumeFactor = Mathf.Clamp01(1.0f - t);
                }
                else
                {
                    volumeFactor = Mathf.Clamp01(1.0f - Mathf.SmoothStep(0f, 1f, t));
                }
            }

            audioSource.volume = maxAudioVolume * volumeFactor;
        }
    }

    private void Update()
    {
        UpdateDistanceAttenuation();

        // Auto-pause et reprise lors des dialogues
        if (pauseDuringDialogue && DialogueManager.Instance != null)
        {
            bool isDialogueActive = DialogueManager.Instance.IsDialogueActive;

            // Début du dialogue -> mettre en pause le xylophone uniquement s'il s'agit du Tournesol
            if (isDialogueActive && !wasDialogueActive)
            {
                bool shouldPause = !pauseOnlyForSunflowerDialogue || IsDialogueWithSunflower();
                if (shouldPause && (isPlaying || (audioSource != null && audioSource.isPlaying)))
                {
                    PauseMelodyForDialogue();
                }
            }
            // Fin du dialogue -> attendre le délai puis reprendre la musique s'il était en pause
            else if (!isDialogueActive && wasDialogueActive)
            {
                if (isPausedForDialogue)
                {
                    if (resumeDialogueRoutine != null) StopCoroutine(resumeDialogueRoutine);
                    resumeDialogueRoutine = StartCoroutine(ResumeAfterDialogueDelayRoutine());
                }
            }

            wasDialogueActive = isDialogueActive;
        }
    }

    private bool IsDialogueWithSunflower()
    {
        // 1. Vérifier la distance avec le joueur : si le joueur est loin du tournesol, ce n'est pas son dialogue !
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }

        if (playerObj != null)
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist > 7.0f)
            {
                return false;
            }
        }

        // 2. Si un composant DialogueTrigger est présent sur cet objet ou ses enfants/parents
        DialogueTrigger dt = GetComponent<DialogueTrigger>();
        if (dt == null) dt = GetComponentInParent<DialogueTrigger>();
        if (dt == null) dt = GetComponentInChildren<DialogueTrigger>();

        if (dt != null)
        {
            return true;
        }

        // 3. Fallback nom d'objet
        string objName = gameObject.name.ToLower();
        if (objName.Contains("devoveo") || objName.Contains("tournesol") || objName.Contains("sunflower") || objName.Contains("xylo"))
        {
            return true;
        }

        return false;
    }

    public void PauseMelodyForDialogue()
    {
        isPausedForDialogue = true;
        if (resumeDialogueRoutine != null)
        {
            StopCoroutine(resumeDialogueRoutine);
            resumeDialogueRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }

        ReturnMalletsToRest();
    }

    private IEnumerator ResumeAfterDialogueDelayRoutine()
    {
        yield return new WaitForSeconds(resumeDelayAfterDialogue);

        if (isPausedForDialogue)
        {
            isPausedForDialogue = false;

            if (useFullWavAudio && fullMelodyWavClip != null && audioSource != null)
            {
                audioSource.UnPause();
            }
            else if (!isPlaying)
            {
                PlayMelody();
            }
        }
    }

    /// <summary>
    /// Active ou masque l'ensemble du modèle 3D du xylophone, ses lames et ses baguettes, et contrôle sa musique.
    /// </summary>
    public void SetXylophoneVisualsAndAudioActive(bool active)
    {
        if (active)
        {
            // Réactiver les visuels du xylophone
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !r.gameObject.name.ToLower().Contains("devoveo") && !r.gameObject.name.ToLower().Contains("tournesol"))
                {
                    r.enabled = true;
                }
            }
            PlayMelody();
        }
        else
        {
            // Stopper la musique
            StopMelody();
            // Masquer le xylophone, ses lames et ses baguettes
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !r.gameObject.name.ToLower().Contains("devoveo") && !r.gameObject.name.ToLower().Contains("tournesol"))
                {
                    r.enabled = false;
                }
            }
        }
    }

    public void PlayMelody()
    {
        StopMelody();
        isPlaying = true;
        isPausedForDialogue = false;

        if (useFullWavAudio && fullMelodyWavClip != null)
        {
            melodyCoroutine = StartCoroutine(PlayWavMelodyRoutine());
        }
        else if (melody != null && melody.Count > 0)
        {
            melodyCoroutine = StartCoroutine(PlayBpmMelodyRoutine());
        }
    }

    public void StopMelody()
    {
        isPlaying = false;
        isPausedForDialogue = false;

        if (melodyCoroutine != null)
        {
            StopCoroutine(melodyCoroutine);
            melodyCoroutine = null;
        }
        if (resumeDialogueRoutine != null)
        {
            StopCoroutine(resumeDialogueRoutine);
            resumeDialogueRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        ReturnMalletsToRest();
    }

    public void PlayNote(int keyIndex)
    {
        PlayNote(keyIndex, null, null);
    }

    public void PlayNote(int keyIndex, AudioClip customClip = null, Color? flashColor = null)
    {
        if (keyIndex < 0 || keys == null || keyIndex >= keys.Length) return;

        bool isHighPitch = keyIndex >= highPitchSplitIndex;
        bool useLeft = isHighPitch ? invertMallets : !invertMallets;

        Transform chosenMallet = useLeft ? leftMallet : rightMallet;
        if (chosenMallet == null) chosenMallet = leftMallet ?? rightMallet;
        if (chosenMallet == null) return;

        Quaternion baseRot = useLeft ? leftInitialRot : rightInitialRot;
        if (useLeft)
        {
            if (leftMalletRoutine != null) StopCoroutine(leftMalletRoutine);
            leftMalletRoutine = StartCoroutine(AnimateMalletStrike(chosenMallet, keyIndex, baseRot, true, customClip, flashColor));
        }
        else
        {
            if (rightMalletRoutine != null) StopCoroutine(rightMalletRoutine);
            rightMalletRoutine = StartCoroutine(AnimateMalletStrike(chosenMallet, keyIndex, baseRot, true, customClip, flashColor));
        }
    }

    public void StartIdleRandomRoutine()
    {
        StopIdleRandomRoutine();
        if (enableRandomIdleAudios)
        {
            idleRandomRoutine = StartCoroutine(IdleRandomAudioRoutine());
        }
    }

    public void StopIdleRandomRoutine()
    {
        if (idleRandomRoutine != null)
        {
            StopCoroutine(idleRandomRoutine);
            idleRandomRoutine = null;
        }
    }

    private int lastIdleAudioIndex = -1;

    private IEnumerator IdleRandomAudioRoutine()
    {
        while (enableRandomIdleAudios)
        {
            float randomDelay = Random.Range(minRandomIdleDelay, maxRandomIdleDelay);
            float elapsed = 0f;

            Debug.Log($"[SunflowerXylophone] ⏳ Cooldown de {randomDelay:F1}s avant le prochain morceau...");

            while (elapsed < randomDelay)
            {
                if (!enableRandomIdleAudios) yield break;

                bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
                if (isPausedForDialogue || isDialogueActive || itemInteractionRoutine != null)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            bool isDialogueActiveNow = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
            if (isPausedForDialogue || isDialogueActiveNow || itemInteractionRoutine != null)
            {
                yield return null;
                continue;
            }

            if (randomIdleAudioClips != null && randomIdleAudioClips.Length > 0)
            {
                int randomIndex = 0;
                if (randomIdleAudioClips.Length > 1)
                {
                    int attempts = 0;
                    do
                    {
                        randomIndex = Random.Range(0, randomIdleAudioClips.Length);
                        attempts++;
                    } while (randomIndex == lastIdleAudioIndex && attempts < 20);
                }
                lastIdleAudioIndex = randomIndex;

                AudioClip chosenClip = randomIdleAudioClips[randomIndex];

                if (chosenClip != null && itemInteractionRoutine == null && !isPausedForDialogue)
                {
                    Debug.Log($"[SunflowerXylophone] 🎵 Morceau [{randomIndex + 1}/{randomIdleAudioClips.Length}] '{chosenClip.name}' ({chosenClip.length:F1}s)...");
                    yield return StartCoroutine(PlayAudioSequenceRoutine(chosenClip, chosenClip.name, randomIndex, Color.white));
                    Debug.Log($"[SunflowerXylophone] ⏹️ Morceau terminé. Attente du prochain cooldown (6 à 10s)...");
                }
            }
        }
    }

    private IEnumerator PlayAudioSequenceRoutine(AudioClip clipToPlay, string identifier, int primaryKeyIndex, Color flashColor, List<MelodyNote> customNotes = null)
    {
        if (clipToPlay == null || audioSource == null) yield break;

        // Réinitialisation de l'AudioSource pour s'assurer que le morceau démarre de zéro
        audioSource.Stop();
        audioSource.clip = clipToPlay;
        audioSource.loop = false;
        audioSource.time = 0f;
        audioSource.Play();

        // Attendre une frame pour laisser Unity initialiser la lecture
        yield return null;

        if (customNotes != null && customNotes.Count > 0)
        {
            customNotes.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            float leadTime = moveDuration + strikeDownDuration;
            int noteIdx = 0;

            while (audioSource != null && audioSource.isPlaying && noteIdx < customNotes.Count && !isPausedForDialogue && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
            {
                float currentAudioTime = audioSource.time;
                MelodyNote note = customNotes[noteIdx];

                float timeToStartMove = note.timestamp - leadTime - currentAudioTime;

                if (timeToStartMove <= 0f)
                {
                    TriggerNoteAnimation(note, playKeySoundsWithWav, flashColor);
                    noteIdx++;
                }
                else
                {
                    yield return null;
                }
            }

            while (audioSource != null && audioSource.isPlaying && !isPausedForDialogue && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
            {
                yield return null;
            }
        }
        else
        {
            string cleanName = string.IsNullOrEmpty(identifier) ? "idle" : identifier.Replace("(Clone)", "").Trim().ToLower();
            int seed = cleanName.GetHashCode() ^ (primaryKeyIndex * 1009);
            System.Random rng = new System.Random(seed);

            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            float tapInterval = itemTapBeatSubdivision * secondsPerBeat;
            if (tapInterval < 0.15f) tapInterval = 0.15f;

            float nextTapTime = 0f;
            float clipLength = clipToPlay.length;

            while (audioSource != null && audioSource.isPlaying && !isPausedForDialogue && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
            {
                float currentTime = audioSource.time;
                float timeRemaining = clipLength > 0f ? (clipLength - currentTime) : 1f;
                float strokeTotalTime = moveDuration + strikeDownDuration;

                if (currentTime >= nextTapTime && timeRemaining > strokeTotalTime)
                {
                    int numKeys = keys != null && keys.Length > 0 ? keys.Length : 8;
                    int randomKeyIndex = rng.Next(0, numKeys);

                    MelodyNote autoNote = new MelodyNote(randomKeyIndex, currentTime);
                    TriggerNoteAnimation(autoNote, playKeySoundsWithWav, flashColor);

                    nextTapTime = currentTime + tapInterval;
                }

                yield return null;
            }
        }

        ReturnMalletsToRest();
    }

    /// <summary>
    /// Joue la note/morceau et applique le dialogue & couleur associés à un objet porté par le joueur quand il parle au Tournesol.
    /// </summary>
    public void PlayNoteForCarriedItem(CarriableItem item)
    {
        if (item == null) return;

        if (itemInteractionRoutine != null)
        {
            StopCoroutine(itemInteractionRoutine);
        }

        // 1. Récupérer le dialogue associé à cet objet (sur l'item ou dans la liste itemAudioSequences du Tournesol)
        DialogueData dialogueToPlay = item.ItemDialogue;
        if (dialogueToPlay == null && itemAudioSequences != null && itemAudioSequences.Length > 0)
        {
            foreach (var seq in itemAudioSequences)
            {
                if (seq.keyIndex == item.ItemKeyIndex && seq.itemDialogue != null)
                {
                    dialogueToPlay = seq.itemDialogue;
                    break;
                }
            }
        }

        // 2. Si un dialogue est assigné pour cet objet, jouer d'abord le dialogue puis lancer la musique une fois terminé !
        if (dialogueToPlay != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToPlay, () =>
            {
                itemInteractionRoutine = StartCoroutine(PlayNoteForCarriedItemRoutine(item));
            });
        }
        else
        {
            itemInteractionRoutine = StartCoroutine(PlayNoteForCarriedItemRoutine(item));
        }
    }

    private IEnumerator PlayNoteForCarriedItemRoutine(CarriableItem item)
    {
        if (item == null) yield break;

        // 1. Sauvegarder toutes les propriétés de l'objet avant sa consommation / destruction
        AudioClip clipToPlay = item.ItemAudioClip;
        Color flashColor = item.ItemColor;
        int primaryKeyIndex = item.ItemKeyIndex;
        bool isRock = item.IsSpecialRockItem;
        string itemName = item.gameObject.name;
        List<MelodyNote> noteSequence = null;

        if (item.ItemMelodyNotes != null && item.ItemMelodyNotes.Length > 0)
        {
            noteSequence = new List<MelodyNote>(item.ItemMelodyNotes);
        }

        if (itemAudioSequences != null && itemAudioSequences.Length > 0)
        {
            foreach (var seq in itemAudioSequences)
            {
                if (seq.keyIndex == primaryKeyIndex || (clipToPlay == null && seq.audioClip != null))
                {
                    if (clipToPlay == null) clipToPlay = seq.audioClip;
                    if (seq.itemColor != Color.clear && seq.itemColor != Color.black) flashColor = seq.itemColor;
                    if (noteSequence == null || noteSequence.Count == 0) noteSequence = seq.melodyNotes;
                    break;
                }
            }
        }

        if (clipToPlay == null) clipToPlay = item.ItemNoteSound;

        // Supprimer définitivement l'indicateur "?" de l'objet
        item.PermanentlyHideIndicator();

        // Verrouiller le joueur pour qu'il assiste à la scène sans bouger
        PlayerLockManager.SetPlayerLocked(true);

        try
        {
            // Mettre en pause la musique de fond de la zone avec un fondu de sortie
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseZoneMusic(1.0f);
            }

            // Stopper le jeu aléatoire en cours
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Pause initiale avant la disparition
            float preDissolveWait = Mathf.Max(0.25f, delayBeforeItemNote * 0.4f);
            yield return new WaitForSeconds(preDissolveWait);

            Debug.Log($"[SunflowerXylophone] ✨ Disparition en poussière de '{itemName}' juste avant la mélodie...");

            // --- FONDU DE POUSSIÈRE ET DISPARITION DE L'OBJET ---
            // Juste avant la mélodie, l'objet s'évapore dans un nuage de poussière magique.
            // Au même moment précis où il a disparu, le player reprend son animation idle !
            yield return StartCoroutine(DisappearItemInDustRoutine(item, flashColor));

            // Petit temps naturel pour laisser la poussière tournoyer doucement avant le premier coup de baguette
            yield return new WaitForSeconds(0.35f);

            // --- ÉVÉNEMENT SPÉCIAL CONCERT ROCK ---
            if (isRock)
            {
                yield return StartCoroutine(PlaySpecialRockConcertRoutine(clipToPlay, flashColor, itemName));
            }
            else
            {
                // Lancer la lecture audio et les animations de frappe des baguettes
                if (clipToPlay != null)
                {
                    yield return StartCoroutine(PlayAudioSequenceRoutine(clipToPlay, itemName, primaryKeyIndex, flashColor, noteSequence));
                }

                // Remettre les baguettes au repos
                ReturnMalletsToRest();

                // --- DÉVERROUILLER LE JOUEUR IMMÉDIATEMENT APRÈS LA MÉLODIE ! ---
                PlayerLockManager.SetPlayerLocked(false);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ResumeZoneMusic(1.0f);
                }

                // Pause pour le Tournesol uniquement (le joueur peut déjà bouger librement !)
                if (delayAfterItemNote > 0f)
                {
                    yield return new WaitForSeconds(delayAfterItemNote);
                }

                // Reprise selon la configuration
                if (resumeMelodyAfterItem)
                {
                    Debug.Log($"[SunflowerXylophone] 🌻 Morceau de l'objet terminé. Reprise de la mélodie...");
                    PlayMelody();
                }
                else
                {
                    Debug.Log($"[SunflowerXylophone] 🌻 Morceau de l'objet terminé. Reprise du cycle aléatoire hors dialogue.");
                    StartIdleRandomRoutine();
                }
            }
        }
        finally
        {
            // Toujours déverrouiller le joueur et reprendre la musique de zone à la fin
            PlayerLockManager.SetPlayerLocked(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeZoneMusic(1.0f);
            }

            itemInteractionRoutine = null;
        }
    }

    /// <summary>
    /// Fait disparaître l'objet porté dans un nuage de poussière féerique juste avant que le tournesol ne joue la mélodie.
    /// Au moment précis où l'objet a disparu, item.Consume() est appelé et le player reprend immédiatement son animation 'idle'.
    /// </summary>
    private IEnumerator DisappearItemInDustRoutine(CarriableItem item, Color flashColor)
    {
        if (item == null) yield break;

        // 1. Préparer l'objet pour la dissolution (empêcher le recentrage automatique sur le joueur par LateUpdate)
        item.PrepareForDissolve();

        Vector3 startPos = item.transform.position;
        Vector3 startScale = item.transform.localScale;

        // 2. Faire spawner le nuage de poussière magique double couche (poussière + étincelles)
        Color dustTint = (flashColor != Color.clear && flashColor != Color.black) ? flashColor : new Color(0.95f, 0.88f, 0.72f);
        ProceduralDustEffect.Spawn(startPos, dustTint);

        // Récupérer tous les SpriteRenderers de l'objet pour le fondu d'opacité
        SpriteRenderer[] srs = item.GetComponentsInChildren<SpriteRenderer>(true);
        Dictionary<SpriteRenderer, Color> origColors = new Dictionary<SpriteRenderer, Color>();
        foreach (var sr in srs)
        {
            if (sr != null) origColors[sr] = sr.color;
        }

        // Récupérer également les matériaux des MeshRenderers si l'objet est un modèle 3D
        MeshRenderer[] mrs = item.GetComponentsInChildren<MeshRenderer>(true);
        List<Material> clonedMaterials = new List<Material>();
        foreach (var mr in mrs)
        {
            if (mr != null && mr.materials != null)
            {
                foreach (var mat in mr.materials)
                {
                    if (mat != null) clonedMaterials.Add(mat);
                }
            }
        }

        // 3. Animation du fondu : rétrécissement vers 0, légère ascension et fondu alpha vers 0
        float fadeDuration = 0.50f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            if (item != null)
            {
                // Rétrécissement vers 0
                item.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, tSmooth);
                // Légère ascension délicate dans la poussière
                item.transform.position = startPos + Vector3.up * (0.12f * tSmooth);
                // Rotation douce et légère
                item.transform.Rotate(Vector3.forward, 45f * Time.deltaTime);

                // Fondu alpha des sprites
                foreach (var kvp in origColors)
                {
                    if (kvp.Key != null)
                    {
                        Color c = kvp.Value;
                        kvp.Key.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, tSmooth));
                    }
                }

                // Fondu alpha des matériaux 3D
                foreach (var mat in clonedMaterials)
                {
                    if (mat != null)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            mat.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, tSmooth));
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, tSmooth)));
                        }
                    }
                }
            }

            yield return null;
        }

        // 4. L'OBJET A TOTALEMENT DISPARU : Au même moment précis, libérer le joueur et forcer l'animation 'idle' !
        if (item != null)
        {
            item.Consume();
        }

        // Forcer immédiatement et explicitement l'animation 'idle' sur le joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }

        if (playerObj != null)
        {
            Animator playerAnim = playerObj.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = playerObj.GetComponentInChildren<Animator>();

            if (playerAnim != null)
            {
                playerAnim.SetBool("isHolding", false);
                playerAnim.SetBool("isWalking", false);
                playerAnim.SetBool("isJumping", false);
                playerAnim.Play("idle", 0, 0f);
            }
        }
    }

    private IEnumerator PlayWavMelodyRoutine()
    {
        while (isPlaying)
        {
            melody.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            audioSource.clip = fullMelodyWavClip;
            audioSource.time = 0f;
            audioSource.Play();

            float leadTime = moveDuration + strikeDownDuration;

            int noteIndex = 0;
            while ((audioSource.isPlaying || isPausedForDialogue) && isPlaying && noteIndex < melody.Count)
            {
                if (isPausedForDialogue)
                {
                    yield return null;
                    continue;
                }

                float currentAudioTime = audioSource.time;
                MelodyNote note = melody[noteIndex];

                float timeToStartMove = note.timestamp - leadTime - currentAudioTime;

                if (timeToStartMove <= 0f)
                {
                    TriggerNoteAnimation(note, playKeySoundsWithWav);
                    noteIndex++;
                }
                else
                {
                    yield return null;
                }
            }

            while ((audioSource.isPlaying || isPausedForDialogue) && isPlaying)
            {
                yield return null;
            }

            if (loopMelody && isPlaying && !isPausedForDialogue)
            {
                ReturnMalletsToRest();
                yield return new WaitForSeconds(loopDelay);
            }
            else if (!isPausedForDialogue)
            {
                isPlaying = false;
            }
        }

        ReturnMalletsToRest();
    }

    private IEnumerator PlayBpmMelodyRoutine()
    {
        while (isPlaying)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);

            for (int i = 0; i < melody.Count; i++)
            {
                if (!isPlaying) yield break;

                while (isPausedForDialogue)
                {
                    yield return null;
                }

                MelodyNote note = melody[i];
                TriggerNoteAnimation(note, true);

                float waitTime = note.beatDuration * secondsPerBeat;
                yield return new WaitForSeconds(waitTime);
            }

            if (loopMelody && isPlaying && !isPausedForDialogue)
            {
                yield return new WaitForSeconds(loopDelay);
            }
            else if (!isPausedForDialogue)
            {
                isPlaying = false;
            }
        }

        ReturnMalletsToRest();
    }

    private void TriggerNoteAnimation(MelodyNote note, bool playSound, Color? flashColor = null)
    {
        if (keys == null || note.keyIndex < 0 || note.keyIndex >= keys.Length) return;

        bool useLeft;
        if (note.forceCustomMallet)
        {
            useLeft = note.useLeftMallet;
        }
        else
        {
            bool isHighPitch = note.keyIndex >= highPitchSplitIndex;
            useLeft = isHighPitch ? invertMallets : !invertMallets;
        }

        Transform mallet = useLeft ? leftMallet : rightMallet;
        if (mallet == null) mallet = leftMallet ?? rightMallet;
        if (mallet == null) return;

        Quaternion baseRot = useLeft ? leftInitialRot : rightInitialRot;

        if (useLeft)
        {
            if (leftMalletRoutine != null) StopCoroutine(leftMalletRoutine);
            leftMalletRoutine = StartCoroutine(AnimateMalletStrike(mallet, note.keyIndex, baseRot, playSound, null, flashColor));
        }
        else
        {
            if (rightMalletRoutine != null) StopCoroutine(rightMalletRoutine);
            rightMalletRoutine = StartCoroutine(AnimateMalletStrike(mallet, note.keyIndex, baseRot, playSound, null, flashColor));
        }
    }

    private IEnumerator AnimateMalletStrike(Transform mallet, int keyIndex, Quaternion baseRotation, bool playSound, AudioClip customClip = null, Color? flashColor = null)
    {
        Transform targetKey = keys[keyIndex];
        if (targetKey == null) yield break;

        Vector3 keyTargetPos = targetKey.position;
        Vector3 hoverTargetPos = keyTargetPos + Vector3.up * hoverHeight;
        Vector3 startPos = mallet.position;

        // 1. Déplacement horizontal vers la lame
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (isPausedForDialogue) yield break;
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            mallet.position = Vector3.Lerp(startPos, hoverTargetPos, t);
            yield return null;
        }
        mallet.position = hoverTargetPos;

        // 2. Frappe vers le bas
        elapsed = 0f;
        Quaternion startRot = mallet.rotation;
        Quaternion strikeRot = baseRotation * Quaternion.Euler(strikeTiltAngle, 0f, 0f);

        while (elapsed < strikeDownDuration)
        {
            if (isPausedForDialogue) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / strikeDownDuration;
            mallet.position = Vector3.Lerp(hoverTargetPos, keyTargetPos, t);
            mallet.rotation = Quaternion.Slerp(startRot, strikeRot, t);
            yield return null;
        }

        mallet.position = keyTargetPos;
        mallet.rotation = strikeRot;

        // --- IMPACT ! ---
        if (playSound)
        {
            if (customClip != null)
            {
                if (audioSource != null) audioSource.PlayOneShot(customClip);
                else AudioSource.PlayClipAtPoint(customClip, keyTargetPos);
            }
            else
            {
                PlaySoundForKey(keyIndex);
            }
        }

        TriggerKeyHitEffect(targetKey, flashColor);

        // 3. Remontée
        elapsed = 0f;
        while (elapsed < retractUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / retractUpDuration;
            mallet.position = Vector3.Lerp(keyTargetPos, hoverTargetPos, t);
            mallet.rotation = Quaternion.Slerp(strikeRot, baseRotation, t);
            yield return null;
        }

        mallet.position = hoverTargetPos;
        mallet.rotation = baseRotation;
    }

    private void TriggerKeyHitEffect(Transform targetKey, Color? flashColor = null)
    {
        if (targetKey == null) return;

        if (activeKeyBounceRoutines.TryGetValue(targetKey, out Coroutine existingRoutine) && existingRoutine != null)
        {
            StopCoroutine(existingRoutine);
        }

        activeKeyBounceRoutines[targetKey] = StartCoroutine(AnimateKeyBounceAndScale(targetKey, flashColor));
    }

    private IEnumerator AnimateKeyBounceAndScale(Transform targetKey, Color? flashColor = null)
    {
        if (!defaultKeyLocalPositions.TryGetValue(targetKey, out Vector3 origLocalPos))
        {
            origLocalPos = targetKey.localPosition;
            defaultKeyLocalPositions[targetKey] = origLocalPos;
        }

        if (!defaultKeyLocalScales.TryGetValue(targetKey, out Vector3 origScale))
        {
            origScale = targetKey.localScale;
            defaultKeyLocalScales[targetKey] = origScale;
        }

        SpriteRenderer keySR = targetKey.GetComponent<SpriteRenderer>();
        if (keySR == null) keySR = targetKey.GetComponentInChildren<SpriteRenderer>();
        Color origColor = keySR != null ? keySR.color : Color.white;

        Vector3 sunkenPos = origLocalPos - new Vector3(0, keySinkDepth, 0);
        Vector3 expandedScale = origScale * keyHitScaleMultiplier;

        float downTime = keyBounceDuration * 0.35f;
        float elapsed = 0f;

        while (elapsed < downTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / downTime;
            targetKey.localPosition = Vector3.Lerp(origLocalPos, sunkenPos, t);
            targetKey.localScale = Vector3.Lerp(origScale, expandedScale, t);

            if (flashColor.HasValue && keySR != null)
            {
                keySR.color = Color.Lerp(origColor, flashColor.Value, t);
            }

            yield return null;
        }

        targetKey.localPosition = sunkenPos;
        targetKey.localScale = expandedScale;

        float upTime = keyBounceDuration * 0.65f;
        elapsed = 0f;

        while (elapsed < upTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / upTime;
            targetKey.localPosition = Vector3.Lerp(sunkenPos, origLocalPos, t);
            targetKey.localScale = Vector3.Lerp(expandedScale, origScale, t);

            if (flashColor.HasValue && keySR != null)
            {
                keySR.color = Color.Lerp(flashColor.Value, origColor, t);
            }

            yield return null;
        }

        targetKey.localPosition = origLocalPos;
        targetKey.localScale = origScale;

        if (keySR != null)
        {
            keySR.color = origColor;
        }
    }

    private void PlaySoundForKey(int keyIndex)
    {
        if (!useFullWavAudio && keyAudioClips != null && keyIndex < keyAudioClips.Length && keyAudioClips[keyIndex] != null)
        {
            if (audioSource != null)
            {
                audioSource.dopplerLevel = 0f;
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(keyAudioClips[keyIndex]);
            }
        }
    }

    private void ReturnMalletsToRest()
    {
        if (leftMallet != null && leftInitialPos != Vector3.zero)
        {
            StartCoroutine(AnimateReturnToRest(leftMallet, leftInitialPos, leftInitialRot));
        }

        if (rightMallet != null && rightInitialPos != Vector3.zero)
        {
            StartCoroutine(AnimateReturnToRest(rightMallet, rightInitialPos, rightInitialRot));
        }
    }

    private IEnumerator AnimateReturnToRest(Transform mallet, Vector3 restPos, Quaternion restRot)
    {
        Vector3 startPos = mallet.position;
        Quaternion startRot = mallet.rotation;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mallet.position = Vector3.Lerp(startPos, restPos, t);
            mallet.rotation = Quaternion.Slerp(startRot, restRot, t);
            yield return null;
        }

        mallet.position = restPos;
        mallet.rotation = restRot;
    }

    /// <summary>
    /// Récupère le Transform du personnage Tournesol (distinct de l'instrument xylophone).
    /// </summary>
    public Transform GetSunflowerCharacterTransform()
    {
        if (sunflowerCharacter != null) return sunflowerCharacter;

        // 1. Chercher dans les composants enfants de cet objet qui contiennent un Animator (et ne sont pas les baguettes)
        Animator[] anims = GetComponentsInChildren<Animator>(true);
        foreach (var anim in anims)
        {
            if (anim.transform != transform && anim.transform != leftMallet && anim.transform != rightMallet)
            {
                sunflowerCharacter = anim.transform;
                return sunflowerCharacter;
            }
        }

        // 2. Chercher dans le parent ou les frères (siblings)
        if (transform.parent != null)
        {
            Animator parentAnim = transform.parent.GetComponentInChildren<Animator>(true);
            if (parentAnim != null && parentAnim.transform != transform && parentAnim.transform != leftMallet && parentAnim.transform != rightMallet)
            {
                sunflowerCharacter = parentAnim.transform;
                return sunflowerCharacter;
            }
        }

        // 3. Chercher par nom d'objet ("Tournesol", "Devoveo", "Sunflower") dans la scène
        GameObject foundObj = GameObject.Find("Tournesol");
        if (foundObj == null) foundObj = GameObject.Find("Devoveo");
        if (foundObj == null) foundObj = GameObject.Find("Sunflower");
        if (foundObj != null)
        {
            sunflowerCharacter = foundObj.transform;
            return sunflowerCharacter;
        }

        // Fallback sur cet objet si aucun personnage distinct n'est trouvé
        return transform;
    }

    /// <summary>
    /// Séquence cinématique ROCK CONCERT ULTIME - version spectaculaire extrême.
    /// </summary>
    private IEnumerator PlaySpecialRockConcertRoutine(CarriableItem item)
    {
        AudioClip clip = item != null ? item.ItemAudioClip : null;
        if (clip == null && item != null) clip = item.ItemNoteSound;
        Color col = item != null ? item.ItemColor : Color.white;
        string name = item != null ? item.gameObject.name : "Rock";
        return PlaySpecialRockConcertRoutine(clip, col, name);
    }

    private IEnumerator PlaySpecialRockConcertRoutine(AudioClip clipToPlay, Color concertColor, string itemName)
    {
        Debug.Log($"[SunflowerXylophone] 🎸🔥 ROCK CONCERT ULTIME avec '{itemName}' !");

        // ============================================================
        //   ROCK CONCERT ULTIME — Séquence cinématique complète
        // ============================================================

        Transform sunChar = GetSunflowerCharacterTransform();

        // 1. Verrouiller le joueur pendant TOUTE l'action
        PlayerLockManager.SetPlayerLocked(true);

        // 2. Récupérer le clip audio à jouer s'il n'est pas déjà spécifié
        if (clipToPlay == null && itemAudioSequences != null && itemAudioSequences.Length > 0)
        {
            foreach (var seq in itemAudioSequences)
            {
                if (seq.audioClip != null) { clipToPlay = seq.audioClip; break; }
            }
        }

        // 3. Bandes noires cinéma (letterbox)
        GameObject topBarObj = null;
        GameObject bottomBarObj = null;
        RectTransform topBar = null;
        RectTransform bottomBar = null;
        CreateConcertCinemaBars(out topBarObj, out bottomBarObj, out topBar, out bottomBar);
        StartCoroutine(AnimateConcertCinemaBars(topBar, bottomBar, true, 1.0f));

        // --- CARTE TITRE PUNK "ROCK CONCERT" ---
        GameObject titleCardObj = null;
        if (showConcertTitleCard)
        {
            titleCardObj = CreateConcertTitleCard();
            StartCoroutine(AnimateConcertTitleCard(titleCardObj, concertTitleDuration));
        }

        // 4. Sauvegarder l'éclairage original
        Color origSky = RenderSettings.ambientSkyColor;
        Color origEquator = RenderSettings.ambientEquatorColor;
        Color origGround = RenderSettings.ambientGroundColor;
        UnityEngine.Rendering.AmbientMode origAmbientMode = RenderSettings.ambientMode;

        Light sunLight = null;
        float origSunIntensity = 1f;
        Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional && l.enabled)
            {
                sunLight = l;
                origSunIntensity = l.intensity;
                break;
            }
        }

        // --- SPOTLIGHT PRINCIPAL sur le Tournesol ---
        Light activeSpotlight = concertSpotlight;
        bool createdSpotlight = false;
        if (activeSpotlight == null)
        {
            GameObject spotObj = new GameObject("ConcertSpotlight_Main");
            spotObj.transform.position = sunChar.position + new Vector3(0f, 8.0f, -3.0f);
            spotObj.transform.rotation = Quaternion.LookRotation((sunChar.position + Vector3.up * 1.5f) - spotObj.transform.position);
            activeSpotlight = spotObj.AddComponent<Light>();
            activeSpotlight.type = LightType.Spot;
            activeSpotlight.spotAngle = 55f;
            activeSpotlight.innerSpotAngle = 30f;
            activeSpotlight.range = 28f;
            activeSpotlight.color = concertSpotlightColor;
            activeSpotlight.intensity = 0f;
            createdSpotlight = true;
        }
        else
        {
            activeSpotlight.gameObject.SetActive(true);
            activeSpotlight.transform.position = sunChar.position + new Vector3(0f, 8.0f, -3.0f);
            activeSpotlight.transform.rotation = Quaternion.LookRotation((sunChar.position + Vector3.up * 1.5f) - activeSpotlight.transform.position);
            activeSpotlight.color = concertSpotlightColor;
            activeSpotlight.intensity = 0f;
        }

        // --- PROJECTEURS LATÉRAUX DE SCÈNE COLORÉS (Stage Wash Lights) ---
        List<Light> stageLights = new List<Light>();
        Color[] stageColors = new Color[]
        {
            new Color(0.8f, 0.1f, 1.0f),   // Violet
            new Color(0.1f, 0.5f, 1.0f),   // Bleu
            new Color(1.0f, 0.15f, 0.1f),  // Rouge
            new Color(0.1f, 1.0f, 0.4f),   // Vert
            new Color(1.0f, 0.6f, 0.0f),   // Orange
            new Color(1.0f, 0.1f, 0.6f),   // Rose
        };
        if (enableStageLights)
        {
            int count = Mathf.Clamp(stageLightCount, 2, 6);
            for (int si = 0; si < count; si++)
            {
                float angleStep = (si % 2 == 0 ? -1f : 1f) * (3.5f + si * 1.2f);
                Vector3 stageLightPos = sunChar.position + new Vector3(angleStep, 6.0f + si * 0.5f, -1.5f - si * 0.4f);
                GameObject slObj = new GameObject($"ConcertStageLight_{si}");
                slObj.transform.position = stageLightPos;
                slObj.transform.rotation = Quaternion.LookRotation((sunChar.position + Vector3.up * 1.2f) - stageLightPos);
                Light sl = slObj.AddComponent<Light>();
                sl.type = LightType.Spot;
                sl.spotAngle = 45f;
                sl.innerSpotAngle = 20f;
                sl.range = 22f;
                sl.color = stageColors[si % stageColors.Length];
                sl.intensity = 0f;
                stageLights.Add(sl);
            }
        }

        // --- FILL LIGHT (Lumière d'accentuation directe sur le Tournesol) ---
        GameObject fillLightObj = new GameObject("ConcertSunflower_FillLight");
        fillLightObj.transform.SetParent(sunChar, false);
        fillLightObj.transform.localPosition = new Vector3(0f, 0.6f, -0.8f);
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 12f;
        fillLight.color = concertSpotlightColor;
        fillLight.intensity = 0f;

        // 5. Fondu vers le noir + allumage des lumières de concert
        float elapsed = 0f;
        float fadeDarkDuration = 2.0f;
        Color darkAmbient = new Color(0.01f, 0.01f, 0.02f);

        while (elapsed < fadeDarkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDarkDuration;
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(origSky, darkAmbient, tSmooth);
            RenderSettings.ambientEquatorColor = Color.Lerp(origEquator, darkAmbient, tSmooth);
            RenderSettings.ambientGroundColor = Color.Lerp(origGround, darkAmbient, tSmooth);

            if (sunLight != null) sunLight.intensity = Mathf.Lerp(origSunIntensity, 0f, tSmooth);
            if (activeSpotlight != null) activeSpotlight.intensity = Mathf.Lerp(0f, concertSpotlightIntensity, tSmooth);
            if (fillLight != null) fillLight.intensity = Mathf.Lerp(0f, concertFillLightIntensity, tSmooth);

            // Allumage progressif des projecteurs latéraux
            foreach (Light sl in stageLights)
            {
                if (sl != null) sl.intensity = Mathf.Lerp(0f, 18f, tSmooth);
            }

            yield return null;
        }

        // 6. Caméra + Lévitation du Tournesol
        Vector3 startSunPos = sunChar.position;
        Vector3 targetLevitatePos = startSunPos + Vector3.up * levitateHeight;

        Camera mainCam = Camera.main;
        Unity.Cinemachine.CinemachineCamera cineCam = Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        Transform origCineFollow = cineCam != null ? cineCam.Follow : null;

        GameObject playerObj = GameObject.FindWithTag("Player");
        Vector3 playerPos = playerObj != null ? playerObj.transform.position : startSunPos;

        // Proxy caméra — panoramique fluide Joueur → Fleur
        GameObject camProxyObj = new GameObject("ConcertCamera_SmoothProxy");
        camProxyObj.transform.position = playerPos;
        if (cineCam != null) cineCam.Follow = camProxyObj.transform;

        float camPanElapsed = 0f;
        float camPanDuration = 1.4f;
        while (camPanElapsed < camPanDuration)
        {
            camPanElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, camPanElapsed / camPanDuration);
            camProxyObj.transform.position = Vector3.Lerp(playerPos, startSunPos, t);

            // Balayage rotatif des projecteurs pendant le panoramique
            float sweepAngle = Time.time * 60f;
            for (int si = 0; si < stageLights.Count; si++)
            {
                Light sl = stageLights[si];
                if (sl == null) continue;
                float phase = si * (Mathf.PI * 2f / stageLights.Count);
                float swayX = Mathf.Sin(Time.time * 1.4f + phase) * 1.5f;
                float swayZ = Mathf.Cos(Time.time * 1.1f + phase) * 0.8f;
                Vector3 sweepTarget = sunChar.position + new Vector3(swayX, 1.2f, swayZ);
                sl.transform.rotation = Quaternion.Lerp(sl.transform.rotation,
                    Quaternion.LookRotation(sweepTarget - sl.transform.position), Time.deltaTime * 4f);
            }

            yield return null;
        }
        camProxyObj.transform.position = startSunPos;

        // Lévitation du Tournesol avec suivi caméra
        Vector3 startCamPos = mainCam != null ? mainCam.transform.position : Vector3.zero;
        elapsed = 0f;
        while (elapsed < levitateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / levitateDuration);
            sunChar.position = Vector3.Lerp(startSunPos, targetLevitatePos, t);
            camProxyObj.transform.position = sunChar.position;

            // Ajuster la position du spotlight principal
            if (activeSpotlight != null)
            {
                Vector3 spotTargetPos = sunChar.position + new Vector3(0f, 8.0f, -3.0f);
                activeSpotlight.transform.position = Vector3.Lerp(activeSpotlight.transform.position, spotTargetPos, Time.deltaTime * 3f);
                activeSpotlight.transform.rotation = Quaternion.LookRotation(sunChar.position - activeSpotlight.transform.position);
            }

            if (cineCam == null && mainCam != null)
                mainCam.transform.position = Vector3.Lerp(startCamPos, startCamPos + Vector3.up * levitateHeight, t);

            yield return null;
        }
        sunChar.position = targetLevitatePos;
        camProxyObj.transform.position = targetLevitatePos;

        // Horodatage de départ de la lévitation (pour la courbe en infini)
        float levitationStartTime = Time.time;

        // 7. Animation Rock (DevoveoGuitar -> DevoveoRock)
        Animator anim = sunChar.GetComponent<Animator>();
        if (anim == null) anim = sunChar.GetComponentInChildren<Animator>(true);
        if (anim == null) anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        if (anim != null)
        {
            anim.enabled = true;

            int rockHash = !string.IsNullOrEmpty(rockAnimationState) ? Animator.StringToHash(rockAnimationState) : 0;
            int devoveoGuitarHash = Animator.StringToHash("DevoveoGuitar");
            int devoveoRockHash = Animator.StringToHash("DevoveoRock");

            if (rockHash != 0 && anim.HasState(0, rockHash))
            {
                anim.Play(rockHash, 0, 0f);
            }
            else if (anim.HasState(0, devoveoGuitarHash))
            {
                anim.Play(devoveoGuitarHash, 0, 0f);
            }
            else if (anim.HasState(0, devoveoRockHash))
            {
                anim.Play(devoveoRockHash, 0, 0f);
            }
            else if (rockHash != 0)
            {
                anim.SetBool(rockAnimationState, true);
            }
        }

        // 8. Lancement audio avec Fade In
        float origAudioVolume = audioSource != null && audioSource.volume > 0.05f ? audioSource.volume : 1.0f;
        if (audioSource != null && clipToPlay != null)
        {
            audioSource.Stop();
            audioSource.clip = clipToPlay;
            audioSource.loop = false;
            audioSource.time = 0f;
            audioSource.volume = 0f;
            audioSource.Play();
            StartCoroutine(FadeInAudioSourceRoutine(audioSource, origAudioVolume, audioFadeInDuration));
        }

        // 9. Boucle principale du concert
        bool event12SecTriggered = false;
        GameObject fire1Obj = null;
        GameObject fire2Obj = null;

        CylindricalWaterfall wf = backgroundWaterfall != null ? backgroundWaterfall : Object.FindFirstObjectByType<CylindricalWaterfall>();
        Material wfMat = null;
        Color origWfDeep = Color.white, origWfShallow = Color.white, origWfFoam = Color.white, origWfFresnel = Color.white;
        if (wf != null)
        {
            MeshRenderer mr = wf.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                wfMat = mr.material;
                if (wfMat.HasProperty("_DeepColor")) origWfDeep = wfMat.GetColor("_DeepColor");
                if (wfMat.HasProperty("_ShallowColor")) origWfShallow = wfMat.GetColor("_ShallowColor");
                if (wfMat.HasProperty("_FoamColor")) origWfFoam = wfMat.GetColor("_FoamColor");
                if (wfMat.HasProperty("_FresnelColor")) origWfFresnel = wfMat.GetColor("_FresnelColor");
            }
        }

        float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
        float tapInterval = itemTapBeatSubdivision * secondsPerBeat;
        if (tapInterval < 0.15f) tapInterval = 0.15f;
        float nextTapTime = 0f;
        float currentBeatPulseOffset = 0f;

        // Horloge musicale lissée (audioSource.time avance par paquets DSP -> saccades)
        float smoothClock = 0f;
        int lastBeatIndex = -1;
        float lastBeatTime = -10f;
        float kickAmp = 0f;
        float kickRoll = 0f;
        float kickDirX = 0f;
        float kickPulse = 0f;

        concertShakeCam = mainCam;
        concertShakeOffset = Vector3.zero;
        concertShakeRoll = 0f;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnConcertBeginCameraRendering;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += OnConcertEndCameraRendering;

        float origFOV = 40f;
        if (cineCam != null) origFOV = cineCam.Lens.FieldOfView;
        else if (mainCam != null) origFOV = mainCam.fieldOfView;
        if (origFOV < 5f) origFOV = 40f;

        // Durée totale de la musique (pour calculer le ratio de dézoom)
        float clipDuration = clipToPlay != null ? clipToPlay.length : 1f;
        if (clipDuration < 1f) clipDuration = 1f;

        // Base FOV courante : part de origFOV et dérive vers origFOV + progressiveZoomOutAmount
        float currentBaseFOV = origFOV;

        System.Random rng = new System.Random(42);

        // Couleurs de strobe alternées pour les projecteurs latéraux
        int stageLightColorIndex = 0;

        // Phase de départ de la trajectoire en infini (démarrage fluide depuis la position stationnaire)
        float infinityPhase = 0f;
        float infinityBlendIn = 2.5f; // Temps en secondes pour entrer dans la trajectoire ∞ en douceur

        while (audioSource != null && audioSource.isPlaying)
        {
            float currentTrackTime = audioSource.time;

            // Horloge lissée : avance au deltaTime, corrigée doucement vers le temps audio réel
            smoothClock += Time.deltaTime;
            float clockDrift = currentTrackTime - smoothClock;
            if (Mathf.Abs(clockDrift) > 0.1f) smoothClock = currentTrackTime;
            else smoothClock += clockDrift * Mathf.Clamp01(Time.deltaTime * 4f);

            // Détection du beat calée sur la grille (aucune dérive cumulée)
            bool onBeat = false;
            float beatClock = smoothClock - concertBeatOffset;
            if (beatClock >= 0f)
            {
                int beatIndex = Mathf.FloorToInt(beatClock / secondsPerBeat);
                if (beatIndex > lastBeatIndex)
                {
                    lastBeatIndex = beatIndex;
                    lastBeatTime = concertBeatOffset + beatIndex * secondsPerBeat;
                    onBeat = true;

                    bool downbeat = beatsPerBar > 0 && (beatIndex % beatsPerBar) == 0;
                    float accent = downbeat ? downbeatAccent : 1f;
                    kickAmp = (event12SecTriggered ? fireBeatCameraShakeIntensity : beatCameraShakeIntensity) * accent;
                    kickRoll = (event12SecTriggered ? fireBeatShakeRollDegrees : beatShakeRollDegrees) * accent * ((beatIndex % 2 == 0) ? 1f : -1f);
                    kickDirX = ((beatIndex % 2 == 0) ? 1f : -1f) * Random.Range(0.4f, 1f);
                    kickPulse = beatPulseAmount * accent * (event12SecTriggered ? 1.4f : 1f);
                }
            }

            // ---- LEMNISCATE DE BERNOULLI (∞) : Mouvement en infini pendant la lévitation ----
            infinityPhase += Time.deltaTime * infinityDriftSpeed;
            // Paramétrisation de la lemniscate : x = cos(t)/(1+sin²(t)), z = sin(t)*cos(t)/(1+sin²(t))
            float sinP = Mathf.Sin(infinityPhase);
            float cosP = Mathf.Cos(infinityPhase);
            float denom = 1f + sinP * sinP;
            float infX = cosP / denom;
            float infZ = sinP * cosP / denom;
            // Fondu d'entrée progressif dans la trajectoire (smoothstep sur les premières secondes)
            float infBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(currentTrackTime / infinityBlendIn));
            Vector3 infinityOffset = new Vector3(infX, 0f, infZ) * (infinityDriftAmplitude * infBlend);
            sunChar.position = targetLevitatePos + infinityOffset;
            camProxyObj.transform.position = sunChar.position;

            // ---- PULSATION FOV SUR LE BEAT ----
            if (onBeat)
            {

                // Changement de couleur des projecteurs latéraux sur chaque beat
                stageLightColorIndex = (stageLightColorIndex + 1) % stageColors.Length;
                for (int si = 0; si < stageLights.Count; si++)
                {
                    if (stageLights[si] != null)
                        stageLights[si].color = stageColors[(stageLightColorIndex + si) % stageColors.Length];
                }
            }

            if (enableCameraBeatPulse)
            {
                // Dézoom progressif : le FOV de base grossit doucement au fil de la musique
                float songProgress = Mathf.Clamp01(currentTrackTime / clipDuration);
                // Courbe ease-in-out pour rendre le dézoom plus notable vers la fin
                float zoomCurve = Mathf.SmoothStep(0f, 1f, songProgress);
                currentBaseFOV = origFOV + progressiveZoomOutAmount * zoomCurve;

                // Punch FOV : attaque instantanée puis retour exponentiel calé sur le temps depuis le beat
                float tPulse = Mathf.Max(0f, smoothClock - lastBeatTime);
                currentBeatPulseOffset = kickPulse * Mathf.Exp(-beatPulseReturnSpeed * tPulse);
                // Les pulsations beat zooment (diminuent le FOV) autour du baseFOV qui grandit
                float pulsedFOV = currentBaseFOV - currentBeatPulseOffset;
                if (cineCam != null) cineCam.Lens.FieldOfView = pulsedFOV;
                if (mainCam != null) mainCam.fieldOfView = pulsedFOV;
            }

            // ---- PUNCH CAMÉRA SUR LE BEAT (impact sec + rebond ressort + roll) ----
            {
                float tb = Mathf.Max(0f, smoothClock - lastBeatTime);
                float env = Mathf.Exp(-beatShakeDecay * tb);
                float spring = Mathf.Cos(tb * Mathf.PI * 2f * beatShakeSpringFrequency);
                float grit = (Mathf.PerlinNoise(smoothClock * 28f, 0.37f) - 0.5f) * 2f;

                concertShakeOffset = new Vector3(
                    kickAmp * env * (0.45f * kickDirX * spring + 0.25f * grit),
                    -kickAmp * env * spring,
                    0f);
                concertShakeRoll = kickRoll * env * spring;
            }

            // ---- PROJECTEURS LATÉRAUX : rotation / balayage continu ----
            for (int si = 0; si < stageLights.Count; si++)
            {
                Light sl = stageLights[si];
                if (sl == null) continue;
                float phase = si * (Mathf.PI * 2f / stageLights.Count);
                // Balayage sinusoïdal autour du Tournesol
                float swayX = Mathf.Sin(Time.time * 1.6f + phase) * 2.0f;
                float swayY = Mathf.Cos(Time.time * 0.9f + phase) * 0.5f;
                float swayZ = Mathf.Sin(Time.time * 1.2f + phase + 1.0f) * 1.0f;
                Vector3 sweepTarget = sunChar.position + new Vector3(swayX, 1.2f + swayY, swayZ);
                sl.transform.rotation = Quaternion.Lerp(sl.transform.rotation,
                    Quaternion.LookRotation(sweepTarget - sl.transform.position), Time.deltaTime * 5f);

                // Scintillement d'intensité sur le beat
                float flicker = 1f + 0.25f * Mathf.Sin(Time.time * Mathf.PI * bpm / 20f + phase);
                sl.intensity = 18f * flicker;
            }

            // ---- BAGUETTES AU RYTHME ----
            if (currentTrackTime >= nextTapTime)
            {
                int numKeys = keys != null && keys.Length > 0 ? keys.Length : 8;
                TriggerNoteAnimation(new MelodyNote(rng.Next(0, numKeys), currentTrackTime), playKeySoundsWithWav, concertColor);
                nextTapTime = currentTrackTime + tapInterval;
            }

            // ---- ÉVÉNEMENT À 12 SECONDES : FLAMMES + FLASH STROBOSCOPIQUE ----
            if (!event12SecTriggered && currentTrackTime >= 12.0f)
            {
                event12SecTriggered = true;
                Debug.Log("[SunflowerXylophone] 🔥💥 FLAMMES !");

                if (firePoint1 != null && fireColumnPrefab != null)
                    fire1Obj = Instantiate(fireColumnPrefab, firePoint1.position, firePoint1.rotation);
                if (firePoint2 != null && fireColumnPrefab != null)
                    fire2Obj = Instantiate(fireColumnPrefab, firePoint2.position, firePoint2.rotation);

                // Flash stroboscopique blanc à l'apparition du feu
                StartCoroutine(FireSpawnFlashRoutine(mainCam, fillLight, activeSpotlight, fireSpawnFlashIntensity));
            }

            // ---- ÉVOLUTION DES COULEURS APRÈS 12 SEC ----
            if (event12SecTriggered)
            {
                float redProgress = Mathf.Clamp01((currentTrackTime - 12.0f) / 4.0f);

                if (wfMat != null)
                {
                    if (wfMat.HasProperty("_DeepColor")) wfMat.SetColor("_DeepColor",
                        Color.Lerp(origWfDeep, new Color(0.95f, 0.22f, 0.02f, 0.92f), redProgress));
                    if (wfMat.HasProperty("_ShallowColor")) wfMat.SetColor("_ShallowColor",
                        Color.Lerp(origWfShallow, new Color(1.0f, 0.52f, 0.08f, 0.88f), redProgress));
                    if (wfMat.HasProperty("_FoamColor")) wfMat.SetColor("_FoamColor",
                        Color.Lerp(origWfFoam, new Color(1.0f, 0.78f, 0.35f, 0.95f), redProgress));
                    if (wfMat.HasProperty("_FresnelColor")) wfMat.SetColor("_FresnelColor",
                        Color.Lerp(origWfFresnel, new Color(1.0f, 0.38f, 0.05f, 1.0f), redProgress));
                }

                Color redGlow = Color.Lerp(darkAmbient, new Color(0.6f, 0.04f, 0.04f), redProgress);
                RenderSettings.ambientSkyColor = redGlow;
                RenderSettings.ambientEquatorColor = redGlow;
                RenderSettings.ambientGroundColor = Color.Lerp(darkAmbient, new Color(0.25f, 0.02f, 0.02f), redProgress);

                // Teindre progressivement le fill light en rouge-orange
                if (fillLight != null)
                    fillLight.color = Color.Lerp(concertSpotlightColor, new Color(1f, 0.35f, 0.02f), redProgress);

                // Teindre les projecteurs en rouge/orange alternés
                for (int si = 0; si < stageLights.Count; si++)
                {
                    if (stageLights[si] == null) continue;
                    Color fireColor = (si % 2 == 0) ? new Color(1f, 0.2f, 0f) : new Color(1f, 0.6f, 0f);
                    stageLights[si].color = Color.Lerp(stageLights[si].color, fireColor, redProgress * 0.5f);
                }
            }

            yield return null;
        }

        // ==============================================================
        //  10. FIN DE MUSIQUE — Restauration progressive pendant descente
        // ==============================================================
        Debug.Log("[SunflowerXylophone] 🎸 Fin du concert ! Transition de retour...");

        // Arrêter le punch caméra
        StopConcertShake();

        // Couper les flammes immédiatement
        if (fire1Obj != null) Destroy(fire1Obj);
        if (fire2Obj != null) Destroy(fire2Obj);

        // Retirer les bandes cinéma rapidement
        float descentDuration = 0.8f;
        if (topBar != null && bottomBar != null)
            StartCoroutine(AnimateConcertCinemaBars(topBar, bottomBar, false, descentDuration));

        // Reset animation Rock → Devoveo AVANT la descente
        if (anim != null)
        {
            int devoveoHash = !string.IsNullOrEmpty(devoveoAnimationState) ? Animator.StringToHash(devoveoAnimationState) : 0;
            int idleHash = Animator.StringToHash("idle");
            int upperIdleHash = Animator.StringToHash("Idle");

            if (devoveoHash != 0 && anim.HasState(0, devoveoHash))
                anim.Play(devoveoHash, 0, 0f);
            else if (anim.HasState(0, idleHash))
                anim.Play(idleHash, 0, 0f);
            else if (anim.HasState(0, upperIdleHash))
                anim.Play(upperIdleHash, 0, 0f);
            else if (!string.IsNullOrEmpty(rockAnimationState))
                anim.SetBool(rockAnimationState, false);
        }

        // Cacher la guitare après le concert
        Transform[] sunTransforms = sunChar.GetComponentsInChildren<Transform>(true);
        foreach (var t in sunTransforms)
        {
            if (t != null && t.name == "scene (1)")
            {
                t.gameObject.SetActive(false);
            }
        }

        Color currentSky = RenderSettings.ambientSkyColor;
        Color currentEquator = RenderSettings.ambientEquatorColor;
        Color currentGround = RenderSettings.ambientGroundColor;
        float currentSunIntensity = sunLight != null ? sunLight.intensity : origSunIntensity;
        float currentSpotIntensity = activeSpotlight != null ? activeSpotlight.intensity : 0f;
        float currentFillIntensity = fillLight != null ? fillLight.intensity : 0f;
        Color currentWfDeep = wfMat != null && wfMat.HasProperty("_DeepColor") ? wfMat.GetColor("_DeepColor") : origWfDeep;
        Color currentWfShallow = wfMat != null && wfMat.HasProperty("_ShallowColor") ? wfMat.GetColor("_ShallowColor") : origWfShallow;
        Color currentWfFoam = wfMat != null && wfMat.HasProperty("_FoamColor") ? wfMat.GetColor("_FoamColor") : origWfFoam;
        Color currentWfFresnel = wfMat != null && wfMat.HasProperty("_FresnelColor") ? wfMat.GetColor("_FresnelColor") : origWfFresnel;
        Vector3 currentCamPos = mainCam != null ? mainCam.transform.position : Vector3.zero;

        // Position de départ pour la descente = position courante du tournesol (qui peut être décalée par la lemniscate)
        Vector3 levitateEndPos = sunChar.position;

        elapsed = 0f;
        while (elapsed < descentDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / descentDuration);

            // Descente depuis la dernière position de la trajectoire ∞ vers le sol (recentrage + descente)
            sunChar.position = Vector3.Lerp(levitateEndPos, startSunPos, t);
            camProxyObj.transform.position = sunChar.position;

            RenderSettings.ambientSkyColor = Color.Lerp(currentSky, origSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(currentEquator, origEquator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(currentGround, origGround, t);

            if (sunLight != null) sunLight.intensity = Mathf.Lerp(currentSunIntensity, origSunIntensity, t);
            if (activeSpotlight != null) activeSpotlight.intensity = Mathf.Lerp(currentSpotIntensity, 0f, t);
            if (fillLight != null) fillLight.intensity = Mathf.Lerp(currentFillIntensity, 0f, t);

            // Extinction des projecteurs latéraux
            foreach (Light sl in stageLights)
            {
                if (sl != null) sl.intensity = Mathf.Lerp(sl.intensity, 0f, t);
            }

            if (wfMat != null)
            {
                if (wfMat.HasProperty("_DeepColor")) wfMat.SetColor("_DeepColor", Color.Lerp(currentWfDeep, origWfDeep, t));
                if (wfMat.HasProperty("_ShallowColor")) wfMat.SetColor("_ShallowColor", Color.Lerp(currentWfShallow, origWfShallow, t));
                if (wfMat.HasProperty("_FoamColor")) wfMat.SetColor("_FoamColor", Color.Lerp(currentWfFoam, origWfFoam, t));
                if (wfMat.HasProperty("_FresnelColor")) wfMat.SetColor("_FresnelColor", Color.Lerp(currentWfFresnel, origWfFresnel, t));
            }

            if (cineCam == null && mainCam != null)
                mainCam.transform.position = Vector3.Lerp(currentCamPos, startCamPos, t);

            yield return null;
        }

        // Finalisation complète
        sunChar.position = startSunPos;
        RenderSettings.ambientMode = origAmbientMode;
        RenderSettings.ambientSkyColor = origSky;
        RenderSettings.ambientEquatorColor = origEquator;
        RenderSettings.ambientGroundColor = origGround;
        if (sunLight != null) sunLight.intensity = origSunIntensity;
        if (fillLightObj != null) Destroy(fillLightObj);
        if (createdSpotlight && activeSpotlight != null) Destroy(activeSpotlight.gameObject);
        else if (activeSpotlight != null) activeSpotlight.gameObject.SetActive(false);

        // Destruction des projecteurs latéraux
        foreach (Light sl in stageLights)
        {
            if (sl != null) Destroy(sl.gameObject);
        }
        stageLights.Clear();

        if (wfMat != null)
        {
            if (wfMat.HasProperty("_DeepColor")) wfMat.SetColor("_DeepColor", origWfDeep);
            if (wfMat.HasProperty("_ShallowColor")) wfMat.SetColor("_ShallowColor", origWfShallow);
            if (wfMat.HasProperty("_FoamColor")) wfMat.SetColor("_FoamColor", origWfFoam);
            if (wfMat.HasProperty("_FresnelColor")) wfMat.SetColor("_FresnelColor", origWfFresnel);
        }

        // Déverrouiller le joueur dès que la fleur a atterri !
        PlayerLockManager.SetPlayerLocked(false);

        // Panoramique rapide de recentrage caméra sur le joueur
        Vector3 endPlayerPos = playerObj != null ? playerObj.transform.position : startSunPos;
        float returnPanElapsed = 0f;
        float returnPanDuration = 0.5f;
        while (returnPanElapsed < returnPanDuration)
        {
            returnPanElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, returnPanElapsed / returnPanDuration);
            camProxyObj.transform.position = Vector3.Lerp(startSunPos, endPlayerPos, t);
            yield return null;
        }

        // Restaurer FOV & cible Cinemachine
        if (cineCam != null) cineCam.Lens.FieldOfView = origFOV;
        if (mainCam != null) mainCam.fieldOfView = origFOV;
        if (cineCam != null && origCineFollow != null) cineCam.Follow = origCineFollow;

        if (camProxyObj != null) Destroy(camProxyObj);
        if (topBarObj != null && topBarObj.transform.parent != null) Destroy(topBarObj.transform.parent.gameObject);
        if (titleCardObj != null) Destroy(titleCardObj.transform.parent != null ? titleCardObj.transform.parent.gameObject : titleCardObj);

        ReturnMalletsToRest();
        if (audioSource != null) audioSource.volume = origAudioVolume;

        itemInteractionRoutine = null;
        StartIdleRandomRoutine();
    }

    // =====================================================================
    //   HELPERS CONCERT — Tremblement, Flash, Titre Punk
    // =====================================================================

    /// <summary>Applique le punch caméra juste avant le rendu (après Cinemachine), puis le retire.</summary>
    private void OnConcertBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == null || cam != concertShakeCam || concertShakeApplied) return;
        Transform t = cam.transform;
        concertShakeSavedPos = t.position;
        concertShakeSavedRot = t.rotation;
        t.position = concertShakeSavedPos + concertShakeSavedRot * concertShakeOffset;
        t.rotation = concertShakeSavedRot * Quaternion.Euler(0f, 0f, concertShakeRoll);
        concertShakeApplied = true;
    }

    private void OnConcertEndCameraRendering(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == null || cam != concertShakeCam || !concertShakeApplied) return;
        cam.transform.SetPositionAndRotation(concertShakeSavedPos, concertShakeSavedRot);
        concertShakeApplied = false;
    }

    private void StopConcertShake()
    {
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnConcertBeginCameraRendering;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= OnConcertEndCameraRendering;
        if (concertShakeApplied && concertShakeCam != null)
            concertShakeCam.transform.SetPositionAndRotation(concertShakeSavedPos, concertShakeSavedRot);
        concertShakeApplied = false;
        concertShakeOffset = Vector3.zero;
        concertShakeRoll = 0f;
        concertShakeCam = null;
    }

    private void OnDisable()
    {
        StopConcertShake();
    }

    /// <summary>Flash blanc stroboscopique à l'apparition des flammes.</summary>
    private IEnumerator FireSpawnFlashRoutine(Camera cam, Light fillLt, Light spotLt, float flashIntensity)
    {
        // Créer une image blanche en overlay pour le flash
        GameObject flashCanvasObj = new GameObject("ConcertFireFlash_Canvas");
        Canvas flashCanvas = flashCanvasObj.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 1000;
        flashCanvasObj.AddComponent<CanvasScaler>();

        GameObject flashImgObj = new GameObject("FlashImage");
        flashImgObj.transform.SetParent(flashCanvasObj.transform, false);
        UnityEngine.UI.Image flashImg = flashImgObj.AddComponent<UnityEngine.UI.Image>();
        flashImg.color = new Color(1f, 0.8f, 0.3f, 0f);
        RectTransform flashRect = flashImg.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        // Phase 1 : Flash rapide (0 → 1)
        float flashInDuration = 0.07f;
        float elapsed = 0f;
        while (elapsed < flashInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.85f, elapsed / flashInDuration);
            flashImg.color = new Color(1f, 0.75f, 0.2f, alpha);
            if (fillLt != null) fillLt.intensity = concertFillLightIntensity + flashIntensity * (1f - elapsed / flashInDuration) * 15f;
            if (spotLt != null) spotLt.intensity = concertSpotlightIntensity + flashIntensity * (1f - elapsed / flashInDuration) * 10f;
            yield return null;
        }

        // Phase 2 : Flash rapide (1 → 0)
        float flashOutDuration = 0.55f;
        elapsed = 0f;
        while (elapsed < flashOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.85f, 0f, elapsed / flashOutDuration);
            flashImg.color = new Color(1f, 0.6f, 0.1f, alpha);
            yield return null;
        }

        Destroy(flashCanvasObj);
    }

    /// <summary>Crée le titre punk 'ROCK CONCERT' en overlay.</summary>
    private GameObject CreateConcertTitleCard()
    {
        GameObject canvasObj = new GameObject("ConcertTitle_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        canvasObj.AddComponent<CanvasScaler>();

        // Fond noir semi-transparent derrière le titre
        GameObject bgObj = new GameObject("ConcertTitle_BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.0f);
        RectTransform bgRect = bgImg.rectTransform;
        bgRect.anchorMin = new Vector2(0f, 0.35f);
        bgRect.anchorMax = new Vector2(1f, 0.65f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Titre principal
        GameObject titleObj = new GameObject("ConcertTitle_Text");
        titleObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "🎸 ROCK CONCERT 🎸";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(concertTitleColor.r, concertTitleColor.g, concertTitleColor.b, 0f);
        RectTransform textRect = titleText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.38f);
        textRect.anchorMax = new Vector2(1f, 0.62f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Sous-titre
        GameObject subObj = new GameObject("ConcertTitle_Sub");
        subObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text subText = subObj.AddComponent<UnityEngine.UI.Text>();
        subText.text = "♪ LE TOURNESOL EN CONCERT ♪";
        subText.fontSize = 32;
        subText.fontStyle = FontStyle.Italic;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(1f, 1f, 0.7f, 0f);
        RectTransform subRect = subText.rectTransform;
        subRect.anchorMin = new Vector2(0.1f, 0.33f);
        subRect.anchorMax = new Vector2(0.9f, 0.45f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        return titleObj;
    }

    /// <summary>Anime l'apparition et la disparition du titre punk.</summary>
    private IEnumerator AnimateConcertTitleCard(GameObject titleObj, float displayDuration)
    {
        if (titleObj == null) yield break;
        UnityEngine.UI.Text mainText = titleObj.GetComponent<UnityEngine.UI.Text>();
        // Chercher aussi le sous-titre et le fond dans le canvas parent
        Transform canvasT = titleObj.transform.parent;
        UnityEngine.UI.Text subText = null;
        UnityEngine.UI.Image bgImg = null;
        if (canvasT != null)
        {
            foreach (Transform child in canvasT)
            {
                var t = child.GetComponent<UnityEngine.UI.Text>();
                if (t != null && t != mainText) subText = t;
                var img = child.GetComponent<UnityEngine.UI.Image>();
                if (img != null) bgImg = img;
            }
        }

        // Fade in
        float fadeIn = 0.4f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeIn);
            // Pulsation de scale pour l'entrée
            float scaleImpact = 1f + 0.15f * Mathf.Sin(elapsed / fadeIn * Mathf.PI);
            if (titleObj != null) titleObj.transform.localScale = Vector3.one * scaleImpact;
            if (mainText != null) mainText.color = new Color(concertTitleColor.r, concertTitleColor.g, concertTitleColor.b, alpha);
            if (subText != null) subText.color = new Color(1f, 1f, 0.7f, alpha * 0.85f);
            if (bgImg != null) bgImg.color = new Color(0f, 0f, 0f, alpha * 0.55f);
            yield return null;
        }
        if (mainText != null) mainText.color = new Color(concertTitleColor.r, concertTitleColor.g, concertTitleColor.b, 1f);

        // Affichage : légère pulsation
        float holdStart = Time.time;
        float holdDuration = displayDuration - fadeIn - 0.5f;
        while (Time.time - holdStart < holdDuration)
        {
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 4f);
            if (titleObj != null) titleObj.transform.localScale = Vector3.one * pulse;
            // Scintillement de la couleur du titre
            float flicker = 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(Time.time * 12f));
            if (mainText != null) mainText.color = new Color(concertTitleColor.r, concertTitleColor.g * flicker, concertTitleColor.b, 1f);
            yield return null;
        }

        // Fade out
        float fadeOut = 0.5f;
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.SmoothStep(1f, 0f, elapsed / fadeOut);
            if (mainText != null) mainText.color = new Color(concertTitleColor.r, concertTitleColor.g, concertTitleColor.b, alpha);
            if (subText != null) subText.color = new Color(1f, 1f, 0.7f, alpha * 0.85f);
            if (bgImg != null) bgImg.color = new Color(0f, 0f, 0f, alpha * 0.55f);
            yield return null;
        }

        if (canvasT != null) Destroy(canvasT.gameObject);
    }

    /// <summary>
    /// Effectue un fondu d'apparition progressif de la musique (Fade In).
    /// </summary>
    private IEnumerator FadeInAudioSourceRoutine(AudioSource source, float targetVol, float duration)
    {
        if (source == null || duration <= 0f) yield break;

        float elapsed = 0f;
        source.volume = 0f;

        while (elapsed < duration && source != null && source.isPlaying)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(0f, targetVol, t);
            yield return null;
        }

        if (source != null)
        {
            source.volume = targetVol;
        }
    }

    private void CreateConcertCinemaBars(out GameObject topObj, out GameObject bottomObj, out RectTransform topRect, out RectTransform bottomRect)
    {
        GameObject canvasObj = new GameObject("ConcertCinemaUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        topObj = new GameObject("CinemaBar_Top");
        topObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image topImg = topObj.AddComponent<UnityEngine.UI.Image>();
        topImg.color = Color.black;
        topRect = topImg.rectTransform;
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 0f);

        bottomObj = new GameObject("CinemaBar_Bottom");
        bottomObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bottomImg = bottomObj.AddComponent<UnityEngine.UI.Image>();
        bottomImg.color = Color.black;
        bottomRect = bottomImg.rectTransform;
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(0f, 0f);
    }

    private IEnumerator AnimateConcertCinemaBars(RectTransform topBar, RectTransform bottomBar, bool show, float duration)
    {
        if (topBar == null || bottomBar == null) yield break;

        float targetHeight = show ? 90f : 0f;
        float startHeight = topBar.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float h = Mathf.Lerp(startHeight, targetHeight, t);

            topBar.sizeDelta = new Vector2(0f, h);
            bottomBar.sizeDelta = new Vector2(0f, h);

            yield return null;
        }

        topBar.sizeDelta = new Vector2(0f, targetHeight);
        bottomBar.sizeDelta = new Vector2(0f, targetHeight);
    }
}
