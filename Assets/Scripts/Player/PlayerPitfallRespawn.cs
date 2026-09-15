using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gère la détection de chute dans le vide et la réapparition du personnage style "Paper Mario: La Porte Millénaire" :
/// 1. Enregistre en continu la dernière plateforme sûre où le joueur a marché (y compris les nénuphars).
/// 2. Pendant la chute dans le vide, la caméra reste figée au niveau de la plateforme.
/// 3. Le joueur réapparaît en tombant du ciel au-dessus de la plateforme pendant que la caméra le voit arriver et s'écraser.
/// 4. À l'impact avec le sol, écrase le personnage en crêpe (squash) et l'étourdit (stun) pendant 1.5 seconde.
/// 5. Restaure l'échelle normale avec rebond élastique, redonne le contrôle et déverrouille le suivi caméra.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("2.5D RPG/Player Pitfall Respawn")]
public class PlayerPitfallRespawn : MonoBehaviour
{
    [Header("Détection du Vide")]
    [Tooltip("Distance de chute (en mètres) sous la dernière plateforme sûre avant de déclencher la réapparition.")]
    [SerializeField] private float maxFallDistanceBelowSafeGround = 5f;

    [Tooltip("Altitude Y absolue de secours en dessous de laquelle la chute dans le vide est déclenchée.")]
    [SerializeField] private float fallThresholdY = -15f;

    [Tooltip("Intervalle (en secondes) d'enregistrement de la position sûre.")]
    [SerializeField] private float safePositionRecordInterval = 0.1f;

    [Header("Réapparition (Chute du Ciel)")]
    [Tooltip("Hauteur au-dessus de la plateforme à laquelle le joueur réapparaît pour tomber du ciel.")]
    [SerializeField] private float skyDropHeight = 8f;

    [Tooltip("Vitesse verticale initiale appliquée lors de la réapparition dans le ciel.")]
    [SerializeField] private float initialFallSpeed = -3f;

    [Tooltip("Distance de recul vers l'intérieur de la plateforme pour éviter de réapparaître trop près du bord (défaut : 1.5m).")]
    [SerializeField] private float edgeInlandOffset = 1.5f;

    [Header("Écrasement & Étourdissement (Paper Mario)")]
    [Tooltip("Durée de l'étourdissement (stun) après l'impact au sol (défaut : 1.5s).")]
    [SerializeField] private float stunDuration = 1.5f;

    [Tooltip("Échelle en Y pendant l'étourdissement au sol (défaut : 0.5).")]
    [SerializeField] private float stunScaleY = 0.5f;

    [Tooltip("Durée de transition pour reprendre l'échelle normale à la fin du stun (en secondes, 0 = instantané).")]
    [SerializeField] private float recoveryTime = 0.08f;

    [Header("Cible Visuelle (Optionnel)")]
    [Tooltip("Transform visuel à écraser. Si vide, utilise le Transform de ce GameObject.")]
    [SerializeField] private Transform visualTransform;

    [Header("Effets Optionnels")]
    [Tooltip("Particules d'impact ou de poussière au moment où le joueur s'écrase au sol.")]
    [SerializeField] private ParticleSystem impactParticles;

    [Tooltip("Son joué lors de l'écrasement au sol.")]
    [SerializeField] private AudioSource impactAudio;

    private PlayerMovement playerMovement;
    private Rigidbody rb;
    private Vector3 lastSafePosition;
    private Vector3 originalScale;
    private bool isRespawning = false;
    private float recordTimer = 0f;
    private CinemachineHelper cinemachineHelper;
    private bool isScaleOverridden = false;
    private Vector3 currentTargetScale;
    private readonly List<Vector3> safePositionHistory = new List<Vector3>();
    private Collider lastSafeCollider;

    public bool IsRespawning => isRespawning;
    public Vector3 LastSafePosition => lastSafePosition;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();

        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        originalScale = visualTransform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }
        lastSafePosition = transform.position;
    }

    private void OnValidate()
    {
        if (stunScaleY <= 0f)
        {
            stunScaleY = 0.5f;
        }
    }

    private void Start()
    {
        cinemachineHelper = FindFirstObjectByType<CinemachineHelper>();

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        // Enregistrer la position initiale comme point de sauvegarde par défaut si grounded
        if (playerMovement != null && playerMovement.CheckGrounded(out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.gameObject != null && !IsLiquidWaterOrHazard(hit.collider.gameObject))
            {
                lastSafePosition = hit.point + Vector3.up * 0.05f;
                lastSafeCollider = hit.collider;
                safePositionHistory.Add(lastSafePosition);
            }
        }
    }

    private bool HasSolidGroundBelow()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.3f;
        float rayDist = maxFallDistanceBelowSafeGround + 1.0f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.isTrigger && hit.normal.y > 0.4f && !IsLiquidWaterOrHazard(hit.collider.gameObject))
            {
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (isRespawning) return;

        // Si le joueur est en train de glisser sur une racine/toboggan, ignorer la détection de chute dans le vide
        if (playerMovement != null && playerMovement.IsSliding)
        {
            if (cinemachineHelper != null && cinemachineHelper.IsCameraLockedToPlatform)
            {
                UnlockCameraFromPlatform();
            }

            // Mettre à jour en continu la position sûre le long de la racine
            lastSafePosition = transform.position + Vector3.up * 0.05f;
            return;
        }

        bool isGrounded = playerMovement.IsGrounded();
        bool hasGroundBelow = HasSolidGroundBelow();

        // 1. Verrouiller la caméra au niveau de la plateforme lors d'une chute dans le vide
        if (!isGrounded && transform.position.y <= (lastSafePosition.y - 0.05f))
        {
            if (!hasGroundBelow)
            {
                LockCameraOnPlatform();
            }
        }
        else if (isGrounded && cinemachineHelper != null && cinemachineHelper.IsCameraLockedToPlatform)
        {
            UnlockCameraFromPlatform();
        }

        // 2. Détection de chute relative : le joueur est tombé sous sa plateforme
        bool fallenBelowSafeGround = transform.position.y < (lastSafePosition.y - maxFallDistanceBelowSafeGround);
        bool fallenBelowThreshold = transform.position.y < fallThresholdY;

        if (fallenBelowSafeGround || fallenBelowThreshold)
        {
            Debug.Log($"[PlayerPitfallRespawn] Chute dans le vide détectée ! Y joueur: {transform.position.y:F1}, Safe Y: {lastSafePosition.y:F1}");
            TriggerPitfallRespawn();
            return;
        }

        // 3. Enregistrement périodique d'une position sûre sur la plateforme
        recordTimer += Time.deltaTime;
        if (recordTimer >= safePositionRecordInterval)
        {
            recordTimer = 0f;
            TryRecordSafeGround();
        }
    }

    private void LateUpdate()
    {
        // Enforce scale in LateUpdate so that the Animator can NEVER overwrite the stun scale
        if (isScaleOverridden && visualTransform != null)
        {
            visualTransform.localScale = currentTargetScale;
        }
    }

    private void SetStunScale(Vector3 scale)
    {
        currentTargetScale = scale;
        isScaleOverridden = true;
        if (visualTransform != null)
        {
            visualTransform.localScale = scale;
        }
    }

    private void ClearStunScale()
    {
        isScaleOverridden = false;
        if (visualTransform != null)
        {
            visualTransform.localScale = originalScale;
        }
    }

    private void LockCameraOnPlatform(Vector3? customPos = null)
    {
        if (cinemachineHelper == null)
        {
            cinemachineHelper = FindFirstObjectByType<CinemachineHelper>();
        }

        if (cinemachineHelper != null)
        {
            cinemachineHelper.LockCameraToPlatform(customPos ?? lastSafePosition);
        }
    }

    private void UnlockCameraFromPlatform()
    {
        if (cinemachineHelper == null)
        {
            cinemachineHelper = FindFirstObjectByType<CinemachineHelper>();
        }

        if (cinemachineHelper != null && cinemachineHelper.IsCameraLockedToPlatform)
        {
            cinemachineHelper.UnlockCameraFromPlatform();
        }
    }

    /// <summary>
    /// Vérifie si l'objet est de l'eau liquide ou un piège (les nénuphars sont exclus et comptent comme des plateformes sûres).
    /// </summary>
    private bool IsLiquidWaterOrHazard(GameObject go)
    {
        if (go == null) return false;

        // Les nénuphars (WaterLilly) sont TOUJOURS des plateformes sûres et marchables
        if (go.GetComponent<WaterLilly>() != null || go.GetComponentInParent<WaterLilly>() != null)
        {
            return false;
        }

        string name = go.name;
        if (name.IndexOf("waterlily", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("lily", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("nenuph", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        // Détection de l'eau liquide (ex: "humide", "water", "eau", "liquid", etc.)
        if (name.IndexOf("humide", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("liquid", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("poison", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("hazard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("water", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("eau", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Enregistre la position actuelle si le sol sous le joueur est plat, stable et n'est pas de l'eau liquide.
    /// </summary>
    private void TryRecordSafeGround()
    {
        if (isRespawning) return;
        if (!playerMovement.IsGrounded()) return;

        if (playerMovement.CheckGrounded(out RaycastHit hit))
        {
            // Vérifier que la pente est marchable (normale orientée vers le haut) et non trigger
            if (hit.normal.y > 0.55f && !hit.collider.isTrigger)
            {
                if (!IsLiquidWaterOrHazard(hit.collider.gameObject))
                {
                    Vector3 groundPos = hit.point + Vector3.up * 0.05f;
                    lastSafePosition = groundPos;
                    lastSafeCollider = hit.collider;

                    if (safePositionHistory.Count == 0 || Vector3.Distance(safePositionHistory[safePositionHistory.Count - 1], groundPos) > 0.2f)
                    {
                        safePositionHistory.Add(groundPos);
                        if (safePositionHistory.Count > 15)
                        {
                            safePositionHistory.RemoveAt(0);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calcule une position sûre reculée vers l'intérieur de la plateforme pour ne pas réapparaître au bord du précipice.
    /// </summary>
    private Vector3 GetSafeInlandRespawnPosition()
    {
        Vector3 basePos = lastSafePosition;

        // 1. Si le sol était un nénuphar (WaterLilly), se diriger directement vers le centre du nénuphar
        if (lastSafeCollider != null)
        {
            if (lastSafeCollider.GetComponent<WaterLilly>() != null || 
                lastSafeCollider.GetComponentInParent<WaterLilly>() != null ||
                lastSafeCollider.name.ToLower().Contains("lily") ||
                lastSafeCollider.name.ToLower().Contains("nenuph"))
            {
                Vector3 center = lastSafeCollider.bounds.center;
                center.y = lastSafePosition.y;
                return Vector3.Lerp(lastSafePosition, center, 0.7f);
            }
        }

        // 2. Si on a un historique de déplacement sur la plateforme, prendre une position de quelques fractions de seconde avant le bord
        if (safePositionHistory.Count >= 4)
        {
            int index = Mathf.Max(0, safePositionHistory.Count - 4);
            basePos = safePositionHistory[index];
        }

        // 3. Calcul de la direction opposée à la chute (recul vers l'intérieur)
        Vector3 fallDir = transform.position - basePos;
        fallDir.y = 0f;

        Vector3 inlandDir = Vector3.zero;
        if (fallDir.sqrMagnitude > 0.01f)
        {
            inlandDir = -fallDir.normalized;
        }
        else if (safePositionHistory.Count >= 2)
        {
            Vector3 moveDir = safePositionHistory[safePositionHistory.Count - 1] - safePositionHistory[0];
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                inlandDir = -moveDir.normalized;
            }
        }

        Vector3 targetInlandPos = basePos + inlandDir * edgeInlandOffset;

        // 4. Vérification par Raycast que la position candidate atterrit bien sur du sol solide marchable
        if (Physics.Raycast(targetInlandPos + Vector3.up * 2f, Vector3.down, out RaycastHit testHit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (testHit.normal.y > 0.55f && !IsLiquidWaterOrHazard(testHit.collider.gameObject))
            {
                return testHit.point + Vector3.up * 0.05f;
            }
        }

        // Si le recul dépasse de l'autre côté, tenter un recul plus court
        Vector3 halfInlandPos = basePos + inlandDir * (edgeInlandOffset * 0.5f);
        if (Physics.Raycast(halfInlandPos + Vector3.up * 2f, Vector3.down, out RaycastHit halfHit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (halfHit.normal.y > 0.55f && !IsLiquidWaterOrHazard(halfHit.collider.gameObject))
            {
                return halfHit.point + Vector3.up * 0.05f;
            }
        }

        return basePos;
    }

    /// <summary>
    /// Détection si le joueur touche un collider d'eau liquide ou de vide.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (isRespawning) return;

        if (IsLiquidWaterOrHazard(collision.gameObject))
        {
            Debug.Log($"[PlayerPitfallRespawn] Collision avec eau/vide : {collision.gameObject.name}");
            TriggerPitfallRespawn();
        }
    }

    /// <summary>
    /// Détection si le joueur entre dans un trigger de vide ou d'eau.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isRespawning) return;

        if (other.GetComponent<PitfallZone>() != null)
        {
            Debug.Log($"[PlayerPitfallRespawn] Entrée dans PitfallZone : {other.gameObject.name}");
            TriggerPitfallRespawn();
            return;
        }

        if (IsLiquidWaterOrHazard(other.gameObject))
        {
            Debug.Log($"[PlayerPitfallRespawn] Trigger eau/vide : {other.gameObject.name}");
            TriggerPitfallRespawn();
        }
    }

    /// <summary>
    /// Déclenche la séquence de réapparition chute du ciel + écrasement style Paper Mario.
    /// </summary>
    public void TriggerPitfallRespawn()
    {
        if (isRespawning) return;
        StartCoroutine(PaperMarioRespawnRoutine());
    }

    private IEnumerator PaperMarioRespawnRoutine()
    {
        isRespawning = true;

        // 1. Calcul de la position sûre reculée à l'intérieur de la plateforme
        Vector3 respawnGroundPos = GetSafeInlandRespawnPosition();
        lastSafePosition = respawnGroundPos;

        // 2. Verrouiller immédiatement les commandes et figer la caméra sur la plateforme
        playerMovement.IsInputLocked = true;
        LockCameraOnPlatform(respawnGroundPos);

        // Figer complètement la vélocité physique avant téléportation
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 3. Calcul de la position dans le ciel directement au-dessus de la plateforme reculée
        Vector3 skyPosition = respawnGroundPos + Vector3.up * skyDropHeight;

        Debug.Log($"[PlayerPitfallRespawn] Téléportation vers le ciel : {skyPosition:F2} (Plateforme reculée du bord: {respawnGroundPos:F2})");

        // Téléportation physique réelle et synchronisée
        rb.position = skyPosition;
        transform.position = skyPosition;
        visualTransform.localScale = originalScale;
        Physics.SyncTransforms();

        // Réactiver la physique avec chute verticale pure (bloquer X et Z pour atterrir pile sur la plateforme)
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        rb.linearVelocity = new Vector3(0f, initialFallSpeed, 0f);

        // Laisser une frame fixe pour que PhysX valide la nouvelle position
        yield return new WaitForFixedUpdate();

        // 4. Chute du ciel : attendre l'impact avec le sol (maximum 2.5 secondes)
        float fallTimeout = 0f;
        while (!playerMovement.IsGrounded() && fallTimeout < 2.5f)
        {
            fallTimeout += Time.deltaTime;
            yield return null;
        }

        // Sécurité : si après 2.5s le joueur n'a pas touché le sol, le positionner directement sur la plateforme
        if (!playerMovement.IsGrounded())
        {
            rb.isKinematic = true;
            rb.position = respawnGroundPos;
            transform.position = respawnGroundPos;
            Physics.SyncTransforms();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
        }

        // 4. Impact au sol : échelle de 0.5 en Y au moment du contact
        Debug.Log($"[PlayerPitfallRespawn] Impact au sol ! Scale Y = {stunScaleY} pendant toute la durée du stun ({stunDuration}s).");
        rb.linearVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        PlayImpactEffects();

        Vector3 stunScale = new Vector3(originalScale.x, originalScale.y * stunScaleY, originalScale.z);
        SetStunScale(stunScale);

        // 5. Maintien de l'échelle de 0.5 en Y pendant TOUTE la durée du stun (1.5 seconde)
        float stunElapsed = 0f;
        while (stunElapsed < stunDuration)
        {
            stunElapsed += Time.deltaTime;
            SetStunScale(stunScale);
            yield return null;
        }

        // 6. Reprise de l'échelle normale
        if (recoveryTime > 0f)
        {
            float elapsedRecovery = 0f;
            while (elapsedRecovery < recoveryTime)
            {
                elapsedRecovery += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedRecovery / recoveryTime);
                SetStunScale(Vector3.Lerp(stunScale, originalScale, t));
                yield return null;
            }
        }
        ClearStunScale();

        // 7. Rétablir les compagnons de groupe près du joueur
        if (GroupManager.Instance != null)
        {
            GroupManager.Instance.TeleportPartyToLeader();
            GroupManager.Instance.ReapplyAllCollisions();
        }

        // 8. Rétablir les contraintes standard, déverrouiller la caméra et rendre les commandes
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        playerMovement.IsInputLocked = false;
        isRespawning = false;
        UnlockCameraFromPlatform();
        Debug.Log("[PlayerPitfallRespawn] Récupération terminée. Caméra déverrouillée et contrôles rendus au joueur.");
    }

    private void PlayImpactEffects()
    {
        if (impactParticles != null)
        {
            impactParticles.transform.position = transform.position;
            impactParticles.Play();
        }

        if (impactAudio != null)
        {
            impactAudio.Play();
        }
    }

    private void OnDisable()
    {
        if (isRespawning)
        {
            StopAllCoroutines();
            ClearStunScale();
            if (playerMovement != null)
            {
                playerMovement.IsInputLocked = false;
            }
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            isRespawning = false;
        }
        else
        {
            ClearStunScale();
        }
        UnlockCameraFromPlatform();
    }

    private void OnDrawGizmosSelected()
    {
        // Visualiser le seuil de vide et la dernière position sûre dans l'éditeur
        Gizmos.color = Color.red;
        Vector3 pos = transform.position;
        pos.y = fallThresholdY;
        Gizmos.DrawWireCube(pos, new Vector3(30f, 0.1f, 30f));

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastSafePosition, 0.4f);
            Gizmos.DrawLine(lastSafePosition, lastSafePosition + Vector3.up * skyDropHeight);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastSafePosition + Vector3.up * skyDropHeight, 0.3f);
        }
    }
}
