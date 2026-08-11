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

    [Header("Orientation par rapport à la Caméra")]
    [Tooltip("Si coché, les directions Z/X s'alignent avec l'orientation de la caméra principale.")]
    [SerializeField] private bool moveRelativeToCamera = false;

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

    private void Start()
    {
        // Récupération automatique des composants sur le GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
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
    }

    private void Update()
    {
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

        // Gestion du Coyote Time
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Détecter la demande de saut avec Coyote Time
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && coyoteTimeCounter > 0f)
        {
            shouldJump = true;
            coyoteTimeCounter = 0f; // Éviter le saut multiple en l'air
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
    }

    /// <summary>
    /// Vérifie si le joueur touche le sol.
    /// </summary>
    public bool IsGrounded()
    {
        if (playerCollider == null) return true;

        // Commence légèrement au-dessus du bas du collider pour éviter de commencer à l'intérieur du sol
        Vector3 origin = new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.1f, playerCollider.bounds.center.z);
        
        // Raycast vers le bas de 0.2m (donc dépasse de 0.1m sous le collider)
        return Physics.Raycast(origin, Vector3.down, 0.2f);
    }

    private void FixedUpdate()
    {
        // Application du déplacement via le Rigidbody si disponible
        if (rb != null)
        {
            float targetYVelocity = rb.linearVelocity.y;

            // Appliquer la vitesse de saut si demandée
            if (shouldJump)
            {
                targetYVelocity = jumpForce;
                shouldJump = false;
            }

            if (moveDirection.magnitude < 0.01f)
            {
                // Bloque la position X et Z dans le moteur physique pour stopper tout glissement sur les pentes
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                
                // On applique la vitesse verticale tout en bloquant la vitesse horizontale
                rb.linearVelocity = new Vector3(0f, targetYVelocity, 0f);
            }
            else
            {
                // Libère X et Z pour le déplacement, tout en maintenant les rotations figées
                rb.constraints = RigidbodyConstraints.FreezeRotation;

                Vector3 targetVelocity = moveDirection * speed;
                targetVelocity.y = targetYVelocity;
                
                rb.linearVelocity = targetVelocity;
            }
        }
    }
}

