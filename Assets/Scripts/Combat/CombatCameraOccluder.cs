using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Détecte et masque automatiquement TOUS les éléments du décor (plantes, nénuphars, arbres, murs, etc.)
/// qui recouvrent l'écran ou se trouvent entre la caméra et le joueur pendant le combat.
/// </summary>
[AddComponentMenu("2.5D RPG/Combat/Combat Camera Occluder")]
public class CombatCameraOccluder : MonoBehaviour
{
    public enum HideMode
    {
        DisableRenderer,  // Désactiver complètement le composant Renderer (100% invisible)
        ShadowsOnly,      // Rendre le décor invisible pour la caméra mais conserver son ombre au sol (pour les maillages 3D)
        AlphaFade         // Estomper la couleur alpha des matériaux
    }

    private class RendererState
    {
        public Renderer renderer;
        public UnityEngine.Rendering.ShadowCastingMode originalShadowCasting;
        public bool originalEnabled;
        public Dictionary<Material, Color> originalColors;
    }

    [Header("Configuration de l'Occlusion")]
    [Tooltip("Masque de calques pour les objets de décor pouvant bloquer la vue.")]
    [SerializeField] private LayerMask occlusionLayers = ~0;

    [Tooltip("Calques à exclure de l'occlusion (ces calques ne seront jamais masqués).")]
    [SerializeField] private LayerMask excludedLayers;

    [Tooltip("Rayon du couloir de détection physique (SphereCast) autour de la ligne de vue Caméra-Joueur.")]
    [SerializeField] private float occlusionRadius = 2.0f;

    [Tooltip("Distance en mètres en arrière de la caméra pour englober les objets collés à l'objectif.")]
    [SerializeField] private float cameraBackOffset = 3.0f;

    [Tooltip("Rayon de détection directe autour de la position physique de la caméra (pour les grandes plantes/feuilles collées).")]
    [SerializeField] private float cameraProximityRadius = 3f;

    [Tooltip("Marge d'écran Viewport autour du joueur (0.35 = 70% de la surface de l'écran autour du joueur).")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float viewportScreenMargin = 0.35f;

    [Tooltip("Décalage vertical (Y) sur le personnage pour cibler le buste/centre du corps.")]
    [SerializeField] private float targetYOffset = 0.8f;

    [Tooltip("Mode de masquage des obstacles (DisableRenderer recommandé pour une disparition totale).")]
    [SerializeField] private HideMode hideMode = HideMode.DisableRenderer;

    [Tooltip("Activer la détection par projection écran Viewport 2D (masque toute plante recouvrant le joueur sur l'image).")]
    [SerializeField] private bool useViewportCheck = true;

    [Tooltip("Si vrai, masque l'ensemble des Renderers du même objet parent (ex: tige + feuille complète du nénuphar).")]
    [SerializeField] private bool hideFullParentObject = true;

    [Tooltip("Si vrai, ignore les colliders configurés en mode Trigger en physique.")]
    [SerializeField] private bool ignoreTriggers = true;

    [Tooltip("Opacité visée lorsque HideMode est réglé sur AlphaFade (0 = transparent).")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeAlphaTarget = 0.2f;

    private Transform cameraTransform;
    private Transform playerTransform;
    private Transform bossTransform;
    private RadialCombatGrid combatGrid;
    private Camera cachedCamera;

    private Dictionary<Renderer, RendererState> trackedOcclusions = new Dictionary<Renderer, RendererState>();
    private List<Renderer> cachedSceneRenderers = new List<Renderer>();
    private float nextCacheScanTime = 0f;
    private bool isInitialized = false;

    /// <summary>
    /// Initialise le composant avec les références de la caméra, du joueur, du boss et de la grille.
    /// </summary>
    public void Initialize(Transform cam, Transform player, Transform boss, RadialCombatGrid grid, LayerMask layers, float radius, HideMode mode)
    {
        cameraTransform = cam;
        playerTransform = player;
        bossTransform = boss;
        combatGrid = grid;
        occlusionLayers = layers;

        // Retirer automatiquement le layer Bullet du masque de calques d'occlusion s'il existe
        int bulletLayer = LayerMask.NameToLayer("Bullet");
        if (bulletLayer != -1)
        {
            occlusionLayers &= ~(1 << bulletLayer);
        }

        occlusionRadius = radius;
        hideMode = mode;
        cachedCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : OcclusionCullingManager.MainCamera;
        isInitialized = true;

        RefreshSceneRenderersCache();
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void OnDestroy()
    {
        RestoreAll();
    }

    private void LateUpdate()
    {
        if (isInitialized)
        {
            UpdateOcclusion();
        }
    }

    /// <summary>
    /// Rafraîchit la liste des Renderers présents dans la scène pour la détection écran/géométrique.
    /// </summary>
    public void RefreshSceneRenderersCache()
    {
        cachedSceneRenderers.Clear();
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;
            if (IsIgnoredRenderer(rend)) continue;

            cachedSceneRenderers.Add(rend);
        }

        nextCacheScanTime = Time.time + 5.0f; // Re-scanner la scène régulièrement (toutes les 5 secondes)
    }

    private bool IsIgnoredRenderer(Renderer rend)
    {
        if (rend == null) return true;

        // Ignorer UI CanvasRenderer ou LineRenderer de la grille
        if (rend is CanvasRenderer || rend is LineRenderer) return true;

        GameObject go = rend.gameObject;

        // 0. Ignorer si le layer fait partie des calques exclus
        if (excludedLayers.value != 0 && (excludedLayers.value & (1 << go.layer)) != 0) return true;

        // 1. Ignorer le layer Bullet
        int bulletLayer = LayerMask.NameToLayer("Bullet");
        if (bulletLayer != -1 && go.layer == bulletLayer) return true;

        // 2. Ignorer les annonces de dégâts et les projectiles par composant
        if (go.GetComponent<DamageNumberPopup>() != null || go.GetComponentInParent<DamageNumberPopup>() != null) return true;
        if (go.GetComponent<InkProjectile>() != null || go.GetComponentInParent<InkProjectile>() != null) return true;
        if (go.GetComponent<FallingProjectile>() != null || go.GetComponentInParent<FallingProjectile>() != null) return true;

        // 3. Ignorer la cascade / waterfall (ne jamais masquer la cascade)
        if (go.GetComponent<CylindricalWaterfall>() != null || go.GetComponentInParent<CylindricalWaterfall>() != null || go.GetComponentInChildren<CylindricalWaterfall>() != null) return true;

        string goNameLower = go.name.ToLower();
        if (goNameLower.Contains("waterfall") || goNameLower.Contains("cascade") || goNameLower.Contains("cylindricalwaterfall")) return true;

        if (go.transform.parent != null)
        {
            string parentNameLower = go.transform.parent.name.ToLower();
            if (parentNameLower.Contains("waterfall") || parentNameLower.Contains("cascade") || parentNameLower.Contains("cylindricalwaterfall")) return true;
        }

        Transform t = rend.transform;

        // 3. Ne pas masquer le joueur ou ses enfants
        if (playerTransform != null && (t == playerTransform || t.IsChildOf(playerTransform))) return true;

        // 4. Ne pas masquer le boss ou ses enfants
        if (bossTransform != null && (t == bossTransform || t.IsChildOf(bossTransform))) return true;

        // 5. Ne pas masquer la grille polaire de combat
        if (combatGrid != null && (t == combatGrid.transform || t.IsChildOf(combatGrid.transform))) return true;

        // 6. Ne pas masquer la caméra elle-même
        if (cameraTransform != null && (t == cameraTransform || t.IsChildOf(cameraTransform))) return true;

        // 7. Ne pas masquer le sol
        if (IsGroundObject(rend)) return true;

        return false;
    }

    private bool IsGroundObject(Renderer rend)
    {
        if (rend == null) return false;

        GameObject go = rend.gameObject;
        int layer = go.layer;

        // Ignorer si le layer s'appelle Ground, Sol ou Terrain
        string layerName = LayerMask.LayerToName(layer);
        if (!string.IsNullOrEmpty(layerName))
        {
            string lLower = layerName.ToLower();
            if (lLower.Contains("ground") || lLower.Contains("sol") || lLower.Contains("terrain") || lLower.Contains("floor"))
            {
                return true;
            }
        }

        // Ignorer selon le nom du GameObject ou de son parent
        string nameLower = go.name.ToLower();
        if (nameLower.Contains("ground") || nameLower.Contains("sol") || nameLower.Contains("terrain") || nameLower.Contains("floor") || nameLower.Contains("plancher") || nameLower.Contains("water_base"))
        {
            return true;
        }

        if (go.transform.parent != null)
        {
            string parentNameLower = go.transform.parent.name.ToLower();
            if (parentNameLower.Contains("ground") || parentNameLower.Contains("sol") || parentNameLower.Contains("terrain") || parentNameLower.Contains("floor"))
            {
                return true;
            }
        }

        // Ignorer selon le Bounding Box : si l'objet est plat au sol (hauteur max <= Y de la grille + 0.35m)
        float groundReferenceY = combatGrid != null ? combatGrid.transform.position.y : 0f;
        if (rend.bounds.max.y <= (groundReferenceY + 0.35f))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Met à jour la détection d'occlusion et applique/restaure l'état des renderers.
    /// </summary>
    public void UpdateOcclusion()
    {
        if (!isInitialized || cameraTransform == null || playerTransform == null) return;

        if (cachedCamera == null && cameraTransform != null)
        {
            cachedCamera = cameraTransform.GetComponent<Camera>();
        }

        // Re-scanner périodiquement les renderers
        if (Time.time >= nextCacheScanTime)
        {
            RefreshSceneRenderersCache();
        }

        Vector3 camPos = cameraTransform.position;
        Vector3 targetPos = playerTransform.position + Vector3.up * targetYOffset;
        Vector3 camToPlayerDir = targetPos - camPos;
        float distanceCamToPlayer = camToPlayerDir.magnitude;

        if (distanceCamToPlayer < 0.1f) return;

        camToPlayerDir.Normalize();

        // Ligne de détection étendue en arrière de la caméra
        Vector3 startPos = camPos - camToPlayerDir * cameraBackOffset;
        Vector3 segmentVector = targetPos - startPos;
        float segmentLength = segmentVector.magnitude;

        HashSet<Renderer> newlyOccluded = new HashSet<Renderer>();

        // --- METHODE 1 : Physics SphereCastAll (pour les objets avec colliders) ---
        QueryTriggerInteraction triggerInteraction = ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;
        RaycastHit[] hits = Physics.SphereCastAll(startPos, occlusionRadius, camToPlayerDir, segmentLength, occlusionLayers, triggerInteraction);

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null) continue;

            AddTargetRenderers(hitTransform, newlyOccluded);
        }

        // --- METHODE 2 : Projection 2D Viewport & Bounding Box (Masque TOUTE plante recouvrant le joueur à l'écran) ---
        if (useViewportCheck && cachedCamera != null)
        {
            for (int i = 0; i < cachedSceneRenderers.Count; i++)
            {
                Renderer rend = cachedSceneRenderers[i];
                if (rend == null) continue;

                // Si le renderer est désactivé et non suivi, on passe
                if (!rend.enabled && !trackedOcclusions.ContainsKey(rend)) continue;

                if (IsRendererObscuringScreen(rend, cachedCamera, targetPos, camPos))
                {
                    AddTargetRenderers(rend.transform, newlyOccluded);
                }
            }
        }

        // --- 1. Appliquer le masquage sur les nouveaux renderers détectés ---
        foreach (Renderer rend in newlyOccluded)
        {
            if (rend == null) continue;

            if (!trackedOcclusions.TryGetValue(rend, out RendererState state))
            {
                state = new RendererState
                {
                    renderer = rend,
                    originalShadowCasting = rend.shadowCastingMode,
                    originalEnabled = rend.enabled,
                    originalColors = new Dictionary<Material, Color>()
                };

                if (hideMode == HideMode.AlphaFade && rend.sharedMaterials != null)
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat != null && mat.HasProperty("_Color"))
                        {
                            state.originalColors[mat] = mat.color;
                        }
                    }
                }

                trackedOcclusions.Add(rend, state);
            }

            ApplyHideMode(state);
        }

        // --- 2. Restaurer les renderers qui ne sont plus gênants ---
        List<Renderer> toRestore = new List<Renderer>();
        foreach (var kvp in trackedOcclusions)
        {
            if (!newlyOccluded.Contains(kvp.Key))
            {
                toRestore.Add(kvp.Key);
            }
        }

        for (int i = 0; i < toRestore.Count; i++)
        {
            Renderer rend = toRestore[i];
            if (trackedOcclusions.TryGetValue(rend, out RendererState state))
            {
                RestoreState(state);
                trackedOcclusions.Remove(rend);
            }
        }
    }

    private void AddTargetRenderers(Transform targetTransform, HashSet<Renderer> set)
    {
        if (targetTransform == null) return;

        Transform parentToUse = targetTransform;
        if (hideFullParentObject && targetTransform.parent != null && !IsIgnoredRenderer(targetTransform.parent.GetComponent<Renderer>()))
        {
            parentToUse = targetTransform.parent;
        }

        Renderer[] renderers = parentToUse.GetComponentsInChildren<Renderer>();
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            if (IsIgnoredRenderer(rend)) continue;

            set.Add(rend);
        }
    }

    private bool IsRendererObscuringScreen(Renderer rend, Camera cam, Vector3 playerTargetPos, Vector3 camPos)
    {
        Bounds bounds = rend.bounds;

        Vector3 playerCamSpace = cam.transform.InverseTransformPoint(playerTargetPos);
        Vector3 boundsCenterCamSpace = cam.transform.InverseTransformPoint(bounds.center);

        float boundsRadius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float objectMinZ = boundsCenterCamSpace.z - boundsRadius;
        float playerZ = playerCamSpace.z;

        // Si l'objet est nettement derrière le joueur en profondeur, il ne bloque pas la vue
        if (objectMinZ > playerZ + 1.0f) return false;

        // Si l'objet est loin derrière la caméra (z < -4.0m), ignorer
        if (boundsCenterCamSpace.z < -4.0f) return false;

        // 1. Détection de proximité directe à la caméra (plantes/feuilles collées à l'objectif)
        float distToCam = Vector3.Distance(bounds.center, camPos);
        if (distToCam <= (cameraProximityRadius + boundsRadius * 0.8f))
        {
            return true;
        }

        // 2. Projection Viewport 2D (écran)
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        Vector3[] corners = new Vector3[8]
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x,  e.y,  e.z)
        };

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        bool anyCornerInFront = false;

        for (int i = 0; i < 8; i++)
        {
            Vector3 vp = cam.WorldToViewportPoint(corners[i]);
            if (vp.z > -1.0f) anyCornerInFront = true;

            minX = Mathf.Min(minX, vp.x);
            maxX = Mathf.Max(maxX, vp.x);
            minY = Mathf.Min(minY, vp.y);
            maxY = Mathf.Max(maxY, vp.y);
        }

        if (!anyCornerInFront) return false;

        // Position du joueur à l'écran (Viewport 0..1)
        Vector3 playerVp = cam.WorldToViewportPoint(playerTargetPos);

        // Zone d'écran couverte autour du joueur
        float playerMinX = playerVp.x - viewportScreenMargin;
        float playerMaxX = playerVp.x + viewportScreenMargin;
        float playerMinY = playerVp.y - viewportScreenMargin;
        float playerMaxY = playerVp.y + viewportScreenMargin;

        bool overlapsX = maxX >= playerMinX && minX <= playerMaxX;
        bool overlapsY = maxY >= playerMinY && minY <= playerMaxY;

        if (overlapsX && overlapsY && boundsCenterCamSpace.z <= playerZ + 0.5f)
        {
            return true;
        }

        return false;
    }

    private void ApplyHideMode(RendererState state)
    {
        if (state.renderer == null) return;

        switch (hideMode)
        {
            case HideMode.DisableRenderer:
                state.renderer.enabled = false;
                break;

            case HideMode.ShadowsOnly:
                if (state.renderer is SpriteRenderer)
                {
                    state.renderer.enabled = false;
                }
                else
                {
                    state.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    state.renderer.enabled = true;
                }
                break;

            case HideMode.AlphaFade:
                if (state.originalColors != null)
                {
                    foreach (Material mat in state.renderer.materials)
                    {
                        if (mat != null && mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            c.a = fadeAlphaTarget;
                            mat.color = c;
                        }
                    }
                }
                break;
        }
    }

    private void RestoreState(RendererState state)
    {
        if (state.renderer == null) return;

        state.renderer.shadowCastingMode = state.originalShadowCasting;
        state.renderer.enabled = state.originalEnabled;

        if (state.originalColors != null && state.originalColors.Count > 0)
        {
            foreach (var kvp in state.originalColors)
            {
                if (kvp.Key != null && kvp.Key.HasProperty("_Color"))
                {
                    kvp.Key.color = kvp.Value;
                }
            }
        }
    }

    /// <summary>
    /// Restaure immédiatement tous les décors modifiés à leur état d'origine.
    /// </summary>
    public void RestoreAll()
    {
        foreach (var kvp in trackedOcclusions)
        {
            RestoreState(kvp.Value);
        }
        trackedOcclusions.Clear();
    }
}
