using UnityEngine;

/// <summary>
/// Détecte le terrain et l'entoure de nuages de manière procédurale.
/// Bloque également le joueur à la limite définie pour l'empêcher de traverser.
/// </summary>
[AddComponentMenu("2.5D RPG/Cloud Boundary Manager")]
public class CloudBoundaryManager : MonoBehaviour
{
    public enum BoundaryShape
    {
        Rectangle,
        Circle
    }

    [Header("Forme et Cible")]
    [Tooltip("Forme de la délimitation (Rectangle calqué sur le terrain ou Cercle centré).")]
    [SerializeField] private BoundaryShape shape = BoundaryShape.Rectangle;
    
    [Tooltip("Détecter automatiquement le terrain actif de la scène.")]
    [SerializeField] private bool autoDetectTerrain = true;
    
    [Tooltip("Terrain cible (laissé vide si détection auto).")]
    [SerializeField] private Terrain targetTerrain;

    [Header("Paramètres Manuels (Si pas de terrain)")]
    [SerializeField] private Vector3 customCenter = Vector3.zero;
    [SerializeField] private Vector3 customSize = new Vector3(100f, 0f, 100f);
    
    [Header("Génération des Nuages")]
    [Tooltip("Le préfabriqué de nuage à instancier (idéalement une sphère ou un mesh de nuage avec le shader Dreamcore).")]
    [SerializeField] private GameObject cloudPrefab;
    
    [Tooltip("Distance moyenne entre chaque nuage le long de la frontière.")]
    [SerializeField] private float cloudSpacing = 6f;
    
    [Tooltip("Décalage vers l'extérieur par rapport à la bordure du terrain.")]
    [SerializeField] private float boundaryOffset = 5f;
    
    [Tooltip("Hauteur des nuages par rapport au sol.")]
    [SerializeField] private float heightOffset = 2f;
    
    [Tooltip("Variation aléatoire de la hauteur pour donner du naturel.")]
    [SerializeField] private float heightVariance = 1.5f;

    [Header("Mur de Nuages (Hauteur)")]
    [Tooltip("Nombre de rangées de nuages empilées en hauteur pour former un mur.")]
    [SerializeField] private int wallLayers = 3;

    [Tooltip("Espace vertical entre chaque rangée de nuages.")]
    [SerializeField] private float verticalSpacing = 4f;

    [Tooltip("Décalage horizontal des rangées alternées pour combler les trous (effet brique).")]
    [SerializeField] private bool staggerLayers = true;

    [Header("Variations des Nuages")]
    [Tooltip("Activer la rotation aléatoire (axe Y à 360° et légère inclinaison X/Z) pour briser la répétition.")]
    [SerializeField] private bool randomRotation = true;

    [Range(0.5f, 100f)] [SerializeField] private float scaleMin = 8f;
    [Range(0.5f, 100f)] [SerializeField] private float scaleMax = 15f;

    [Header("Contraintes du Joueur")]
    [Tooltip("Empêcher activement le joueur de traverser la limite.")]
    [SerializeField] private bool enforcePlayerBoundary = true;
    
    [Tooltip("Distance de sécurité (marge) entre la barrière de nuages et la zone limite du joueur.")]
    [SerializeField] private float playerBuffer = 3f;
    
    [Tooltip("Le transform du joueur à bloquer. Si vide, cherche le composant PlayerMovement.")]
    [SerializeField] private Transform playerTransform;

    // Variables de calcul interne
    private Vector3 center;
    private Vector3 size;
    private float circleRadius;
    private bool initialized = false;

    private void Start()
    {
        InitializeBoundary();
        SpawnClouds();
    }

    private void LateUpdate()
    {
        if (enforcePlayerBoundary && initialized)
        {
            ClampPlayerPosition();
        }
    }

    /// <summary>
    /// Initialise les limites de la frontière en fonction du terrain ou des paramètres manuels.
    /// </summary>
    private void InitializeBoundary()
    {
        if (autoDetectTerrain && targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
            if (targetTerrain == null)
            {
                targetTerrain = FindFirstObjectByType<Terrain>();
            }
        }

        if (targetTerrain != null)
        {
            Vector3 terrainPos = targetTerrain.transform.position;
            Vector3 terrainSize = targetTerrain.terrainData.size;
            
            center = terrainPos + new Vector3(terrainSize.x * 0.5f, 0f, terrainSize.z * 0.5f);
            size = terrainSize;
        }
        else
        {
            center = customCenter;
            size = customSize;
            Debug.LogWarning("Aucun Terrain détecté. Utilisation des paramètres de taille manuels.", this);
        }

        // Si c'est un cercle, le rayon est la moitié de la plus grande dimension
        circleRadius = Mathf.Max(size.x, size.z) * 0.5f + boundaryOffset;
        initialized = true;
    }

    /// <summary>
    /// Spawne les nuages tout autour du terrain.
    /// </summary>
    [ContextMenu("Générer les nuages")]
    public void SpawnClouds()
    {
        if (cloudPrefab == null)
        {
            Debug.LogError("Veuillez assigner un 'Cloud Prefab' sur le script CloudBoundaryManager.", this);
            return;
        }

        // Nettoyage des anciens nuages éventuellement générés en mode édition
        ClearExistingClouds();

        if (!initialized)
        {
            InitializeBoundary();
        }

        if (shape == BoundaryShape.Rectangle)
        {
            SpawnRectangularBoundary();
        }
        else
        {
            SpawnCircularBoundary();
        }
    }

    private void SpawnRectangularBoundary()
    {
        float minX = center.x - (size.x * 0.5f) - boundaryOffset;
        float maxX = center.x + (size.x * 0.5f) + boundaryOffset;
        float minZ = center.z - (size.z * 0.5f) - boundaryOffset;
        float maxZ = center.z + (size.z * 0.5f) + boundaryOffset;

        float width = maxX - minX;
        float length = maxZ - minZ;
        float perimeter = 2f * (width + length);
        int numClouds = Mathf.FloorToInt(perimeter / cloudSpacing);

        for (int layer = 0; layer < wallLayers; layer++)
        {
            float layerHeightOffset = layer * verticalSpacing;
            float layerStagger = (staggerLayers && layer % 2 != 0) ? (cloudSpacing * 0.5f) : 0f;

            for (int i = 0; i < numClouds; i++)
            {
                float currentDist = i * cloudSpacing + layerStagger;
                Vector3 spawnPos = GetPointOnRectPerimeter(minX, minZ, maxX, maxZ, currentDist);
                
                // Calcul de la hauteur avec le terrain
                float groundHeight = GetHeightAtPosition(spawnPos);
                spawnPos.y = groundHeight + heightOffset + layerHeightOffset + Random.Range(-heightVariance * 0.5f, heightVariance * 0.5f);

                InstantiateCloudInstance(spawnPos);
            }
        }
    }

    private void SpawnCircularBoundary()
    {
        float perimeter = 2f * Mathf.PI * circleRadius;
        int numClouds = Mathf.FloorToInt(perimeter / cloudSpacing);

        for (int layer = 0; layer < wallLayers; layer++)
        {
            float layerHeightOffset = layer * verticalSpacing;
            float angleOffset = (staggerLayers && layer % 2 != 0) ? (Mathf.PI / numClouds) : 0f;

            for (int i = 0; i < numClouds; i++)
            {
                float angle = ((i * 2f * Mathf.PI) / numClouds) + angleOffset;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * circleRadius;
                Vector3 spawnPos = center + offset;

                // Calcul de la hauteur avec le terrain
                float groundHeight = GetHeightAtPosition(spawnPos);
                spawnPos.y = groundHeight + heightOffset + layerHeightOffset + Random.Range(-heightVariance * 0.5f, heightVariance * 0.5f);

                InstantiateCloudInstance(spawnPos);
            }
        }
    }

    private void InstantiateCloudInstance(Vector3 position)
    {
        // On instancie avec la rotation d'origine du prefab
        Quaternion baseRotation = cloudPrefab.transform.rotation;
        GameObject cloud = Instantiate(cloudPrefab, position, baseRotation, transform);
        
        // Si la rotation aléatoire est cochée, on conserve l'axe X d'origine et on fait varier Y/Z
        if (randomRotation)
        {
            float prefabX = baseRotation.eulerAngles.x;
            float randomY = Random.Range(0f, 360f);
            float randomZ = baseRotation.eulerAngles.z + Random.Range(-5f, 5f);
            cloud.transform.rotation = Quaternion.Euler(prefabX, randomY, randomZ);
        }
        
        float scale = Random.Range(scaleMin, scaleMax);
        cloud.transform.localScale = new Vector3(scale, scale * Random.Range(0.8f, 1.2f), scale);
    }

    private Vector3 GetPointOnRectPerimeter(float minX, float minZ, float maxX, float maxZ, float distance)
    {
        float width = maxX - minX;
        float length = maxZ - minZ;

        // Côté bas (de gauche à droite)
        if (distance < width)
        {
            return new Vector3(minX + distance, 0f, minZ);
        }
        distance -= width;

        // Côté droit (du bas vers le haut)
        if (distance < length)
        {
            return new Vector3(maxX, 0f, minZ + distance);
        }
        distance -= length;

        // Côté haut (de droite à gauche)
        if (distance < width)
        {
            return new Vector3(maxX - distance, 0f, maxZ);
        }
        distance -= width;

        // Côté gauche (du haut vers le bas)
        return new Vector3(minX, 0f, maxZ - distance);
    }

    private float GetHeightAtPosition(Vector3 position)
    {
        if (targetTerrain != null)
        {
            return targetTerrain.SampleHeight(position) + targetTerrain.transform.position.y;
        }
        return center.y;
    }

    private void ClearExistingClouds()
    {
        // Supprime les enfants pour pouvoir régénérer proprement
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Force le joueur à rester à l'intérieur des limites de la frontière.
    /// </summary>
    private void ClampPlayerPosition()
    {
        if (playerTransform == null)
        {
            // Tente de trouver le joueur
            PlayerMovement pMove = FindFirstObjectByType<PlayerMovement>();
            if (pMove != null)
            {
                playerTransform = pMove.transform;
            }
            else
            {
                return; // Impossible de trouver le joueur
            }
        }

        Vector3 playerPos = playerTransform.position;

        if (shape == BoundaryShape.Rectangle)
        {
            float minX = center.x - (size.x * 0.5f) - boundaryOffset + playerBuffer;
            float maxX_ = center.x + (size.x * 0.5f) + boundaryOffset - playerBuffer;
            float minZ = center.z - (size.z * 0.5f) - boundaryOffset + playerBuffer;
            float maxZ_ = center.z + (size.z * 0.5f) + boundaryOffset - playerBuffer;

            float clampedX = Mathf.Clamp(playerPos.x, minX, maxX_);
            float clampedZ = Mathf.Clamp(playerPos.z, minZ, maxZ_);

            if (clampedX != playerPos.x || clampedZ != playerPos.z)
            {
                playerTransform.position = new Vector3(clampedX, playerPos.y, clampedZ);
            }
        }
        else // Circle
        {
            Vector3 flatPlayerPos = new Vector3(playerPos.x, 0f, playerPos.z);
            Vector3 flatCenter = new Vector3(center.x, 0f, center.z);
            float dist = Vector3.Distance(flatPlayerPos, flatCenter);
            float maxRadius = circleRadius - playerBuffer;

            if (dist > maxRadius)
            {
                Vector3 dir = (flatPlayerPos - flatCenter).normalized;
                Vector3 clampedFlat = flatCenter + dir * maxRadius;
                playerTransform.position = new Vector3(clampedFlat.x, playerPos.y, clampedFlat.z);
            }
        }
    }



    private void OnDrawGizmosSelected()
    {
        // Dessine la frontière de sécurité en vert dans la scène pour réglage facile
        if (!initialized)
        {
            InitializeBoundary();
        }

        Gizmos.color = Color.green;
        if (shape == BoundaryShape.Rectangle)
        {
            Vector3 gizmoSize = new Vector3(size.x + (boundaryOffset * 2f), 5f, size.z + (boundaryOffset * 2f));
            Gizmos.DrawWireCube(new Vector3(center.x, center.y + heightOffset, center.z), gizmoSize);
            
            // Zone limite du joueur
            Gizmos.color = Color.red;
            Vector3 playerGizmoSize = new Vector3(gizmoSize.x - (playerBuffer * 2f), 4.8f, gizmoSize.z - (playerBuffer * 2f));
            Gizmos.DrawWireCube(new Vector3(center.x, center.y + heightOffset, center.z), playerGizmoSize);
        }
        else
        {
            Gizmos.DrawWireSphere(new Vector3(center.x, center.y + heightOffset, center.z), circleRadius);
            
            // Zone limite du joueur
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(center.x, center.y + heightOffset, center.z), circleRadius - playerBuffer);
        }
    }
}
