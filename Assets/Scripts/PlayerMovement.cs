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

    [Header("Orientation par rapport à la Caméra")]
    [Tooltip("Si coché, les directions Z/X s'alignent avec l'orientation de la caméra principale.")]
    [SerializeField] private bool moveRelativeToCamera = false;

    private SpriteRenderer spriteRenderer;
    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Camera mainCamera;

    private void Start()
    {
        // Récupération automatique du SpriteRenderer sur le GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();
        
        // Récupération du PlayerInput et recherche de l'action "Move"
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions.FindAction("Move");
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
        Vector3 moveDirection = Vector3.zero;

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

        // Application du déplacement (via Rigidbody ou transform en fallback)
        if (rb != null)
        {
            Vector3 targetVelocity = moveDirection * speed;
            targetVelocity.y = rb.linearVelocity.y; // Préserver la gravité
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
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
}

