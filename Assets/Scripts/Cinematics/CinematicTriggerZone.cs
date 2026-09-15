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

    protected Vector3 savedPreCinematicOffset;
    protected Quaternion savedPreCinematicRotation = Quaternion.identity;
    protected float savedPreCinematicFOV = 40f;
    protected bool hasSavedPreCinematicState = false;

    protected void SavePreCinematicState()
    {
        if (virtualCamera != null)
        {
            savedPreCinematicRotation = virtualCamera.transform.rotation;
            savedPreCinematicFOV = virtualCamera.Lens.FieldOfView;
            if (playerMovement != null)
            {
                savedPreCinematicOffset = virtualCamera.transform.position - playerMovement.transform.position;
            }
            else if (cameraHelper != null)
            {
                savedPreCinematicOffset = cameraHelper.OriginalFollowOffset;
            }
            hasSavedPreCinematicState = true;
        }
    }

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

    protected virtual void EnsureReferences()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }
        if (virtualCamera != null && cameraHelper == null)
        {
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
        }
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
    }

    protected virtual IEnumerator ExecuteCinematicRoutine()
    {
        Debug.Log($"[CinematicTriggerZone] Déclenchement de la cinématique sur '{gameObject.name}'");

        // 1. Récupération des références caméra et joueur
        EnsureReferences();

        if (virtualCamera == null)
        {
            Debug.LogError("[CinematicTriggerZone] Aucune CinemachineCamera trouvée dans la scène !");
            yield break;
        }

        SavePreCinematicState();

        if (cameraHelper != null)
        {
            cameraHelper.SaveOriginalSettings();
            cameraHelper.enabled = false;
        }

        // 2. Geler le joueur
        LockPlayer();

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
            yield return StartCoroutine(RunDialogue(dialogueData));
        }

        // 6. Temporisation après le dialogue
        if (delayAfterDialogue > 0f)
        {
            yield return new WaitForSeconds(delayAfterDialogue);
        }

        // 7. Transition de retour vers la caméra de jeu
        yield return StartCoroutine(TransitionCameraBack());

        // 8. Réactiver le joueur
        UnlockPlayer();

        Debug.Log($"[CinematicTriggerZone] Fin de la cinématique sur '{gameObject.name}'");
    }

    protected void LockPlayer()
    {
        PlayerLockManager.SetPlayerLocked(true);
    }

    protected void UnlockPlayer()
    {
        PlayerLockManager.SetPlayerLocked(false);
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            Animator anim = playerMovement.GetComponent<Animator>();
            if (anim == null) anim = playerMovement.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Play("idle");
            }
        }
    }

    protected IEnumerator RunDialogue(DialogueData data)
    {
        if (data == null || DialogueManager.Instance == null) yield break;

        bool dialogueFinished = false;
        DialogueManager.Instance.StartDialogue(data, () =>
        {
            dialogueFinished = true;
            if (playerMovement != null) playerMovement.enabled = false;
        });

        yield return new WaitUntil(() => dialogueFinished);
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
        float targetPitch = zoomPitch >= 0f ? zoomPitch : startRot.eulerAngles.x;
        Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, 0f);
        
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

        Vector3 targetOffset;
        Quaternion targetRot;
        float targetFOV;

        Vector3 playerPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;

        if (cameraHelper != null)
        {
            cameraHelper.GetCalculatedFollowOffsetAndRotation(playerPos, out targetOffset, out targetRot);
            targetFOV = cameraHelper.OriginalFOV;
        }
        else
        {
            targetOffset = (hasSavedPreCinematicState && savedPreCinematicOffset.sqrMagnitude > 0.01f)
                ? savedPreCinematicOffset
                : new Vector3(0f, 4f, -10f);

            targetRot = (hasSavedPreCinematicState && savedPreCinematicRotation != Quaternion.identity)
                ? savedPreCinematicRotation
                : startRot;

            targetFOV = hasSavedPreCinematicState ? savedPreCinematicFOV : 40f;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, transitionOutDuration);

        // Garder Follow à null pour un déplacement manuel parfaitement lisse au retour
        virtualCamera.Follow = null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float t = transitionCurve.Evaluate(normalizedTime);

            Vector3 currentTargetPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;
            Vector3 targetPos = currentTargetPos + targetOffset;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            virtualCamera.transform.position = currentPos;
            virtualCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        Vector3 finalPlayerPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;
        Vector3 finalCameraPos = finalPlayerPos + targetOffset;
        virtualCamera.transform.position = finalCameraPos;
        virtualCamera.transform.rotation = targetRot;
        virtualCamera.Lens.FieldOfView = targetFOV;

        // Réactiver le helper et réaligner la cible Cinemachine sur le joueur sans aucun saut de cadrage
        if (cameraHelper != null)
        {
            cameraHelper.ResetDynamicState();
            cameraHelper.enabled = true;
            cameraHelper.SetTargetPlayer(playerMovement != null ? playerMovement.transform : null);
            cameraHelper.UpdateCameraSettings(false);
        }
        else if (playerMovement != null)
        {
            virtualCamera.Follow = playerMovement.transform;
        }

        // Réinitialiser l'historique de position de Cinemachine pour éviter tout saut de cadre
        virtualCamera.ForceCameraPosition(finalCameraPos, targetRot);

        // Laisser 1 frame à Cinemachine et CinemachineHelper pour caler leur état 100% stable avant le déverrouillage du joueur
        yield return null;
    }
}
