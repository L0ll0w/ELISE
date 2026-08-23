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

    protected override IEnumerator ExecuteCinematicRoutine()
    {
        Debug.Log($"[GardenerCinematicTriggerZone] Déclenchement de la cinématique sur '{gameObject.name}'");

        // 1. Récupération des références caméra et joueur (comme dans la classe parente)
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
        }

        if (virtualCamera == null)
        {
            Debug.LogError("[GardenerCinematicTriggerZone] Aucune CinemachineCamera trouvée dans la scène !");
            yield break;
        }

        // Trouver le joueur dans la scène
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        // Geler le joueur et désactiver le CinemachineHelper
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (cameraHelper != null)
        {
            cameraHelper.SaveOriginalSettings();
            cameraHelper.enabled = false;
        }

        // Attendre avant de commencer le déplacement de la caméra (le joueur est gelé pendant ce temps)
        if (delayBeforeCameraMove > 0f)
        {
            yield return new WaitForSeconds(delayBeforeCameraMove);
        }

        // 2. Transition de la caméra vers la cible de focus (le Jardinier)
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
        if (dialogueData != null && DialogueManager.Instance != null)
        {
            bool dialogueFinished = false;
            
            DialogueManager.Instance.StartDialogue(dialogueData, () =>
            {
                dialogueFinished = true;
                // Forcer le joueur à rester figé après la fermeture de la fenêtre de dialogue
                if (playerMovement != null)
                {
                    playerMovement.enabled = false;
                }
            });

            // Attendre que le joueur ait fini de lire et fermé la boîte de dialogue
            yield return new WaitUntil(() => dialogueFinished);
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
        if (secondDialogueData != null && DialogueManager.Instance != null)
        {
            bool secondDialogueFinished = false;
            
            DialogueManager.Instance.StartDialogue(secondDialogueData, () =>
            {
                secondDialogueFinished = true;
                if (playerMovement != null)
                {
                    playerMovement.enabled = false;
                }
            });

            yield return new WaitUntil(() => secondDialogueFinished);
        }

        // 6. ANIMATION : Le jardinier se retourne et s'enfuit vers la droite
        if (gardenerTransform != null)
        {
            // Retourner sur l'animation Levitate au moment où il s'en va
            if (gardenerAnimator != null)
            {
                gardenerAnimator.Play("levitate");
            }

            // Se tourner vers la droite (direction de fuite)
            if (gardenerSprite != null)
            {
                gardenerSprite.flipX = true; // flipX inversé (true = regarde à droite)
            }

            Vector3 exitPos = new Vector3(
                gardenerTransform.position.x + gardenerExitDistance,
                gardenerTransform.position.y,
                gardenerTransform.position.z
            );

            // Démarrer la fuite (on attend 1 seconde pour le laisser démarrer sa course avant de ramener la caméra)
            StartCoroutine(MoveGardenerRoutine(gardenerTransform, exitPos, gardenerMoveSpeed, gardenerSprite));
            yield return new WaitForSeconds(1.0f);
        }

        // 7. Transition de retour vers la caméra du joueur
        yield return StartCoroutine(TransitionCameraBack());

        // 8. Réactiver le joueur et détruire/désactiver le jardinier si besoin (on le détruit après sa fuite pour nettoyer la scène)
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Nettoyage : Détruire ou repositionner le jardinier s'il s'est enfui
        if (gardenerTransform != null)
        {
            if (gardenerPostCinematicTarget != null)
            {
                // Attendre la fin de la course de fuite (durée de fuite = 15m / 4m/s = 3.75s)
                // On a déjà attendu 1.0s de fuite + la transition de retour caméra (transitionOutDuration, 2.0s par défaut).
                // On attend encore 2 secondes pour être sûr qu'il est hors écran et a fini sa course.
                yield return new WaitForSeconds(2.0f);

                if (gardenerTransform != null)
                {
                    gardenerTransform.position = gardenerPostCinematicTarget.position;
                    gardenerTransform.rotation = gardenerPostCinematicTarget.rotation;

                    if (gardenerAnimator != null)
                    {
                        gardenerAnimator.Play("idle");
                    }
                    if (gardenerSprite != null)
                    {
                        gardenerSprite.flipX = false; // Réinitialiser le regard vers la gauche par défaut
                    }
                }
            }
            else
            {
                Destroy(gardenerTransform.gameObject, 3f);
            }
        }

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
    /// Coroutine déplaçant de manière fluide le Transform vers la cible.
    /// </summary>
    private IEnumerator MoveGardenerRoutine(Transform gardener, Vector3 target, float speed, SpriteRenderer sprite)
    {
        Vector3 startPosition = gardener.position;
        float distance = Vector3.Distance(startPosition, target);
        
        if (distance > 0.05f)
        {
            float duration = distance / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Calculer la position de base
                Vector3 basePos = Vector3.Lerp(startPosition, target, t);

                // Ajouter l'oscillation de lévitation sur Y si activée
                if (enableGardenerLevitation)
                {
                    basePos.y += Mathf.Sin(elapsed * gardenerLevitationSpeed) * gardenerLevitationAmount;
                }

                gardener.position = basePos;
                yield return null;
            }
        }

        gardener.position = target;
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
