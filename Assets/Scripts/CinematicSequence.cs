using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Cinemachine;

/// <summary>
/// Gestionnaire de séquences cinématographiques modulable pour cinématiques d'introduction ou de jeu.
/// Propose des transitions lissées avec courbe d'animation (EaseInOut), un micro-mouvement de travelling continu (Drift)
/// et des bandes noires cinéma optionnelles à hauteur personnalisable !
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/Cinematic Sequence")]
public class CinematicSequence : MonoBehaviour
{
    public enum TransitionType
    {
        Cut,    // Transition nette et instantanée (changement d'angle brut)
        Smooth  // Transition douce et lissée avec accélération/décélération
    }

    [System.Serializable]
    public class CinematicShot
    {
        [Tooltip("Nom indicatif du plan (ex: 1. Fleur, 2. Statues, 3. Plan large)")]
        public string shotName = "Nouveau Plan";

        [Tooltip("Cible Transform sur laquelle la caméra s'aligne (ex: le haut de la fleur, une statue...)")]
        public Transform target;

        [Tooltip("Durée d'affichage de ce plan en secondes.")]
        public float duration = 3f;

        [Header("Positionnement Caméra (Offset 3D)")]
        [Tooltip("Décalage horizontal (X) : Négatif = Gauche, Positif = Droite.")]
        public float offsetX = 0f;

        [Tooltip("Hauteur (Y) de la caméra par rapport à la cible.")]
        public float height = 4f;

        [Tooltip("Distance de recul (Z) de la caméra par rapport à la cible.")]
        public float distance = 10f;

        [Header("Orientation & Zoom")]
        [Tooltip("Inclinaison X (Pitch) en degrés (Regard vers le bas/haut).")]
        [Range(0f, 85f)]
        public float pitchAngle = 20f;

        [Tooltip("Rotation Y (Yaw) en degrés (Pivotement Gauche/Droite).")]
        [Range(-180f, 180f)]
        public float yawAngle = 0f;

        [Tooltip("Field Of View (FOV) pour zoomer ou dézoomer. Valeur conseillée : entre 20 et 60 (ne pas mettre 0).")]
        [Range(5f, 120f)]
        public float fov = 40f;

        [Header("Transitions & Fluidité")]
        [Tooltip("Type de transition vers ce plan.")]
        public TransitionType transition = TransitionType.Cut;

        [Tooltip("Durée de la transition lissée (en secondes).")]
        public float transitionDuration = 2f;

        [Tooltip("Courbe d'accélération/décélération du mouvement.")]
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Mouvement Cinéma Continu (Slow Drift)")]
        [Tooltip("Si activé, la caméra continue d'avancer/zoomer très lentement pendant le plan pour un rendu vivant (Slow Pan/Travelling).")]
        public bool enableSlowDrift = true;

        [Tooltip("Intensité du zoom lent pendant le maintien du plan.")]
        public float slowDriftAmount = 1.5f;

        [Header("Effets de Fondu")]
        [Tooltip("Effectue un fondu au noir (Fade In) au début de ce plan.")]
        public bool fadeInAtStart = false;

        [Tooltip("Temps d'attente maintenu dans le noir complet AVANT de commencer à estomper vers le jeu.")]
        public float fadeInHoldDuration = 1.5f;

        [Tooltip("Durée du fondu d'apparition (Fade In) en secondes.")]
        public float fadeDuration = 1.2f;

        [Tooltip("Effectue un fondu au noir (Fade Out) à la fin de ce plan.")]
        public bool fadeOutAtEnd = false;
        
        [Tooltip("Durée du fondu de disparition (Fade Out vers le noir) en secondes.")]
        public float fadeOutDuration = 1.2f;
    }

    [Header("Options Cinéma (Style Film)")]
    [Tooltip("Afficher les bandes noires cinéma (Letterbox 21:9) au-dessus et en dessous pendant la cinématique.")]
    [SerializeField] private bool enableCinemaBars = true;

    [Tooltip("Hauteur/Épaisseur des bandes noires cinéma en pixels (ex: 90, 120, 150).")]
    [Range(20f, 300f)]
    [SerializeField] private float cinemaBarHeight = 90f;

    [Header("Prévisualisation en Direct (Mode Édition)")]
    [Tooltip("Activer l'aperçu en direct dans la fenêtre Game / Scene pendant le réglage des paramètres.")]
    [SerializeField] private bool enableLivePreview = true;

    [Tooltip("Index du plan actuellement prévisualisé (0 = 1er plan, 1 = 2ème plan, etc.).")]
    [Min(0)]
    [SerializeField] private int previewShotIndex = 0;

    [Header("Liste des Plans de la Séquence")]
    [SerializeField] private List<CinematicShot> shots = new List<CinematicShot>();

    [Header("Paramètres de Déclenchement")]
    [Tooltip("Lancer automatiquement la cinématique au démarrage du jeu.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Désactiver le PlayerMovement pendant la cinématique.")]
    [SerializeField] private bool disablePlayerControls = true;

    [Header("Références Caméra & Fondu UI")]
    [Tooltip("Caméra Cinemachine principale (si laissée vide, cherchera automatiquement dans la scène).")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Tooltip("CanvasGroup utilisé pour le fondu au noir (si laissé vide, sera créé automatiquement en jeu).")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Remplacement du Joueur (Fin de Séquence)")]
    [Tooltip("Activer le remplacement du joueur actuel par un préfabriqué à la fin de la cinématique.")]
    [SerializeField] private bool replacePlayerAtEnd = false;

    [Tooltip("Le préfabriqué du joueur à instancier (ex: Prefabs/Player).")]
    [SerializeField] private GameObject playerPrefab;

    [Tooltip("Le GameObject du joueur actuel à remplacer (si laissé vide, cherchera le joueur actuel).")]
    [SerializeField] private GameObject playerToReplace;

    [Header("Événements")]
    [Tooltip("Déclenché à la fin complète de la cinématique.")]
    public UnityEvent onSequenceComplete;

    [Tooltip("Déclenché à chaque changement de plan (renvoie l'index du plan).")]
    public UnityEvent<int> onShotStart;

    private CinemachineHelper cinemachineHelper;
    private PlayerMovement playerMovement;
    private RectTransform topCinemaBar;
    private RectTransform bottomCinemaBar;
    private bool isPlaying = false;

    private void OnValidate()
    {
        // Sécurité FOV : empêche les FOV à 0 qui provoquent l'écran violet en URP
        if (shots != null)
        {
            foreach (var shot in shots)
            {
                if (shot != null && shot.fov < 5f)
                {
                    shot.fov = 40f;
                }
            }
        }

        // Mise à jour de la caméra en direct dans l'onglet Game dès qu'on touche à un slider/champ dans l'Inspector !
        if (!Application.isPlaying && enableLivePreview)
        {
            PreviewCurrentShot();
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying) return;

        InitReferences();

        if (playOnStart && shots != null && shots.Count > 0)
        {
            // Positionne immédiatement la caméra sur le 1er plan avant même le premier rendu d'image
            ApplyShotInstant(shots[0]);

            // Si le tout premier plan commence par un FadeIn, l'écran doit démarrer 100% NOIR immédiatement dès Awake
            if (shots[0].fadeInAtStart && fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
            }
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (playOnStart)
        {
            PlaySequence();
        }
    }

    private void InitReferences()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (cinemachineCamera != null)
        {
            cinemachineHelper = cinemachineCamera.GetComponent<CinemachineHelper>();
        }

        // Sauvegarder les paramètres d'origine de la caméra avant qu'ils ne soient modifiés par la cinématique
        if (cinemachineHelper != null)
        {
            cinemachineHelper.SaveOriginalSettings();
        }

        playerMovement = FindFirstObjectByType<PlayerMovement>();

        // Création automatique d'un écran de fondu noir et de bandes noires si non assignés
        if ((fadeCanvasGroup == null || topCinemaBar == null) && Application.isPlaying)
        {
            CreateAutoFadeAndCinemaCanvas();
        }
    }

    /// <summary>
    /// Prévisualise instantanément le plan sélectionné dans l'onglet Game/Scene pendant l'édition.
    /// </summary>
    public void PreviewCurrentShot()
    {
        if (shots == null || shots.Count == 0) return;

        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (cinemachineCamera == null) return;

        int index = Mathf.Clamp(previewShotIndex, 0, shots.Count - 1);
        CinematicShot shot = shots[index];

        if (shot != null && shot.target != null)
        {
            float safeFOV = shot.fov < 5f ? 40f : shot.fov;

            cinemachineCamera.Follow = shot.target;
            cinemachineCamera.transform.position = shot.target.position + new Vector3(shot.offsetX, shot.height, -shot.distance);
            cinemachineCamera.transform.rotation = Quaternion.Euler(shot.pitchAngle, shot.yawAngle, 0f);
            cinemachineCamera.Lens.FieldOfView = safeFOV;
        }
    }

    /// <summary>
    /// Lance la séquence cinématographique en jeu.
    /// </summary>
    public void PlaySequence()
    {
        if (isPlaying) return;
        StartCoroutine(ExecuteSequenceRoutine());
    }

    private IEnumerator ExecuteSequenceRoutine()
    {
        isPlaying = true;

        if (cinemachineHelper != null)
        {
            cinemachineHelper.enabled = false;
        }

        if (disablePlayerControls && playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Animation d'apparition des bandes noires cinéma
        if (enableCinemaBars && topCinemaBar != null && bottomCinemaBar != null)
        {
            StartCoroutine(AnimateCinemaBarsRoutine(true, 1f));
        }

        for (int i = 0; i < shots.Count; i++)
        {
            CinematicShot currentShot = shots[i];
            onShotStart?.Invoke(i);

            // 1. Placer ou transitionner la caméra vers le plan
            if (currentShot.transition == TransitionType.Cut || i == 0)
            {
                ApplyShotInstant(currentShot);
            }
            else
            {
                yield return StartCoroutine(ApplyShotSmoothRoutine(currentShot));
            }

            // 2. Effectuer le fondu d'apparition (Fade In)
            if (currentShot.fadeInAtStart && fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;

                // Temps d'attente au noir complet configurable avant d'estomper le noir
                if (currentShot.fadeInHoldDuration > 0f)
                {
                    yield return new WaitForSeconds(currentShot.fadeInHoldDuration);
                }

                yield return StartCoroutine(FadeRoutine(1f, 0f, currentShot.fadeDuration));
            }

            // 3. Maintenir le plan avec un micro-mouvement de travelling (Slow Drift) très élégant
            if (currentShot.enableSlowDrift && currentShot.duration > 0f)
            {
                yield return StartCoroutine(SlowDriftHoldRoutine(currentShot));
            }
            else
            {
                yield return new WaitForSeconds(currentShot.duration);
            }

            // 4. Effectuer le fondu de disparition (Fade Out vers le noir)
            if (currentShot.fadeOutAtEnd && fadeCanvasGroup != null)
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, currentShot.fadeOutDuration));
            }
        }

        // Retrait des bandes noires à la fin
        if (enableCinemaBars && topCinemaBar != null && bottomCinemaBar != null)
        {
            yield return StartCoroutine(AnimateCinemaBarsRoutine(false, 0.8f));
        }

        // Remplacement du joueur si configuré
        if (replacePlayerAtEnd && playerPrefab != null)
        {
            GameObject targetToReplaceObj = playerToReplace != null ? playerToReplace : (playerMovement != null ? playerMovement.gameObject : GameObject.FindGameObjectWithTag("Player"));

            if (targetToReplaceObj != null)
            {
                Vector3 spawnPos = targetToReplaceObj.transform.position;
                Quaternion spawnRot = targetToReplaceObj.transform.rotation;

                // Instanciation de la vraie prefab du joueur
                GameObject newPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
                newPlayer.name = playerPrefab.name; // Nettoyer le suffixe "(Clone)"

                // Mettre à jour le cache local pour réactiver les contrôles sur la bonne instance
                playerMovement = newPlayer.GetComponent<PlayerMovement>();

                // Mettre à jour le GroupManager si disponible
                if (GroupManager.Instance != null)
                {
                    GroupManager.Instance.SetLeader(newPlayer.transform);
                }

                // Réorienter toutes les caméras possédant un CinemachineHelper sur le nouveau joueur
                CinemachineHelper[] allHelpers = FindObjectsByType<CinemachineHelper>(FindObjectsSortMode.None);
                if (allHelpers != null && allHelpers.Length > 0)
                {
                    foreach (var helper in allHelpers)
                    {
                        helper.SetTargetPlayer(newPlayer.transform);
                        helper.enabled = true;
                    }
                }
                else if (cinemachineHelper != null)
                {
                    cinemachineHelper.SetTargetPlayer(newPlayer.transform);
                }

                // Détruire l'ancien joueur
                Destroy(targetToReplaceObj);
            }
            else
            {
                Debug.LogWarning("CinematicSequence: Impossible de remplacer le joueur car aucun joueur à remplacer n'a été trouvé.");
            }
        }

        if (cinemachineHelper != null)
        {
            cinemachineHelper.enabled = true;
            // Si le joueur a été remplacé, SetTargetPlayer a déjà fait un appel à UpdateCameraSettings(true).
            // Sinon, on fait un appel UpdateCameraSettings(true) pour assurer une transition propre vers la caméra de jeu.
            if (!replacePlayerAtEnd)
            {
                cinemachineHelper.UpdateCameraSettings(true);
            }
        }

        if (disablePlayerControls && playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Ne masque l'écran noir final que si le dernier plan n'a PAS demandé de FadeOutAtEnd
        bool lastShotFadedOut = (shots.Count > 0 && shots[shots.Count - 1].fadeOutAtEnd);
        if (!lastShotFadedOut && fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }

        isPlaying = false;
        onSequenceComplete?.Invoke();
    }

    private void ApplyShotInstant(CinematicShot shot)
    {
        if (cinemachineCamera == null || shot.target == null) return;

        float safeFOV = shot.fov < 5f ? 40f : shot.fov;

        cinemachineCamera.Follow = shot.target;

        var follow = cinemachineCamera.GetComponent<CinemachineFollow>();
        if (follow == null) follow = cinemachineCamera.GetComponentInChildren<CinemachineFollow>();
        if (follow != null)
        {
            follow.FollowOffset = new Vector3(shot.offsetX, shot.height, -shot.distance);
        }

        cinemachineCamera.transform.position = shot.target.position + new Vector3(shot.offsetX, shot.height, -shot.distance);
        cinemachineCamera.transform.rotation = Quaternion.Euler(shot.pitchAngle, shot.yawAngle, 0f);
        cinemachineCamera.Lens.FieldOfView = safeFOV;
    }

    private IEnumerator ApplyShotSmoothRoutine(CinematicShot shot)
    {
        if (cinemachineCamera == null || shot.target == null) yield break;

        cinemachineCamera.Follow = shot.target;

        var follow = cinemachineCamera.GetComponent<CinemachineFollow>();
        if (follow == null) follow = cinemachineCamera.GetComponentInChildren<CinemachineFollow>();

        Vector3 startPos = cinemachineCamera.transform.position;
        Quaternion startRot = cinemachineCamera.transform.rotation;
        float startFOV = cinemachineCamera.Lens.FieldOfView;

        Vector3 targetPos = shot.target.position + new Vector3(shot.offsetX, shot.height, -shot.distance);
        Quaternion targetRot = Quaternion.Euler(shot.pitchAngle, shot.yawAngle, 0f);
        float targetFOV = shot.fov < 5f ? 40f : shot.fov;

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, shot.transitionDuration);
        AnimationCurve curve = shot.movementCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = curve.Evaluate(normalizedTime);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            cinemachineCamera.transform.position = currentPos;
            cinemachineCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            if (follow != null)
            {
                follow.FollowOffset = currentPos - shot.target.position;
            }

            yield return null;
        }

        cinemachineCamera.transform.position = targetPos;
        cinemachineCamera.transform.rotation = targetRot;
        cinemachineCamera.Lens.FieldOfView = targetFOV;

        if (follow != null)
        {
            follow.FollowOffset = targetPos - shot.target.position;
        }
    }

    private IEnumerator SlowDriftHoldRoutine(CinematicShot shot)
    {
        if (cinemachineCamera == null || shot.target == null) yield break;

        var follow = cinemachineCamera.GetComponent<CinemachineFollow>();
        if (follow == null) follow = cinemachineCamera.GetComponentInChildren<CinemachineFollow>();

        Vector3 basePos = shot.target.position + new Vector3(shot.offsetX, shot.height, -shot.distance);
        float baseFOV = shot.fov < 5f ? 40f : shot.fov;

        float elapsed = 0f;
        float totalTime = shot.duration;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / totalTime;

            // Micro-zooming très doux + léger travelling vers l'avant pendant le maintien
            float currentFOV = Mathf.Lerp(baseFOV, baseFOV - shot.slowDriftAmount, progress);
            Vector3 currentPos = Vector3.Lerp(basePos, basePos + cinemachineCamera.transform.forward * (shot.slowDriftAmount * 0.1f), progress);

            cinemachineCamera.Lens.FieldOfView = currentFOV;
            cinemachineCamera.transform.position = currentPos;

            if (follow != null)
            {
                follow.FollowOffset = currentPos - shot.target.position;
            }

            yield return null;
        }
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        fadeCanvasGroup.alpha = fromAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = toAlpha;
    }

    private IEnumerator AnimateCinemaBarsRoutine(bool show, float duration)
    {
        if (topCinemaBar == null || bottomCinemaBar == null) yield break;

        float targetHeight = show ? cinemaBarHeight : 0f;
        float startHeight = topCinemaBar.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float h = Mathf.Lerp(startHeight, targetHeight, t);

            topCinemaBar.sizeDelta = new Vector2(0f, h);
            bottomCinemaBar.sizeDelta = new Vector2(0f, h);

            yield return null;
        }

        topCinemaBar.sizeDelta = new Vector2(0f, targetHeight);
        bottomCinemaBar.sizeDelta = new Vector2(0f, targetHeight);
    }

    private void CreateAutoFadeAndCinemaCanvas()
    {
        GameObject canvasObj = new GameObject("CinematicUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 1. Fond noir pour les fondus
        GameObject fadeObj = new GameObject("FadeLayer");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;

        RectTransform fadeRect = fadeImage.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;

        fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
        bool startBlack = (shots != null && shots.Count > 0 && shots[0].fadeInAtStart);
        fadeCanvasGroup.alpha = startBlack ? 1f : 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        // 2. Bandes Noires Cinéma (Top & Bottom Letterbox)
        GameObject topBarObj = new GameObject("CinemaBar_Top");
        topBarObj.transform.SetParent(canvasObj.transform, false);
        Image topImg = topBarObj.AddComponent<Image>();
        topImg.color = Color.black;
        topCinemaBar = topImg.rectTransform;
        topCinemaBar.anchorMin = new Vector2(0f, 1f);
        topCinemaBar.anchorMax = new Vector2(1f, 1f);
        topCinemaBar.pivot = new Vector2(0.5f, 1f);
        topCinemaBar.sizeDelta = new Vector2(0f, 0f);

        GameObject bottomBarObj = new GameObject("CinemaBar_Bottom");
        bottomBarObj.transform.SetParent(canvasObj.transform, false);
        Image bottomImg = bottomBarObj.AddComponent<Image>();
        bottomImg.color = Color.black;
        bottomCinemaBar = bottomImg.rectTransform;
        bottomCinemaBar.anchorMin = new Vector2(0f, 0f);
        bottomCinemaBar.anchorMax = new Vector2(1f, 0f);
        bottomCinemaBar.pivot = new Vector2(0.5f, 0f);
        bottomCinemaBar.sizeDelta = new Vector2(0f, 0f);
    }
}
