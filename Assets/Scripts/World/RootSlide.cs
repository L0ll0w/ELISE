using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permet à un personnage de glisser le long d'une racine ou surface courbe (toboggan)
/// en suivant une trajectoire Spline Catmull-Rom définie par des Waypoints.
/// </summary>
[AddComponentMenu("2.5D RPG/Root Slide")]
public class RootSlide : MonoBehaviour
{
    [Header("Points de la Trajectoire (Waypoints)")]
    [Tooltip("Liste des points de passage définissant la courbure de la racine du haut vers le bas.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Tooltip("Rayon de la zone de déclenchement au premier waypoint si aucun Trigger Collider n'est assigné.")]
    [SerializeField] private float startTriggerRadius = 1.5f;

    [Header("Paramètres de Glissade")]
    [Tooltip("Vitesse initiale de la glissade au sommet (m/s).")]
    [SerializeField] private float initialSpeed = 6f;

    [Tooltip("Accélération progressive le long de la descente (m/s²).")]
    [SerializeField] private float acceleration = 5f;

    [Tooltip("Vitesse maximale atteignable pendant la glissade.")]
    [SerializeField] private float maxSpeed = 18f;

    [Header("Éjection en Fin de Parcours")]
    [Tooltip("Force d'éjection vers l'avant à la fin du toboggan.")]
    [SerializeField] private float exitForwardImpulse = 5f;

    [Tooltip("Force d'éjection vers le haut à la sortie.")]
    [SerializeField] private float exitUpwardImpulse = 2.5f;

    [Header("Effets Visuels & Sonores")]
    [Tooltip("Effet sonore joué en boucle pendant la glissade.")]
    [SerializeField] private AudioClip slideSFX;

    [Tooltip("Volume du son de glissade (0 à 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float slideSFXVolume = 0.8f;

    [Tooltip("Effet sonore joué lors de l'éjection en bas.")]
    [SerializeField] private AudioClip exitSFX;

    [Tooltip("Volume du son d'éjection (0 à 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float exitSFXVolume = 0.8f;

    [Tooltip("Nom du paramètre booléen dans l'Animator pour la glissade (ex: isSliding).")]
    [SerializeField] private string slideAnimParam = "isSliding";

    [Header("Visualisation Editeur")]
    [Tooltip("Couleur du tracé Gizmo dans la vue Scène.")]
    [SerializeField] private Color pathColor = new Color(0f, 0.8f, 1f, 0.9f);

    private bool isSliding = false;
    private AudioSource loopingAudioSource;

    // Cache pour la réchantillonnage de la spline par distance arc
    private struct SplineSample
    {
        public float distance;
        public Vector3 position;
        public Vector3 tangent;
    }

    private List<SplineSample> splineSamples = new List<SplineSample>();
    private float totalSplineLength = 0f;

    private void Awake()
    {
        // Si aucun waypoint n'a été spécifié dans l'Inspector, on récupère les enfants automatiquement
        if (waypoints == null || waypoints.Count == 0)
        {
            AutoAssignChildWaypoints();
        }

        RebuildSplineSamples();
    }

    private void OnValidate()
    {
        if (waypoints != null && waypoints.Count >= 2)
        {
            RebuildSplineSamples();
        }
    }

    /// <summary>
    /// Remplit automatiquement la liste des waypoints avec les enfants du GameObject actuel.
    /// </summary>
    [ContextMenu("Auto-Assign Child Waypoints")]
    public void AutoAssignChildWaypoints()
    {
        waypoints.Clear();
        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }
    }

    /// <summary>
    /// Génère un waypoint enfant supplémentaire à la fin de la liste.
    /// </summary>
    [ContextMenu("Add New Waypoint at End")]
    public void AddNewWaypointAtEnd()
    {
        Vector3 spawnPos = transform.position;
        if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null)
        {
            spawnPos = waypoints[waypoints.Count - 1].position + Vector3.down * 2f + Vector3.forward * 2f;
        }

        GameObject newPoint = new GameObject($"Waypoint_{waypoints.Count + 1}");
        newPoint.transform.SetParent(transform);
        newPoint.transform.position = spawnPos;
        waypoints.Add(newPoint.transform);
        RebuildSplineSamples();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSliding) return;

        // Détecter si le collider appartient au joueur
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = other.GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            // Vérifier si le joueur est proche du point de départ (premier waypoint)
            if (waypoints.Count >= 2 && waypoints[0] != null)
            {
                float distToStart = Vector3.Distance(other.bounds.center, waypoints[0].position);
                float allowedRadius = Mathf.Max(startTriggerRadius, 2.5f);
                if (distToStart <= allowedRadius)
                {
                    StartCoroutine(SlideRoutine(playerMovement));
                }
            }
        }
    }

    /// <summary>
    /// Coroutine gérant le déroulement complet de la glissade le long du toboggan.
    /// </summary>
    private IEnumerator SlideRoutine(PlayerMovement player)
    {
        if (player == null || splineSamples.Count < 2) yield break;

        isSliding = true;

        // 1. Verrouiller les contrôles du joueur et signaler qu'il glisse
        player.IsInputLocked = true;
        player.IsSliding = true;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        Animator animator = player.GetComponent<Animator>();

        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // 2. Gestion de l'animation (détection automatique de Bool, Trigger, Int ou nom d'état)
        AnimPlayType playedAnimType = TryPlaySlideAnimation(animator, slideAnimParam, out int slideHash);

        // 3. Démarrer le son de glissade en boucle
        if (slideSFX != null)
        {
            loopingAudioSource = gameObject.AddComponent<AudioSource>();
            loopingAudioSource.clip = slideSFX;
            loopingAudioSource.volume = slideSFXVolume;
            loopingAudioSource.loop = true;
            loopingAudioSource.spatialBlend = 0.5f;
            loopingAudioSource.Play();
        }

        // 4. Parcourir la spline basée sur la distance arc
        float currentDistance = 0f;
        float currentSpeed = initialSpeed;

        while (currentDistance < totalSplineLength)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
            currentDistance += currentSpeed * Time.deltaTime;

            if (currentDistance > totalSplineLength)
            {
                currentDistance = totalSplineLength;
            }

            // Récupérer la position et la tangente échantillonnées à cette distance
            GetSampleAtDistance(currentDistance, out Vector3 targetPos, out Vector3 targetTangent);

            // Ajuster la hauteur pour placer le bas du personnage sur la racine
            player.transform.position = targetPos;

            // Orienter le SpriteRenderer en X selon la direction de glissade (tangente)
            if (spriteRenderer != null)
            {
                if (targetTangent.x < -0.05f)
                {
                    spriteRenderer.flipX = true;
                }
                else if (targetTangent.x > 0.05f)
                {
                    spriteRenderer.flipX = false;
                }
            }

            yield return null;
        }

        // 5. Arrêter le son de glissade
        if (loopingAudioSource != null)
        {
            loopingAudioSource.Stop();
            Destroy(loopingAudioSource);
        }

        // 6. Désactiver l'animation de glissade
        StopSlideAnimation(animator, playedAnimType, slideHash);

        // 7. Restaurer le Rigidbody et appliquer l'impulsion d'éjection
        GetSampleAtDistance(totalSplineLength, out _, out Vector3 exitTangent);
        Vector3 forwardDir = exitTangent.normalized;
        Vector3 ejectionVelocity = forwardDir * exitForwardImpulse + Vector3.up * exitUpwardImpulse;

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = ejectionVelocity;
        }

        // 8. Jouer le son d'éjection/atterrissage
        if (exitSFX != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(exitSFX, exitSFXVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(exitSFX, player.transform.position, exitSFXVolume);
            }
        }

        // 9. Attendre la chute et le contact avec le sol avant de repasser sur l'animation Idle
        yield return new WaitForSeconds(0.08f); // Petit délai initial pour quitter le sommet du dernier waypoint

        float timeoutTimer = 0f;
        while (!player.IsGrounded() && timeoutTimer < 2.0f)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }

        // 10. Désactiver l'animation de glissade et forcer le retour à Idle lors de l'impact sol
        if (animator != null)
        {
            animator.speed = 1f;
            StopSlideAnimation(animator, playedAnimType, slideHash);

            int idleHash = Animator.StringToHash("Idle");
            int lowerIdleHash = Animator.StringToHash("idle");

            if (animator.HasState(0, idleHash))
            {
                animator.Play(idleHash, 0, 0f);
            }
            else if (animator.HasState(0, lowerIdleHash))
            {
                animator.Play(lowerIdleHash, 0, 0f);
            }
        }

        player.IsSliding = false;
        player.IsInputLocked = false;
        isSliding = false;
    }

    /// <summary>
    /// Reconstruit la table d'échantillonnage de la spline Catmull-Rom pour garantir un mouvement à vitesse constante par distance.
    /// </summary>
    private void RebuildSplineSamples()
    {
        splineSamples.Clear();
        totalSplineLength = 0f;

        List<Vector3> validPoints = GetValidWaypointPositions();
        if (validPoints.Count < 2) return;

        const int stepsPerSegment = 20;
        int numSegments = validPoints.Count - 1;

        Vector3 lastPos = validPoints[0];
        splineSamples.Add(new SplineSample
        {
            distance = 0f,
            position = lastPos,
            tangent = GetCatmullRomTangent(validPoints, 0, 0f)
        });

        for (int seg = 0; seg < numSegments; seg++)
        {
            for (int step = 1; step <= stepsPerSegment; step++)
            {
                float t = (float)step / stepsPerSegment;
                Vector3 pos = GetCatmullRomPosition(validPoints, seg, t);
                Vector3 tan = GetCatmullRomTangent(validPoints, seg, t);

                float distStep = Vector3.Distance(lastPos, pos);
                totalSplineLength += distStep;

                splineSamples.Add(new SplineSample
                {
                    distance = totalSplineLength,
                    position = pos,
                    tangent = tan
                });

                lastPos = pos;
            }
        }
    }

    private void GetSampleAtDistance(float distance, out Vector3 position, out Vector3 tangent)
    {
        if (splineSamples.Count == 0)
        {
            position = transform.position;
            tangent = transform.forward;
            return;
        }

        if (distance <= 0f)
        {
            position = splineSamples[0].position;
            tangent = splineSamples[0].tangent;
            return;
        }

        if (distance >= totalSplineLength)
        {
            position = splineSamples[splineSamples.Count - 1].position;
            tangent = splineSamples[splineSamples.Count - 1].tangent;
            return;
        }

        // Recherche dichotomique ou linéaire du segment de distance
        for (int i = 0; i < splineSamples.Count - 1; i++)
        {
            if (distance >= splineSamples[i].distance && distance <= splineSamples[i + 1].distance)
            {
                float segLen = splineSamples[i + 1].distance - splineSamples[i].distance;
                float factor = segLen > 0.0001f ? (distance - splineSamples[i].distance) / segLen : 0f;

                position = Vector3.Lerp(splineSamples[i].position, splineSamples[i + 1].position, factor);
                tangent = Vector3.Lerp(splineSamples[i].tangent, splineSamples[i + 1].tangent, factor);
                return;
            }
        }

        position = splineSamples[splineSamples.Count - 1].position;
        tangent = splineSamples[splineSamples.Count - 1].tangent;
    }

    private List<Vector3> GetValidWaypointPositions()
    {
        List<Vector3> pts = new List<Vector3>();
        if (waypoints == null) return pts;

        foreach (Transform wp in waypoints)
        {
            if (wp != null)
            {
                pts.Add(wp.position);
            }
        }
        return pts;
    }

    private Vector3 GetCatmullRomPosition(List<Vector3> pts, int segmentIndex, float t)
    {
        int count = pts.Count;
        if (count < 2) return Vector3.zero;

        Vector3 p0 = pts[Mathf.Clamp(segmentIndex - 1, 0, count - 1)];
        Vector3 p1 = pts[segmentIndex];
        Vector3 p2 = pts[Mathf.Clamp(segmentIndex + 1, 0, count - 1)];
        Vector3 p3 = pts[Mathf.Clamp(segmentIndex + 2, 0, count - 1)];

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 GetCatmullRomTangent(List<Vector3> pts, int segmentIndex, float t)
    {
        int count = pts.Count;
        if (count < 2) return Vector3.forward;

        Vector3 p0 = pts[Mathf.Clamp(segmentIndex - 1, 0, count - 1)];
        Vector3 p1 = pts[segmentIndex];
        Vector3 p2 = pts[Mathf.Clamp(segmentIndex + 1, 0, count - 1)];
        Vector3 p3 = pts[Mathf.Clamp(segmentIndex + 2, 0, count - 1)];

        float t2 = t * t;

        Vector3 tangent = 0.5f * (
            (-p0 + p2) +
            2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
            3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2
        );

        return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
    }

    private enum AnimPlayType { Bool, Trigger, Int, DirectState, None }

    private AnimPlayType TryPlaySlideAnimation(Animator animator, string paramOrStateName, out int hash)
    {
        hash = 0;
        if (animator == null || string.IsNullOrEmpty(paramOrStateName)) return AnimPlayType.None;

        int targetHash = Animator.StringToHash(paramOrStateName);

        // 1. Chercher dans les paramètres de l'Animator (Bool, Trigger, Int)
        foreach (var p in animator.parameters)
        {
            if (p.nameHash == targetHash || string.Equals(p.name, paramOrStateName, System.StringComparison.OrdinalIgnoreCase))
            {
                hash = p.nameHash;
                if (p.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(p.nameHash, true);
                    return AnimPlayType.Bool;
                }
                else if (p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(p.nameHash);
                    return AnimPlayType.Trigger;
                }
                else if (p.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(p.nameHash, 1);
                    return AnimPlayType.Int;
                }
            }
        }

        // 2. Tenter de jouer directement l'état d'animation par son nom
        if (animator.HasState(0, targetHash))
        {
            hash = targetHash;
            animator.Play(targetHash, 0, 0f);
            return AnimPlayType.DirectState;
        }

        // 3. Fallback : essayer de jouer "jump" ou "isJumping" si le paramètre spécifié n'existe pas
        int jumpHash = Animator.StringToHash("jump");
        int isJumpingHash = Animator.StringToHash("isJumping");
        foreach (var p in animator.parameters)
        {
            if (p.nameHash == isJumpingHash && p.type == AnimatorControllerParameterType.Bool)
            {
                hash = isJumpingHash;
                animator.SetBool(isJumpingHash, true);
                return AnimPlayType.Bool;
            }
            else if (p.nameHash == jumpHash && p.type == AnimatorControllerParameterType.Trigger)
            {
                hash = jumpHash;
                animator.SetTrigger(jumpHash);
                return AnimPlayType.Trigger;
            }
        }

        // Si aucun n'est trouvé, afficher un message explicatif dans la console Unity avec la liste des paramètres
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var p in animator.parameters)
        {
            sb.Append($"'{p.name}', ");
        }
        Debug.LogWarning($"[RootSlide] Aucun paramètre ou état nommé '{paramOrStateName}' trouvé dans l'Animator du Joueur. Paramètres disponibles : [{sb.ToString().TrimEnd(',', ' ')}]. Tu peux saisir le nom exact de ton animation dans le champ 'Slide Anim Param' sur l'Inspecteur du script RootSlide.", this);

        return AnimPlayType.None;
    }

    private void StopSlideAnimation(Animator animator, AnimPlayType type, int hash)
    {
        if (animator == null || type == AnimPlayType.None) return;

        if (type == AnimPlayType.Bool)
        {
            animator.SetBool(hash, false);
        }
        else if (type == AnimPlayType.Int)
        {
            animator.SetInteger(hash, 0);
        }
    }

    private void OnDrawGizmos()
    {
        List<Vector3> pts = GetValidWaypointPositions();
        if (pts.Count < 2) return;

        Gizmos.color = pathColor;

        // Dessin du tracé continu de la spline
        const int samples = 50;
        Vector3 prevPos = pts[0];

        for (int i = 1; i <= samples; i++)
        {
            float dist = (totalSplineLength > 0f) ? ((float)i / samples) * totalSplineLength : (float)i / samples;
            GetSampleAtDistance(dist, out Vector3 pos, out _);
            Gizmos.DrawLine(prevPos, pos);
            prevPos = pos;
        }

        // Dessin des sphères sur les waypoints
        for (int i = 0; i < pts.Count; i++)
        {
            if (i == 0)
            {
                Gizmos.color = Color.green; // Départ
                Gizmos.DrawWireSphere(pts[i], startTriggerRadius);
                Gizmos.DrawSphere(pts[i], 0.35f);
            }
            else if (i == pts.Count - 1)
            {
                Gizmos.color = Color.red; // Arrivée / Éjection
                Gizmos.DrawSphere(pts[i], 0.35f);

                // Dessin d'un vecteur d'éjection
                GetSampleAtDistance(totalSplineLength, out Vector3 endPos, out Vector3 endTangent);
                Vector3 ejectionVector = endTangent.normalized * exitForwardImpulse + Vector3.up * exitUpwardImpulse;
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(endPos, ejectionVector * 0.5f);
            }
            else
            {
                Gizmos.color = Color.yellow; // Intermédiaire
                Gizmos.DrawSphere(pts[i], 0.25f);
            }
        }
    }
}
