using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Composant pour tout objet ramassable et portable par le joueur.
/// Hérite d'Interactable : lorsque le joueur interagit (touche E / bouton d'interaction),
/// il ramasse l'objet au-dessus de sa tête et le joueur passe en animation/sprite "Tennir" (Hold).
/// Une deuxième interaction permet de poser ou lancer l'objet.
/// Masque l'indicateur d'interaction ("?") lors du ramassage et empêche de ramasser d'autres objets simultanément.
/// </summary>
[AddComponentMenu("2.5D RPG/World/Carriable Item")]
public class CarriableItem : Interactable
{
    // Référence globale statique pour l'objet actuellement porté par le joueur
    public static CarriableItem CurrentlyCarriedItem { get; private set; }

    public static bool IsPlayerCarryingAnyItem()
    {
        return CurrentlyCarriedItem != null && CurrentlyCarriedItem.isCarried;
    }

    [Header("Positionnement au-dessus du Joueur")]
    [Tooltip("Décalage de position par rapport aux pieds du joueur lorsqu'il porte l'objet (ex: (0, 1.8, 0))")]
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.8f, 0f);

    [Tooltip("L'objet doit-il suivre le flip du sprite du joueur (gauche/droite) ?")]
    [SerializeField] private bool matchPlayerFacing = true;

    [Header("Animation & Sprite du Joueur")]
    [Tooltip("Nom du paramètre booléen dans l'Animator du joueur (ex: 'isHolding', 'isCarrying', 'Tennir')")]
    [SerializeField] private string animatorBoolParameter = "isHolding";

    [Tooltip("Nom de l'état/animation à jouer dans l'Animator (laisser vide si vous utilisez le paramètre booléen)")]
    [SerializeField] private string animatorStateToPlay = "Tennir";

    [Tooltip("Nom de l'état/animation Idle à rejouer lors de la dépose de l'objet (ex: 'idle' ou 'Idle')")]
    [SerializeField] private string dropIdleStateName = "idle";

    [Tooltip("Sprite optionnel pour remplacer directement le sprite du joueur pendant qu'il porte l'objet (si pas d'Animator)")]
    [SerializeField] private Sprite playerHoldSprite;

    [Header("Lancer / Poser l'Objet")]
    [Tooltip("Autoriser le joueur à poser ou lancer l'objet en ré-appuyant sur la touche d'interaction")]
    [SerializeField] private bool allowDrop = true;

    [Tooltip("Distance devant le joueur où l'objet est posé")]
    [SerializeField] private float dropForwardDistance = 1.0f;

    [Tooltip("Force de lancer vers l'avant (0 pour simplement poser l'objet au sol)")]
    [SerializeField] private float throwForce = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickUpSound;
    [SerializeField] private AudioClip dropSound;

    [Header("Association Xylophone / Note & Couleur")]
    [Tooltip("Index de la lame/note du xylophone associée à cet objet (0 = grave, 7 = aigu)")]
    [SerializeField] private int itemKeyIndex = 0;

    [Tooltip("Couleur associée à cet objet (utilisée pour le feedback visuel sur le xylophone)")]
    [SerializeField] private Color itemColor = Color.white;

    [Tooltip("Clip audio optionnel pour jouer un son de note spécifique à cet objet (si vide, utilise le son de la lame du xylophone)")]
    [SerializeField] private AudioClip itemNoteSound;

    public int ItemKeyIndex => itemKeyIndex;
    public Color ItemColor => itemColor;
    public AudioClip ItemNoteSound => itemNoteSound;

    private float preventDropTime = -1f;

    /// <summary>
    /// Empêche de poser/lancer l'objet pendant cette frame (ex: lorsque le joueur interagit avec un PNJ ou le Tournesol).
    /// </summary>
    public void PreventDropThisFrame()
    {
        preventDropTime = Time.unscaledTime;
    }

    [Header("Réapparition en cas de Chute (Vide / Fosse)")]
    [Tooltip("Point de réapparition (Transform) où l'objet réapparaît en tombant du ciel s'il tombe dans le vide. Si vide, utilise la position initiale de l'objet.")]
    [SerializeField] private Transform customRespawnPoint;

    [Tooltip("Point de réapparition alternatif sous forme de coordonnées 3D (si customRespawnPoint n'est pas assigné).")]
    [SerializeField] private Vector3 customRespawnPosition = Vector3.zero;

    [Tooltip("Distance de chute Y (en mètres/unités) en dessous de l'endroit où l'objet a été posé avant de déclencher la réapparition (défaut : 10).")]
    [SerializeField] private float fallDistanceBelowPlaced = 10f;

    [Tooltip("Altitude Y absolue (ex: -15m) en dessous de laquelle la chute dans le vide est automatiquement détectée.")]
    [SerializeField] private float fallVoidYThreshold = -15f;

    [Tooltip("Hauteur dans le ciel (en mètres/unités) depuis laquelle l'objet réapparaît pour tomber du ciel (défaut : 10 unités).")]
    [SerializeField] private float skyDropHeight = 10f;

    [Tooltip("Vitesse verticale de chute initiale appliquée au départ dans le ciel.")]
    [SerializeField] private float initialFallSpeed = -2f;

    [Tooltip("Effet sonore joué lors de l'impact de l'objet au sol à l'atterrissage.")]
    [SerializeField] private AudioClip skyLandSound;

    [Tooltip("Effet de particules joué lors de l'impact au sol.")]
    [SerializeField] private ParticleSystem skyLandParticles;

    public Transform CustomRespawnPoint
    {
        get => customRespawnPoint;
        set => customRespawnPoint = value;
    }

    public Vector3 CustomRespawnPosition
    {
        get => customRespawnPosition;
        set => customRespawnPosition = value;
    }

    // État interne
    private bool isCarried = false;
    private Transform carryingPlayer;
    private Animator playerAnimator;
    private SpriteRenderer playerSpriteRenderer;
    private Sprite originalPlayerSprite;
    private Rigidbody rb;
    private Collider itemCollider;
    private AudioSource audioSource;
    private float pickUpTime = -1f;
    private float originalMass = 1f;
    private Vector3 initialSpawnPosition;
    private Vector3 lastPlacedPosition;
    private Vector3 itemOriginalScale;
    private bool isRespawningFromSky = false;

    public bool IsCarried => isCarried;

    protected override void Start()
    {
        base.Start();

        // Déparentager le customRespawnPoint au lancement du jeu s'il est un enfant de l'objet
        if (customRespawnPoint != null)
        {
            customRespawnPoint.SetParent(null);
        }

        initialSpawnPosition = transform.position;
        lastPlacedPosition = transform.position;
        itemOriginalScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            originalMass = rb.mass;
        }

        itemCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Empêche l'affichage du prompt d'interaction si cet objet est porté OU si le joueur porte déjà un autre objet.
    /// </summary>
    protected override bool CanInteract()
    {
        if (isCarried) return false;
        if (IsPlayerCarryingAnyItem()) return false;
        return base.CanInteract();
    }

    protected override void Interact()
    {
        if (!isCarried)
        {
            if (IsPlayerCarryingAnyItem()) return;

            // Trouver le joueur à proximité
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null) playerObj = movement.gameObject;
            }

            if (playerObj != null)
            {
                PickUp(playerObj);
            }
        }
        else if (allowDrop && Time.unscaledTime - pickUpTime > 0.2f)
        {
            Drop();
        }
    }

    /// <summary>
    /// Ramasse l'objet et le place au-dessus de la tête du joueur.
    /// </summary>
    public void PickUp(GameObject playerObj)
    {
        if (isCarried || IsPlayerCarryingAnyItem()) return;

        isCarried = true;
        CurrentlyCarriedItem = this;
        carryingPlayer = playerObj.transform;
        pickUpTime = Time.unscaledTime;
        hasInteracted = true;

        // Cacher et détruire l'indicateur d'interaction ("?")
        HideIndicator();
        CleanupIndicator();

        // 1. Désactiver la physique et TOUTES les collisions de l'objet et de ses enfants pendant le transport
        if (rb != null)
        {
            originalMass = rb.mass;
            rb.mass = 1f;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider[] allItemColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allItemColliders)
        {
            if (col != null) col.enabled = false;
        }

        Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>(true);
        foreach (Collider pCol in playerColliders)
        {
            foreach (Collider iCol in allItemColliders)
            {
                if (pCol != null && iCol != null)
                {
                    Physics.IgnoreCollision(pCol, iCol, true);
                }
            }
        }

        // 2. Configurer l'animation et le sprite du Joueur
        playerAnimator = playerObj.GetComponent<Animator>();
        if (playerAnimator == null) playerAnimator = playerObj.GetComponentInChildren<Animator>();

        playerSpriteRenderer = playerObj.GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null) playerSpriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();

        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(animatorBoolParameter))
            {
                playerAnimator.SetBool(animatorBoolParameter, true);
            }

            if (!string.IsNullOrEmpty(animatorStateToPlay))
            {
                playerAnimator.Play(animatorStateToPlay);
            }
        }

        if (playerHoldSprite != null && playerSpriteRenderer != null)
        {
            originalPlayerSprite = playerSpriteRenderer.sprite;
            playerSpriteRenderer.sprite = playerHoldSprite;
        }

        // 3. Son de ramassage
        if (pickUpSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(pickUpSound);
            else AudioSource.PlayClipAtPoint(pickUpSound, transform.position);
        }

        // Repositionnement initial au-dessus du joueur
        UpdateCarryPosition();
    }

    /// <summary>
    /// Pose ou lance l'objet au sol.
    /// </summary>
    public void Drop()
    {
        if (!isCarried) return;

        Transform oldCarrier = carryingPlayer;

        if (CurrentlyCarriedItem == this)
        {
            CurrentlyCarriedItem = null;
        }

        isCarried = false;
        isPlayerInRange = false;
        hasInteracted = false;
        HideIndicator();

        // 1. Restaurer l'animation et le sprite du joueur
        Animator anim = playerAnimator;
        if (anim == null && oldCarrier != null)
        {
            anim = oldCarrier.GetComponent<Animator>();
            if (anim == null) anim = oldCarrier.GetComponentInChildren<Animator>();
        }

        if (anim != null)
        {
            if (!string.IsNullOrEmpty(animatorBoolParameter))
            {
                anim.SetBool(animatorBoolParameter, false);
            }

            bool statePlayed = false;
            if (!string.IsNullOrEmpty(dropIdleStateName))
            {
                int stateHash = Animator.StringToHash(dropIdleStateName);
                if (anim.HasState(0, stateHash))
                {
                    anim.Play(stateHash, 0, 0f);
                    statePlayed = true;
                }
            }

            if (!statePlayed)
            {
                int lowerIdleHash = Animator.StringToHash("idle");
                int upperIdleHash = Animator.StringToHash("Idle");
                if (anim.HasState(0, lowerIdleHash))
                {
                    anim.Play(lowerIdleHash, 0, 0f);
                }
                else if (anim.HasState(0, upperIdleHash))
                {
                    anim.Play(upperIdleHash, 0, 0f);
                }
            }
        }

        if (playerHoldSprite != null && playerSpriteRenderer != null && originalPlayerSprite != null)
        {
            playerSpriteRenderer.sprite = originalPlayerSprite;
        }

        // 2. Positionner l'objet devant le joueur
        if (carryingPlayer != null)
        {
            Vector3 dropDir = carryingPlayer.forward;
            if (playerSpriteRenderer != null && playerSpriteRenderer.flipX)
            {
                dropDir = -carryingPlayer.right;
            }

            Vector3 targetDropPos = carryingPlayer.position + dropDir * dropForwardDistance;
            transform.position = targetDropPos;
        }

        // 3. Réactiver les collisions et la physique
        Collider[] allItemColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allItemColliders)
        {
            if (col != null) col.enabled = true;
        }

        if (oldCarrier != null)
        {
            Collider[] playerColliders = oldCarrier.GetComponentsInChildren<Collider>(true);
            foreach (Collider pCol in playerColliders)
            {
                foreach (Collider iCol in allItemColliders)
                {
                    if (pCol != null && iCol != null)
                    {
                        Physics.IgnoreCollision(pCol, iCol, false);
                    }
                }
            }
        }

        if (rb != null)
        {
            rb.mass = originalMass;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (throwForce > 0f && carryingPlayer != null)
            {
                Vector3 throwDir = carryingPlayer.forward + Vector3.up * 0.5f;
                rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
            }
        }

        // 4. Son de dépose
        if (dropSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(dropSound);
            else AudioSource.PlayClipAtPoint(dropSound, transform.position);
        }

        carryingPlayer = null;
        hasInteracted = false;
        lastPlacedPosition = transform.position;
    }

    protected override void Update()
    {
        if (isCarried)
        {
            HideIndicator();
            UpdateCarryPosition();

            // Écouter l'input pour poser l'objet si allowDrop est activé (et pas d'interaction en cours ni PNJ à proximité)
            if (allowDrop && Time.unscaledTime - pickUpTime > 0.2f && Time.unscaledTime - preventDropTime > 0.1f)
            {
                if (Interactable.IsPlayerNearOtherInteractable())
                {
                    return;
                }

                bool interact = false;
                #if ENABLE_INPUT_SYSTEM
                if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
                {
                    interact = true;
                }
                #else
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton2))
                {
                    interact = true;
                }
                #endif

                if (interact)
                {
                    Drop();
                }
            }
            return;
        }

        // Détection de chute dans le vide : si l'objet chute de 10 unités sous son point de dépose (ou seuil absolu)
        bool hasFallenBelowPlaced = transform.position.y < (lastPlacedPosition.y - fallDistanceBelowPlaced);
        bool hasFallenBelowAbsoluteThreshold = transform.position.y < fallVoidYThreshold;

        if (!isCarried && !isRespawningFromSky && (hasFallenBelowPlaced || hasFallenBelowAbsoluteThreshold))
        {
            TriggerRespawnFromSky();
        }

        base.Update();
    }

    /// <summary>
    /// Déclenche la réapparition de l'objet qui tombe du ciel (style Paper Mario) sur le point choisi.
    /// </summary>
    /// <param name="delay">Délai optionnel (en secondes) avant d'amorcer la chute du ciel.</param>
    public void TriggerRespawnFromSky(float delay = 0f)
    {
        if (isRespawningFromSky) return;
        StartCoroutine(RespawnFromSkyRoutine(delay));
    }

    private Vector3 GetTargetRespawnPosition()
    {
        if (customRespawnPoint != null)
        {
            return customRespawnPoint.position;
        }
        if (customRespawnPosition != Vector3.zero)
        {
            return customRespawnPosition;
        }
        return initialSpawnPosition;
    }

    private IEnumerator RespawnFromSkyRoutine(float delay = 0f)
    {
        isRespawningFromSky = true;

        // Si l'objet était porté au moment de la chute, forcer le lâcher
        if (isCarried)
        {
            Drop();
        }

        isPlayerInRange = false;
        hasInteracted = false;
        HideIndicator();

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Vector3 groundTargetPos = GetTargetRespawnPosition();
        Vector3 skyPosition = groundTargetPos + Vector3.up * skyDropHeight;

        // Figer temporairement la physique et réactiver tous les colliders
        Collider[] allItemColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allItemColliders)
        {
            if (col != null) col.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = skyPosition;
        }

        transform.position = skyPosition;
        transform.localScale = itemOriginalScale;
        Physics.SyncTransforms();

        // Réactiver la physique pour la chute verticale pure depuis le ciel
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            rb.linearVelocity = new Vector3(0f, initialFallSpeed, 0f);
        }

        yield return new WaitForFixedUpdate();

        // Chute : attendre d'atteindre l'altitude du sol cible ou un timeout de sécurité
        float timeout = 0f;
        while (transform.position.y > (groundTargetPos.y + 0.1f) && timeout < 3.0f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        // Atterrissage au sol
        transform.position = groundTargetPos;
        if (rb != null)
        {
            rb.position = groundTargetPos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
            Physics.SyncTransforms();
        }

        // Effets sonores et visuels d'impact à l'atterrissage
        if (skyLandSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(skyLandSound);
            else AudioSource.PlayClipAtPoint(skyLandSound, groundTargetPos);
        }

        if (skyLandParticles != null)
        {
            ParticleSystem ps = Instantiate(skyLandParticles, groundTargetPos, Quaternion.identity);
            Destroy(ps.gameObject, 2.0f);
        }

        // Écrasement (squash) temporaire style Paper Mario à l'impact
        Vector3 squashScale = new Vector3(itemOriginalScale.x * 1.3f, itemOriginalScale.y * 0.4f, itemOriginalScale.z * 1.3f);
        transform.localScale = squashScale;

        float squashDuration = 0.12f;
        float elapsed = 0f;
        while (elapsed < squashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / squashDuration;
            transform.localScale = Vector3.Lerp(squashScale, itemOriginalScale, t);
            yield return null;
        }

        transform.localScale = itemOriginalScale;
        lastPlacedPosition = groundTargetPos;
        isRespawningFromSky = false;
    }

    private void LateUpdate()
    {
        if (isCarried)
        {
            UpdateCarryPosition();
        }
    }

    private void UpdateCarryPosition()
    {
        if (carryingPlayer == null) return;

        Vector3 targetPos = carryingPlayer.position + carryOffset;
        transform.position = targetPos;

        // Ajuster l'ordre de rendu du sprite de l'objet pour qu'il s'affiche devant/au-dessus du joueur
        SpriteRenderer itemSR = GetComponent<SpriteRenderer>();
        if (itemSR != null && playerSpriteRenderer != null)
        {
            itemSR.sortingOrder = playerSpriteRenderer.sortingOrder + 1;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (CurrentlyCarriedItem == this)
        {
            CurrentlyCarriedItem = null;
        }
    }
}
