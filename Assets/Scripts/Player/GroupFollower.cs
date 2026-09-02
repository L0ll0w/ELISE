using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère le déplacement d'un compagnon du groupe, l'orientation de son sprite,
/// ses animations de déplacement et la désactivation de ses contrôles de joueur s'il en a.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("2.5D RPG/Group Follower")]
public class GroupFollower : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Facteur multiplicateur de vitesse pour l'animation (ex: vitesse réelle * multiplicateur).")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Paramètres d'Animation")]
    [Tooltip("Nom du paramètre booléen dans l'Animator pour indiquer si le personnage bouge.")]
    [SerializeField] private string isMovingParameterName = "IsMoving";
    [Tooltip("Nom du paramètre float dans l'Animator pour indiquer la vitesse du personnage.")]
    [SerializeField] private string speedParameterName = "Speed";

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CharacterController characterController;
    private Rigidbody rb;

    private Vector3 lastPosition;
    private bool hasAnimator;
    private int isMovingHash;
    private int speedHash;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        // Désactivation des scripts de contrôle utilisateur pour éviter qu'ils ne bougent avec les touches
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        PlayerInput pi = GetComponent<PlayerInput>();
        if (pi != null) pi.enabled = false;

        // Configuration de l'animator si présent
        hasAnimator = animator != null;
        if (hasAnimator)
        {
            isMovingHash = Animator.StringToHash(isMovingParameterName);
            speedHash = Animator.StringToHash(speedParameterName);
        }

        lastPosition = transform.position;
    }

    /// <summary>
    /// Déplace le compagnon vers la position cible et gère l'orientation du sprite et l'animation.
    /// </summary>
    /// <param name="targetPosition">Position cible dans le monde 3D.</param>
    /// <param name="maxSpeed">Vitesse maximale autorisée (utilisée en cas de déplacement par transform).</param>
    public void MoveTo(Vector3 targetPosition, float maxSpeed)
    {
        Vector3 currentPosition = transform.position;
        Vector3 movement = targetPosition - currentPosition;

        // 1. Déplacement du personnage
        if (characterController != null && characterController.enabled)
        {
            // Application du déplacement via le CharacterController pour gérer les collisions et la gravité
            Vector3 velocity = movement;
            if (!characterController.isGrounded)
            {
                velocity.y += Physics.gravity.y * Time.deltaTime;
            }
            characterController.Move(velocity);
        }
        else if (rb != null && !rb.isKinematic)
        {
            // Déplacement via Rigidbody si non cinématique
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Déplacement direct par Transform (interpolé)
            transform.position = Vector3.MoveTowards(currentPosition, targetPosition, maxSpeed * Time.deltaTime);
        }

        // Recalcul de la position réelle après déplacement pour l'orientation et l'animation
        Vector3 newPosition = transform.position;
        Vector3 realMovement = newPosition - lastPosition;
        float actualSpeed = realMovement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        // 2. Flip du Sprite
        if (realMovement.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (realMovement.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }

        // 3. Gestion de l'Animator
        if (hasAnimator)
        {
            bool isMoving = actualSpeed > 0.1f;
            
            // On vérifie si les paramètres existent dans l'animator avant de les définir
            if (HasParameter(isMovingParameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(isMovingHash, isMoving);
            }
            
            if (HasParameter(speedParameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(speedHash, actualSpeed * speedMultiplier);
            }
        }

        lastPosition = newPosition;
    }

    /// <summary>
    /// Téléporte directement le compagnon à une position donnée (ex: lors d'un spawn ou d'un changement de scène).
    /// </summary>
    public void TeleportTo(Vector3 targetPosition)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = targetPosition;
            characterController.enabled = true;
        }
        else if (rb != null)
        {
            rb.position = targetPosition;
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            transform.position = targetPosition;
        }

        lastPosition = targetPosition;

        if (hasAnimator)
        {
            if (HasParameter(isMovingParameterName, AnimatorControllerParameterType.Bool))
                animator.SetBool(isMovingHash, false);
            if (HasParameter(speedParameterName, AnimatorControllerParameterType.Float))
                animator.SetFloat(speedHash, 0f);
        }
    }

    /// <summary>
    /// Helper pour vérifier si un paramètre existe dans l'Animator.
    /// </summary>
    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
            {
                return true;
            }
        }
        return false;
    }
}
