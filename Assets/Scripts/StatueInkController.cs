using System.Collections;
using UnityEngine;

/// <summary>
/// Gère le cycle d'animation des statues pleurant de l'encre :
/// 1. L'encre coule le long de la statue à une vitesse aléatoire.
/// 2. Attente / formation de la goutte aux pieds de la statue.
/// 3. Chute physique de la goutte vers le sol.
/// 4. Impact et croissance d'une flaque organique au sol.
/// 5. Optionnellement, évaporation de la flaque et réinitialisation en boucle.
/// </summary>
[AddComponentMenu("2.5D RPG/Statue Ink Controller")]
public class StatueInkController : MonoBehaviour
{
    [Header("Configuration Statue")]
    [Tooltip("Le renderer de la statue (ex: JugeFlower MeshRenderer).")]
    [SerializeField] private Renderer statueRenderer;

    [Tooltip("L'index du matériau de la statue à modifier (généralement 0).")]
    [SerializeField] private int statueMaterialSlot = 0;

    [Tooltip("Vitesse minimale de l'écoulement le long du corps.")]
    [Range(0.05f, 2f)]
    [SerializeField] private float minInkSpeed = 0.15f;

    [Tooltip("Vitesse maximale de l'écoulement le long du corps.")]
    [Range(0.05f, 2f)]
    [SerializeField] private float maxInkSpeed = 0.35f;

    [Header("Configuration Goutte d'Encre")]
    [Tooltip("Le GameObject représentant la goutte de liquide (ex: petite sphère noire).")]
    [SerializeField] private GameObject inkDropObject;

    [Tooltip("Le point de départ de la goutte (ex: les pieds de la statue).")]
    [SerializeField] private Transform dropStartPoint;

    [Tooltip("Le point d'impact de la goutte (ex: la position de la flaque au sol).")]
    [SerializeField] private Transform dropEndPoint;

    [Tooltip("Temps d'attente minimal au pied avant que la goutte ne se détache.")]
    [SerializeField] private float minDropDelay = 0.4f;

    [Tooltip("Temps d'attente maximal au pied avant que la goutte ne se détache.")]
    [SerializeField] private float maxDropDelay = 1.2f;

    [Tooltip("Vitesse de chute de la goutte (en mètres par seconde).")]
    [SerializeField] private float dropFallSpeed = 6.0f;

    [Header("Configuration Flaque au Sol")]
    [Tooltip("Le renderer du plan/disque de la flaque au sol.")]
    [SerializeField] private Renderer puddleRenderer;

    [Tooltip("L'index du matériau de la flaque à modifier (généralement 0).")]
    [SerializeField] private int puddleMaterialSlot = 0;

    [Tooltip("Taille maximale atteinte par la flaque (valeur du paramètre _PuddleSize dans le shader).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float maxPuddleSize = 0.8f;

    [Tooltip("Vitesse minimale de croissance de la flaque.")]
    [SerializeField] private float minPuddleGrowthSpeed = 0.3f;

    [Tooltip("Vitesse maximale de croissance de la flaque.")]
    [SerializeField] private float maxPuddleGrowthSpeed = 0.6f;

    [Header("Cycle & Reset")]
    [Tooltip("Est-ce que le cycle se répète indéfiniment ?")]
    [SerializeField] private bool loopCycle = true;

    [Tooltip("Durée de maintien de la flaque à sa taille maximale avant de commencer à s'évaporer.")]
    [SerializeField] private float puddleVisibleDuration = 4.0f;

    [Tooltip("Vitesse de disparition/évaporation de la flaque et nettoyage de la statue lors du reset.")]
    [SerializeField] private float fadeOutSpeed = 0.3f;

    private MaterialPropertyBlock statuePropBlock;
    private MaterialPropertyBlock puddlePropBlock;
    private Coroutine inkCycleCoroutine;

    private static readonly int InkProgressId = Shader.PropertyToID("_InkProgress");
    private static readonly int PuddleSizeId = Shader.PropertyToID("_PuddleSize");

    private void Start()
    {
        statuePropBlock = new MaterialPropertyBlock();
        puddlePropBlock = new MaterialPropertyBlock();

        // Initialisation des objets de la scène
        if (statueRenderer == null)
        {
            statueRenderer = GetComponent<Renderer>();
        }

        ResetVisuals();

        // Lancement du cycle d'animation
        if (gameObject.activeInHierarchy)
        {
            inkCycleCoroutine = StartCoroutine(InkCycleRoutine());
        }
    }

    private void OnDisable()
    {
        if (inkCycleCoroutine != null)
        {
            StopCoroutine(inkCycleCoroutine);
            inkCycleCoroutine = null;
        }
    }

    private void OnEnable()
    {
        if (statuePropBlock != null && inkCycleCoroutine == null && gameObject.activeInHierarchy)
        {
            inkCycleCoroutine = StartCoroutine(InkCycleRoutine());
        }
    }

    /// <summary>
    /// Réinitialise l'encre sur la statue, masque la goutte et réduit la flaque à zéro.
    /// </summary>
    public void ResetVisuals()
    {
        SetInkProgress(0f);
        SetPuddleSize(0f);
        if (inkDropObject != null)
        {
            inkDropObject.SetActive(false);
        }
    }

    private void SetInkProgress(float progress)
    {
        if (statueRenderer == null) return;
        statueRenderer.GetPropertyBlock(statuePropBlock, statueMaterialSlot);
        statuePropBlock.SetFloat(InkProgressId, progress);
        statueRenderer.SetPropertyBlock(statuePropBlock, statueMaterialSlot);
    }

    private void SetPuddleSize(float size)
    {
        if (puddleRenderer == null) return;
        puddleRenderer.GetPropertyBlock(puddlePropBlock, puddleMaterialSlot);
        puddlePropBlock.SetFloat(PuddleSizeId, size);
        puddleRenderer.SetPropertyBlock(puddlePropBlock, puddleMaterialSlot);
    }

    private IEnumerator InkCycleRoutine()
    {
        while (true)
        {
            // 1. Initialiser le cycle
            ResetVisuals();
            yield return new WaitForSeconds(1.0f); // Courte pause de début

            // 2. L'encre coule le long du corps de la statue
            float flowProgress = 0f;
            float currentFlowSpeed = Random.Range(minInkSpeed, maxInkSpeed);
            while (flowProgress < 1f)
            {
                flowProgress += Time.deltaTime * currentFlowSpeed;
                SetInkProgress(Mathf.Clamp01(flowProgress));
                yield return null;
            }
            SetInkProgress(1f);

            // 3. Attente au niveau des pieds (formation de la goutte)
            float currentDelay = Random.Range(minDropDelay, maxDropDelay);
            yield return new WaitForSeconds(currentDelay);

            // 4. Chute de la goutte
            if (inkDropObject != null && dropStartPoint != null && dropEndPoint != null)
            {
                inkDropObject.transform.position = dropStartPoint.position;
                inkDropObject.SetActive(true);

                float t = 0f;
                Vector3 startPos = dropStartPoint.position;
                Vector3 endPos = dropEndPoint.position;
                float distance = Vector3.Distance(startPos, endPos);
                
                // Calculer la durée en fonction de la vitesse souhaitée
                float duration = (distance > 0f) ? (distance / dropFallSpeed) : 0.5f;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    // Accélération de la chute (simule la gravité)
                    float easeIn = t * t;
                    inkDropObject.transform.position = Vector3.Lerp(startPos, endPos, easeIn);
                    yield return null;
                }
                
                // Désactiver la goutte à l'impact
                inkDropObject.SetActive(false);
            }
            else
            {
                // Fallback s'il n'y a pas d'objets assignés pour simuler la chute
                yield return new WaitForSeconds(0.3f);
            }

            // 5. La flaque d'encre apparaît au sol et s'étend
            float puddleSize = 0f;
            float currentGrowthSpeed = Random.Range(minPuddleGrowthSpeed, maxPuddleGrowthSpeed);
            while (puddleSize < maxPuddleSize)
            {
                puddleSize += Time.deltaTime * currentGrowthSpeed;
                SetPuddleSize(Mathf.Min(puddleSize, maxPuddleSize));
                yield return null;
            }
            SetPuddleSize(maxPuddleSize);

            // 6. Temps d'exposition de la flaque maximale
            yield return new WaitForSeconds(puddleVisibleDuration);

            // Si on ne boucle pas, on s'arrête ici
            if (!loopCycle)
            {
                break;
            }

            // 7. Évaporation de la flaque et disparition de l'encre (Transition de réinitialisation)
            float fadeProgress = 1f;
            while (fadeProgress > 0f)
            {
                fadeProgress -= Time.deltaTime * fadeOutSpeed;
                float clampedProgress = Mathf.Clamp01(fadeProgress);
                
                SetPuddleSize(clampedProgress * maxPuddleSize);
                SetInkProgress(clampedProgress); // L'encre sur la statue s'estompe en même temps
                yield return null;
            }
            
            ResetVisuals();
            yield return new WaitForSeconds(2.0f); // Temps mort entre deux coulées
        }
    }
}
