using UnityEngine;

/// <summary>
/// Arrosoir de taille normale (sprite tenu en main par un PNJ).
/// Indépendant de GiantWateringCan.
///
/// Le système de particules est créé SANS parent (à la racine de la scène) et suit le bec chaque frame :
/// - l'échelle / le flip du PNJ n'agrandit jamais les gouttes,
/// - la direction du jet suit automatiquement l'orientation (flipX ou scale.x négatif).
/// </summary>
[AddComponentMenu("2.5D RPG/Hand Watering Can")]
public class HandWateringCan : MonoBehaviour
{
    [Header("Arrosage")]
    [Tooltip("Commence à verser automatiquement quand l'objet est activé (Faux par défaut pour éviter que l'eau ne coule avant l'élévation).")]
    [SerializeField] private bool waterOnEnable = false;

    [Header("Cible d'Arrosage (Optionnel)")]
    [Tooltip("Point de destination (Transform) où l'eau doit atterrir. Si assigné, la trajectoire parabolique de l'eau est calculée automatiquement vers cette cible !")]
    [SerializeField] private Transform targetPoint;

    [Header("Bec (point d'émission)")]
    [Tooltip("Optionnel : un Transform enfant placé exactement sur le bec. Prioritaire sur l'offset.")]
    [SerializeField] private Transform spoutPoint;

    [Tooltip("Position du bec par rapport au centre de l'arrosoir (en unités monde), quand le sprite regarde à droite. Inversé automatiquement en X quand il est retourné.")]
    [SerializeField] private Vector3 spoutOffset = new Vector3(0.15f, 0.05f, 0.2f);

    [Tooltip("Le bec de l'arrosoir est-il orienté vers la DROITE sur le sprite de base (non flippé) ?")]
    [SerializeField] private bool spoutFacesRightByDefault = false;

    [Header("Jet d'eau (Manuel si aucune cible n'est assignée)")]
    [Tooltip("Vitesse horizontale du jet (vers l'avant du bec).")]
    [SerializeField] private float forwardSpeed = 1.2f;

    [Tooltip("Vitesse verticale initiale (négatif = vers le bas).")]
    [SerializeField] private float downSpeed = -0.2f;

    [Tooltip("Vitesse en profondeur / vers le fond (axe Z).")]
    [SerializeField] private float depthSpeed = 0.2f;

    [Tooltip("Gravité appliquée aux gouttes.")]
    [SerializeField] private float gravity = 0.6f;

    [Tooltip("Taille des gouttes (min / max) en unités monde.")]
    [SerializeField] private Vector2 dropSize = new Vector2(0.02f, 0.04f);

    [Tooltip("Durée de vie des gouttes (min / max) en secondes.")]
    [SerializeField] private Vector2 dropLifetime = new Vector2(0.5f, 0.8f);

    [Tooltip("Nombre de gouttes par seconde.")]
    [SerializeField] private float emissionRate = 40f;

    [Tooltip("Dispersion aléatoire du jet.")]
    [SerializeField] private float spread = 0.03f;

    [Tooltip("Couleur des gouttes.")]
    [SerializeField] private Color dropColor = new Color(0.55f, 0.85f, 1f, 0.9f);

    [Tooltip("Ordre de rendu (pour passer devant les sprites).")]
    [SerializeField] private int sortingOrder = 50;

    private ParticleSystem waterParticles;
    private SpriteRenderer canSprite;
    private bool isWatering;

    public bool IsWatering => isWatering;
    public Transform TargetPoint => targetPoint;

    public void SetTargetPoint(Transform newTarget)
    {
        targetPoint = newTarget;
        if (isWatering && waterParticles != null)
        {
            UpdateEmitter();
        }
    }

    private void Awake()
    {
        canSprite = GetComponent<SpriteRenderer>();
        if (canSprite == null) canSprite = GetComponentInChildren<SpriteRenderer>();
        CreateParticles();
    }

    private void OnEnable()
    {
        if (waterOnEnable)
        {
            StartWatering();
        }
        else
        {
            StopWatering();
        }
    }

    private void OnDisable()
    {
        StopWatering();
    }

    private void OnDestroy()
    {
        if (waterParticles != null) Destroy(waterParticles.gameObject);
    }

    public void StartWatering()
    {
        isWatering = true;
        if (waterParticles == null) CreateParticles();
        waterParticles.gameObject.SetActive(true);
        UpdateEmitter();
        waterParticles.Play();
    }

    public void StopWatering()
    {
        isWatering = false;
        if (waterParticles != null)
        {
            waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            waterParticles.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (waterParticles != null && isWatering)
        {
            UpdateEmitter();
        }
    }

    /// <summary>
    /// Le bec regarde-t-il vers la droite à l'écran en ce moment ?
    /// Combine le flipX du sprite et le signe de l'échelle monde (flip du GameObject parent).
    /// </summary>
    private bool IsFacingRight()
    {
        bool right = spoutFacesRightByDefault;
        if (canSprite != null && canSprite.flipX) right = !right;
        if (transform.lossyScale.x < 0f) right = !right;
        return right;
    }

    /// <summary>
    /// Place l'émetteur sur le bec et oriente la vitesse du jet selon le sens actuel.
    /// Si targetPoint est renseigné, calcule automatiquement la vitesse parabolique exacte vers la cible.
    /// </summary>
    private void UpdateEmitter()
    {
        float dir = IsFacingRight() ? 1f : -1f;

        Vector3 spoutPos;
        if (spoutPoint != null)
        {
            spoutPos = spoutPoint.position;
        }
        else
        {
            spoutPos = transform.position + new Vector3(spoutOffset.x * dir, spoutOffset.y, spoutOffset.z);
        }

        Transform pt = waterParticles.transform;
        pt.position = spoutPos;
        pt.rotation = Quaternion.identity;

        var main = waterParticles.main;
        main.startSpeed = 0f;

        var vel = waterParticles.velocityOverLifetime;

        if (targetPoint != null)
        {
            Vector3 targetPos = targetPoint.position;
            Vector3 delta = targetPos - spoutPos;

            // Hauteur à descendre (h > 0 si le bec est au-dessus de la cible)
            float h = -delta.y;
            float g = Mathf.Max(0.1f, gravity * 9.81f);
            float initialVy = downSpeed; // Vitesse initiale vers le bas (ex: -0.2f)

            float tFall;
            if (h > 0f)
            {
                // Temps de chute exact depuis la hauteur du bec jusqu'à la cible sous gravité
                float v0 = Mathf.Abs(initialVy);
                tFall = (v0 + Mathf.Sqrt(v0 * v0 + 2f * g * h)) / g;
            }
            else
            {
                tFall = Mathf.Max(0.1f, (dropLifetime.x + dropLifetime.y) * 0.5f);
            }

            tFall = Mathf.Clamp(tFall, 0.2f, 2.5f);

            // Ajuster la durée de vie pour que l'eau disparaisse pile à l'atterrissage sur la cible
            main.startLifetime = new ParticleSystem.MinMaxCurve(tFall * 0.85f, tFall * 1.05f);

            float vx = delta.x / tFall;
            float vy = initialVy; // L'eau s'écoule directement vers le bas depuis le bec (pas d'arc vers le haut)
            float vz = delta.z / tFall;

            vel.x = new ParticleSystem.MinMaxCurve(vx);
            vel.y = new ParticleSystem.MinMaxCurve(vy);
            vel.z = new ParticleSystem.MinMaxCurve(vz);
        }
        else
        {
            vel.x = new ParticleSystem.MinMaxCurve(forwardSpeed * dir);
            vel.y = new ParticleSystem.MinMaxCurve(downSpeed);
            vel.z = new ParticleSystem.MinMaxCurve(depthSpeed);
        }
    }

    private void CreateParticles()
    {
        if (waterParticles != null) return;

        // Objet racine (sans parent) => aucune influence de l'échelle / du flip du PNJ
        GameObject go = new GameObject($"{name}_WaterDrops");
        go.SetActive(false); // Inactif au départ pour éviter un autoplay à la création
        waterParticles = go.AddComponent<ParticleSystem>();
        waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = waterParticles.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(dropLifetime.x, dropLifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(dropSize.x, dropSize.y);
        main.startColor = dropColor;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 300;

        var emission = waterParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = waterParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.001f, spread);

        var vel = waterParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        var col = waterParticles.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var renderer = waterParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
        if (canSprite != null) renderer.sortingLayerID = canSprite.sortingLayerID;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader) { name = "HandWateringCanDrops" };
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetColor("_BaseColor", Color.white);
            }
            renderer.sharedMaterial = mat;
        }
    }

    private void OnDrawGizmosSelected()
    {
        canSprite = canSprite != null ? canSprite : GetComponent<SpriteRenderer>();
        float dir = IsFacingRight() ? 1f : -1f;
        Vector3 p = spoutPoint != null
            ? spoutPoint.position
            : transform.position + new Vector3(spoutOffset.x * dir, spoutOffset.y, spoutOffset.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p, 0.03f);

        if (targetPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPoint.position, 0.1f);
            Gizmos.DrawLine(p, targetPoint.position);
        }
        else
        {
            Gizmos.DrawLine(p, p + new Vector3(forwardSpeed * dir, downSpeed - 0.3f, depthSpeed) * 0.3f);
        }
    }
}
