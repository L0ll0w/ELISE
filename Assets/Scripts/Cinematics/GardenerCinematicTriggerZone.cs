using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Script de cinématique spécialisé pour le Jardinier.
/// Gère le lancer d'arrosoir, le détachement du jardinier, son déplacement près du joueur,
/// son second dialogue, son départ vers la droite et le retour caméra final.
/// </summary>
[AddComponentMenu("2.5D RPG/Gardener Cinematic Trigger Zone")]
public class GardenerCinematicTriggerZone : CinematicTriggerZone
{
    [Header("Séquence Spécifique Jardinier")]
    [Tooltip("Le deuxième dialogue à lancer une fois le jardinier placé à côté du joueur.")]
    [SerializeField] private DialogueData secondDialogueData;

    [Tooltip("L'arrosoir géant. Si non assigné, sera recherché automatiquement dans la scène.")]
    [SerializeField] private GiantWateringCan wateringCan;

    [Tooltip("Le transform du Jardinier. Si non assigné, sera recherché parmi les enfants de l'arrosoir.")]
    [SerializeField] private Transform gardenerTransform;

    [Header("Paramètres d'Animation")]
    [Tooltip("Hauteur maximale à laquelle l'arrosoir est lancé.")]
    [SerializeField] private float throwHeight = 35f;

    [Tooltip("Durée de l'animation de lancer de l'arrosoir (en secondes).")]
    [SerializeField] private float throwDuration = 4f;

    [Tooltip("Vitesse de déplacement du jardinier (en unités/seconde).")]
    [SerializeField] private float gardenerMoveSpeed = 4f;

    [Tooltip("Décalage horizontal (X) par rapport au joueur où le jardinier s'arrête (à sa droite).")]
    [SerializeField] private float gardenerPlayerOffsetX = 2f;

    [Tooltip("Décalage vertical (Y) par rapport au joueur où le jardinier s'arrête (hauteur de lévitation de base au sol).")]
    [SerializeField] private float gardenerPlayerOffsetY = 0f;

    [Tooltip("Distance vers la droite parcourue par le jardinier pour s'enfuir.")]
    [SerializeField] private float gardenerExitDistance = 15f;

    [Tooltip("Décalage de profondeur (Z) par rapport au joueur où le jardinier s'arrête (un peu plus en arrière/haut sur l'écran).")]
    [SerializeField] private float gardenerPlayerOffsetZ = 1f;

    [Tooltip("Délai d'attente (en secondes) après l'arrivée du jardinier et avant le second dialogue.")]
    [SerializeField] private float delayBeforeSecondDialogue = 1.2f;

    [Header("Effet de Lévitation du Jardinier")]
    [Tooltip("Activer l'effet d'oscillation verticale pendant les déplacements du jardinier.")]
    [SerializeField] private bool enableGardenerLevitation = true;

    [Tooltip("Amplitude de l'oscillation verticale de lévitation (en mètres).")]
    [SerializeField] private float gardenerLevitationAmount = 0.25f;

    [Tooltip("Vitesse de l'oscillation de lévitation.")]
    [SerializeField] private float gardenerLevitationSpeed = 8f;

    [Header("Paramètres Post-Cinématique")]
    [Tooltip("Le point où repositionner le jardinier à la fin de la cinématique. S'il est défini, le jardinier y sera téléporté au lieu d'être détruit.")]
    [SerializeField] private Transform gardenerPostCinematicTarget;

    [Tooltip("Le flag de l'histoire à définir à True dans StoryStateManager une fois cette cinématique terminée.")]
    [SerializeField] private string flagToSetOnComplete = "gardener_intro_completed";

    [Header("Option de Saut (Skip)")]
    [Tooltip("Sauter cette première cinématique et téléporter le joueur directement à la suivante.")]
    [SerializeField] private bool skipFirstCinematic = false;

    [Tooltip("Le point de téléportation (Transform) devant la seconde rencontre avec le jardinier.")]
    [SerializeField] private Transform secondCinematicTeleportTarget;

    private void Start()
    {
        if (skipFirstCinematic && secondCinematicTeleportTarget != null)
        {
            StartCoroutine(SkipCinematicRoutine());
        }
    }

    private IEnumerator SkipCinematicRoutine()
    {
        // Attendre une frame pour laisser les scripts s'initialiser
        yield return null;

        // 1. Téléporter le joueur vers le point cible si spécifié
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && secondCinematicTeleportTarget != null)
        {
            Debug.Log($"[GardenerCinematicTriggerZone] skipFirstCinematic est actif. Téléportation du joueur vers {secondCinematicTeleportTarget.position}");
            
            Rigidbody playerRb = pm.GetComponent<Rigidbody>();
            bool wasKinematic = playerRb != null ? playerRb.isKinematic : false;
            if (playerRb != null) playerRb.isKinematic = true;

            pm.transform.position = secondCinematicTeleportTarget.position;

            if (playerRb != null) playerRb.isKinematic = wasKinematic;

            Animator playerAnim = pm.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = pm.GetComponentInChildren<Animator>();
            if (playerAnim != null)
            {
                playerAnim.Play("idle");
                playerAnim.SetBool("isWalking", false);
            }
        }

        // 2. Téléporter le Jardinier vers sa destination post-cinématique si spécifiée
        if (wateringCan == null)
        {
            wateringCan = FindFirstObjectByType<GiantWateringCan>();
        }

        if (wateringCan != null)
        {
            if (gardenerTransform == null)
            {
                foreach (Transform child in wateringCan.transform)
                {
                    if (child.name != "SpoutPoint" && child.name != "WaterSpoutParticles" && child.GetComponent<SpriteRenderer>() != null)
                    {
                        gardenerTransform = child;
                        break;
                    }
                }
            }
            // Désactiver l'arrosage et l'arrosoir (qui s'envole normalement)
            wateringCan.StopWatering();
            wateringCan.gameObject.SetActive(false);
        }

        if (gardenerTransform != null && gardenerPostCinematicTarget != null)
        {
            Debug.Log($"[GardenerCinematicTriggerZone] skipFirstCinematic est actif. Téléportation du jardinier vers {gardenerPostCinematicTarget.position}");
            
            // Détacher le jardinier de l'arrosoir
            gardenerTransform.SetParent(null, true);

            // Remettre la rotation Z à 0
            Vector3 euler = gardenerTransform.eulerAngles;
            euler.z = 0f;
            gardenerTransform.eulerAngles = euler;

            // Téléporter
            gardenerTransform.position = gardenerPostCinematicTarget.position;
            gardenerTransform.rotation = gardenerPostCinematicTarget.rotation;

            // Jouer l'animation Idle/Levitate
            Animator gardenerAnimator = gardenerTransform.GetComponent<Animator>();
            if (gardenerAnimator == null) gardenerAnimator = gardenerTransform.GetComponentInChildren<Animator>();
            if (gardenerAnimator != null)
            {
                gardenerAnimator.Play("idle");
            }

            SpriteRenderer gardenerSprite = gardenerTransform.GetComponent<SpriteRenderer>();
            if (gardenerSprite != null)
            {
                gardenerSprite.flipX = false; // Regard vers la gauche par défaut
            }
        }

        // Valider le flag de fin de première cinématique
        if (StoryStateManager.Instance != null && !string.IsNullOrEmpty(flagToSetOnComplete))
        {
            StoryStateManager.Instance.SetFlag(flagToSetOnComplete, true);
        }

        // Désactiver le déclencheur pour éviter les doubles déclenchements
        gameObject.SetActive(false);
    }

    protected override IEnumerator ExecuteCinematicRoutine()
    {
        Debug.Log($"[GardenerCinematicTriggerZone] Déclenchement de la cinématique sur '{gameObject.name}'");

        // 1. Récupération des références caméra et joueur
        EnsureReferences();

        if (virtualCamera == null)
        {
            Debug.LogError("[GardenerCinematicTriggerZone] Aucune CinemachineCamera trouvée dans la scène !");
            yield break;
        }

        SavePreCinematicState();

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

        // 2. Résolution automatique du Jardinier si focusTarget n'est pas configuré dans l'inspecteur
        if (focusTarget == null)
        {
            if (gardenerTransform != null)
            {
                focusTarget = gardenerTransform;
            }
            else
            {
                if (wateringCan == null) wateringCan = FindFirstObjectByType<GiantWateringCan>();
                if (wateringCan != null)
                {
                    foreach (Transform child in wateringCan.transform)
                    {
                        if (child.name != "SpoutPoint" && child.name != "WaterSpoutParticles" && child.GetComponent<SpriteRenderer>() != null)
                        {
                            gardenerTransform = child;
                            focusTarget = child;
                            break;
                        }
                    }
                }

                if (focusTarget == null)
                {
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
        }

        // Transition de la caméra vers la cible de focus (le Jardinier)
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

        // 3. Déclenchement du premier dialogue
        if (dialogueData != null)
        {
            yield return StartCoroutine(RunDialogue(dialogueData));
        }

        // 4. ANIMATION : Lancer d'arrosoir + Déplacement du Jardinier
        if (wateringCan == null)
        {
            wateringCan = FindFirstObjectByType<GiantWateringCan>();
        }

        if (wateringCan != null)
        {
            // Résoudre automatiquement le transform du jardinier s'il n'est pas assigné (normalement son enfant)
            if (gardenerTransform == null)
            {
                foreach (Transform child in wateringCan.transform)
                {
                    // Chercher un enfant possédant un SpriteRenderer et qui n'est pas lié à l'eau
                    if (child.name != "SpoutPoint" && child.name != "WaterSpoutParticles" && child.GetComponent<SpriteRenderer>() != null)
                    {
                        gardenerTransform = child;
                        break;
                    }
                }
                
                // Fallback : premier enfant si aucun correspondant trouvé
                if (gardenerTransform == null && wateringCan.transform.childCount > 0)
                {
                    gardenerTransform = wateringCan.transform.GetChild(0);
                }
            }

            // Arrêter l'inclinaison et l'arrosage
            wateringCan.StopWatering();
            
            // Désactiver le composant GiantWateringCan pour éviter qu'il n'interfère avec notre animation
            wateringCan.enabled = false;
        }

        SpriteRenderer gardenerSprite = null;
        Animator gardenerAnimator = null;
        if (gardenerTransform != null)
        {
            gardenerSprite = gardenerTransform.GetComponent<SpriteRenderer>();
            gardenerAnimator = gardenerTransform.GetComponent<Animator>();
            if (gardenerAnimator == null)
            {
                gardenerAnimator = gardenerTransform.GetComponentInChildren<Animator>();
            }

            // IMPORTANT : Détacher le jardinier de l'arroseur pour qu'il ne s'envole pas avec !
            // On conserve sa position mondiale exacte.
            gardenerTransform.SetParent(null, true);

            // Remettre la rotation Z à 0 pour éviter qu'il ne soit penché comme l'arroseur
            Vector3 euler = gardenerTransform.eulerAngles;
            euler.z = 0f;
            gardenerTransform.eulerAngles = euler;
        }

        // Lancer l'animation de propulsion de l'arroseur
        if (wateringCan != null)
        {
            StartCoroutine(ThrowWateringCanRoutine(wateringCan.transform));
        }

        // Passer le jardinier sur l'animation Idle au moment du lancer
        if (gardenerAnimator != null)
        {
            gardenerAnimator.Play("idle");
        }

        // Laisser un peu de temps (délai de 1 seconde) après le lancer d'arroseur
        yield return new WaitForSeconds(1.0f);

        // Faire déplacer le jardinier vers la droite du joueur
        if (gardenerTransform != null && playerMovement != null)
        {
            // La cible est à la droite du joueur (sur l'axe X), à une hauteur spécifique (sur l'axe Y) et un peu plus en arrière/haut (sur l'axe Z)
            Vector3 targetPosition = new Vector3(
                playerMovement.transform.position.x + gardenerPlayerOffsetX,
                playerMovement.transform.position.y + gardenerPlayerOffsetY,
                playerMovement.transform.position.z + gardenerPlayerOffsetZ
            );

            // Déplacer le jardinier et déplacer la caméra principale avec lui manuellement
            yield return StartCoroutine(MoveGardenerWithCameraRoutine(gardenerTransform, targetPosition, gardenerMoveSpeed, gardenerSprite));

            // Assurer que le jardinier fait bien face au joueur (le joueur est à gauche, donc le jardinier doit regarder à gauche)
            if (gardenerSprite != null)
            {
                gardenerSprite.flipX = false; // flipX inversé (false = regarde à gauche)
            }
        }

        // Petite pause dramatique avant le 2ème dialogue
        yield return new WaitForSeconds(delayBeforeSecondDialogue);

        // 5. Déclenchement du second dialogue
        if (secondDialogueData != null)
        {
            yield return StartCoroutine(RunDialogue(secondDialogueData));
        }

        // 6. ANIMATION : Le jardinier se retourne et s'enfuit bien vers la droite de l'écran (la caméra reste totalement fixe)
        if (gardenerTransform != null)
        {
            yield return StartCoroutine(MoveGardenerExitRoutine(gardenerTransform, gardenerMoveSpeed, gardenerSprite, gardenerAnimator));
        }

        // 7. Transition de retour fluide de la caméra vers le joueur (une fois le jardinier sorti et téléporté)
        yield return StartCoroutine(TransitionCameraBack());

        // 8. Réactiver le joueur
        UnlockPlayer();

        if (StoryStateManager.Instance != null && !string.IsNullOrEmpty(flagToSetOnComplete))
        {
            StoryStateManager.Instance.SetFlag(flagToSetOnComplete, true);
        }

        Debug.Log($"[GardenerCinematicTriggerZone] Fin de la cinématique sur '{gameObject.name}'");
    }

    /// <summary>
    /// Coroutine simulant le jet parabolique de l'arroseur très haut et très droit.
    /// L'arroseur retombe dans le fond du décor (avec un offset sur l'axe Z) sans être masqué.
    /// </summary>
    private IEnumerator ThrowWateringCanRoutine(Transform can)
    {
        Vector3 startPos = can.position;
        float elapsed = 0f;
        float backgroundZOffset = 20f; // Décale l'arroseur de 20 unités dans le fond (Z) pendant le vol

        // Calcul des constantes physiques pour la trajectoire sous gravité
        // On veut atteindre throwHeight après t_peak secondes (ex: 40% de la durée totale)
        float t_peak = throwDuration * 0.4f; 
        float gravity = (2f * throwHeight) / (t_peak * t_peak);
        float initialVerticalVelocity = gravity * t_peak;
        
        // Vitesse constante sur l'axe Z pour atteindre l'arrière-plan
        float zVelocity = backgroundZOffset / throwDuration;

        // Laisser chuter l'arroseur pendant toute la durée spécifiée multipliée par 1.6 pour lui laisser le temps de tomber très bas
        float totalTime = throwDuration * 1.6f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;

            // Formule physique de trajectoire : y = y0 + v0*t - 0.5*g*t^2
            float currentY = startPos.y + (initialVerticalVelocity * elapsed) - (0.5f * gravity * elapsed * elapsed);
            float currentZ = startPos.z + (zVelocity * elapsed);
            float currentX = startPos.x;

            can.position = new Vector3(currentX, currentY, currentZ);

            // Faire tourner l'arroseur dans les airs
            can.Rotate(Vector3.forward * 450f * Time.deltaTime, Space.Self);
            can.Rotate(Vector3.up * 180f * Time.deltaTime, Space.World);

            yield return null;
        }

        // Désactiver l'arroseur à la fin pour nettoyer la scène (il est déjà très bas hors de l'écran)
        can.gameObject.SetActive(false);
    }

    /// <summary>
    /// Fait fuir le jardinier en ligne droite vers la droite de l'écran (sans aucune diagonale).
    /// La caméra reste totalement fixe pendant son départ.
    /// Dès qu'il sort du champ de vision de la caméra, il est immédiatement téléporté à sa prochaine cinématique.
    /// </summary>
    private IEnumerator MoveGardenerExitRoutine(Transform gardener, float speed, SpriteRenderer sprite, Animator anim)
    {
        if (gardener == null) yield break;

        if (anim != null)
        {
            anim.Play("levitate");
        }

        if (sprite != null)
        {
            sprite.flipX = true; // Regard vers la droite (direction de fuite)
        }

        Camera mainCam = Camera.main;
        Vector3 screenRight;
        if (mainCam != null)
        {
            screenRight = mainCam.transform.right;
        }
        else if (virtualCamera != null)
        {
            screenRight = virtualCamera.transform.right;
        }
        else
        {
            screenRight = Vector3.right;
        }

        screenRight.y = 0f;
        screenRight = screenRight.sqrMagnitude > 0.001f ? screenRight.normalized : Vector3.right;

        Vector3 startPosition = gardener.position;
        float elapsed = 0f;
        float exitSpeed = Mathf.Max(speed, 5.5f); // Vitesse dynamique assurant un départ franc et vif
        bool isOffScreen = false;
        float maxTimeout = 5.0f; // Sécurité anti-blocage

        while (elapsed < maxTimeout && !isOffScreen)
        {
            elapsed += Time.deltaTime;
            Vector3 currentPos = startPosition + screenRight * (exitSpeed * elapsed);

            if (enableGardenerLevitation)
            {
                currentPos.y += Mathf.Sin(elapsed * gardenerLevitationSpeed) * gardenerLevitationAmount;
            }

            gardener.position = currentPos;

            // Détection de la sortie d'écran par la droite
            if (mainCam != null)
            {
                Vector3 viewportPos = mainCam.WorldToViewportPoint(gardener.position);
                // Le jardinier est entièrement sorti de l'écran par la droite dès que viewport.x > 1.12
                if (viewportPos.x > 1.12f)
                {
                    isOffScreen = true;
                }
            }
            else
            {
                if (Vector3.Distance(startPosition, currentPos) >= gardenerExitDistance)
                {
                    isOffScreen = true;
                }
            }

            yield return null;
        }

        // Téléportation immédiate à la prochaine cinématique dès qu'il quitte l'écran
        TeleportGardenerToNextCinematic(gardener, sprite, anim);
    }

    private void TeleportGardenerToNextCinematic(Transform gardener, SpriteRenderer sprite, Animator anim)
    {
        if (gardener == null) return;

        // Si le point cible n'a pas été assigné manuellement dans l'inspecteur, le rechercher automatiquement
        if (gardenerPostCinematicTarget == null)
        {
            GameObject pt = GameObject.Find("pointtp");
            if (pt != null)
            {
                gardenerPostCinematicTarget = pt.transform;
            }
            else
            {
                GardenerSecondCinematicTriggerZone secondZone = FindFirstObjectByType<GardenerSecondCinematicTriggerZone>();
                if (secondZone != null)
                {
                    gardenerPostCinematicTarget = secondZone.transform;
                }
            }
        }

        gardener.SetParent(null, true);

        if (gardenerPostCinematicTarget != null)
        {
            gardener.position = gardenerPostCinematicTarget.position;
            gardener.rotation = gardenerPostCinematicTarget.rotation;

            Vector3 euler = gardener.eulerAngles;
            euler.z = 0f;
            gardener.eulerAngles = euler;

            if (anim == null) anim = gardener.GetComponent<Animator>() ?? gardener.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Play("idle");
            }

            if (sprite == null) sprite = gardener.GetComponent<SpriteRenderer>() ?? gardener.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.flipX = false; // Regard vers la gauche par défaut (vers le joueur qui arrivera)
            }

            Debug.Log($"[GardenerCinematicTriggerZone] Jardinier téléporté avec succès à sa prochaine cinématique : {gardenerPostCinematicTarget.position}");
        }
        else
        {
            Destroy(gardener.gameObject, 1f);
        }
    }

    /// <summary>
    /// Déplace le jardinier tout en déplaçant la caméra principale manuellement pour le suivre sans Cinemachine Follow (évite les sauts d'amorti).
    /// </summary>
    private IEnumerator MoveGardenerWithCameraRoutine(Transform gardener, Vector3 target, float speed, SpriteRenderer sprite)
    {
        Vector3 startPosition = gardener.position;
        float distance = Vector3.Distance(startPosition, target);
        
        if (distance > 0.05f)
        {
            float duration = distance / speed;
            float elapsed = 0f;

            float targetYaw = zoomYaw >= 0f ? zoomYaw : (virtualCamera != null ? virtualCamera.transform.rotation.eulerAngles.y : 0f);
            Vector3 cameraOffset = Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(0f, zoomHeight, -zoomOutDistance);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Calculer la position de base
                Vector3 basePos = Vector3.Lerp(startPosition, target, t);

                // Ajouter l'oscillation de lévitation sur Y si activée
                Vector3 gardenerPos = basePos;
                if (enableGardenerLevitation)
                {
                    gardenerPos.y += Mathf.Sin(elapsed * gardenerLevitationSpeed) * gardenerLevitationAmount;
                }

                gardener.position = gardenerPos;

                // Déplacer la caméra principale manuellement à côté de sa trajectoire de base (mouvement linéaire et stable)
                if (virtualCamera != null)
                {
                    virtualCamera.transform.position = basePos + cameraOffset;
                }

                yield return null;
            }
        }

        gardener.position = target;
        if (virtualCamera != null)
        {
            float targetYaw = zoomYaw >= 0f ? zoomYaw : virtualCamera.transform.rotation.eulerAngles.y;
            Vector3 cameraOffset = Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(0f, zoomHeight, -zoomOutDistance);
            virtualCamera.transform.position = target + cameraOffset;
        }
    }
}
