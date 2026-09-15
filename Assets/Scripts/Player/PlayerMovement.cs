using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère les déplacements en 3D d'un personnage de RPG 2.5D avec retournement automatique du sprite
/// en utilisant le nouveau système d'input de Unity (Input System).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("2.5D RPG/Player Movement")]
public class PlayerMovement : MonoBehaviour
{
    [Header("Paramètres de Déplacement")]
    [Tooltip("Vitesse de déplacement du personnage.")]
    [SerializeField] private float speed = 5f;

    [Header("Paramètres de Saut")]
    [Tooltip("Force verticale du saut.")]
    [SerializeField] private float jumpForce = 6f;
    [Tooltip("Temps de tolérance après avoir quitté une plateforme pour pouvoir sauter (Coyote Time).")]
    [SerializeField] private float coyoteTime = 0.15f;
    [Tooltip("Temps pendant lequel l'appui sur saut est mémorisé avant de toucher le sol (Jump Buffer).")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [Tooltip("Multiplicateur de gravité appliqué lors de la descente (rend le saut moins flottant).")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [Tooltip("Multiplicateur de gravité appliqué lorsque le bouton de saut est relâché tôt.")]
    [SerializeField] private float lowJumpMultiplier = 2f;
    [Tooltip("Progression de l'animation (0 à 1) à laquelle la pose aérienne se fige tant qu'on est en l'air (ex: 0.58f pour la frame 5).")]
    [SerializeField] private float maxAirJumpNormalizedTime = 0.58f;

    [Header("Orientation par rapport à la Caméra")]
    [Tooltip("Si coché, les directions Z/X s'alignent avec l'orientation de la caméra principale.")]
    [SerializeField] private bool moveRelativeToCamera = false;

    [Header("Effets Sonores (SFX)")]
    [Tooltip("Effet sonore (AudioClip) joué lorsque le joueur saute.")]
    [SerializeField] private AudioClip jumpSFX;
    [Tooltip("Volume de lecture du bruitage de saut (0 à 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpSFXVolume = 1f;
    [Tooltip("AudioSource optionnel pour lire le bruitage. Si vide, utilisera l'AudioSource du joueur ou l'AudioManager.")]
    [SerializeField] private AudioSource audioSource;

    /// <summary>
    /// Effet sonore de saut du joueur.
    /// </summary>
    public AudioClip JumpSFX
    {
        get => jumpSFX;
        set => jumpSFX = value;
    }

    /// <summary>
    /// Volume du bruitage de saut.
    /// </summary>
    public float JumpSFXVolume
    {
        get => jumpSFXVolume;
        set => jumpSFXVolume = Mathf.Clamp01(value);
    }

    public bool MoveRelativeToCamera
    {
        get => moveRelativeToCamera;
        set => moveRelativeToCamera = value;
    }

    /// <summary>
    /// Force de saut par défaut du joueur.
    /// </summary>
    public float DefaultJumpForce => jumpForce;

    /// <summary>
    /// Force de saut actuelle.
    /// </summary>
    public float CurrentJumpForce => currentJumpForce;

    /// <summary>
    /// Indique si le joueur est actuellement propulsé par un rebond.
    /// </summary>
    public bool IsBouncing => isBouncing;

    /// <summary>
    /// Indique si le joueur est actuellement en train de glisser sur une racine / toboggan.
    /// </summary>
    public bool IsSliding
    {
        get => isSlidingOnRoot;
        set => isSlidingOnRoot = value;
    }
    private bool isSlidingOnRoot = false;

    /// <summary>
    /// Verrouille temporairement les déplacements et sauts du joueur tout en laissant la physique active (ex: réapparition, stun).
    /// </summary>
    public bool IsInputLocked
    {
        get => isInputLocked;
        set
        {
            isInputLocked = value;
            if (isInputLocked)
            {
                shouldJump = false;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                moveDirection = Vector3.zero;
                if (animator != null && hasIsWalkingParam)
                {
                    animator.SetBool(isWalkingHash, false);
                }
            }
        }
    }

    /// <summary>
    /// Événement déclenché lorsque le joueur saute.
    /// </summary>
    public event System.Action OnJump;

    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;
    private Collider playerCollider;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Camera mainCamera;
    private Vector3 moveDirection;
    private bool shouldJump = false;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private Animator animator;
    private float currentJumpForce;
    private bool isBouncing = false;
    private bool isInputLocked = false;

    private int isWalkingHash;
    private int isJumpingHash;
    private int jumpTriggerHash;
    private int jumpStateHash;
    private bool hasIsWalkingParam;
    private bool hasIsJumpingParam;
    private bool hasJumpTriggerParam;
    private bool isJumpAnimHolding = false;

    [Header("Détection du Sol")]
    [Tooltip("Masque de collision pour le sol.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    private static readonly RaycastHit[] groundHitBuffer = new RaycastHit[16];
    private int lastGroundedCheckFrame = -1;
    private bool cachedIsGrounded = false;
    private RaycastHit cachedGroundHit;
    private float lastJumpTime = -10f;
    private const float JUMP_COOLDOWN = 0.2f;

    private void Start()
    {
        // Récupération automatique des composants sur le GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            isWalkingHash = Animator.StringToHash("isWalking");
            isJumpingHash = Animator.StringToHash("isJumping");
            jumpTriggerHash = Animator.StringToHash("jump");
            jumpStateHash = Animator.StringToHash("jump");

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == isWalkingHash) hasIsWalkingParam = true;
                else if (p.nameHash == isJumpingHash) hasIsJumpingParam = true;
                else if (p.nameHash == jumpTriggerHash) hasJumpTriggerParam = true;
            }
        }
        currentJumpForce = jumpForce;
        
        // Récupération du PlayerInput et recherche des actions
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions.FindAction("Move");
            jumpAction = playerInput.actions.FindAction("Jump");
        }
        else
        {
            Debug.LogWarning("Le composant [PlayerInput] est manquant sur ce GameObject. Veuillez l'ajouter pour gérer les contrôles.", this);
        }

        if (moveRelativeToCamera)
        {
            mainCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        groundLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
    }

    /// <summary>
    /// Joue l'effet sonore de saut du joueur via l'AudioManager, l'AudioSource local ou en 3D dans le monde.
    /// </summary>
    public void PlayJumpSFX()
    {
        if (jumpSFX == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(jumpSFX, jumpSFXVolume);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(jumpSFX, jumpSFXVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(jumpSFX, transform.position, jumpSFXVolume);
        }
    }

    private void Update()
    {
        // En combat, PlayerMovement doit être désactivé pour laisser le contrôleur de combat gérer le joueur
        if ((RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive) ||
            (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive))
        {
            enabled = false;
            return;
        }

        // Si les inputs sont verrouillés ou qu'un dialogue est actif, ignorer les mouvements et commandes
        if (isInputLocked || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            shouldJump = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            moveDirection = Vector3.zero;

            if (animator != null)
            {
                bool grounded = IsGrounded() || isSlidingOnRoot;
                bool isJumping = (!grounded || shouldJump || isBouncing) && !isSlidingOnRoot;

                if (hasIsWalkingParam) animator.SetBool(isWalkingHash, false);
                if (hasIsJumpingParam) animator.SetBool(isJumpingHash, isJumping);

                // Maintenir la gestion d'animation en l'air même quand les inputs sont verrouillés
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool isInJumpState = stateInfo.shortNameHash == jumpStateHash;

                if (isInJumpState)
                {
                    if (!grounded)
                    {
                        if (stateInfo.normalizedTime >= maxAirJumpNormalizedTime)
                        {
                            if (!isJumpAnimHolding)
                            {
                                animator.Play(jumpStateHash, 0, maxAirJumpNormalizedTime);
                                animator.speed = 0f;
                                isJumpAnimHolding = true;
                            }
                        }
                    }
                    else
                    {
                        if (isJumpAnimHolding)
                        {
                            animator.speed = 1f;
                            isJumpAnimHolding = false;
                        }
                    }
                }
                else
                {
                    if (isJumpAnimHolding || animator.speed == 0f)
                    {
                        animator.speed = 1f;
                        isJumpAnimHolding = false;
                    }
                }
            }
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        // Lecture de l'input Vector2 (depuis le clavier, manette, etc.)
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            horizontal = moveInput.x;
            vertical = moveInput.y;
        }

        // Calcul du vecteur de déplacement dans l'espace 3D
        moveDirection = Vector3.zero;

        if (moveRelativeToCamera && mainCamera != null)
        {
            // Mouvement basé sur l'orientation de la caméra projetée au sol (plan XZ)
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camRight * horizontal + camForward * vertical);
        }
        else
        {
            // Mouvement classique : Horizontal = X (droite/gauche), Vertical = Z (avant/arrière)
            moveDirection = new Vector3(horizontal, 0f, vertical);
        }

        // Normalisation pour éviter d'aller plus vite en diagonale
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Application du déplacement si pas de Rigidbody (fallback)
        if (rb == null)
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }

        // Gestion du Coyote Time (ne pas recharger si le joueur vient de sauter et est en ascension)
        bool isAscendingFromJump = (Time.time - lastJumpTime) < JUMP_COOLDOWN || shouldJump;
        if (IsGrounded() && !isAscendingFromJump)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Gestion du Jump Buffer
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Détecter la demande de saut avec Coyote Time et Jump Buffer (uniquement si pas déjà en pleine ascension de saut)
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isAscendingFromJump)
        {
            shouldJump = true;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f; // Éviter le saut multiple en l'air
            lastJumpTime = Time.time;
            OnJump?.Invoke();
            PlayJumpSFX();

            if (animator != null)
            {
                isJumpAnimHolding = false;
                animator.speed = 1f;
                if (hasJumpTriggerParam)
                {
                    animator.SetTrigger(jumpTriggerHash);
                }
            }
        }

        // Flip automatique du SpriteRenderer
        if (horizontal < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontal > 0.01f)
        {
            spriteRenderer.flipX = false;
        }

        // Mise à jour de l'Animator
        if (animator != null)
        {
            bool grounded = IsGrounded() || isSlidingOnRoot;
            bool isJumping = (!grounded || shouldJump || isBouncing) && !isSlidingOnRoot;
            bool isWalking = moveDirection.magnitude > 0.01f && grounded && !shouldJump && !isSlidingOnRoot;

            if (hasIsWalkingParam) animator.SetBool(isWalkingHash, isWalking);
            if (hasIsJumpingParam) animator.SetBool(isJumpingHash, isJumping);

            // Gestion du blocage à la frame 5 tant que le joueur est en l'air
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInJumpState = stateInfo.shortNameHash == jumpStateHash;

            if (isInJumpState)
            {
                if (!grounded)
                {
                    // En l'air : bloqué à la frame 5 (maxAirJumpNormalizedTime)
                    if (stateInfo.normalizedTime >= maxAirJumpNormalizedTime)
                    {
                        if (!isJumpAnimHolding)
                        {
                            animator.Play(jumpStateHash, 0, maxAirJumpNormalizedTime);
                            animator.speed = 0f;
                            isJumpAnimHolding = true;
                        }
                    }
                }
                else
                {
                    // Au sol : si l'animation était bloquée en l'air, on la libère pour jouer l'atterrissage
                    if (isJumpAnimHolding)
                    {
                        animator.speed = 1f;
                        isJumpAnimHolding = false;
                    }
                }
            }
            else
            {
                if (isJumpAnimHolding || animator.speed == 0f)
                {
                    animator.speed = 1f;
                    isJumpAnimHolding = false;
                }
            }
        }
    }

    /// <summary>
    /// Vérifie si le joueur touche le sol et renvoie les détails du contact.
    /// Utilise une mise en cache intra-frame et RaycastNonAlloc pour un coût zéro GC.
    /// </summary>
    public bool CheckGrounded(out RaycastHit hit)
    {
        if (Time.frameCount == lastGroundedCheckFrame)
        {
            hit = cachedGroundHit;
            return cachedIsGrounded;
        }

        hit = default;
        lastGroundedCheckFrame = Time.frameCount;

        // Si le joueur vient d'initier un saut ou est en pleine ascension, il n'est pas au sol
        if ((Time.time - lastJumpTime) < JUMP_COOLDOWN || shouldJump)
        {
            cachedIsGrounded = false;
            cachedGroundHit = default;
            return false;
        }

        if (playerCollider == null)
        {
            cachedIsGrounded = true;
            cachedGroundHit = default;
            return true;
        }

        Vector3 origin = playerCollider.bounds.center;
        float halfHeight = playerCollider.bounds.extents.y;
        float castDistance = halfHeight + 0.12f;

        Ray ray = new Ray(origin, Vector3.down);
        int hitCount = Physics.RaycastNonAlloc(ray, groundHitBuffer, castDistance, groundLayers, QueryTriggerInteraction.Ignore);

        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = groundHitBuffer[i];

            // Ignorer le collider du joueur lui-même
            if (h.collider == playerCollider)
            {
                continue;
            }

            // Ignorer les triggers
            if (h.collider.isTrigger)
            {
                continue;
            }

            if (h.distance < closestDist)
            {
                closestDist = h.distance;
                hit = h;
                found = true;
            }
        }

        cachedIsGrounded = found;
        cachedGroundHit = hit;
        return found;
    }

    /// <summary>
    /// Vérifie si le joueur touche le sol.
    /// </summary>
    public bool IsGrounded()
    {
        return CheckGrounded(out _);
    }

    private void FixedUpdate()
    {
        if ((RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive) ||
            (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive))
        {
            enabled = false;
            return;
        }

        // Application du déplacement via le Rigidbody si disponible
        if (rb != null && !rb.isKinematic)
        {
            float targetYVelocity = rb.linearVelocity.y;

            // Appliquer la vitesse de saut si demandée
            if (shouldJump)
            {
                targetYVelocity = currentJumpForce;
                shouldJump = false;
                currentJumpForce = jumpForce;
            }
            else
            {
                // Appliquer les modificateurs de gravité personnalisés
                if (targetYVelocity < 0f)
                {
                    isBouncing = false;
                    targetYVelocity += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
                }
                else if (targetYVelocity > 0f && !isBouncing && (jumpAction == null || !jumpAction.IsPressed()))
                {
                    targetYVelocity += Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
                }
            }

            bool isMoving = moveDirection.sqrMagnitude >= 0.0001f;
            bool preserveInertia = isInputLocked && (new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).sqrMagnitude > 0.01f);

            RigidbodyConstraints desiredConstraints = (isMoving || preserveInertia)
                ? RigidbodyConstraints.FreezeRotation
                : (RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);

            if (rb.constraints != desiredConstraints)
            {
                rb.constraints = desiredConstraints;
            }

            if (!isMoving)
            {
                if (!preserveInertia)
                {
                    // On applique la vitesse verticale tout en bloquant la vitesse horizontale
                    rb.linearVelocity = new Vector3(0f, targetYVelocity, 0f);
                }
                else
                {
                    // Conserver l'inertie horizontale (ex: impulsion d'éjection en fin de glissade)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, targetYVelocity, rb.linearVelocity.z);
                }
            }
            else
            {
                Vector3 targetVelocity = moveDirection * speed;
                targetVelocity.y = targetYVelocity;
                rb.linearVelocity = targetVelocity;
            }
        }
    }

    /// <summary>
    /// Modifie temporairement la force du prochain saut (utilisé par ex. par les nénuphars ou tremplins).
    /// </summary>
    /// <param name="force">Nouvelle force verticale de saut.</param>
    public void SetNextJumpForce(float force)
    {
        currentJumpForce = force;
    }

    /// <summary>
    /// Réinitialise la force de saut à sa valeur par défaut.
    /// </summary>
    public void ResetJumpForce()
    {
        currentJumpForce = jumpForce;
    }

    /// <summary>
    /// Applique une impulsion verticale de rebond immédiat (ex: trampoline, rebond automatique).
    /// </summary>
    /// <param name="force">Force verticale du rebond.</param>
    /// <param name="ignoreLowJump">Si vrai, la hauteur du rebond n'est pas tronquée si la touche saut n'est pas maintenue.</param>
    public void Bounce(float force, bool ignoreLowJump = true)
    {
        if (rb == null) return;

        shouldJump = false;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        lastJumpTime = Time.time;
        currentJumpForce = jumpForce;
        OnJump?.Invoke();

        Vector3 vel = rb.linearVelocity;
        vel.y = force;
        rb.linearVelocity = vel;

        if (ignoreLowJump)
        {
            isBouncing = true;
        }

        if (animator != null)
        {
            isJumpAnimHolding = false;
            animator.speed = 1f;
            if (hasJumpTriggerParam) animator.SetTrigger(jumpTriggerHash);
            if (hasIsJumpingParam) animator.SetBool(isJumpingHash, true);
            if (hasIsWalkingParam) animator.SetBool(isWalkingHash, false);
        }
    }

    /// <summary>
    /// Réinitialise complètement l'état aérien du joueur (saut, vélocités, animations) et le remet au sol.
    /// </summary>
    public void ResetAirborneState()
    {
        shouldJump = false;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        isBouncing = false;
        isSlidingOnRoot = false;
        currentJumpForce = jumpForce;
        moveDirection = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.speed = 1f;
            isJumpAnimHolding = false;
            if (hasIsWalkingParam) animator.SetBool(isWalkingHash, false);
            if (hasIsJumpingParam) animator.SetBool(isJumpingHash, false);
        }
    }

    private void OnDisable()
    {
        // Stopper le Rigidbody immédiatement lors de la désactivation pour éviter que le joueur glisse ou tombe indéfiniment
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        moveDirection = Vector3.zero;
        shouldJump = false;
        isBouncing = false;
        isSlidingOnRoot = false;
        currentJumpForce = jumpForce;

        // Forcer l'animation idle lors de la désactivation (cinématiques, dialogues, pause...)
        if (animator != null)
        {
            animator.speed = 1f;
            isJumpAnimHolding = false;
            if (hasIsWalkingParam) animator.SetBool(isWalkingHash, false);
            if (hasIsJumpingParam) animator.SetBool(isJumpingHash, false);
        }
    }
}

