using UnityEngine;

/// <summary>
/// Composant d'occlusion à placer sur n'importe quel objet du décor (plantes, nénuphars, rochers, éléments 2.5D).
/// S'enregistre automatiquement auprès du OcclusionCullingManager pour être masqué lorsqu'il est hors écran ou trop loin.
/// </summary>
[AddComponentMenu("ELISE/Optimization/Cullable Object")]
public class CullableObject : MonoBehaviour
{
    [Header("Paramètres du Culling")]
    [Tooltip("Distance maximale personnalisée d'affichage en mètres (0 = utilise la valeur globale du manager).")]
    [SerializeField] private float customMaxDistance = 0f;

    [Tooltip("Activer le test de cône de vue (Frustum Culling) pour cet objet.")]
    [SerializeField] private bool enableFrustumCulling = true;

    [Tooltip("Si coché, cet objet spécifique ne sera JAMAIS masqué par le culling.")]
    [SerializeField] private bool neverCull = false;

    private Renderer objectRenderer;
    private bool isCurrentlyVisible = true;

    public Renderer ObjectRenderer => objectRenderer;
    public float CustomMaxDistance => customMaxDistance;
    public bool EnableFrustumCulling => enableFrustumCulling;
    public bool NeverCull => neverCull;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void OnEnable()
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer == null)
            {
                objectRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (OcclusionCullingManager.Instance != null)
        {
            OcclusionCullingManager.Instance.RegisterCullable(this);
        }
    }

    private void Start()
    {
        if (OcclusionCullingManager.Instance != null)
        {
            OcclusionCullingManager.Instance.RegisterCullable(this);
        }
    }

    private void OnDisable()
    {
        if (OcclusionCullingManager.Instance != null)
        {
            OcclusionCullingManager.Instance.UnregisterCullable(this);
        }

        // Réactiver le renderer lorsque l'objet est désactivé/réactivé pour éviter de le bloquer
        if (objectRenderer != null && !isCurrentlyVisible)
        {
            objectRenderer.enabled = true;
            isCurrentlyVisible = true;
        }
    }

    /// <summary>
    /// Modifie l'état d'activation du Renderer selon sa visibilité déterminée par le manager.
    /// </summary>
    public void SetVisibility(bool visible)
    {
        if (neverCull) visible = true;

        if (isCurrentlyVisible == visible) return;

        isCurrentlyVisible = visible;
        if (objectRenderer != null)
        {
            objectRenderer.enabled = visible;
        }
    }
}
