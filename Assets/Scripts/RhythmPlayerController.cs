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

    [Tooltip("Effet visuel (prefab de particules) lors d'un déplacement.")]
    [SerializeField] private ParticleSystem moveParticlePrefab;

    [Header("Frames d'Invincibilité")]
    [Tooltip("Durée de l'invincibilité après avoir été touché (en secondes).")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Saut d'Esquive")]
    [Tooltip("Hauteur maximale du saut visuel (offset Y).")]
    [SerializeField] private float jumpHeight = 0.3f;
    [Tooltip("Durée en secondes du saut.")]
    [SerializeField] private float jumpDuration = 0.25f;

    private RadialCombatGrid grid;
    private int currentRing = 0;
    private int currentSector = 0;

    // État du Saut
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float landingSquashTimer = 0f;
    private Vector3 jumpVisualOffset = Vector3.zero;

    public bool IsJumping => isJumping;
    private Vector3 targetPosition;
    
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
            transform.position = targetPosition;
        }

        // Récupérer le sprite renderer s'il a changé lors du tag-team
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

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
        }
    }

    private void Update()
    {
        if (grid == null) return;

        // Gérer le saut visuel (offset Y) avec une trajectoire parabolique (physique du saut de déplacement)
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float progress = jumpTimer / jumpDuration;
            if (progress >= 1f)
            {
                isJumping = false;
                jumpVisualOffset = Vector3.zero;
            }
            else
            {
                // Trajectoire parabolique snappie (y = 4 * x * (1 - x) * height)
                float heightOffset = 4f * progress * (1f - progress) * jumpHeight;
                jumpVisualOffset = Vector3.up * heightOffset;
            }
        }

        // Déplacement visuel fluide vers la case cible + offset de saut
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed) + jumpVisualOffset;

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

        if (!isInputEnabled) return;

        HandleGridInputs();
    }

    public void Jump()
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpTimer = 0f;
            if (animator != null)
            {
                animator.SetTrigger("Spawn"); // Même animation de saut court que pour les déplacements
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

        // 1. Inputs Horizontaux (Gauche/Droite pour tourner autour du boss)
        if (Mathf.Abs(h) > 0.5f)
        {
            if (hasReleasedHorizontal)
            {
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
            }
        }
        else
        {
            hasReleasedHorizontal = true;
        }

        // 2. Inputs Verticaux (Haut/Bas pour changer de cercle)
        if (Mathf.Abs(v) > 0.5f)
        {
            if (hasReleasedVertical)
            {
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
                }
                hasReleasedVertical = false;
            }
        }
        else
        {
            hasReleasedVertical = true;
        }
    }

    private void OnMoveCell(int oldSector, int oldRing)
    {
        // Mettre à jour la cible physique
        targetPosition = grid.GetCellPosition(currentRing, currentSector);
        targetPosition.y += groundYOffset;

        // Déclencher l'animation de saut court de l'animateur si disponible
        if (animator != null)
        {
            animator.SetTrigger("Spawn"); // Réutilisation du trigger de bump/saut de spawn pour simuler un saut rapide
        }

        // Lancer les particules
        if (moveParticlePrefab != null)
        {
            ParticleSystem ps = Instantiate(moveParticlePrefab, transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }
    }

    private void OrientTowardsCenter()
    {
        if (spriteRenderer != null && grid != null)
        {
            // Dans notre perspective 2.5D, si le boss est à gauche du joueur (axe X), on regarde à gauche (flipX = true)
            spriteRenderer.flipX = grid.transform.position.x < transform.position.x;
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
