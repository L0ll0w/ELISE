using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Aide à la configuration et au contrôle d'une caméra 2.5D ultra-personnalisable avec Cinemachine 3.x.
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

    [Tooltip("Décalage horizontal (X) de cadrage (ex: centrer le joueur légèrement à gauche ou à droite).")]
    [SerializeField] private float offsetX = 0f;

    [Tooltip("Inclinaison verticale de la caméra (X Axis Rotation) en degrés.")]
    [Range(0f, 85f)]
    [SerializeField] private float pitchAngle = 20f;

    [Tooltip("Orientation horizontale de la caméra (Y Axis Rotation) en degrés. (0 = vue normale, 45 = vue isométrique).")]
    [Range(-180f, 180f)]
    [SerializeField] private float yawAngle = 0f;

    [Header("Fluidité & Amortissement du Mouvement")]
    [Tooltip("Vitesse de lissage globale de la caméra pour suivre les mouvements du joueur (plus élevé = plus réactif, plus bas = plus fluide/cinématique).")]
    [SerializeField] private float cameraFollowSpeed = 10f;

    [Tooltip("Vitesse de lissage de la rotation de la caméra.")]
    [SerializeField] private float rotationLerpSpeed = 8f;

    [Tooltip("Activer une légère inclinaison de roulis (Roll / axe Z) lors des virages pour un effet cinématique RPG.")]
    [SerializeField] private bool enableTurnRoll = false;

    [Tooltip("Intensité de l'inclinaison de roulis lors des virages.")]
    [Range(0f, 10f)]
    [SerializeField] private float turnRollAmount = 2.5f;

    [Header("Caméra Adaptative (Look-Ahead)")]
    [Tooltip("Activer le Look-Ahead (anticipation de la caméra dans la direction du mouvement).")]
    [SerializeField] private bool enableLookAhead = true;
    [Tooltip("Inverser la direction de l'anticipation (Look-Ahead).")]
    [SerializeField] private bool invertLookAhead = false;
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

    [Header("Évitement de collision Sol & Décor")]
    [Tooltip("Activer le relèvement automatique de la caméra lorsqu'elle risque d'entrer en collision avec le sol (Terrain Hugging).")]
    [SerializeField] private bool preventTerrainClipping = true;

    [Tooltip("Distance de sécurité minimale (en mètres) entre la caméra et le sol.")]
    [SerializeField] private float cameraTerrainSafetyMargin = 1.5f;

    [Tooltip("Masque de calque pour la détection du sol (Terrain et maillages 3D de décor).")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

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

    [Header("Orientation Dynamique vers Point Focal (ex: Cascade)")]
    [Tooltip("Si activé et qu'un point focal est renseigné, la caméra pivote continuellement pour faire face à ce point tout en suivant le joueur.")]
    [SerializeField] private bool faceFocalPoint = false;
    [Tooltip("Transform du point focal (ex: le centre de la cascade cylindrique).")]
    [SerializeField] private Transform focalPointTarget;
    [Tooltip("Vitesse de lissage de la rotation vers le point focal.")]
    [SerializeField] private float focalPointLerpSpeed = 12f;
    [Tooltip("Inverser la direction du regard à 180°.")]
    [SerializeField] private bool invertFocalDirection = false;

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
    private float currentZoomOutOffset = 0f;
    private float currentZoomOutFOVOffset = 0f;
    private float currentRoll = 0f;

    public Vector3 OriginalFollowOffset
    {
        get
        {
            Vector3 baseOffset = new Vector3(offsetX, height, -distance);
            return Quaternion.Euler(0f, yawAngle, 0f) * baseOffset;
        }
    }

    public Quaternion OriginalLocalRotation => Quaternion.Euler(pitchAngle, yawAngle, 0f);
    public float OriginalFOV => defaultFOV > 0f ? defaultFOV : (originalFOV > 0f ? originalFOV : 40f);

    public float Distance
    {
        get => distance;
        set
        {
            distance = Mathf.Max(0.1f, value);
            SyncOriginalSettingsFromFields();
        }
    }

    public float Height
    {
        get => height;
        set
        {
            height = Mathf.Max(0f, value);
            SyncOriginalSettingsFromFields();
        }
    }

    public float OffsetX
    {
        get => offsetX;
        set
        {
            offsetX = value;
            SyncOriginalSettingsFromFields();
        }
    }

    public float PitchAngle
    {
        get => pitchAngle;
        set
        {
            pitchAngle = Mathf.Clamp(value, 0f, 85f);
            SyncOriginalSettingsFromFields();
        }
    }

    public float YawAngle
    {
        get => yawAngle;
        set
        {
            yawAngle = Mathf.Clamp(value, -180f, 180f);
            SyncOriginalSettingsFromFields();
        }
    }

    public float DefaultFOV
    {
        get => defaultFOV;
        set
        {
            defaultFOV = Mathf.Clamp(value, 5f, 120f);
            originalFOV = defaultFOV;
        }
    }

    public float CameraFollowSpeed
    {
        get => cameraFollowSpeed;
        set => cameraFollowSpeed = Mathf.Max(0.1f, value);
    }

    public float RotationLerpSpeed
    {
        get => rotationLerpSpeed;
        set => rotationLerpSpeed = Mathf.Max(0.1f, value);
    }

    private void SyncOriginalSettingsFromFields()
    {
        Vector3 baseOffset = new Vector3(offsetX, height, -distance);
        originalFollowOffset = Quaternion.Euler(0f, yawAngle, 0f) * baseOffset;
        originalLocalRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        originalFOV = defaultFOV > 0f ? defaultFOV : 40f;
    }

    public void SetFocalPointTarget(Transform focalTarget, bool enable = true)
    {
        focalPointTarget = focalTarget;
        faceFocalPoint = enable && focalTarget != null;
    }

    public void ClearFocalPointTarget()
    {
        focalPointTarget = null;
        faceFocalPoint = false;
    }

    private void OnValidate()
    {
        if (distance < 0.1f) distance = 0.1f;
        if (height < 0f) height = 0f;
        if (defaultFOV < 5f) defaultFOV = 40f;

        SyncOriginalSettingsFromFields();
        UpdateCameraSettings();
    }

    public void SaveOriginalSettings()
    {
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

        if (cinemachineCamera != null && !hasSavedOriginalSettings)
        {
            originalFOV = cinemachineCamera.Lens.FieldOfView;
            if (defaultFOV <= 0f)
            {
                defaultFOV = originalFOV;
            }
            else
            {
                originalFOV = defaultFOV;
            }

            originalLocalRotation = transform.localRotation;
            
            if (followComponent != null)
            {
                Vector3 currentOffset = followComponent.FollowOffset;
                if (currentOffset.sqrMagnitude > 0.01f)
                {
                    if (height == 4f && distance == 10f && (currentOffset.y > 0f || Mathf.Abs(currentOffset.z) > 0f))
                    {
                        height = currentOffset.y;
                        distance = Mathf.Abs(currentOffset.z);
                        offsetX = currentOffset.x;
                    }

                    if (pitchAngle == 20f && originalLocalRotation.eulerAngles.x > 0.1f)
                    {
                        pitchAngle = originalLocalRotation.eulerAngles.x;
                    }

                    if (yawAngle == 0f && originalLocalRotation.eulerAngles.y != 0f)
                    {
                        yawAngle = originalLocalRotation.eulerAngles.y;
                    }
                }
                originalBindingMode = followComponent.TrackerSettings.BindingMode;
            }

            SyncOriginalSettingsFromFields();
            hasSavedOriginalSettings = true;
            Debug.Log($"[CinemachineHelper] Saved original camera settings: Height={height}, Distance={distance}, Pitch={pitchAngle}, Yaw={yawAngle}, FOV={defaultFOV}");
        }
    }

    private void Start()
    {
        SaveOriginalSettings();

        // Création d'une cible virtuelle intermédiaire pour l'amorti et le look-ahead
        GameObject dummyObj = new GameObject("Cinemachine_CameraTarget_Proxy");
        dummyTarget = dummyObj.transform;

        if (targetPlayer != null)
        {
            dummyTarget.SetParent(targetPlayer);
            dummyTarget.localPosition = Vector3.zero;
            lastPlayerPos = targetPlayer.position;
        }

        // S'assurer que le CinemachineBrain utilise la boucle LateUpdate synchronisée avec l'interpolation
        if (Camera.main != null)
        {
            CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            }
        }

        UpdateCameraSettings();
    }

    private void LateUpdate()
    {
        if (targetPlayer == null || dummyTarget == null)
        {
            return;
        }

        if (dummyTarget.parent != targetPlayer)
        {
            dummyTarget.SetParent(targetPlayer);
        }

        Vector3 playerPos = targetPlayer.position;
        float deltaTime = Time.deltaTime;
        
        // 1. Calcul de la vitesse et de la pente
        if (deltaTime > 0f)
        {
            float instantVerticalSpeed = (playerPos.y - lastPlayerPos.y) / deltaTime;
            currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, instantVerticalSpeed, deltaTime * slopeSensitivity);
        }

        // 2. Calcul du Look-Ahead (horizontal en coordonnées Monde)
        Vector3 moveDelta = playerPos - lastPlayerPos;
        moveDelta.y = 0f;

        if (moveDelta.magnitude > 2f)
        {
            moveDelta = Vector3.zero;
        }

        if (enableLookAhead && moveDelta.sqrMagnitude > 0.0001f)
        {
            Vector3 moveDir = moveDelta.normalized;
            if (invertLookAhead) moveDir = -moveDir;
            currentLookAhead = Vector3.Lerp(currentLookAhead, moveDir * lookAheadDistance, deltaTime * lookAheadSpeed);
        }
        else
        {
            currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, deltaTime * lookAheadReturnSpeed);
        }

        // 3. Calcul du Roll dynamique lors des virages (Effet RPG cinématique)
        if (enableTurnRoll && deltaTime > 0f)
        {
            Vector3 moveDir = moveDelta.normalized;
            if (moveDelta.sqrMagnitude > 0.0001f)
            {
                float turnAngle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
                float targetRoll = Mathf.Clamp(-turnAngle * 0.1f * turnRollAmount, -15f, 15f);
                currentRoll = Mathf.Lerp(currentRoll, targetRoll, deltaTime * 4f);
            }
            else
            {
                currentRoll = Mathf.Lerp(currentRoll, 0f, deltaTime * 4f);
            }
        }
        else
        {
            currentRoll = 0f;
        }

        // Calcul du zoom out dynamique quand on se dirige vers la caméra
        float targetZoomOut = 0f;
        float targetZoomOutFOV = 0f;

        if (zoomOutTowardsCamera && deltaTime > 0f)
        {
            Vector3 camForward = transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            float speedTowardsCamera = -Vector3.Dot(moveDelta, camForward) / deltaTime;
            
            if (speedTowardsCamera > 0.1f)
            {
                float t = Mathf.Clamp01(speedTowardsCamera / 5f);
                targetZoomOut = t * maxZoomOutDistance;
                targetZoomOutFOV = t * maxZoomOutFOV;
            }
        }

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

        // Convertir le vecteur d'anticipation Monde en espace local du joueur pour que dummyTarget.position soit exactement : playerPos + currentLookAhead
        if (targetPlayer != null)
        {
            dummyTarget.localPosition = targetPlayer.InverseTransformDirection(currentLookAhead);
        }
        else
        {
            dummyTarget.localPosition = currentLookAhead;
        }

        Vector3 targetFollowPos = dummyTarget.position;

        if (targetFollowPos.y < minAbsoluteCameraY)
        {
            targetFollowPos.y = minAbsoluteCameraY;
            dummyTarget.position = targetFollowPos;
        }

        // 4. Calcul de la perspective de base (avec réglages personnalisés)
        float targetPitch = pitchAngle;
        float targetHeight = height;
        float currentDistance = distance;

        if (adaptToSlope)
        {
            float pitchOffset = 0f;
            float heightOffset = 0f;

            if (currentVerticalSpeed < -0.1f)
            {
                float t = Mathf.Clamp01(-currentVerticalSpeed / 3f);
                pitchOffset = t * maxPitchAdjustment;
                heightOffset = t * maxHeightAdjustment;
            }
            else if (currentVerticalSpeed > 0.1f)
            {
                float t = Mathf.Clamp01(currentVerticalSpeed / 3f);
                pitchOffset = -t * (maxPitchAdjustment * 0.4f);
            }

            targetPitch += pitchOffset;
            targetHeight += heightOffset;
        }

        // 5. Évitement de collision sol (3D Raycast & Terrain)
        if (preventTerrainClipping && followComponent != null)
        {
            Vector3 estimatedCamPos = dummyTarget.position + Quaternion.Euler(0f, yawAngle, 0f) * new Vector3(offsetX, targetHeight, -currentDistance);
            float terrainHeightAtCam = GetHeightAtPosition(estimatedCamPos);
            float minCamWorldY = terrainHeightAtCam + cameraTerrainSafetyMargin;

            if (estimatedCamPos.y < minCamWorldY)
            {
                targetHeight = minCamWorldY - dummyTarget.position.y;
                targetPitch = Mathf.Atan2(targetHeight, currentDistance) * Mathf.Rad2Deg;
            }
        }

        // 6. Orientation Yaw (Focal Point ou Yaw personnalisé)
        float targetYaw = yawAngle;
        if (faceFocalPoint && focalPointTarget != null && dummyTarget != null)
        {
            Vector3 dirToFocal = focalPointTarget.position - dummyTarget.position;
            dirToFocal.y = 0f;

            if (dirToFocal.sqrMagnitude > 0.01f)
            {
                dirToFocal.Normalize();
                if (invertFocalDirection) dirToFocal = -dirToFocal;
                targetYaw = Mathf.Atan2(dirToFocal.x, dirToFocal.z) * Mathf.Rad2Deg;
            }
        }

        // 7. Lissage et application finale avec vitesse de suivi et de rotation personnalisables
        if (followComponent != null)
        {
            float activeFollowSpeed = faceFocalPoint ? focalPointLerpSpeed : cameraFollowSpeed;
            float lerpFactor = 1f - Mathf.Exp(-activeFollowSpeed * deltaTime);
            float rotLerpFactor = 1f - Mathf.Exp(-rotationLerpSpeed * deltaTime);
            
            float activeDistance = currentDistance + currentZoomOutOffset;
            Vector3 baseOffset = new Vector3(offsetX, targetHeight, -activeDistance);
            Vector3 targetOffset = Quaternion.Euler(0f, targetYaw, 0f) * baseOffset;

            followComponent.FollowOffset = Vector3.Lerp(followComponent.FollowOffset, targetOffset, lerpFactor);

            Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, currentRoll);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, rotLerpFactor);

            if (defaultFOV > 0f)
            {
                float targetFOVValue = defaultFOV + currentZoomOutFOVOffset;
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFOVValue, lerpFactor);
            }
        }

        lastPlayerPos = playerPos;
    }

    private float GetHeightAtPosition(Vector3 position)
    {
        float highestGroundY = -9999f;
        bool groundFound = false;

        // 1. Raycast physique 3D pour supporter n'importe quel sol de niveau (MeshCollider, BoxCollider, etc.)
        if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            highestGroundY = hit.point.y;
            groundFound = true;
        }

        // 2. Échantillonnage de Terrain Unity s'il y a un Terrain dans la scène
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null)
        {
            activeTerrain = FindFirstObjectByType<Terrain>();
        }

        if (activeTerrain != null)
        {
            float terrainY = activeTerrain.SampleHeight(position) + activeTerrain.transform.position.y;
            if (terrainY > highestGroundY)
            {
                highestGroundY = terrainY;
                groundFound = true;
            }
        }

        return groundFound ? highestGroundY : 0f;
    }

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

        if (dummyTarget != null)
        {
            cinemachineCamera.Follow = dummyTarget;
        }
        else if (targetPlayer != null)
        {
            cinemachineCamera.Follow = targetPlayer;
        }

        cinemachineCamera.LookAt = null;

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
            
            float targetYaw = yawAngle;
            if (faceFocalPoint && focalPointTarget != null && targetPlayer != null)
            {
                Vector3 dirToFocal = focalPointTarget.position - targetPlayer.position;
                dirToFocal.y = 0f;
                if (dirToFocal.sqrMagnitude > 0.01f)
                {
                    dirToFocal.Normalize();
                    if (invertFocalDirection) dirToFocal = -dirToFocal;
                    targetYaw = Mathf.Atan2(dirToFocal.x, dirToFocal.z) * Mathf.Rad2Deg;
                }
            }

            float currentPitch = pitchAngle;
            float currentDist = distance;
            float currentH = height;

            Vector3 baseOffset = new Vector3(offsetX, currentH, -currentDist);
            Vector3 targetOffset = Quaternion.Euler(0f, targetYaw, 0f) * baseOffset;

            if (smoothTransition)
            {
                Vector3 currentTargetPos = dummyTarget != null ? dummyTarget.position : (targetPlayer != null ? targetPlayer.position : transform.position);
                Vector3 calculatedOffset = transform.position - currentTargetPos;

                followComponent.FollowOffset = (faceFocalPoint && focalPointTarget != null) ? targetOffset : calculatedOffset;
                transform.localRotation = Quaternion.Euler(currentPitch, targetYaw, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(currentPitch, targetYaw, 0f);
                followComponent.FollowOffset = targetOffset;
            }

            if (defaultFOV > 0f && cinemachineCamera != null)
            {
                cinemachineCamera.Lens.FieldOfView = defaultFOV;
            }
        }
        else if (!smoothTransition)
        {
            float targetYaw = (faceFocalPoint && focalPointTarget != null && targetPlayer != null) 
                ? Mathf.Atan2((focalPointTarget.position - targetPlayer.position).x, (focalPointTarget.position - targetPlayer.position).z) * Mathf.Rad2Deg 
                : yawAngle;

            float currentPitch = pitchAngle;
            transform.localRotation = Quaternion.Euler(currentPitch, targetYaw, 0f);
            if (defaultFOV > 0f && cinemachineCamera != null)
            {
                cinemachineCamera.Lens.FieldOfView = defaultFOV;
            }
        }
    }

    #region Context Menu Presets
    [ContextMenu("Preset: Standard 2.5D RPG (Height 4m, Dist 10m, Pitch 20°)")]
    public void SetPresetStandard25D()
    {
        height = 4f;
        distance = 10f;
        offsetX = 0f;
        pitchAngle = 20f;
        yawAngle = 0f;
        defaultFOV = 40f;
        OnValidate();
    }

    [ContextMenu("Preset: Isometric 45° (Height 7m, Dist 12m, Pitch 35°, Yaw 45°)")]
    public void SetPresetIsometric()
    {
        height = 7f;
        distance = 12f;
        offsetX = 0f;
        pitchAngle = 35f;
        yawAngle = 45f;
        defaultFOV = 40f;
        OnValidate();
    }

    [ContextMenu("Preset: Top-Down Tactical (Height 10m, Dist 6m, Pitch 55°)")]
    public void SetPresetTopDown()
    {
        height = 10f;
        distance = 6f;
        offsetX = 0f;
        pitchAngle = 55f;
        yawAngle = 0f;
        defaultFOV = 45f;
        OnValidate();
    }

    [ContextMenu("Preset: Side-Scroller (Height 3m, Dist 10m, Pitch 10°, Yaw 90°)")]
    public void SetPresetSideScroller()
    {
        height = 3f;
        distance = 10f;
        offsetX = 0f;
        pitchAngle = 10f;
        yawAngle = 90f;
        defaultFOV = 35f;
        OnValidate();
    }
    #endregion
}
