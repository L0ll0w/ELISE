using UnityEngine;

/// <summary>
/// Projette une ombre ronde et douce sur le sol sous le personnage.
/// S'adapte automatiquement à la hauteur, à la pente du sol et s'estompe lors des sauts.
/// </summary>
[AddComponentMenu("2.5D RPG/Character Shadow")]
public class CharacterShadow : MonoBehaviour
{
    public enum ShadowUpdateMode
    {
        Update,
        FixedUpdate,
        LateUpdate
    }

    [Header("Configuration de l'Ombre")]
    [Tooltip("Sprite à utiliser pour l'ombre. Si vide, une texture ronde et douce sera générée procéduralement.")]
    [SerializeField] private Sprite shadowSprite;
    
    [Tooltip("Taille de base de l'ombre.")]
    [SerializeField] private Vector2 baseSize = new Vector2(0.8f, 0.4f); // Légère ellipse horizontale pour un meilleur effet de perspective
    
    [Tooltip("Couleur et opacité maximale de l'ombre au sol.")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Projection & Alignement")]
    [Tooltip("Offset au-dessus du sol pour éviter le Z-fighting.")]
    [SerializeField] private float groundOffset = 0.02f;
    
    [Tooltip("Distance maximale du raycast pour projeter l'ombre.")]
    [SerializeField] private float maxRaycastDistance = 10f;
    
    [Tooltip("Masque de collision pour détecter le sol.")]
    [SerializeField] private LayerMask groundLayers = ~0; // Par défaut, tout sauf le joueur et les triggers (ajusté au démarrage)

    [Tooltip("Décalage de position de l'ombre (relatif à la rotation du joueur). Permet d'ajuster l'alignement visuel.")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Comportement Dynamique")]
    [Tooltip("Si coché, l'ombre s'incline pour épouser la pente du sol.")]
    [SerializeField] private bool alignWithGroundNormal = true;
    
    [Tooltip("Hauteur à partir de laquelle l'ombre s'estompe complètement.")]
    [SerializeField] private float fadeHeight = 3f;
    
    [Tooltip("Facteur de réduction de taille à la hauteur maximale de saut.")]
    [Range(0f, 1f)]
    [SerializeField] private float minScaleMultiplier = 0.5f;

    [Tooltip("Vitesse de lissage vertical et angulaire (0 = instantané).")]
    [SerializeField] private float smoothSpeed = 0f;

    [Tooltip("Boucle de mise à jour pour repositionner l'ombre. Utilisez FixedUpdate si le joueur saccade en physique, ou LateUpdate si vous utilisez Cinemachine.")]
    [SerializeField] private ShadowUpdateMode updateMode = ShadowUpdateMode.LateUpdate;

    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;
    private Collider playerCollider;

    private void Start()
    {
        playerCollider = GetComponent<Collider>();
        
        // Ignore Raycast layer
        groundLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));

        CreateShadowObject();
    }

    private void CreateShadowObject()
    {
        // Création du GameObject enfant
        shadowObject = new GameObject("PlayerBlobShadow");
        shadowObject.transform.SetParent(transform);
        shadowObject.transform.localPosition = Vector3.zero;
        shadowObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        
        // Assigner le sprite (ou en générer un)
        if (shadowSprite == null)
        {
            shadowSprite = GenerateSoftCircleSprite();
        }
        shadowRenderer.sprite = shadowSprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingOrder = -1; // S'assurer que c'est rendu derrière ou sous le joueur
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
    }

    private void Update()
    {
        if (updateMode == ShadowUpdateMode.Update)
        {
            UpdateShadow();
        }
    }

    private void FixedUpdate()
    {
        if (updateMode == ShadowUpdateMode.FixedUpdate)
        {
            UpdateShadow();
        }
    }

    private void LateUpdate()
    {
        if (updateMode == ShadowUpdateMode.LateUpdate)
        {
            UpdateShadow();
        }
    }

    private void UpdateShadow()
    {
        if (shadowObject == null || shadowRenderer == null) return;

        // Lancer un rayon depuis le centre du collider (ou le pivot) pour s'assurer de partir du dessus du sol
        Vector3 origin = transform.position;
        if (playerCollider != null)
        {
            origin = playerCollider.bounds.center;
        }

        // Ligne d'aide au débogage visible dans la vue Scène de Unity
        Debug.DrawRay(origin, Vector3.down * maxRaycastDistance, Color.red);

        Ray ray = new Ray(origin, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRaycastDistance, groundLayers);
        
        RaycastHit closestHit = default;
        bool foundValidHit = false;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // Ignorer si c'est le collider du joueur ou un de ses enfants
            if (hit.collider == playerCollider || (playerCollider != null && hit.collider.transform.IsChildOf(transform)))
            {
                continue;
            }

            // Ignorer les triggers
            if (hit.collider.isTrigger)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundValidHit = true;
            }
        }

        if (foundValidHit)
        {
            shadowRenderer.enabled = true;

            // Calculer la position cible du sol en coordonnées monde
            Vector3 targetPosition = closestHit.point + closestHit.normal * groundOffset;

            // Position horizontale instantanée (Zéro retard)
            Vector3 playerPos = transform.position;
            Vector3 playerOffset = transform.rotation * positionOffset;
            float targetY = targetPosition.y;
            
            float finalY;
            if (smoothSpeed > 0f && Application.isPlaying)
            {
                // Lissage vertical uniquement pour adoucir le passage sur des marches ou bosses
                finalY = Mathf.Lerp(shadowObject.transform.position.y, targetY, Time.deltaTime * smoothSpeed);
                
                // Sécurité : l'ombre ne doit jamais descendre sous le niveau du sol détecté
                if (finalY < targetY)
                {
                    finalY = targetY;
                }
            }
            else
            {
                finalY = targetY;
            }

            // Assigner la position finale (X/Z instantanés du joueur, Y projeté au sol)
            shadowObject.transform.position = new Vector3(playerPos.x + playerOffset.x, finalY, playerPos.z + playerOffset.z);

            // Déterminer la rotation cible (orientée selon la rotation Y du joueur pour que l'ellipse de l'ombre reste alignée)
            float rotationY = transform.eulerAngles.y;
            Quaternion targetRotation;
            if (alignWithGroundNormal)
            {
                // Oriente le plan selon la normale du sol tout en respectant l'orientation Y du joueur
                targetRotation = Quaternion.FromToRotation(Vector3.up, closestHit.normal) * Quaternion.Euler(90f, rotationY, 0f);
            }
            else
            {
                // Reste à plat horizontalement mais orientée avec le joueur
                targetRotation = Quaternion.Euler(90f, rotationY, 0f);
            }

            if (smoothSpeed > 0f && Application.isPlaying)
            {
                shadowObject.transform.rotation = Quaternion.Slerp(shadowObject.transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
            else
            {
                shadowObject.transform.rotation = targetRotation;
            }

            // Calculer l'estompement en hauteur (par rapport au bas du collider)
            float colliderBottomY = playerCollider != null ? playerCollider.bounds.min.y : transform.position.y;
            float distanceToGround = colliderBottomY - closestHit.point.y;
            float t = Mathf.Clamp01(distanceToGround / fadeHeight);

            // Ajuster l'opacité
            Color targetColor = shadowColor;
            targetColor.a = Mathf.Lerp(shadowColor.a, 0f, t);
            shadowRenderer.color = targetColor;

            // Ajuster la taille en neutralisant le scale du parent (évite de déformer l'ombre avec du squash/stretch sur le joueur)
            float scaleMultiplier = Mathf.Lerp(1f, minScaleMultiplier, t);
            Vector3 parentScale = transform.lossyScale;
            float targetWorldScaleX = baseSize.x * scaleMultiplier;
            float targetWorldScaleY = baseSize.y * scaleMultiplier;
            
            float localScaleX = parentScale.x != 0f ? targetWorldScaleX / parentScale.x : targetWorldScaleX;
            float localScaleY = parentScale.y != 0f ? targetWorldScaleY / parentScale.y : targetWorldScaleY;
            
            shadowObject.transform.localScale = new Vector3(localScaleX, localScaleY, 1f);
        }
        else
        {
            // Pas de sol en dessous -> cacher l'ombre
            shadowRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Génère une texture de cercle flou et doux et l'encapsule dans un Sprite.
    /// Évite d'avoir à importer un fichier image externe.
    /// </summary>
    private Sprite GenerateSoftCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        
        float center = size / 2f;
        float maxDistance = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                float t = distance / maxDistance;
                float alpha = 0f;
                
                if (t < 1f)
                {
                    // Lissage au carré pour un fondu très soft et progressif vers les bords
                    alpha = Mathf.SmoothStep(1f, 0f, t);
                    alpha *= alpha;
                }
                
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha); // Blanc (pour pouvoir teinter en noir ou autre via SpriteRenderer)
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
