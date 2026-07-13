using UnityEngine;

/// <summary>
/// Gère le déplacement autonome (balade/patrouille) d'un ennemi autour de sa position d'origine.
/// Prend en compte les collisions et évite les obstacles via des SphereCasts.
/// S'adapte automatiquement à un CharacterController, un Rigidbody ou un déplacement par Transform.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("2.5D RPG/Enemy Wander")]
public class EnemyWander : MonoBehaviour
{
    [Header("Zone de Balade")]
    [Tooltip("Rayon maximum de déplacement autour du point d'origine où l'ennemi a été posé.")]
    [SerializeField] private float wanderRadius = 5f;

    [Tooltip("Distance minimale parcourue lors d'un déplacement.")]
    [SerializeField] private float minWanderDistance = 2f;

    [Tooltip("Distance maximale parcourue lors d'un déplacement.")]
    [SerializeField] private float maxWanderDistance = 5f;

    [Header("Paramètres de Déplacement")]
    [Tooltip("Vitesse de déplacement de l'ennemi.")]
    [SerializeField] private float speed = 2.5f;

    [Header("Temps d'Attente")]
    [Tooltip("Temps d'attente minimum à chaque destination.")]
    [SerializeField] private float minWaitTime = 1f;

    [Tooltip("Temps d'attente maximum à chaque destination.")]
    [SerializeField] private float maxWaitTime = 4f;

    [Header("Gestion des Collisions")]
    [Tooltip("LayerMask définissant les obstacles à éviter (murs, arbres, etc.).")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Rayon de la sphère de détection pour les obstacles (doit être proche du rayon physique de l'ennemi).")]
    [SerializeField] private float obstacleCheckRadius = 0.4f;

    [Tooltip("Distance de détection d'obstacle devant l'ennemi pendant sa marche. Si un obstacle est détecté à cette distance, il s'arrête.")]
    [SerializeField] private float obstacleDetectionDistance = 0.8f;

    [Header("Paramètres d'Animation")]
    [Tooltip("Nom du paramètre booléen dans l'Animator pour indiquer si le personnage bouge.")]
    [SerializeField] private string isMovingParameterName = "IsMoving";
    [Tooltip("Nom du paramètre float dans l'Animator pour indiquer la vitesse du personnage.")]
    [SerializeField] private string speedParameterName = "Speed";

    private Vector3 originPosition;
    private Vector3 targetPosition;
    private bool isWaiting = true;
    private float waitTimer;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CharacterController characterController;
    private Rigidbody rb;

    private bool hasAnimator;
    private int isMovingHash;
    private int speedHash;
    private Vector3 lastPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        hasAnimator = animator != null;
        if (hasAnimator)
        {
            isMovingHash = Animator.StringToHash(isMovingParameterName);
            speedHash = Animator.StringToHash(speedParameterName);
        }
    }

    private void Start()
    {
        // Enregistre la position initiale comme point d'origine
        originPosition = transform.position;
        lastPosition = transform.position;

        // Commence par attendre un instant aléatoire avant de bouger
        StartWaiting(Random.Range(0f, minWaitTime));
    }

    private void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                ChooseNewTarget();
            }

            // Met à jour l'animation à l'arrêt
            UpdateAnimation(Vector3.zero, 0f);
            return;
        }

        MoveTowardsTarget();
    }

    /// <summary>
    /// Gère le déplacement vers la cible actuelle, en gérant la physique et l'évitement d'obstacles.
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector3 currentPos = transform.position;
        
        // Calcul du vecteur vers la cible (sur le plan horizontal XZ principalement)
        Vector3 targetPosHorizontal = new Vector3(targetPosition.x, currentPos.y, targetPosition.z);
        Vector3 movement = targetPosHorizontal - currentPos;
        float distanceToTarget = movement.magnitude;

        // Si on est arrivé proche de la cible, on attend
        if (distanceToTarget < 0.1f)
        {
            StartWaiting();
            return;
        }

        Vector3 moveDirection = movement.normalized;

        // Évitement de collision proactif durant le déplacement :
        // On projette une sphère en avant pour voir si un obstacle barre la route.
        // On décale le rayon vers le haut pour ne pas toucher le sol.
        Vector3 castOrigin = currentPos + Vector3.up * obstacleCheckRadius;
        if (Physics.SphereCast(castOrigin, obstacleCheckRadius, moveDirection, out RaycastHit hit, obstacleDetectionDistance, obstacleMask))
        {
            // Obstacle détecté ! On s'arrête immédiatement et on choisira une autre destination
            StartWaiting();
            return;
        }

        // Calcul du déplacement pour cette frame
        float step = speed * Time.deltaTime;
        if (step > distanceToTarget)
        {
            step = distanceToTarget;
        }

        Vector3 frameMovement = moveDirection * step;

        // Application du déplacement en fonction des composants disponibles
        if (characterController != null && characterController.enabled)
        {
            // Si l'ennemi n'est pas au sol, on lui applique la gravité
            if (!characterController.isGrounded)
            {
                frameMovement.y += Physics.gravity.y * Time.deltaTime;
            }
            characterController.Move(frameMovement);
        }
        else if (rb != null && !rb.isKinematic)
        {
            // Déplacement via le Rigidbody physique
            rb.MovePosition(currentPos + frameMovement);
        }
        else
        {
            // Déplacement direct par Transform (fallback)
            transform.position = currentPos + frameMovement;
        }

        // Recalcul de la position réelle après le mouvement de physique
        Vector3 newPosition = transform.position;
        Vector3 realMovement = newPosition - lastPosition;
        float actualSpeed = realMovement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        // Flip du Sprite selon la direction horizontale
        if (realMovement.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (realMovement.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }

        // Mise à jour de l'Animator
        UpdateAnimation(realMovement, actualSpeed);

        lastPosition = newPosition;
    }

    /// <summary>
    /// Choisi une nouvelle cible aléatoire valide à l'intérieur du rayon de balade.
    /// </summary>
    private void ChooseNewTarget()
    {
        bool foundTarget = false;
        Vector3 chosenPos = transform.position;

        // Essaye de trouver un point libre jusqu'à 20 fois
        for (int i = 0; i < 20; i++)
        {
            // Angle et distance aléatoires
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minWanderDistance, maxWanderDistance);

            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidatePos = transform.position + direction * distance;

            // Assurer que le point reste dans le wanderRadius par rapport à l'origine
            Vector3 offsetFromOrigin = candidatePos - originPosition;
            Vector2 offset2D = new Vector2(offsetFromOrigin.x, offsetFromOrigin.z);
            if (offset2D.magnitude > wanderRadius)
            {
                // Si la cible dépasse le rayon maximal, on la ramène à la limite du rayon
                offset2D = offset2D.normalized * wanderRadius;
                candidatePos = new Vector3(originPosition.x + offset2D.x, transform.position.y, originPosition.z + offset2D.y);
            }

            // 1. Vérifie s'il y a un obstacle à la position cible elle-même
            if (Physics.CheckSphere(candidatePos + Vector3.up * obstacleCheckRadius, obstacleCheckRadius, obstacleMask))
            {
                continue; // Obstacle présent, on réessaie
            }

            // 2. Vérifie si le chemin vers cette cible est dégagé
            Vector3 pathVec = candidatePos - transform.position;
            float pathDist = pathVec.magnitude;
            if (pathDist > 0.01f)
            {
                Vector3 pathDir = pathVec.normalized;
                Vector3 startCast = transform.position + Vector3.up * obstacleCheckRadius;
                if (Physics.SphereCast(startCast, obstacleCheckRadius, pathDir, out RaycastHit hit, pathDist, obstacleMask))
                {
                    continue; // Chemin bloqué par un obstacle, on réessaie
                }
            }

            // Le point est valide
            chosenPos = candidatePos;
            foundTarget = true;
            break;
        }

        // Si aucun point valide n'est trouvé, on tente de revenir un peu vers l'origine comme solution de secours
        if (!foundTarget)
        {
            Vector3 directionToOrigin = (originPosition - transform.position);
            directionToOrigin.y = 0f;
            if (directionToOrigin.magnitude > 0.5f)
            {
                float fallbackDist = Random.Range(minWanderDistance, Mathf.Min(maxWanderDistance, directionToOrigin.magnitude));
                chosenPos = transform.position + directionToOrigin.normalized * fallbackDist;
            }
            else
            {
                // Si déjà à l'origine et toujours bloqué, on reste sur place
                chosenPos = transform.position;
            }
        }

        // Définir la cible et passer à l'état de marche
        targetPosition = chosenPos;
        isWaiting = false;
        lastPosition = transform.position;
    }

    /// <summary>
    /// Transitionne vers l'état d'attente avec une durée aléatoire.
    /// </summary>
    private void StartWaiting()
    {
        StartWaiting(Random.Range(minWaitTime, maxWaitTime));
    }

    /// <summary>
    /// Transitionne vers l'état d'attente avec une durée spécifiée.
    /// </summary>
    private void StartWaiting(float duration)
    {
        isWaiting = true;
        waitTimer = duration;
        lastPosition = transform.position;
    }

    /// <summary>
    /// Met à jour les paramètres de l'animator si présent.
    /// </summary>
    private void UpdateAnimation(Vector3 realMovement, float actualSpeed)
    {
        if (hasAnimator)
        {
            bool isMoving = actualSpeed > 0.1f;

            if (HasParameter(isMovingParameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(isMovingHash, isMoving);
            }

            if (HasParameter(speedParameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(speedHash, actualSpeed);
            }
        }
    }

    /// <summary>
    /// Vérifie si un paramètre spécifique existe dans l'Animator.
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

    /// <summary>
    /// Permet de visualiser la zone de balade de l'ennemi dans l'éditeur Unity.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? originPosition : transform.position;

        // Zone de balade totale (Rayon d'origine) - Vert semi-transparent
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(origin, 0.1f);
        
        // Cercle représentant le rayon de balade max
        DrawWireCircle(origin, wanderRadius, Color.green);

        // Indicateur des distances de marche min/max autour de la position actuelle
        DrawWireCircle(transform.position, minWanderDistance, new Color(0f, 0.5f, 1f, 0.4f));
        DrawWireCircle(transform.position, maxWanderDistance, new Color(0f, 0.5f, 1f, 0.6f));

        // Ligne reliant l'ennemi à sa cible actuelle
        if (Application.isPlaying && !isWaiting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, obstacleCheckRadius);
        }
    }

    /// <summary>
    /// Dessine un cercle en fils de fer sur le plan XZ.
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        int segments = 32;
        Vector3 lastPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}
