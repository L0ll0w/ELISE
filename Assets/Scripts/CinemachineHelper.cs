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

    private void OnValidate()
    {
        UpdateCameraSettings();
    }

    private void Start()
    {
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
        if (targetPlayer == null || dummyTarget == null) return;

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

        if (moveDelta.sqrMagnitude > 0.0001f)
        {
            Vector3 moveDir = moveDelta.normalized;
            currentLookAhead = Vector3.Lerp(currentLookAhead, moveDir * lookAheadDistance, deltaTime * lookAheadSpeed);
        }
        else
        {
            currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, deltaTime * lookAheadReturnSpeed);
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
        float targetPitch = pitchAngle;
        float targetHeight = height;

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
            Vector3 estimatedCamPos = dummyTarget.position + new Vector3(0f, targetHeight, -distance);

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
                targetPitch = Mathf.Atan2(targetHeight, distance) * Mathf.Rad2Deg;
            }
        }

        // 5. Lissage et application finale
        if (followComponent != null)
        {
            float activePitch = Mathf.Lerp(transform.localEulerAngles.x, targetPitch, deltaTime * 5f);
            float activeHeight = Mathf.Lerp(followComponent.FollowOffset.y, targetHeight, deltaTime * 5f);

            transform.localRotation = Quaternion.Euler(activePitch, 0f, 0f);
            followComponent.FollowOffset = new Vector3(0f, activeHeight, -distance);
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

    public void UpdateCameraSettings()
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

        // Rotation fixe par défaut
        transform.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);

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
            followComponent.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            followComponent.FollowOffset = new Vector3(0f, height, -distance);
        }
    }
}
