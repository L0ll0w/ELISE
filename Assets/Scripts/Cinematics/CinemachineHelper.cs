using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Aide à la configuration et au contrôle d'une caméra 2.5D avec Cinemachine 3.x.
/// Ce script s'applique sur le GameObject de la Cinemachine Camera.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[AddComponentMenu("2.5D RPG/Cinemachine 2.5D Helper")]
public class CinemachineHelper : MonoBehaviour
{
    [Header("Cible à suivre")]
    [SerializeField] private Transform targetPlayer;

    [Header("Réglages de la Perspective 2.5D")]
    [Tooltip("Distance de recul (Z) de la caméra par rapport au joueur.")]
    [SerializeField] private float distance = 10f;

    [Tooltip("Hauteur (Y) de la caméra par rapport au joueur.")]
    [SerializeField] private float height = 4f;

    [Tooltip("Inclinaison verticale de la caméra (X Axis Rotation) en degrés.")]
    [Range(0f, 85f)]
    [SerializeField] private float pitchAngle = 20f;

    [Header("Caméra Adaptative (Look-Ahead)")]
    [Tooltip("Distance maximale vers laquelle la caméra s'avance dans la direction du mouvement.")]
    [SerializeField] private float lookAheadDistance = 3f;
    [Tooltip("Vitesse de transition vers la position avancée.")]
    [SerializeField] private float lookAheadSpeed = 2f;
    [Tooltip("Vitesse de retour au centre quand le joueur s'arrête.")]
    [SerializeField] private float lookAheadReturnSpeed = 3f;

    [Header("Adaptation aux Pentes")]
    [Tooltip("Activer l'inclinaison et la hauteur dynamique de la caméra lors de montées/descentes.")]
    [SerializeField] private bool adaptToSlope = true;
    [Tooltip("Sensibilité de détection de la pente (vitesse de réaction de la caméra).")]
    [SerializeField] private float slopeSensitivity = 4f;
    [Tooltip("Ajustement max du Pitch (inclinaison X) vers le bas quand on descend (évite de voir sous le terrain).")]
    [SerializeField] private float maxPitchAdjustment = 15f;
    [Tooltip("Ajustement max de la hauteur (Y offset) vers le haut quand on descend.")]
    [SerializeField] private float maxHeightAdjustment = 3f;
    [Tooltip("Hauteur Y absolue minimale du point de suivi pour éviter de filmer sous la carte.")]
    [SerializeField] private float minAbsoluteCameraY = 1f;

    [Header("Évitement de collision Terrain")]
    [Tooltip("Activer le relèvement automatique de la caméra lorsqu'elle risque d'entrer en collision avec le sol derrière le joueur (Terrain Hugging).")]
    [SerializeField] private bool preventTerrainClipping = true;
    [Tooltip("Distance de sécurité minimale (en mètres) entre la caméra et le sol.")]
    [SerializeField] private float cameraTerrainSafetyMargin = 1.5f;

    private CinemachineCamera cinemachineCamera;
    private CinemachineFollow followComponent;
    private Transform dummyTarget;
    private Vector3 lastPlayerPos;
    private Vector3 currentLookAhead;
    private float currentVerticalSpeed;
    private float defaultFOV;
    private Vector3 originalFollowOffset;
    private Quaternion originalLocalRotation;
    private float originalFOV;
    private BindingMode originalBindingMode;
    private bool hasSavedOriginalSettings = false;

    public Vector3 OriginalFollowOffset => originalFollowOffset;
    public Quaternion OriginalLocalRotation => originalLocalRotation;
    public float OriginalFOV => originalFOV;

    [Header("Zoom Out dynamique vers la Caméra")]
    [Tooltip("Activer le zoom out quand le joueur se dirige vers la caméra.")]
    [SerializeField] private bool zoomOutTowardsCamera = true;
    [Tooltip("Distance de recul supplémentaire maximale.")]
    [SerializeField] private float maxZoomOutDistance = 5f;
    [Tooltip("Augmentation maximale du FOV lors du zoom out.")]
    [SerializeField] private float maxZoomOutFOV = 10f;
    [Tooltip("Sensibilité/Vitesse de réaction du zoom out.")]
    [SerializeField] private float zoomOutSensitivity = 2f;
    [Tooltip("Vitesse de retour au zoom normal.")]
    [SerializeField] private float zoomOutReturnSpeed = 3f;

    private float currentZoomOutOffset = 0f;
    private float currentZoomOutFOVOffset = 0f;

    private void OnValidate()
    {
        UpdateCameraSettings();
    }

    public void SaveOriginalSettings()
    {
        if (hasSavedOriginalSettings) return;

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (followComponent == null)
        {
            followComponent = GetComponent<CinemachineFollow>();
            if (followComponent == null)
            {
                followComponent = GetComponentInChildren<CinemachineFollow>();
            }
        }

        if (cinemachineCamera != null)
        {
            originalFOV = cinemachineCamera.Lens.FieldOfView;
            defaultFOV = originalFOV;
            originalLocalRotation = transform.localRotation;
            
            if (followComponent != null)
            {
                originalFollowOffset = followComponent.FollowOffset;
                originalBindingMode = followComponent.TrackerSettings.BindingMode;
            }
            else
            {
                originalFollowOffset = new Vector3(0f, height, -distance);
                originalBindingMode = BindingMode.WorldSpace;
            }

            hasSavedOriginalSettings = true;
            
            // Lister tous les composants pour identifier le composant de suivi Cinemachine
            var components = GetComponents<Component>();
            string componentNames = "";
            foreach (var c in components)
            {
                if (c != null) componentNames += c.GetType().Name + ", ";
            }
            Debug.Log($"[CinemachineHelper] Camera components: {componentNames}");
            Debug.Log($"[CinemachineHelper] Saved original settings: Offset={originalFollowOffset}, Rot={originalLocalRotation.eulerAngles}, FOV={originalFOV}");
        }
    }

    private void Start()
    {
        SaveOriginalSettings();

        // Création d'une cible virtuelle intermédiaire pour l'amorti et le look-ahead
        GameObject dummyObj = new GameObject("Cinemachine_CameraTarget_Proxy");
        dummyTarget = dummyObj.transform;

        UpdateCameraSettings();

        if (targetPlayer != null)
        {
            lastPlayerPos = targetPlayer.position;
            dummyTarget.position = targetPlayer.position;
        }
    }

    private void LateUpdate()
    {
        if (targetPlayer == null || dummyTarget == null)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[CinemachineHelper] LateUpdate returned early: targetPlayer={targetPlayer}, dummyTarget={dummyTarget}");
            }
            return;
        }

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[CinemachineHelper] LateUpdate running: Player={targetPlayer.name} ({targetPlayer.position}), dummyTarget={dummyTarget.position}, FollowOffset={followComponent?.FollowOffset}, CameraPos={transform.position}, CameraFollowTarget={cinemachineCamera?.Follow?.name}");
        }

        Vector3 playerPos = targetPlayer.position;
        
        // 1. Calcul de la vitesse et de la pente
        float deltaTime = Time.deltaTime;
        if (deltaTime > 0f)
        {
            float instantVerticalSpeed = (playerPos.y - lastPlayerPos.y) / deltaTime;
            currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, instantVerticalSpeed, deltaTime * slopeSensitivity);
        }

        // 2. Calcul du Look-Ahead (horizontal uniquement)
        Vector3 moveDelta = playerPos - lastPlayerPos;
        moveDelta.y = 0f;

        // Éviter les sauts de zoom lors des téléportations ou chargements de scènes
        if (moveDelta.magnitude > 2f)
        {
            moveDelta = Vector3.zero;
        }

        if (moveDelta.sqrMagnitude > 0.0001f)
        {
            Vector3 moveDir = moveDelta.normalized;
            currentLookAhead = Vector3.Lerp(currentLookAhead, moveDir * lookAheadDistance, deltaTime * lookAheadSpeed);
        }
        else
        {
            currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, deltaTime * lookAheadReturnSpeed);
        }

        // Calcul du zoom out dynamique quand on se dirige vers la caméra
        float targetZoomOut = 0f;
        float targetZoomOutFOV = 0f;

        if (zoomOutTowardsCamera && deltaTime > 0f)
        {
            Vector3 camForward = transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            // Vitesse de déplacement vers la caméra (produit scalaire négatif de moveDelta par rapport à la direction de visée de la caméra)
            float speedTowardsCamera = -Vector3.Dot(moveDelta, camForward) / deltaTime;
            
            if (speedTowardsCamera > 0.1f)
            {
                // Vitesse maximale attendue pour la normalisation (ex: vitesse de course ~ 5f)
                float t = Mathf.Clamp01(speedTowardsCamera / 5f);
                targetZoomOut = t * maxZoomOutDistance;
                targetZoomOutFOV = t * maxZoomOutFOV;
            }
        }

        // Lissage de la transition du zoom out (rapide pour s'éloigner, modéré pour revenir)
        if (targetZoomOut > currentZoomOutOffset)
        {
            currentZoomOutOffset = Mathf.Lerp(currentZoomOutOffset, targetZoomOut, deltaTime * zoomOutSensitivity);
            currentZoomOutFOVOffset = Mathf.Lerp(currentZoomOutFOVOffset, targetZoomOutFOV, deltaTime * zoomOutSensitivity);
        }
        else
        {
            currentZoomOutOffset = Mathf.Lerp(currentZoomOutOffset, targetZoomOut, deltaTime * zoomOutReturnSpeed);
            currentZoomOutFOVOffset = Mathf.Lerp(currentZoomOutFOVOffset, targetZoomOutFOV, deltaTime * zoomOutReturnSpeed);
        }

        // Position de suivi de base
        Vector3 targetFollowPos = playerPos + currentLookAhead;

        // Contrainte absolue de hauteur sur la cible pour éviter de filmer sous le sol
        if (targetFollowPos.y < minAbsoluteCameraY)
        {
            targetFollowPos.y = minAbsoluteCameraY;
        }

        // Application au dummyTarget
        dummyTarget.position = targetFollowPos;

        // 3. Calcul de la perspective de base (avec ou sans pente)
        float targetPitch = hasSavedOriginalSettings ? originalLocalRotation.eulerAngles.x : pitchAngle;
        float targetHeight = hasSavedOriginalSettings ? originalFollowOffset.y : height;
        float currentDistance = hasSavedOriginalSettings ? -originalFollowOffset.z : distance;

        if (adaptToSlope)
        {
            float pitchOffset = 0f;
            float heightOffset = 0f;

            // En descente : on regarde plus vers le bas (pitch +) et on monte un peu la caméra (height +)
            if (currentVerticalSpeed < -0.1f)
            {
                float t = Mathf.Clamp01(-currentVerticalSpeed / 3f); // 3f = vitesse de descente max de référence
                pitchOffset = t * maxPitchAdjustment;
                heightOffset = t * maxHeightAdjustment;
            }
            // En montée : on peut légèrement aplatir l'angle
            else if (currentVerticalSpeed > 0.1f)
            {
                float t = Mathf.Clamp01(currentVerticalSpeed / 3f);
                pitchOffset = -t * (maxPitchAdjustment * 0.4f);
            }

            targetPitch += pitchOffset;
            targetHeight += heightOffset;
        }

        // 4. Évitement de la collision avec le terrain (Terrain Hugging / Riding)
        if (preventTerrainClipping && followComponent != null)
        {
            // Position estimée mondiale de la caméra
            Vector3 estimatedCamPos = dummyTarget.position + new Vector3(0f, targetHeight, -currentDistance);

            // Hauteur du sol du terrain sous cette position de caméra
            float terrainHeightAtCam = GetHeightAtPosition(estimatedCamPos);
            
            // Hauteur minimale acceptable pour la caméra dans le monde
            float minCamWorldY = terrainHeightAtCam + cameraTerrainSafetyMargin;

            if (estimatedCamPos.y < minCamWorldY)
            {
                // On augmente targetHeight pour forcer la caméra à rester au-dessus du sol
                targetHeight = minCamWorldY - dummyTarget.position.y;

                // Si la caméra s'élève, on ajuste dynamiquement le pitch pour continuer à centrer le joueur
                // Angle = atan2(Hauteur, Distance)
                targetPitch = Mathf.Atan2(targetHeight, currentDistance) * Mathf.Rad2Deg;
            }
        }

        // 5. Lissage et application finale
        if (followComponent != null)
        {
            float lerpSpeed = deltaTime * 5f;
            
            // Lissage de l'offset complet (X, Y, Z) incluant le zoom out
            float activeDistance = currentDistance + currentZoomOutOffset;
            Vector3 targetOffset = hasSavedOriginalSettings 
                ? new Vector3(originalFollowOffset.x, targetHeight, originalFollowOffset.z - currentZoomOutOffset)
                : new Vector3(0f, targetHeight, -activeDistance);
            followComponent.FollowOffset = Vector3.Lerp(followComponent.FollowOffset, targetOffset, lerpSpeed);

            // Lissage de la rotation complète (Pitch, Yaw, Roll) pour éviter les sauts d'angle
            Quaternion targetRot = hasSavedOriginalSettings
                ? Quaternion.Euler(targetPitch, originalLocalRotation.eulerAngles.y, originalLocalRotation.eulerAngles.z)
                : Quaternion.Euler(targetPitch, 0f, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, lerpSpeed);

            // Lissage progressif du FOV vers sa valeur par défaut + zoom out
            if (defaultFOV > 0f)
            {
                float targetFOVValue = defaultFOV + currentZoomOutFOVOffset;
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFOVValue, lerpSpeed);
            }
        }

        lastPlayerPos = playerPos;
    }

    private float GetHeightAtPosition(Vector3 position)
    {
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null)
        {
            activeTerrain = FindFirstObjectByType<Terrain>();
        }

        if (activeTerrain != null)
        {
            return activeTerrain.SampleHeight(position) + activeTerrain.transform.position.y;
        }
        return 0f;
    }

    /// <summary>
    /// Modifie la cible de la caméra et réinitialise les variables pour éviter les sauts brusques.
    /// </summary>
    public void SetTargetPlayer(Transform newTarget)
    {
        targetPlayer = newTarget;
        if (dummyTarget != null && newTarget != null)
        {
            dummyTarget.position = newTarget.position;
            lastPlayerPos = newTarget.position;
            currentLookAhead = Vector3.zero;
            currentVerticalSpeed = 0f;
        }
        UpdateCameraSettings(true);
        if (newTarget != null)
        {
            Debug.Log($"[CinemachineHelper] SetTargetPlayer called with {newTarget.name} at position {newTarget.position}");
        }
    }

    public void UpdateCameraSettings(bool smoothTransition = false)
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (cinemachineCamera == null) return;

        // Recherche automatique du joueur s'il n'est pas assigné
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerObj = pm.gameObject;
            }

            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
        }

        // Assigne la cible appropriée (dummyTarget ou joueur de base)
        if (dummyTarget != null)
        {
            cinemachineCamera.Follow = dummyTarget;
        }
        else if (targetPlayer != null)
        {
            cinemachineCamera.Follow = targetPlayer;
        }

        cinemachineCamera.LookAt = null;

        // Configuration de la liaison CinemachineFollow
        if (followComponent == null)
        {
            followComponent = GetComponent<CinemachineFollow>();
            if (followComponent == null)
            {
                followComponent = GetComponentInChildren<CinemachineFollow>();
            }
        }

        if (followComponent != null)
        {
            followComponent.TrackerSettings.BindingMode = hasSavedOriginalSettings ? originalBindingMode : BindingMode.WorldSpace;
            
            if (smoothTransition)
            {
                // Calcule l'offset actuel de la caméra par rapport au joueur pour initier le Lerp sans saut
                Vector3 currentTargetPos = dummyTarget != null ? dummyTarget.position : (targetPlayer != null ? targetPlayer.position : transform.position);
                followComponent.FollowOffset = transform.position - currentTargetPos;
            }
            else
            {
                if (hasSavedOriginalSettings)
                {
                    transform.localRotation = originalLocalRotation;
                    followComponent.FollowOffset = originalFollowOffset;
                }
                else
                {
                    transform.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
                    followComponent.FollowOffset = new Vector3(0f, height, -distance);
                }
            }
        }
        else if (!smoothTransition)
        {
            if (hasSavedOriginalSettings)
            {
                transform.localRotation = originalLocalRotation;
            }
            else
            {
                transform.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            }
        }
    }
}
