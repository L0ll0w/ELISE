using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrôleur centralisé qui gère l'animation des pleurs d'encre pour un groupe de statues.
/// Toutes les statues pleurent dans une unique flaque partagée au sol qui grandit à chaque impact de goutte.
/// </summary>
[AddComponentMenu("2.5D RPG/Statue Ink Controller")]
public class StatueInkController : MonoBehaviour
{
    [System.Serializable]
    public class StatueConfig
    {
        public string name = "Statue 1";

        [Header("Renderer Statue")]
        [Tooltip("Le renderer de la statue.")]
        public Renderer statueRenderer;
        public int statueMaterialSlot = 0;

        [Header("Repères de chute")]
        [Tooltip("Point de départ de la larme (ex: hauteur des yeux ou pieds).")]
        public Transform dropStartPoint;
        [Tooltip("Point d'impact au sol (ex: centre de la flaque).")]
        public Transform dropEndPoint;

        [Tooltip("Optionnel : Spécifiez un objet de goutte unique pour cette statue. Si vide, utilise le prefab global.")]
        public GameObject customInkDropObject;

        [Header("Hauteurs locales d'encre")]
        [Tooltip("Hauteur locale Y de début (les yeux) de la statue.")]
        public float eyeY = 2.8f;
        [Tooltip("Hauteur locale Y de fin (les pieds) de la statue.")]
        public float feetY = 0.0f;

        [HideInInspector] public GameObject runtimeInkDropObject;
        [HideInInspector] public MaterialPropertyBlock statuePropBlock;
    }

    [Header("Liste des Statues")]
    [SerializeField] private List<StatueConfig> statues = new List<StatueConfig>();

    [Header("Configuration Unique de la Flaque")]
    [Tooltip("Le renderer unique de la flaque partagée par toutes les statues.")]
    [SerializeField] private Renderer sharedPuddleRenderer;
    [SerializeField] private int sharedPuddleMaterialSlot = 0;

    [Tooltip("Taille initiale de la flaque au démarrage du jeu.")]
    [Range(0f, 1f)]
    [SerializeField] private float startingPuddleSize = 0.05f;

    [Tooltip("De combien la flaque grandit à chaque fois qu'une goutte tombe au sol.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float puddleGrowthPerDrop = 0.05f;

    [Tooltip("Taille maximale que la flaque peut atteindre, peu importe le nombre de gouttes.")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float maxPuddleSizeLimit = 1.5f;

    [Tooltip("Vitesse d'agrandissement de la flaque lors de l'impact d'une goutte.")]
    [SerializeField] private float puddleGrowthSpeed = 0.5f;

    [Header("Événement de Spawn (Taille Max Flaque)")]
    [Tooltip("Optionnel : Prefab du joueur/objet à instancier quand la flaque atteint sa taille max.")]
    [SerializeField] private GameObject spawnObjectPrefab;

    [Tooltip("Point de spawn du prefab.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("L'Animator de l'objet à faire spawner. Si vide, sera recherché sur le joueur ou l'instance.")]
    [SerializeField] private Animator spawnObjectAnimator;

    [Tooltip("Nom de l'animation d'attente au début de la cinématique (ex: PlayerSpawnIdle).")]
    [SerializeField] private string initialIdleAnimationName = "PlayerSpawnIdle";

    [Tooltip("Le nom du Trigger ou de l'état d'animation de spawn (ex: PlayerSpawn).")]
    [SerializeField] private string spawnAnimationName = "PlayerSpawn";

    [Tooltip("Événement personnalisé déclenché quand la flaque atteint sa taille maximale.")]
    public UnityEngine.Events.UnityEvent onPuddleMaxSizeReached;

    [Header("Configuration Globale des Gouttes")]
    [Tooltip("Le prefab à instancier automatiquement pour chaque goutte (ex: une petite sphère noire).")]
    [SerializeField] private GameObject inkDropPrefab;

    [Tooltip("Le prefab du Particle System pour l'éclaboussure d'encre au sol lors de l'impact.")]
    [SerializeField] private ParticleSystem splashParticlePrefab;

    [Tooltip("Vitesse de chute de la goutte (en mètres par seconde).")]
    [SerializeField] private float dropFallSpeed = 6.0f;

    [Tooltip("Temps d'attente minimal au pied de la statue avant que la goutte ne se détache.")]
    [SerializeField] private float minDropDelay = 0.4f;

    [Tooltip("Temps d'attente maximal au pied de la statue avant que la goutte ne se détache.")]
    [SerializeField] private float maxDropDelay = 1.2f;

    [Header("Configuration Globale de l'Écoulement")]
    [Tooltip("Vitesse minimale de l'écoulement des larmes.")]
    [Range(0.05f, 2f)]
    [SerializeField] private float minInkSpeed = 0.15f;

    [Tooltip("Vitesse maximale de l'écoulement des larmes.")]
    [Range(0.05f, 2f)]
    [SerializeField] private float maxInkSpeed = 0.35f;

    [Tooltip("Vitesse de disparition/séchage de la larme sur la statue après l'impact.")]
    [SerializeField] private float tearFadeSpeed = 0.5f;

    private static readonly int InkProgressId = Shader.PropertyToID("_InkProgress");
    private static readonly int InkTrailProgressId = Shader.PropertyToID("_InkTrailProgress");
    private static readonly int PuddleSizeId = Shader.PropertyToID("_PuddleSize");
    private static readonly int EyeYId = Shader.PropertyToID("_EyeY");
    private static readonly int FeetYId = Shader.PropertyToID("_FeetY");

    private MaterialPropertyBlock puddlePropBlock;
    private float targetPuddleSize;
    private float currentVisualPuddleSize;
    private bool hasTriggeredMaxSizeEvent = false;

    private void Start()
    {
        Debug.Log($"[StatueInkController] Start() exécuté sur '{gameObject.name}'. Nombre de statues configurées : {statues.Count}");
        
        puddlePropBlock = new MaterialPropertyBlock();
        
        // Initialiser la taille de la flaque partagée
        targetPuddleSize = startingPuddleSize;
        currentVisualPuddleSize = startingPuddleSize;
        hasTriggeredMaxSizeEvent = false;
        SetSharedPuddleSize(currentVisualPuddleSize);

        if (inkDropPrefab == null)
        {
            Debug.LogWarning($"[StatueInkController] 'Ink Drop Prefab' n'est pas assigné sur '{gameObject.name}' ! Les gouttes d'encre ne s'instancieront pas.");
        }

        foreach (var statue in statues)
        {
            statue.statuePropBlock = new MaterialPropertyBlock();

            // Vérifications de sécurité pour les repères
            if (statue.dropStartPoint == null || statue.dropEndPoint == null)
            {
                Debug.LogWarning($"[StatueInkController] Les points de départ/arrivée de la goutte ne sont pas assignés pour '{statue.name}' sur '{gameObject.name}' !");
            }

            // Auto-instanciation de la goutte si aucun objet spécifique n'est défini
            if (statue.customInkDropObject != null)
            {
                statue.runtimeInkDropObject = statue.customInkDropObject;
                Debug.Log($"[StatueInkController] '{statue.name}' utilise une goutte personnalisée déjà présente dans la scène : {statue.runtimeInkDropObject.name}");
            }
            else if (inkDropPrefab != null && statue.dropStartPoint != null)
            {
                statue.runtimeInkDropObject = Instantiate(inkDropPrefab, statue.dropStartPoint.position, Quaternion.identity);
                statue.runtimeInkDropObject.name = $"__INK_DROP_{statue.name.Replace(" ", "_")}__";
                statue.runtimeInkDropObject.transform.localScale = inkDropPrefab.transform.localScale;
                Debug.Log($"[StatueInkController] Goutte instanciée avec succès pour '{statue.name}' à la position {statue.dropStartPoint.position}. Nom de l'instance : {statue.runtimeInkDropObject.name}");
            }

            // Fallback pour le renderer s'il n'est pas assigné
            if (statue.statueRenderer == null && statue.dropStartPoint != null)
            {
                statue.statueRenderer = statue.dropStartPoint.GetComponentInParent<Renderer>();
            }

            ResetStatueVisuals(statue);
        }

        // Auto-détection de l'Animator du joueur si non renseigné
        if (spawnObjectAnimator == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                spawnObjectAnimator = pm.GetComponent<Animator>();
                if (spawnObjectAnimator == null) spawnObjectAnimator = pm.GetComponentInChildren<Animator>();
            }
        }

        // Au début de la cinématique, placer le joueur/l'objet sur l'animation d'attente PlayerSpawnIdle
        if (spawnObjectAnimator != null && !string.IsNullOrEmpty(initialIdleAnimationName))
        {
            spawnObjectAnimator.Play(initialIdleAnimationName);
        }

        // Lancement des cycles d'animation individuels
        if (gameObject.activeInHierarchy)
        {
            foreach (var statue in statues)
            {
                StartCoroutine(InkCycleRoutine(statue));
            }
        }
    }

    private void Update()
    {
        // Agrandir la flaque partagée de manière fluide vers la taille cible
        if (currentVisualPuddleSize < targetPuddleSize)
        {
            currentVisualPuddleSize = Mathf.MoveTowards(currentVisualPuddleSize, targetPuddleSize, Time.deltaTime * puddleGrowthSpeed);
            SetSharedPuddleSize(currentVisualPuddleSize);

            // Vérifier si la flaque a atteint sa taille maximale limite
            if (currentVisualPuddleSize >= maxPuddleSizeLimit && !hasTriggeredMaxSizeEvent)
            {
                TriggerMaxSizeEvent();
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        // Nettoyage des instances de gouttes créées à la racine pour éviter les fuites de mémoire
        foreach (var statue in statues)
        {
            if (statue.runtimeInkDropObject != null && statue.customInkDropObject == null)
            {
                Destroy(statue.runtimeInkDropObject);
            }
        }
    }

    private void OnEnable()
    {
        if (statues.Count > 0 && statues[0].statuePropBlock != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            foreach (var statue in statues)
            {
                StartCoroutine(InkCycleRoutine(statue));
            }
        }
    }

    /// <summary>
    /// Réinitialise l'état visuel d'une statue spécifique (larme masquée).
    /// </summary>
    public void ResetStatueVisuals(StatueConfig statue)
    {
        SetInkProgress(statue, 0f);
        SetInkTrailProgress(statue, 0f);
        SetStatueHeights(statue, statue.eyeY, statue.feetY);
        if (statue.runtimeInkDropObject != null)
        {
            statue.runtimeInkDropObject.SetActive(false);
        }
    }

    /// <summary>
    /// Réinitialise complètement la taille de la flaque partagée au sol.
    /// </summary>
    public void ResetPuddle()
    {
        targetPuddleSize = startingPuddleSize;
        currentVisualPuddleSize = startingPuddleSize;
        hasTriggeredMaxSizeEvent = false;
        SetSharedPuddleSize(currentVisualPuddleSize);
    }

    private void TriggerMaxSizeEvent()
    {
        hasTriggeredMaxSizeEvent = true;
        Debug.Log("[StatueInkController] La flaque d'encre a atteint sa taille maximale ! Déclenchement de l'événement de spawn.");

        // 1. Instancier le prefab s'il est renseigné dans l'inspecteur
        if (spawnObjectPrefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            GameObject spawnedInstance = Instantiate(spawnObjectPrefab, pos, rot);

            spawnObjectAnimator = spawnedInstance.GetComponent<Animator>();
            if (spawnObjectAnimator == null) spawnObjectAnimator = spawnedInstance.GetComponentInChildren<Animator>();
        }

        // Auto-détection de l'Animator du joueur si toujours nul
        if (spawnObjectAnimator == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                spawnObjectAnimator = pm.GetComponent<Animator>();
                if (spawnObjectAnimator == null) spawnObjectAnimator = pm.GetComponentInChildren<Animator>();
            }
        }

        // 2. Déclencher l'animation PlayerSpawn quand les conditions sont réunies (flaque max size)
        if (spawnObjectAnimator != null)
        {
            if (!spawnObjectAnimator.gameObject.activeInHierarchy)
            {
                spawnObjectAnimator.gameObject.SetActive(true);
            }
            Debug.Log($"[StatueInkController] Lancement de l'animation de spawn '{spawnAnimationName}' sur '{spawnObjectAnimator.gameObject.name}'.");
            spawnObjectAnimator.SetTrigger(spawnAnimationName);
            spawnObjectAnimator.Play(spawnAnimationName);
        }

        // 3. Déclencher l'événement UnityEvent
        if (onPuddleMaxSizeReached != null)
        {
            onPuddleMaxSizeReached.Invoke();
        }
    }

    private void SetInkProgress(StatueConfig statue, float progress)
    {
        if (statue.statueRenderer == null) return;
        statue.statueRenderer.GetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
        statue.statuePropBlock.SetFloat(InkProgressId, progress);
        statue.statueRenderer.SetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
    }

    private void SetInkTrailProgress(StatueConfig statue, float progress)
    {
        if (statue.statueRenderer == null) return;
        statue.statueRenderer.GetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
        statue.statuePropBlock.SetFloat(InkTrailProgressId, progress);
        statue.statueRenderer.SetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
    }

    private void SetStatueHeights(StatueConfig statue, float eyeY, float feetY)
    {
        if (statue.statueRenderer == null) return;
        statue.statueRenderer.GetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
        statue.statuePropBlock.SetFloat(EyeYId, eyeY);
        statue.statuePropBlock.SetFloat(FeetYId, feetY);
        statue.statueRenderer.SetPropertyBlock(statue.statuePropBlock, statue.statueMaterialSlot);
    }

    private void SetSharedPuddleSize(float size)
    {
        if (sharedPuddleRenderer == null) return;
        sharedPuddleRenderer.GetPropertyBlock(puddlePropBlock, sharedPuddleMaterialSlot);
        puddlePropBlock.SetFloat(PuddleSizeId, size);
        sharedPuddleRenderer.SetPropertyBlock(puddlePropBlock, sharedPuddleMaterialSlot);
    }

    private void OnDropImpact(Vector3 impactPosition)
    {
        // Augmenter la taille cible de la flaque
        targetPuddleSize = Mathf.Min(targetPuddleSize + puddleGrowthPerDrop, maxPuddleSizeLimit);

        // Déclencher l'éclaboussure de particules
        if (splashParticlePrefab != null)
        {
            ParticleSystem splash = Instantiate(splashParticlePrefab, impactPosition, Quaternion.identity);
            splash.Play();
            // Détruire l'instance de particules après sa lecture complète
            Destroy(splash.gameObject, splash.main.duration + splash.main.startLifetime.constantMax);
        }
    }

    private IEnumerator InkCycleRoutine(StatueConfig statue)
    {
        while (true)
        {
            // 1. Réinitialiser la larme sur la statue
            ResetStatueVisuals(statue);
            
            // Attente initiale aléatoire pour désynchroniser les statues
            yield return new WaitForSeconds(Random.Range(0.5f, 4.0f));

            // 2. L'encre coule le long du visage et du corps (Tear flows down)
            float flowProgress = 0f;
            float currentFlowSpeed = Random.Range(minInkSpeed, maxInkSpeed);
            while (flowProgress < 1f)
            {
                flowProgress += Time.deltaTime * currentFlowSpeed;
                SetInkProgress(statue, Mathf.Clamp01(flowProgress));
                yield return null;
            }
            SetInkProgress(statue, 1f);

            // 3. Attente au niveau des pieds (formation de la goutte)
            float currentDelay = Random.Range(minDropDelay, maxDropDelay);
            yield return new WaitForSeconds(currentDelay);

            // 4. Chute de la goutte vers la flaque unique au sol
            if (statue.runtimeInkDropObject != null && statue.dropStartPoint != null && statue.dropEndPoint != null)
            {
                Debug.Log($"[StatueInkController] Début de la chute de la goutte pour '{statue.name}'.");
                statue.runtimeInkDropObject.transform.position = statue.dropStartPoint.position;
                statue.runtimeInkDropObject.SetActive(true);

                float t = 0f;
                Vector3 startPos = statue.dropStartPoint.position;
                Vector3 endPos = statue.dropEndPoint.position;
                float distance = Vector3.Distance(startPos, endPos);
                float duration = (distance > 0f) ? (distance / dropFallSpeed) : 0.5f;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    float easeIn = t * t; // Accélération physique
                    statue.runtimeInkDropObject.transform.position = Vector3.Lerp(startPos, endPos, easeIn);
                    yield return null;
                }
                
                statue.runtimeInkDropObject.SetActive(false);
                Debug.Log($"[StatueInkController] Goutte tombée au sol (Impact) pour '{statue.name}'. Flaque agrandie.");
            }
            else
            {
                Debug.LogWarning($"[StatueInkController] '{statue.name}' : Chute annulée car runtimeInkDropObject, dropStartPoint ou dropEndPoint est NULL.");
                // Attente simulée de chute si pas d'objet 3D
                yield return new WaitForSeconds(0.3f);
            }

            // --- IMPACT ---
            // Agrandir la flaque partagée et lancer les particules
            OnDropImpact(statue.dropEndPoint != null ? statue.dropEndPoint.position : transform.position);

            // 5. Disparition progressive de la larme (séchage du haut vers le bas)
            float fadeProgress = 0f;
            while (fadeProgress < 1f)
            {
                fadeProgress += Time.deltaTime * tearFadeSpeed;
                SetInkTrailProgress(statue, Mathf.Clamp01(fadeProgress));
                yield return null;
            }
            SetInkTrailProgress(statue, 1f);

            // Petit temps mort avant de recommencer un cycle de pleurs
            yield return new WaitForSeconds(Random.Range(2.0f, 6.0f));
        }
    }
}
