using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Déclencheur générique de cinématiques dans le monde (Zone Trigger).
/// Gère le zoom-out ou le focus sur une cible, le gel du joueur, le lancement de dialogues minutés
/// et le retour fluide de la caméra avec reprise des mouvements du joueur.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Cinematic Trigger Zone")]
public class CinematicTriggerZone : MonoBehaviour
{
    [Header("Configuration Déclencheur")]
    [Tooltip("La cinématique ne se déclenche-t-elle qu'une seule fois ?")]
    [SerializeField] protected bool oneShot = true;

    [Header("Configuration Caméra (Focus / Dezoom)")]
    [Tooltip("Cible sur laquelle la caméra fait la mise au point. Si vide, fait un dezoom centré sur le joueur.")]
    [SerializeField] protected Transform focusTarget;

    [Tooltip("Distance de recul (Z) de la caméra par rapport à la cible pendant le focus.")]
    [SerializeField] protected float zoomOutDistance = 15f;

    [Tooltip("Hauteur (Y) de la caméra par rapport à la cible pendant le focus.")]
    [SerializeField] protected float zoomHeight = 6f;

    [Tooltip("Inclinaison verticale de la caméra (X Axis Rotation) en degrés pendant le focus.")]
    [Range(0f, 85f)]
    [SerializeField] protected float zoomPitch = 25f;

    [Tooltip("Orientation horizontale de la caméra (Y Axis Rotation) en degrés pendant le focus. Mettre à -1 pour conserver la rotation actuelle.")]
    [SerializeField] protected float zoomYaw = -1f;

    [Tooltip("Field Of View (FOV) pendant le focus.")]
    [Range(5f, 120f)]
    [SerializeField] protected float zoomFOV = 40f;

    [Tooltip("Temps d'attente en secondes après le déclenchement du trigger avant que la caméra ne commence à bouger.")]
    [SerializeField] protected float delayBeforeCameraMove = 0.0f;

    [Tooltip("Durée de la transition de caméra à l'aller (en secondes).")]
    [SerializeField] protected float transitionInDuration = 2.0f;

    [Tooltip("Durée de la transition de caméra au retour (en secondes).")]
    [SerializeField] protected float transitionOutDuration = 2.0f;

    [Tooltip("Courbe de transition pour le mouvement de caméra.")]
    [SerializeField] protected AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Actions & Dialogue")]
    [Tooltip("Temps d'attente en secondes après l'arrivée de la caméra et avant le déclenchement du dialogue.")]
    [SerializeField] protected float delayBeforeDialogue = 1.0f;

    [Tooltip("Dialogue optionnel à déclencher après le zoom/focus.")]
    [SerializeField] protected DialogueData dialogueData;

    [Tooltip("Temps d'attente en secondes après la fin du dialogue et avant le retour de la caméra.")]
    [SerializeField] protected float delayAfterDialogue = 1.0f;

    protected bool alreadyTriggered = false;
    protected CinemachineCamera virtualCamera;
    protected CinemachineHelper cameraHelper;
    protected PlayerMovement playerMovement;

    private void Awake()
    {
        // S'assurer que le collider est bien configuré en Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered) return;

        // Détecter le joueur
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            }

            if (playerMovement != null)
            {
                if (oneShot)
                {
                    alreadyTriggered = true;
                }

                StartCoroutine(ExecuteCinematicRoutine());
            }
        }
    }

    protected virtual IEnumerator ExecuteCinematicRoutine()
    {
        Debug.Log($"[CinematicTriggerZone] Déclenchement de la cinématique sur '{gameObject.name}'");

        // 1. Récupération des références caméra
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
        }

        if (virtualCamera == null)
        {
            Debug.LogError("[CinematicTriggerZone] Aucune CinemachineCamera trouvée dans la scène !");
            yield break;
        }

        // 2. Geler le joueur et désactiver le CinemachineHelper
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

        // 3. Transition de la caméra vers la cible de focus ou en dezoom sur le joueur
        Transform target = focusTarget != null ? focusTarget : (playerMovement != null ? playerMovement.transform : null);
        
        if (target != null)
        {
            yield return StartCoroutine(TransitionCameraToTarget(target));
        }

        // 4. Temporisation avant le dialogue
        if (delayBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(delayBeforeDialogue);
        }

        // 5. Déclenchement du dialogue
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

        // 6. Temporisation après le dialogue
        if (delayAfterDialogue > 0f)
        {
            yield return new WaitForSeconds(delayAfterDialogue);
        }

        // 7. Transition de retour vers la caméra de jeu
        yield return StartCoroutine(TransitionCameraBack());

        // 8. Réactiver le joueur
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Debug.Log($"[CinematicTriggerZone] Fin de la cinématique sur '{gameObject.name}'");
    }

    protected IEnumerator TransitionCameraToTarget(Transform target)
    {
        var follow = virtualCamera.GetComponent<CinemachineFollow>();
        if (follow == null)
        {
            follow = virtualCamera.GetComponentInChildren<CinemachineFollow>();
        }

        // Utiliser la caméra principale de rendu comme point de départ exact et réel (évite tout décalage d'amorti)
        Camera mainCam = Camera.main;
        Vector3 startPos = mainCam != null ? mainCam.transform.position : virtualCamera.transform.position;
        Quaternion startRot = mainCam != null ? mainCam.transform.rotation : virtualCamera.transform.rotation;
        float startFOV = virtualCamera.Lens.FieldOfView;

        // Calcul des valeurs cibles
        float targetYaw = zoomYaw >= 0f ? zoomYaw : startRot.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(zoomPitch, targetYaw, 0f);
        
        // Tourner le décalage de la caméra selon la rotation Y ciblée
        Vector3 localOffset = new Vector3(0f, zoomHeight, -zoomOutDistance);
        Vector3 targetOffset = Quaternion.Euler(0f, targetYaw, 0f) * localOffset;
        Vector3 targetPos = target.position + targetOffset;

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, transitionInDuration);

        // Détacher la cible pour couper tout calcul automatique pendant la transition et la phase stationnaire
        virtualCamera.Follow = null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = transitionCurve.Evaluate(normalizedTime);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            virtualCamera.transform.position = currentPos;
            virtualCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, zoomFOV, t);

            yield return null;
        }

        // Assurer le placement final exact et rester stationnaire sans suivi Cinemachine
        virtualCamera.transform.position = targetPos;
        virtualCamera.transform.rotation = targetRot;
        virtualCamera.Lens.FieldOfView = zoomFOV;

        // Laisser Follow à null pendant toute la phase stationnaire pour éviter tout sursaut de Cinemachine.
        // La caméra restera parfaitement figée sur place jusqu'au retour.
    }

    protected IEnumerator TransitionCameraBack()
    {
        Vector3 startPos = virtualCamera.transform.position;
        Quaternion startRot = virtualCamera.transform.rotation;
        float startFOV = virtualCamera.Lens.FieldOfView;

        // Calcul des valeurs cibles d'origine
        Vector3 targetOffset = cameraHelper != null ? cameraHelper.OriginalFollowOffset : new Vector3(0f, 4f, -10f);
        Quaternion targetRot = cameraHelper != null ? cameraHelper.OriginalLocalRotation : Quaternion.Euler(20f, 0f, 0f);
        float targetFOV = cameraHelper != null ? cameraHelper.OriginalFOV : 40f;

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, transitionOutDuration);

        // Garder Follow à null pour un déplacement manuel parfaitement lisse au retour
        virtualCamera.Follow = null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = transitionCurve.Evaluate(normalizedTime);

            // Calculer la position cible dynamique du joueur (au cas où il ait bougé très légèrement)
            Vector3 playerPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;
            Vector3 targetPos = playerPos + targetOffset;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            virtualCamera.transform.position = currentPos;
            virtualCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        // Réactiver le helper en mode instantané à l'arrivée (vu que nous sommes déjà parfaitement calés)
        if (cameraHelper != null)
        {
            cameraHelper.enabled = true;
            cameraHelper.UpdateCameraSettings(false); // Pas de smoothTransition car on est déjà placé
        }

        // Réinitialiser le cache d'amorti/historique pour le retour au joueur
        virtualCamera.ForceCameraPosition(virtualCamera.transform.position, virtualCamera.transform.rotation);
    }
}
