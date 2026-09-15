using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestionnaire centralisé d'Occlusion Culling, Distance Culling et de mise en cache de la caméra.
/// Optimise drastiquement le framerate (CPU/GPU) en désactivant le rendu des objets hors écran ou éloignés.
/// </summary>
[DefaultExecutionOrder(-1000)]
[AddComponentMenu("ELISE/Optimization/Occlusion Culling Manager")]
public class OcclusionCullingManager : MonoBehaviour
{
    public static OcclusionCullingManager Instance { get; private set; }

    [Header("Cache Caméra")]
    [SerializeField] private Camera mainCamera;

    [Header("Distance Culling Natif (Layer Culling C++)")]
    [Tooltip("Activer le culling natif par calque sur la caméra principale.")]
    [SerializeField] private bool enableLayerCulling = true;

    [Tooltip("Distance de culling par défaut pour le calque Default (en mètres).")]
    [SerializeField] private float defaultLayerCullDistance = 80f;

    [Tooltip("Distance de culling pour la végétation / nénuphars (Layer 20 WaterLilly).")]
    [SerializeField] private float waterLillyCullDistance = 45f;

    [Header("Calques Exclus (Ne JAMAIS masquer)")]
    [Tooltip("Masque de calques qui ne doivent JAMAIS être masqués par l'occlusion ou la distance.")]
    [SerializeField] private LayerMask excludedLayers;

    [Header("Frustum & Distance Culling Dynamique (C# Batch)")]
    [Tooltip("Activer le culling dynamique par cône de vue et distance sur les objets enregistrés.")]
    [SerializeField] private bool enableDynamicCulling = true;

    [Tooltip("Distance maximale globale d'affichage des objets Cullable (en mètres).")]
    [SerializeField] private float maxCullDistance = 60f;

    [Tooltip("Nombre d'objets vérifiés par frame (batching) pour éviter tout ralentissement.")]
    [SerializeField] private int objectsPerFrame = 40;

    private Plane[] frustumPlanes;
    private List<CullableObject> registeredCullables = new List<CullableObject>();
    private int currentBatchIndex = 0;
    private float nextFrustumUpdate = 0f;

    /// <summary>
    /// Accès rapide et optimisé à la caméra principale sans repasser par Camera.main.
    /// </summary>
    public static Camera MainCamera
    {
        get
        {
            if (Instance != null && Instance.mainCamera != null)
            {
                return Instance.mainCamera;
            }
            return Camera.main;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        SetupLayerCulling();
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        SetupLayerCulling();
    }

    private void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        SetupLayerCulling();
    }

    /// <summary>
    /// Configure le Layer Culling natif d'Unity (exécuté en C++ dans le moteur de rendu).
    /// </summary>
    public void SetupLayerCulling()
    {
        if (!enableLayerCulling || mainCamera == null) return;

        float[] distances = new float[32];

        // Remplir la distance par défaut pour tous les calques sauf les calques exclus
        for (int i = 0; i < 32; i++)
        {
            if ((excludedLayers.value & (1 << i)) != 0)
            {
                distances[i] = 0f; // 0f dans Unity = Ne JAMAIS masquer ce calque par distance
            }
            else
            {
                distances[i] = defaultLayerCullDistance;
            }
        }

        // Calques spécifiques
        int waterLillyLayer = LayerMask.NameToLayer("WaterLilly");
        if (waterLillyLayer != -1)
        {
            distances[waterLillyLayer] = waterLillyCullDistance;
        }

        // Appliquer à la caméra principale
        mainCamera.layerCullDistances = distances;
        mainCamera.layerCullSpherical = true;
    }

    /// <summary>
    /// Enregistre un objet Cullable auprès du gestionnaire d'occlusion.
    /// </summary>
    public void RegisterCullable(CullableObject cullable)
    {
        if (cullable != null && !registeredCullables.Contains(cullable))
        {
            registeredCullables.Add(cullable);
        }
    }

    /// <summary>
    /// Désenregistre un objet Cullable lorsqu'il est détruit ou désactivé.
    /// </summary>
    public void UnregisterCullable(CullableObject cullable)
    {
        if (cullable != null)
        {
            registeredCullables.Remove(cullable);
        }
    }

    private void Update()
    {
        if (!enableDynamicCulling || registeredCullables.Count == 0 || mainCamera == null) return;

        // Mettre à jour les plans de frustum de la caméra toutes les quelques frames
        if (Time.time >= nextFrustumUpdate)
        {
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            nextFrustumUpdate = Time.time + 0.05f; // ~20 updates par seconde
        }

        if (frustumPlanes == null) return;

        Vector3 camPos = mainCamera.transform.position;
        int count = registeredCullables.Count;
        int processedThisFrame = 0;

        while (processedThisFrame < objectsPerFrame && count > 0)
        {
            if (currentBatchIndex >= registeredCullables.Count)
            {
                currentBatchIndex = 0;
            }

            CullableObject obj = registeredCullables[currentBatchIndex];
            if (obj != null && obj.ObjectRenderer != null)
            {
                float distSqr = (obj.transform.position - camPos).sqrMagnitude;
                float effectiveMaxDist = obj.CustomMaxDistance > 0 ? obj.CustomMaxDistance : maxCullDistance;
                float maxDistSqr = effectiveMaxDist * effectiveMaxDist;

                bool shouldBeVisible = true;

                // 0. Vérifier si le calque est exclu
                if ((excludedLayers.value & (1 << obj.gameObject.layer)) != 0)
                {
                    shouldBeVisible = true;
                }
                // 1. Test de Distance
                else if (distSqr > maxDistSqr)
                {
                    shouldBeVisible = false;
                }
                // 2. Test de Frustum (si dans la limite de distance)
                else if (obj.EnableFrustumCulling)
                {
                    shouldBeVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, obj.ObjectRenderer.bounds);
                }

                obj.SetVisibility(shouldBeVisible);
            }
            else if (obj == null)
            {
                registeredCullables.RemoveAt(currentBatchIndex);
                count--;
                continue;
            }

            currentBatchIndex++;
            processedThisFrame++;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
