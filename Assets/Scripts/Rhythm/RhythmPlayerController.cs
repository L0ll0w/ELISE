using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôle les mouvements discrets du joueur sur la grille circulaire (couloirs concentriques et secteurs angulaires).
/// Gère également l'orientation du sprite pour faire face au centre de l'arène.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Rhythm Player Controller")]
public class RhythmPlayerController : MonoBehaviour
{
    [Header("Configuration des Déplacements")]
    [Tooltip("Vitesse de déplacement visuel (Lerp) vers la case cible.")]
    [SerializeField] private float lerpSpeed = 15f;

    [Tooltip("Délai minimum (cooldown) entre chaque déplacement de case en secondes pour éviter le spam.")]
    [SerializeField] private float moveCooldown = 0.2f;

    [Tooltip("Si activé, maintenir une direction permet d'avancer de case en case au rythme du cooldown. Sinon, nécessite de relâcher la touche/stick.")]
    [SerializeField] private bool allowHoldToRepeat = false;

    [Tooltip("Effet visuel (prefab de particules) lors d'un déplacement.")]
    [SerializeField] private ParticleSystem moveParticlePrefab;

    [Header("Frames d'Invincibilité")]
    [Tooltip("Durée de l'invincibilité après avoir été touché (en secondes).")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Saut d'Esquive")]
    [Tooltip("Hauteur maximale du saut visuel (offset Y).")]
    [SerializeField] private float jumpHeight = 0.65f;
    [Tooltip("Durée en secondes du saut.")]
    [SerializeField] private float jumpDuration = 0.25f;

    private RadialCombatGrid grid;
    private int currentRing = 0;
    private int currentSector = 0;

    // État du Saut
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpCooldownTimer = 0f;
    private float landingSquashTimer = 0f;
    private Vector3 jumpVisualOffset = Vector3.zero;

    // Cooldown de déplacement entre les cases
    private float moveCooldownTimer = 0f;

    public float MoveCooldown
    {
        get => moveCooldown;
        set => moveCooldown = Mathf.Max(0f, value);
    }

    public bool AllowHoldToRepeat
    {
        get => allowHoldToRepeat;
        set => allowHoldToRepeat = value;
    }

    public bool IsJumping => isJumping;
    private Vector3 targetPosition;
    private Vector3 groundPosition;
    
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float invincibilityTimer = 0f;
    private bool isInputEnabled = false;
    private float groundYOffset = 0f;

    // Détection d'inputs discrets (une pression = une case)
    private bool hasReleasedHorizontal = true;
    private bool hasReleasedVertical = true;

    private PlayerInput playerInput;
    private InputAction moveAction;

    private static readonly int idleHash = Animator.StringToHash("idle");
    private static readonly int walkHash = Animator.StringToHash("walk");
    private static readonly int jumpHash = Animator.StringToHash("jump");
    private static readonly int danceHash = Animator.StringToHash("dance");
    private static readonly int faceDanceHash = Animator.StringToHash("facedance");
    private static readonly int isJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int isWalkingHash = Animator.StringToHash("isWalking");

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }
        if (playerInput != null)
        {
            moveAction = playerInput.actions.FindAction("Move");
        }
    }

    /// <summary>
    /// Initialise le contrôleur sur une grille et à une position de départ.
    /// </summary>
    public void Initialize(RadialCombatGrid combatGrid, int startRing, int startSector)
    {
        grid = combatGrid;
        currentRing = startRing;
        currentSector = startSector;
        isInputEnabled = true;
        moveCooldownTimer = 0f;

        // Calculer le décalage de hauteur du pivot du joueur par rapport au sol (0.47f par défaut pour le prefab Player)
        float yOffset = 0.47f;
        
        // Désactiver temporairement tous les colliders du joueur pour éviter que le rayon ne se heurte lui-même
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool[] collidersState = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            collidersState[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 10f))
        {
            float computedOffset = transform.position.y - hit.point.y;
            // Ne retenir que si la valeur calculée est cohérente pour un personnage debout au sol
            if (computedOffset >= 0.1f && computedOffset <= 1.2f)
            {
                yOffset = computedOffset;
            }
        }

        // Restaurer l'état des colliders
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = collidersState[i];
        }

        groundYOffset = yOffset;

        // Passer le Rigidbody en mode cinématique pour éviter les conflits de physique/gravité avec la grille
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (grid != null)
        {
            targetPosition = grid.GetCellPosition(currentRing, currentSector);
            targetPosition.y += groundYOffset;
            groundPosition = targetPosition;
            transform.position = targetPosition;
        }

        // Récupérer le sprite renderer s'il a changé lors du tag-team
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.Play(danceHash);
            animator.SetBool(isWalkingHash, false);
            animator.SetBool(isJumpingHash, false);
        }

        // Orienter immédiatement le joueur vers le boss
        OrientTowardsCenter();
    }

    private void OnDestroy()
    {
        // Restaurer le Rigidbody en mode physique normal à la fin du combat
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    /// <summary>
    /// Active ou désactive la détection des inputs du joueur.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        if (!enabled)
        {
            hasReleasedHorizontal = true;
            hasReleasedVertical = true;
            moveCooldownTimer = 0f;
        }
    }

    private void Update()
    {
        if (grid == null) return;

        // Gestion du cooldown anti-spam de saut
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // Gestion du cooldown anti-spam de déplacement entre les cases
        if (moveCooldownTimer > 0f)
        {
            moveCooldownTimer -= Time.deltaTime;
        }

        // Gérer le saut visuel (offset Y) avec une courbe asymétrique dynamique et retombée lourde (Snappy Landing)
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float progress = jumpTimer / jumpDuration;
            if (progress >= 1f)
            {
                isJumping = false;
                jumpVisualOffset = Vector3.zero;
                landingSquashTimer = 0.08f; // Déclencher l'effet d'impact à la réception au sol
            }
            else
            {
                // Courbe asymétrique : Montée vive (0 à 0.4) et chute accélérée par la gravité (0.4 à 1.0)
                float heightOffset;
                float apexTime = 0.40f;
                if (progress < apexTime)
                {
                    float t = progress / apexTime;
                    heightOffset = Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
                }
                else
                {
                    float t = (progress - apexTime) / (1f - apexTime);
                    heightOffset = (1f - Mathf.Pow(t, 2.2f)) * jumpHeight;
                }

                jumpVisualOffset = Vector3.up * Mathf.Max(0f, heightOffset);
            }
        }

        // Effet de Squash & Impact à l'atterrissage au sol
        if (landingSquashTimer > 0f)
        {
            landingSquashTimer -= Time.deltaTime;
            float t = landingSquashTimer / 0.08f;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.80f, 1.18f), t);
            }
        }
        else if (spriteRenderer != null && spriteRenderer.transform.localScale != Vector3.one && !isJumping)
        {
            spriteRenderer.transform.localScale = Vector3.Lerp(spriteRenderer.transform.localScale, Vector3.one, Time.deltaTime * 25f);
        }

        // Déplacement visuel du point d'ancrage sol vers la case cible + offset de saut indépendant
        if (groundPosition == Vector3.zero) groundPosition = transform.position;
        groundPosition = Vector3.Lerp(groundPosition, targetPosition, Time.deltaTime * lerpSpeed);
        transform.position = groundPosition + jumpVisualOffset;

        // Faire face à l'ennemi (le centre de la grille)
        OrientTowardsCenter();

        // Gérer le timer d'invincibilité
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            // Clignotement visuel simple pour indiquer l'invincibilité
            if (spriteRenderer != null)
            {
                float blink = Mathf.PingPong(Time.time * 20f, 1f);
                spriteRenderer.color = new Color(1f, 1f, 1f, blink > 0.5f ? 0.3f : 0.8f);
            }
        }
        else
        {
            if (spriteRenderer != null && spriteRenderer.color.a < 1.0f)
            {
                spriteRenderer.color = Color.white; // Restaurer la couleur normale
            }
        }

        // Maintenir en continu l'état d'animation approprié pendant le combat (dance en combat normal, facedance pendant le jugement)
        if (animator != null && !isShootingAnimation && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            int currentHash = stateInfo.shortNameHash;

            bool isJudgment = RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsInJudgment;
            int targetAnimHash = isJudgment ? faceDanceHash : danceHash;

            // Pendant le jugement, le joueur doit toujours être en facedance. En combat normal, en dance.
            if (currentHash == idleHash || currentHash == walkHash || currentHash == jumpHash || (isJudgment && currentHash == danceHash))
            {
                animator.Play(targetAnimHash);
            }
            animator.SetBool(isJumpingHash, false);
            animator.SetBool(isWalkingHash, false);
        }

        if (!isInputEnabled) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        HandleGridInputs();
    }

    public void Jump()
    {
        if (!isJumping && jumpCooldownTimer <= 0f)
        {
            isJumping = true;
            jumpTimer = 0f;
            jumpCooldownTimer = jumpDuration + 0.05f; // Cooldown anti-spam

            // En combat rythmique, le joueur conserve son animation de danse pendant le saut
            if (animator != null)
            {
                animator.SetBool(isJumpingHash, false);
                animator.SetBool(isWalkingHash, false);
                animator.ResetTrigger("jump");
                animator.ResetTrigger("Spawn");

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != danceHash && !isShootingAnimation)
                {
                    animator.Play(danceHash);
                }
            }
        }
    }

    private void HandleGridInputs()
    {
        // Détecter le saut (Espace au clavier ou bouton A/Sud sur manette)
        bool jumpPressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;

        if (jumpPressed)
        {
            Jump();
        }

        float h = 0f;
        float v = 0f;

        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            h = moveInput.x;
            v = moveInput.y;
        }
        else
        {
            // Fallback direct clavier si l'action n'est pas trouvée
            if (Keyboard.current != null)
            {
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
                else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
            }
        }

        bool inputH = Mathf.Abs(h) > 0.5f;
        bool inputV = Mathf.Abs(v) > 0.5f;

        // Réinitialiser les états de relâchement si les inputs reviennent au centre
        if (!inputH) hasReleasedHorizontal = true;
        if (!inputV) hasReleasedVertical = true;

        // Si le cooldown anti-spam est actif, aucun nouveau déplacement n'est autorisé
        if (moveCooldownTimer > 0f) return;

        // Déterminer la priorité d'axe pour éviter les déplacements diagonaux accidentels simultanés
        bool checkHorizontalFirst = Mathf.Abs(h) >= Mathf.Abs(v);

        if (checkHorizontalFirst)
        {
            if (TryMoveHorizontal(h) || TryMoveVertical(v))
            {
                moveCooldownTimer = moveCooldown;
            }
        }
        else
        {
            if (TryMoveVertical(v) || TryMoveHorizontal(h))
            {
                moveCooldownTimer = moveCooldown;
            }
        }
    }

    private bool TryMoveHorizontal(float h)
    {
        if (Mathf.Abs(h) <= 0.5f) return false;
        if (!hasReleasedHorizontal && !allowHoldToRepeat) return false;

        int previousSector = currentSector;
        if (h > 0f)
        {
            // Droite (sens anti-horaire pour aller vers la droite de l'écran)
            currentSector = (currentSector + 1) % grid.SectorsCount;
        }
        else
        {
            // Gauche (sens horaire pour aller vers la gauche de l'écran)
            currentSector = (currentSector - 1 + grid.SectorsCount) % grid.SectorsCount;
        }

        OnMoveCell(previousSector, currentRing);
        hasReleasedHorizontal = false;
        return true;
    }

    private bool TryMoveVertical(float v)
    {
        if (Mathf.Abs(v) <= 0.5f) return false;
        if (!hasReleasedVertical && !allowHoldToRepeat) return false;

        int previousRing = currentRing;
        if (v > 0f)
        {
            // Aller vers le cercle intérieur (se rapprocher du boss)
            currentRing = Mathf.Max(currentRing - 1, 0);
        }
        else
        {
            // Aller vers le cercle extérieur (s'éloigner du boss, stick en arrière)
            currentRing = Mathf.Min(currentRing + 1, grid.RingsCount - 1);
        }

        if (currentRing != previousRing)
        {
            OnMoveCell(currentSector, previousRing);
            hasReleasedVertical = false;
            return true;
        }

        return false;
    }

    private void OnMoveCell(int oldSector, int oldRing)
    {
        // Mettre à jour la cible physique
        targetPosition = grid.GetCellPosition(currentRing, currentSector);
        targetPosition.y += groundYOffset;

        // Lancer les particules
        if (moveParticlePrefab != null)
        {
            ParticleSystem ps = Instantiate(moveParticlePrefab, transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }
    }

    private bool isShootingAnimation = false;
    public bool IsShootingAnimation
    {
        get => isShootingAnimation;
        set => isShootingAnimation = value;
    }

    private void OrientTowardsCenter()
    {
        if (spriteRenderer != null && grid != null)
        {
            bool isJudgment = RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsInJudgment;
            if (isJudgment)
            {
                spriteRenderer.flipX = false;
                return;
            }

            Camera mainCam = Camera.main;
            Vector3 targetPos = grid.transform.position;
            Vector3 playerPos = transform.position;

            bool isTargetToScreenLeft;
            if (mainCam != null)
            {
                float targetScreenX = mainCam.WorldToScreenPoint(targetPos).x;
                float playerScreenX = mainCam.WorldToScreenPoint(playerPos).x;
                isTargetToScreenLeft = targetScreenX < playerScreenX;
            }
            else
            {
                isTargetToScreenLeft = targetPos.x < playerPos.x;
            }

            if (isShootingAnimation)
            {
                // Sens inversé selon la demande du joueur pour être toujours orienté face à l'ennemi
                spriteRenderer.flipX = isTargetToScreenLeft;
            }
            else
            {
                // Animation Dance/Idle normale
                spriteRenderer.flipX = isTargetToScreenLeft;
            }
        }
    }

    /// <summary>
    /// Déclenche l'invincibilité temporaire (après un coup par exemple).
    /// </summary>
    public void TriggerInvincibility()
    {
        invincibilityTimer = invincibilityDuration;
    }

    // Accesseurs
    public int CurrentRing => currentRing;
    public int CurrentSector => currentSector;
    public bool IsInvincible => invincibilityTimer > 0f;
    public Vector3 TargetPosition => targetPosition;
}
