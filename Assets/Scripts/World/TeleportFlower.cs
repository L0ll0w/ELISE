using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gère le comportement interactif de la fleur de téléportation (TpFlower) style Paper Mario / RPG 2.5D :
/// 1. Interaction avec la fleur source (A).
/// 2. Le joueur saute en son centre (saut parabolique). Si un objet est porté, il conserve son animation de portage.
/// 3. La fleur A joue son animation de fermeture (TpClose). L'objet porté est masqué avec le joueur.
/// 4. Fondu au noir (UIFadeManager).
/// 5. Pendant le noir, téléportation du joueur et de l'objet porté au centre de la fleur destination (B).
/// 6. La caméra se positionne sur la fleur B (Cinemachine), puis le fondu disparaît.
/// 7. La fleur B joue son animation d'ouverture (TpOpen). Le joueur et l'objet réapparaissent.
/// 8. Le joueur effectue un petit saut hors de la fleur avec son objet.
/// 9. À l'arrivée, l'animation de portage de l'objet est réappliquée et garantie (animation 'holding').
/// Le trajet retour (B -> A) fonctionne de façon rigoureusement identique.
/// </summary>
[SelectionBase]
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/World/Teleport Flower")]
public class TeleportFlower : Interactable
{
    [Header("Connexion & Téléportation")]
    [Tooltip("La fleur de téléportation de destination reliée à celle-ci.")]
    [SerializeField] private TeleportFlower connectedFlower;

    [Tooltip("Si vrai, lie automatiquement la fleur cible à celle-ci dans les deux sens.")]
    [SerializeField] private bool autoConnectBidirectional = true;

    [Header("Détection d'Interaction & Portée")]
    [Tooltip("Distance maximale dans le monde (en mètres) pour autoriser l'interaction avec cette fleur.")]
    [SerializeField] private float maxInteractionDistance = 2.8f;

    [Header("Points Clés & Positionnement")]
    [Tooltip("Point central où le joueur atterrit dans la fleur. Si non assigné, utilise la position de la fleur + centerOffset.")]
    [SerializeField] private Transform centerPoint;

    [Tooltip("Décalage vertical / local du centre par rapport à la fleur si aucun centerPoint n'est assigné.")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Point de sortie où le joueur atterrit lors du saut hors de la fleur. Si non assigné, calculé en avant de la fleur.")]
    [SerializeField] private Transform exitPoint;

    [Tooltip("Distance devant la fleur pour le point d'atterrissage de sortie (si aucun exitPoint Transform n'est assigné).")]
    [SerializeField] private float exitDistance = 2.0f;

    [Header("Animations de la Fleur")]
    [Tooltip("Animator contrôlant la fleur. Trouvé automatiquement sur ce GameObject ou ses enfants si vide.")]
    [SerializeField] private Animator flowerAnimator;

    [Tooltip("Nom de l'animation de fermeture de la fleur.")]
    [SerializeField] private string closeAnimState = "TpClose";

    [Tooltip("Nom de l'animation d'ouverture de la fleur.")]
    [SerializeField] private string openAnimState = "TpOpen";

    [Tooltip("Nom de l'animation d'attente ouverte de la fleur.")]
    [SerializeField] private string idleOpenAnimState = "IdleOpen";

    [Tooltip("Durée de l'animation de fermeture (en secondes).")]
    [SerializeField] private float closeAnimDuration = 0.67f;

    [Tooltip("Durée de l'animation d'ouverture (en secondes).")]
    [SerializeField] private float openAnimDuration = 0.67f;

    [Header("Paramètres de Saut du Joueur")]
    [Tooltip("Durée du saut d'entrée vers le centre de la fleur (en secondes).")]
    [SerializeField] private float jumpInDuration = 0.45f;

    [Tooltip("Hauteur de l'arche parabolique du saut d'entrée.")]
    [SerializeField] private float jumpInArcHeight = 1.3f;

    [Tooltip("Durée du petit saut de sortie de la fleur (en secondes).")]
    [SerializeField] private float jumpOutDuration = 0.45f;

    [Tooltip("Hauteur de l'arche parabolique du petit saut de sortie.")]
    [SerializeField] private float jumpOutArcHeight = 0.9f;

    [Tooltip("Masque de calque du sol pour caler l'atterrissage du saut de sortie.")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("Paramètres de Fondu et Caméra")]
    [Tooltip("Durée du fondu vers le noir après la fermeture (en secondes).")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Tooltip("Temps d'attente au noir complet pour laisser la caméra et la physique se stabiliser avant le fondu d'ouverture.")]
    [SerializeField] private float blackScreenSettleTime = 0.2f;

    [Tooltip("Durée du fondu d'ouverture vers le jeu après que la caméra est arrivée sur la fleur B (en secondes).")]
    [SerializeField] private float fadeInDuration = 0.4f;

    [Tooltip("Masquer le sprite du joueur et de l'objet porté pendant qu'il est enfermé dans la fleur / pendant le TP au noir.")]
    [SerializeField] private bool hidePlayerWhileEnclosed = true;

    public enum ArrivalCameraMode
    {
        [Tooltip("Ne touche pas à l'orientation actuelle de la caméra.")]
        Unchanged,
        [Tooltip("La caméra se fixe sur 'Arrival Focus Target' (ex : le soleil).")]
        LookAtTarget,
        [Tooltip("La caméra retrouve son orientation d'origine (ex : vers la cascade).")]
        RestoreOriginal
    }

    [Header("Caméra à l'arrivée sur CETTE fleur")]
    [Tooltip("Ce que fait la caméra pendant le fondu noir quand on arrive sur cette fleur.")]
    [SerializeField] private ArrivalCameraMode arrivalCameraMode = ArrivalCameraMode.Unchanged;

    [Tooltip("Cible regardée à l'arrivée (ex : le Soleil). Utilisé si mode = LookAtTarget.")]
    [SerializeField] private Transform arrivalFocusTarget;

    [Tooltip("La caméra lève/baisse la tête pour cadrer la cible (nécessaire pour un soleil en hauteur).")]
    [SerializeField] private bool arrivalLookVertically = true;

    [Tooltip("Inverser la direction du regard (si la caméra regarde à l'opposé).")]
    [SerializeField] private bool arrivalInvertDirection = false;

    [Header("Effets Sonores & Visuels (Optionnels)")]
    [Tooltip("Effet sonore joué lors de la fermeture de la fleur.")]
    [SerializeField] private AudioClip closeSFX;

    [Tooltip("Effet sonore joué lors de l'ouverture de la fleur.")]
    [SerializeField] private AudioClip openSFX;

    [Tooltip("Effet sonore personnalisé pour le saut d'entrée (si vide, utilise le bruitage de saut du joueur).")]
    [SerializeField] private AudioClip customJumpInSFX;

    [Tooltip("Effet sonore personnalisé pour le saut de sortie (si vide, utilise le bruitage de saut du joueur).")]
    [SerializeField] private AudioClip customJumpOutSFX;

    [Tooltip("Particules jouées lors de la fermeture de la fleur.")]
    [SerializeField] private ParticleSystem closeParticles;

    [Tooltip("Particules jouées lors de l'ouverture de la fleur.")]
    [SerializeField] private ParticleSystem openParticles;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // État interne partagé et instance
    private static TeleportFlower activeTeleportFlower = null;
    public static TeleportFlower ActiveTeleportFlower => activeTeleportFlower;
    public static bool IsAnyFlowerTeleporting => activeTeleportFlower != null;
    private static float globalCooldownUntil = 0f;
    private bool isTeleporting = false;

    public TeleportFlower ConnectedFlower
    {
        get => connectedFlower;
        set => connectedFlower = value;
    }

    public bool IsTeleporting => isTeleporting || (activeTeleportFlower != null);

    protected override void Reset()
    {
        base.Reset();

        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null) cap.enabled = false;

        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;

        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        if (maxScale > 0.001f)
        {
            col.radius = maxInteractionDistance / maxScale;
            col.center = new Vector3(0f, 0.8f / maxScale, 0f);
        }
        else
        {
            col.radius = 2.2f;
            col.center = new Vector3(0f, 0.4f, 0f);
        }

        if (flowerAnimator == null)
        {
            flowerAnimator = GetComponent<Animator>();
            if (flowerAnimator == null) flowerAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Awake()
    {
        // Désactiver tout CapsuleCollider parasite pour éviter tout conflit de triggers multiples
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.enabled = false;
        }

        // S'assurer qu'un SphereCollider trigger existe et calibrer précisément son rayon à maxInteractionDistance
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = gameObject.AddComponent<SphereCollider>();
        }

        sphere.isTrigger = true;
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        if (maxScale > 0.001f)
        {
            sphere.radius = maxInteractionDistance / maxScale;
            sphere.center = new Vector3(0f, 0.8f / maxScale, 0f);
        }
        else
        {
            sphere.radius = maxInteractionDistance;
            sphere.center = new Vector3(0f, 0.8f, 0f);
        }

        if (flowerAnimator == null)
        {
            flowerAnimator = GetComponent<Animator>();
            if (flowerAnimator == null)
            {
                flowerAnimator = GetComponentInChildren<Animator>();
            }
        }
    }

    protected override void Start()
    {
        base.Start();

        if (flowerAnimator == null)
        {
            flowerAnimator = GetComponent<Animator>();
            if (flowerAnimator == null)
            {
                flowerAnimator = GetComponentInChildren<Animator>();
            }
        }

        // Toujours s'assurer que la fleur est en IdleOpen au démarrage
        PlayAnimation(idleOpenAnimState);

        // Auto-connexion bidirectionnelle
        ResolveBidirectionalConnection();

        groundLayerMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
    }

    private void ResolveBidirectionalConnection()
    {
        if (connectedFlower != null)
        {
            if (autoConnectBidirectional && connectedFlower.connectedFlower == null)
            {
                connectedFlower.connectedFlower = this;
                Debug.Log($"[TeleportFlower] Auto-liaison bidirectionnelle : {connectedFlower.name} -> {gameObject.name}");
            }
        }
        else if (autoConnectBidirectional)
        {
            // Trouver si une autre fleur dans la scène pointe déjà vers nous
            TeleportFlower[] all = Object.FindObjectsByType<TeleportFlower>(FindObjectsSortMode.None);
            foreach (var other in all)
            {
                if (other != this && other.connectedFlower == this)
                {
                    connectedFlower = other;
                    Debug.Log($"[TeleportFlower] Auto-résolution du retour : {gameObject.name} reliée en retour à {other.name}");
                    break;
                }
            }
        }
    }

    private GameObject GetPlayerObject()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }
        return playerObj;
    }

    protected override void Update()
    {
        // Détection de distance dynamique stricte pour garantir l'indépendance de chaque fleur
        if (!isTeleporting && activeTeleportFlower == null && Time.time >= globalCooldownUntil)
        {
            GameObject playerObj = GetPlayerObject();
            if (playerObj != null)
            {
                float dist = Vector3.Distance(transform.position, playerObj.transform.position);
                if (dist <= maxInteractionDistance && CanInteract())
                {
                    isPlayerInRange = true;
                }
                else
                {
                    isPlayerInRange = false;
                }
            }
            else
            {
                isPlayerInRange = false;
            }
        }
        else
        {
            isPlayerInRange = false;
        }

        base.Update();
    }

    /// <summary>
    /// Vérifie si l'interaction est possible avec CETTE fleur spécifique.
    /// </summary>
    protected override bool CanInteract()
    {
        if (Time.time < globalCooldownUntil) return false;
        if (isTeleporting || activeTeleportFlower != null) return false;
        
        // Sécurité auto-liaison si vide
        if (connectedFlower == null)
        {
            ResolveBidirectionalConnection();
        }

        if (connectedFlower == null) return false;

        GameObject playerObj = GetPlayerObject();
        if (playerObj == null) return false;

        // Contrôle strict de la distance : interdit à une fleur lointaine de capturer l'input
        float dist = Vector3.Distance(transform.position, playerObj.transform.position);
        if (dist > maxInteractionDistance)
        {
            return false;
        }

        return base.CanInteract();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (isTeleporting || activeTeleportFlower != null || Time.time < globalCooldownUntil) return;

        GameObject playerObj = GetPlayerObject();
        if (playerObj != null && (other.gameObject == playerObj || other.transform.IsChildOf(playerObj.transform)))
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist <= maxInteractionDistance)
            {
                base.OnTriggerEnter(other);
            }
        }
    }

    /// <summary>
    /// Déclenchement de l'interaction (le joueur appuie sur la touche d'action).
    /// </summary>
    protected override void Interact()
    {
        if (Time.time < globalCooldownUntil)
        {
            hasInteracted = false;
            return;
        }

        if (isTeleporting || activeTeleportFlower != null)
        {
            hasInteracted = false;
            return;
        }

        if (connectedFlower == null)
        {
            ResolveBidirectionalConnection();
        }

        if (connectedFlower == null)
        {
            Debug.LogWarning($"[TeleportFlower] '{gameObject.name}' n'a aucune fleur connectée pour la téléportation !");
            hasInteracted = false;
            return;
        }

        GameObject playerObj = GetPlayerObject();
        if (playerObj == null)
        {
            hasInteracted = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerObj.transform.position);
        if (dist > maxInteractionDistance)
        {
            // Trop loin : cette fleur refuse l'interaction
            hasInteracted = false;
            isPlayerInRange = false;
            return;
        }

        PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
        if (pm == null)
        {
            pm = Object.FindFirstObjectByType<PlayerMovement>();
        }

        if (pm == null)
        {
            Debug.LogError("[TeleportFlower] PlayerMovement introuvable dans la scène !");
            hasInteracted = false;
            return;
        }

        StartCoroutine(TeleportSequenceRoutine(pm));
    }

    /// <summary>
    /// Séquence complète de téléportation cinématique et fluide (aller comme retour) :
    /// 1. Saut d'entrée dans le centre de CETTE fleur (portage d'objet préservé).
    /// 2. Fermeture (TpClose) de cette fleur.
    /// 3. Fondu au noir (UIFadeManager).
    /// 4. Téléportation du joueur et de l'objet porté au centre de la fleur cible.
    /// 5. Recadrage immédiat de la caméra, puis fondu d'ouverture vers le jeu.
    /// 6. Ouverture (TpOpen) de la fleur cible.
    /// 7. Saut de sortie du joueur vers le sol.
    /// 8. Retour des fleurs en IdleOpen et rétablissement de l'animation de portage de l'objet si porté.
    /// </summary>
    private IEnumerator TeleportSequenceRoutine(PlayerMovement pm)
    {
        activeTeleportFlower = this;
        isTeleporting = true;
        if (connectedFlower != null)
        {
            connectedFlower.isTeleporting = true;
        }

        Debug.Log($"[TeleportFlower] Début de la séquence de téléportation : {gameObject.name} -> {connectedFlower?.name}");

        // 1. Verrouiller les contrôles du joueur et masquer les indicateurs
        PlayerLockManager.SetPlayerLocked(true);
        HideIndicator();
        if (connectedFlower != null) connectedFlower.HideIndicator();

        Transform playerTransform = pm.transform;
        Rigidbody playerRb = pm.GetComponent<Rigidbody>();
        CharacterController playerCC = pm.GetComponent<CharacterController>();
        Collider playerCollider = pm.GetComponent<Collider>();
        SpriteRenderer playerSR = pm.GetComponent<SpriteRenderer>();
        PlayerPitfallRespawn pitfall = pm.GetComponent<PlayerPitfallRespawn>();

        // Vérifier si un objet est actuellement porté par le joueur
        CarriableItem carriedItem = CarriableItem.CurrentlyCarriedItem;
        bool isCarrying = carriedItem != null && carriedItem.IsCarried;

        bool wasKinematic = playerRb != null && playerRb.isKinematic;
        bool wasColEnabled = playerCollider != null && playerCollider.enabled;
        bool wasPitfallEnabled = pitfall != null && pitfall.enabled;

        // Désactiver temporairement PlayerPitfallRespawn pendant le transport pour éviter toute détection de chute relative
        if (pitfall != null)
        {
            pitfall.enabled = false;
        }

        try
        {
            // Préparer la physique du joueur pour le saut scripté
            if (playerCC != null) playerCC.enabled = false;
            if (playerCollider != null) playerCollider.enabled = false;
            if (playerRb != null)
            {
                if (!playerRb.isKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }
                playerRb.isKinematic = true;
            }

            // ETAPE 1 : Le joueur saute en son centre (centre de CETTE fleur)
            Vector3 playerStartPos = playerTransform.position;
            Vector3 flowerCenterThis = GetCenterPosition();

            // Orientation du sprite vers le centre
            if (playerSR != null)
            {
                float dirX = flowerCenterThis.x - playerStartPos.x;
                if (dirX < -0.05f) playerSR.flipX = true;
                else if (dirX > 0.05f) playerSR.flipX = false;
            }

            // Son & animation de saut
            if (customJumpInSFX != null)
            {
                PlaySFX(customJumpInSFX);
            }
            else
            {
                pm.PlayJumpSFX();
            }

            Animator playerAnim = pm.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = pm.GetComponentInChildren<Animator>();

            if (isCarrying && carriedItem != null)
            {
                // Si le joueur porte un objet, conserver l'animation de portage pendant le saut
                carriedItem.ReapplyCarryAnimation();
            }
            else if (playerAnim != null)
            {
                playerAnim.SetTrigger("jump");
                playerAnim.SetBool("isJumping", true);
                playerAnim.SetBool("isWalking", false);
            }

            yield return StartCoroutine(JumpArcRoutine(playerTransform, playerStartPos, flowerCenterThis, jumpInArcHeight, jumpInDuration));

            // ETAPE 2 : La fleur lance son animation de fermeture (TpClose)
            PlayAnimation(closeAnimState);
            PlaySFX(closeSFX);
            PlayParticles(closeParticles);

            if (isCarrying && carriedItem != null)
            {
                carriedItem.ReapplyCarryAnimation();
            }
            else if (playerAnim != null)
            {
                playerAnim.SetBool("isJumping", false);
                playerAnim.Play("idle");
            }

            // Centre visuel du joueur (relatif à son pivot) pour y ranger l'objet porté
            Vector3 itemCenterOffset = (playerSR != null)
                ? playerSR.bounds.center - playerTransform.position
                : Vector3.up * 0.5f;

            // Attente de la durée de fermeture de la fleur
            float closeWait = Mathf.Max(0.05f, closeAnimDuration);

            // Pendant que la fleur se referme, l'objet descend au centre du joueur
            if (isCarrying && carriedItem != null)
            {
                StartCoroutine(BlendCarriedItemRoutine(carriedItem, itemCenterOffset, 0f, 1f, closeWait));
            }

            yield return new WaitForSeconds(closeWait);

            // Masquer le joueur (et l'objet porté) enfermé dans la fleur
            if (hidePlayerWhileEnclosed)
            {
                SetPlayerRenderersVisible(playerTransform, false);
            }

            // ETAPE 3 : Fondu au noir après l'animation
            if (UIFadeManager.Instance != null)
            {
                yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(1f, fadeOutDuration));
            }
            else
            {
                yield return new WaitForSeconds(fadeOutDuration);
            }

            // ETAPE 4 : Pendant le noir, le joueur et son objet sont tp à l'autre fleur connectée
            Vector3 flowerCenterTarget = connectedFlower.GetCenterPosition();
            Vector3 targetExitPos = connectedFlower.GetExitPosition();

            playerTransform.position = flowerCenterTarget;
            if (isCarrying && carriedItem != null)
            {
                carriedItem.ForceUpdatePosition(flowerCenterTarget);
            }
            Physics.SyncTransforms();

            // Mettre à jour immédiatement la position sûre de pitfall à l'altitude d'arrivée
            if (pitfall != null)
            {
                pitfall.SetSafePosition(targetExitPos);
            }

            // Placer la fleur de destination en état fermé immédiatement pour préparer l'ouverture
            connectedFlower.PlayAnimation(closeAnimState);
            if (connectedFlower.flowerAnimator != null)
            {
                connectedFlower.flowerAnimator.Play(closeAnimState, 0, 1f);
            }

            // Téléporter également le groupe de compagnons
            if (GroupManager.Instance != null)
            {
                GroupManager.Instance.TeleportPartyToLeader();
                GroupManager.Instance.ReapplyAllCollisions();
            }

            // ETAPE 5 : La caméra se déplace à la deuxième fleur, puis le fondu disparaît
            yield return StartCoroutine(SettleCameraAtTarget(playerTransform));

            if (UIFadeManager.Instance != null)
            {
                yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(0f, fadeInDuration));
            }
            else
            {
                yield return new WaitForSeconds(fadeInDuration);
            }

            // ETAPE 6 : La deuxième fleur fait son animation de tpOpen
            connectedFlower.PlayAnimation(openAnimState);
            connectedFlower.PlaySFX(connectedFlower.openSFX);
            connectedFlower.PlayParticles(connectedFlower.openParticles);

            // Réapparition du joueur (et de l'objet) à l'intérieur de la fleur qui s'ouvre
            if (hidePlayerWhileEnclosed)
            {
                SetPlayerRenderersVisible(playerTransform, true);
            }

            if (isCarrying && carriedItem != null)
            {
                carriedItem.ReapplyCarryAnimation();
            }

            float openWait = Mathf.Max(0.05f, openAnimDuration);

            // En même temps que la fleur s'ouvre, l'objet remonte au-dessus de la tête
            if (isCarrying && carriedItem != null)
            {
                StartCoroutine(BlendCarriedItemRoutine(carriedItem, itemCenterOffset, 1f, 0f, openWait));
            }

            yield return new WaitForSeconds(openWait);

            // ETAPE 7 : Le joueur fait un petit saut en dehors de la fleur
            Vector3 exitPos = connectedFlower.GetExitPosition();

            if (playerSR != null)
            {
                float exitDirX = exitPos.x - flowerCenterTarget.x;
                if (exitDirX < -0.05f) playerSR.flipX = true;
                else if (exitDirX > 0.05f) playerSR.flipX = false;
            }

            if (connectedFlower.customJumpOutSFX != null)
            {
                PlaySFX(connectedFlower.customJumpOutSFX);
            }
            else
            {
                pm.PlayJumpSFX();
            }

            if (isCarrying && carriedItem != null)
            {
                carriedItem.ReapplyCarryAnimation();
            }
            else if (playerAnim != null)
            {
                playerAnim.SetTrigger("jump");
                playerAnim.SetBool("isJumping", true);
                playerAnim.SetBool("isWalking", false);
            }

            yield return StartCoroutine(JumpArcRoutine(playerTransform, flowerCenterTarget, exitPos, jumpOutArcHeight, jumpOutDuration));

            // ETAPE 8 : La fleur revient après sur son animation de IdleOpen
            connectedFlower.PlayAnimation(idleOpenAnimState);

            // Rétablir la fleur de départ sur IdleOpen également
            PlayAnimation(idleOpenAnimState);

            // Restauration de l'état du joueur : si un objet est porté, rétablir et garantir l'animation de portage !
            if (isCarrying && carriedItem != null && carriedItem.IsCarried)
            {
                carriedItem.ReapplyCarryAnimation();
            }
            else if (playerAnim != null)
            {
                playerAnim.SetBool("isJumping", false);
                playerAnim.Play("idle");
            }

            if (playerCollider != null) playerCollider.enabled = wasColEnabled;
            if (playerRb != null)
            {
                playerRb.isKinematic = wasKinematic;
                if (!playerRb.isKinematic)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }
            }
            if (playerCC != null) playerCC.enabled = true;

            pm.ResetAirborneState();
            Physics.SyncTransforms();

            if (pitfall != null)
            {
                pitfall.SetSafePosition(exitPos);
                pitfall.enabled = wasPitfallEnabled;
            }

            // Rendre les commandes au joueur
            PlayerLockManager.SetPlayerLocked(false);

            // Sécurité supplémentaire : réaffirmer l'animation de portage après le déverrouillage
            if (isCarrying && carriedItem != null && carriedItem.IsCarried)
            {
                carriedItem.ReapplyCarryAnimation();
            }

            Debug.Log($"[TeleportFlower] Téléportation terminée avec succès de {gameObject.name} vers {connectedFlower.name} ! (Portage d'objet conservé : {isCarrying})");
        }
        finally
        {
            // Nettoyage et sécurité garantis même en cas d'interruption
            if (hidePlayerWhileEnclosed && playerTransform != null)
            {
                SetPlayerRenderersVisible(playerTransform, true);
            }
            if (playerCollider != null) playerCollider.enabled = wasColEnabled;
            if (playerCC != null) playerCC.enabled = true;
            if (pitfall != null) pitfall.enabled = wasPitfallEnabled;

            PlayerLockManager.SetPlayerLocked(false);

            if (isCarrying && carriedItem != null && carriedItem.IsCarried)
            {
                carriedItem.SetCarryCenterBlend(Vector3.zero, 0f);
                carriedItem.ReapplyCarryAnimation();
            }

            isTeleporting = false;
            hasInteracted = false;
            isPlayerInRange = false;

            if (connectedFlower != null)
            {
                connectedFlower.isTeleporting = false;
                connectedFlower.hasInteracted = false;
                connectedFlower.isPlayerInRange = false;
            }

            activeTeleportFlower = null;
            globalCooldownUntil = Time.time + 0.6f;
        }
    }

    /// <summary>
    /// Fait glisser l'objet porté entre le dessus de la tête (0) et le centre du joueur (1), en douceur.
    /// </summary>
    private IEnumerator BlendCarriedItemRoutine(CarriableItem item, Vector3 centerOffset, float from, float to, float duration)
    {
        if (item == null) yield break;
        duration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (item == null || !item.IsCarried) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            item.SetCarryCenterBlend(centerOffset, Mathf.Lerp(from, to, t));
            yield return null;
        }
        if (item != null && item.IsCarried) item.SetCarryCenterBlend(centerOffset, to);
    }

    /// <summary>
    /// Anime une trajectoire parabolique fluide pour le joueur et synchronise l'objet porté si présent.
    /// </summary>
    private IEnumerator JumpArcRoutine(Transform targetTransform, Vector3 startPos, Vector3 targetPos, float arcHeight, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.05f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Interpolation horizontale linéaire
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Arche parabolique : 4 * h * t * (1 - t)
            float heightOffset = 4f * arcHeight * t * (1f - t);
            currentPos.y += heightOffset;

            targetTransform.position = currentPos;

            // Synchronisation instantanée de l'objet porté au-dessus du joueur
            if (CarriableItem.CurrentlyCarriedItem != null && CarriableItem.CurrentlyCarriedItem.IsCarried)
            {
                CarriableItem.CurrentlyCarriedItem.ForceUpdatePosition(currentPos);
            }

            yield return null;
        }

        targetTransform.position = targetPos;
        if (CarriableItem.CurrentlyCarriedItem != null && CarriableItem.CurrentlyCarriedItem.IsCarried)
        {
            CarriableItem.CurrentlyCarriedItem.ForceUpdatePosition(targetPos);
        }
        Physics.SyncTransforms();
    }

    public ArrivalCameraMode ArrivalCamMode { get => arrivalCameraMode; set => arrivalCameraMode = value; }
    public Transform ArrivalFocusTarget { get => arrivalFocusTarget; set => arrivalFocusTarget = value; }

    /// <summary>
    /// Positionne immédiatement et stabilise la caméra sur la cible avant de lever le fondu noir.
    /// </summary>
    private IEnumerator SettleCameraAtTarget(Transform playerTransform)
    {
        try
        {
            CinemachineCamera[] vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var vcam in vcams)
            {
                if (vcam == null) continue;
                CinemachineHelper helper = vcam.GetComponent<CinemachineHelper>();
                if (helper == null) helper = vcam.GetComponentInChildren<CinemachineHelper>();

                if (helper != null)
                {
                    connectedFlower?.ApplyArrivalCamera(helper);
                    helper.UnlockCameraFromPlatform();
                    helper.SetTargetPlayer(playerTransform);
                    helper.SnapToTarget(playerTransform.position);
                }
                else
                {
                    vcam.Follow = playerTransform;
                    vcam.ForceCameraPosition(playerTransform.position, vcam.transform.rotation);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[TeleportFlower] SettleCameraAtTarget avertissement : {ex.Message}");
        }

        // Laisser au moins 1 frame et le délai de stabilisation pour que Cinemachine et PhysX soient 100% stables
        yield return null;
        if (blackScreenSettleTime > 0f)
        {
            yield return new WaitForSeconds(blackScreenSettleTime);
        }
    }

    /// <summary>
    /// Détermine le mode effectif de caméra pour l'arrivée sur CETTE fleur (avec détection automatique intelligente).
    /// </summary>
    public ArrivalCameraMode GetEffectiveCameraMode()
    {
        if (arrivalCameraMode != ArrivalCameraMode.Unchanged)
        {
            return arrivalCameraMode;
        }

        // 1. Cible explicitement assignée
        if (arrivalFocusTarget != null)
        {
            return ArrivalCameraMode.LookAtTarget;
        }

        // 2. Si cette fleur est la fleur du soleil (en altitude ou nommée Sun)
        if (IsSunFlower())
        {
            return ArrivalCameraMode.LookAtTarget;
        }

        // 3. Si on revient depuis la fleur du soleil vers le bas (cascade)
        if (connectedFlower != null && connectedFlower.IsSunFlower())
        {
            return ArrivalCameraMode.RestoreOriginal;
        }

        return ArrivalCameraMode.Unchanged;
    }

    public bool IsSunFlower()
    {
        string n = gameObject.name.ToLower();
        if (n.Contains("sun") || n.Contains("soleil")) return true;
        // Dans Nowhere dev 1, la plateforme du soleil est à Y ≈ 171m (les fleurs du bas sont à Y ≈ 121m)
        if (transform.position.y > 150f) return true;
        if (connectedFlower != null && transform.position.y > connectedFlower.transform.position.y + 30f) return true;
        return false;
    }

    public Transform GetEffectiveFocusTarget()
    {
        if (arrivalFocusTarget != null) return arrivalFocusTarget;

        GameObject sun = GameObject.Find("SUN");
        if (sun == null) sun = GameObject.Find("Sun");
        if (sun == null) sun = GameObject.Find("the_sun");
        if (sun == null)
        {
            SunGlow glow = Object.FindFirstObjectByType<SunGlow>();
            if (glow != null) sun = glow.gameObject;
        }
        if (sun == null)
        {
            SunEyeBlinker blinker = Object.FindFirstObjectByType<SunEyeBlinker>();
            if (blinker != null) sun = blinker.gameObject;
        }

        return sun != null ? sun.transform : null;
    }

    /// <summary>
    /// Applique le réglage caméra d'arrivée de CETTE fleur (appelé pendant le fondu noir).
    /// </summary>
    public void ApplyArrivalCamera(CinemachineHelper helper)
    {
        if (helper == null) return;
        ArrivalCameraMode mode = GetEffectiveCameraMode();
        switch (mode)
        {
            case ArrivalCameraMode.LookAtTarget:
                Transform target = GetEffectiveFocusTarget();
                if (target != null)
                {
                    helper.SetFocalOverride(target, arrivalLookVertically, arrivalInvertDirection);
                    Debug.Log($"[TeleportFlower] Caméra fixée sur '{target.name}' à l'arrivée sur {name}.");
                }
                else
                {
                    Debug.LogWarning($"[TeleportFlower] {name} : mode LookAtTarget mais aucune cible soleil trouvée.");
                }
                break;

            case ArrivalCameraMode.RestoreOriginal:
                helper.ClearFocalOverride();
                Debug.Log($"[TeleportFlower] Caméra restaurée à son orientation d'origine à l'arrivée sur {name}.");
                break;
        }
    }

    /// <summary>
    /// Lance un état d'animation sur l'Animator de la fleur.
    /// </summary>
    public void PlayAnimation(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return;

        if (flowerAnimator == null)
        {
            flowerAnimator = GetComponent<Animator>();
            if (flowerAnimator == null) flowerAnimator = GetComponentInChildren<Animator>();
        }

        if (flowerAnimator != null && flowerAnimator.gameObject.activeInHierarchy)
        {
            flowerAnimator.Play(stateName, 0, 0f);
        }
    }

    /// <summary>
    /// Calcule la position centrale mondiale de cette fleur.
    /// </summary>
    public Vector3 GetCenterPosition()
    {
        if (centerPoint != null)
        {
            return centerPoint.position;
        }

        return transform.position + centerOffset;
    }

    /// <summary>
    /// Calcule la position de sortie mondiale après le petit saut hors de la fleur.
    /// </summary>
    public Vector3 GetExitPosition()
    {
        Vector3 exitPos;
        if (exitPoint != null)
        {
            exitPos = exitPoint.position;
        }
        else
        {
            // En avant de la fleur selon son orientation
            Vector3 forwardDir = transform.forward;
            if (forwardDir.sqrMagnitude < 0.01f) forwardDir = Vector3.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();

            exitPos = transform.position + forwardDir * exitDistance;
        }

        // Échantillonnage du sol pour atterrir parfaitement à fleur de sol
        Ray ray = new Ray(exitPos + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 6f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            exitPos.y = hit.point.y + 0.05f;
        }

        return exitPos;
    }

    private void SetPlayerRenderersVisible(Transform playerTransform, bool visible)
    {
        if (playerTransform == null) return;
        SpriteRenderer[] srs = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.enabled = visible;
        }

        // Si le joueur porte un objet, masquer/afficher également l'objet porté
        if (CarriableItem.CurrentlyCarriedItem != null && CarriableItem.CurrentlyCarriedItem.IsCarried)
        {
            SpriteRenderer[] itemSrs = CarriableItem.CurrentlyCarriedItem.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in itemSrs)
            {
                sr.enabled = visible;
            }

            MeshRenderer[] itemMrs = CarriableItem.CurrentlyCarriedItem.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in itemMrs)
            {
                mr.enabled = visible;
            }
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, sfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, sfxVolume);
        }
    }

    private void PlayParticles(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualisation de la zone d'interaction (Blanc transparent)
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);

        // Visualisation du point central (Jaune)
        Vector3 center = GetCenterPosition();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.35f);

        // Visualisation du point de sortie (Vert)
        Vector3 exit = GetExitPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(exit, 0.3f);

        // Flèche de saut de sortie
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, exit);

        // Ligne reliant à la fleur connectée
        if (connectedFlower != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.7f);
            Vector3 otherCenter = connectedFlower.GetCenterPosition();
            Gizmos.DrawLine(center, otherCenter);

            // Petite flèche / sphère vers la fleur connectée
            Gizmos.DrawWireSphere(otherCenter, 0.25f);
        }
    }
}
