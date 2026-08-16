using UnityEngine;

/// <summary>
/// Contrôleur de l'arrosoir géant. Gère l'inclinaison physique et le système de particules d'eau.
/// Peut être contrôlé directement par l'Inspecteur (isWatering / isTilted) ou par interaction (touche E).
/// </summary>
[AddComponentMenu("2.5D RPG/Giant Watering Can")]
public class GiantWateringCan : Interactable
{
    [Header("Configuration de l'arrosage")]
    [Tooltip("L'arrosoir est-il en train de verser de l'eau ? (Coché = à l'infini)")]
    [SerializeField] private bool isWatering = true;

    [Tooltip("L'arrosoir est-il incliné par défaut ?")]
    [SerializeField] private bool isTilted = true;

    [Header("Repères et Particules")]
    [Tooltip("Point d'émission du jet d'eau. Si vide, un point est créé automatiquement à l'avant.")]
    [SerializeField] private Transform spoutPoint;

    [Tooltip("Le système de particules d'eau. Si vide, il sera généré et configuré automatiquement avec un rendu premium.")]
    [SerializeField] private ParticleSystem waterParticles;

    [Header("Réglages de rotation")]
    [Tooltip("Vitesse d'inclinaison/redressement de l'arrosoir.")]
    [SerializeField] private float tiltSpeed = 2.0f;

    [Tooltip("Rotation locale ciblée quand l'arrosoir est redressé (repos).")]
    [SerializeField] private Vector3 uprightLocalRotation = new Vector3(-85.613f, -90f, 90f);

    [Tooltip("Rotation locale ciblée quand l'arrosoir est incliné (arrosage).")]
    [SerializeField] private Vector3 tiltedLocalRotation = new Vector3(-40f, -90f, 90f);

    [Header("Effet de lévitation (Arrosage)")]
    [Tooltip("Activer l'effet de flottement/lévitation de l'arrosoir pendant l'arrosage.")]
    [SerializeField] private bool enableHover = true;

    [Tooltip("Amplitude verticale du flottement (en mètres).")]
    [SerializeField] private float hoverAmount = 0.25f;

    [Tooltip("Amplitude horizontale du balancement.")]
    [SerializeField] private float swayAmount = 0.12f;

    [Tooltip("Vitesse de l'oscillation.")]
    [SerializeField] private float hoverSpeed = 1.8f;

    private Quaternion targetRotation;
    private Vector3 baseLocalPosition;
    private float hoverTimer;

    protected override void Start()
    {
        // Appeler le Start de la classe de base Interactable pour la détection
        base.Start();

        // Configurer les composants de particules
        InitializeWaterParticles();

        // Sauvegarder la position locale d'origine pour l'effet de flottement
        baseLocalPosition = transform.localPosition;

        // Appliquer directement la rotation initiale en fonction des flags
        Vector3 initialEuler = isTilted ? tiltedLocalRotation : uprightLocalRotation;
        transform.localEulerAngles = initialEuler;
    }

    protected override void Update()
    {
        // Appeler la boucle de base (gestion de l'interaction et du billboard de l'icône)
        base.Update();

        // 1. Gérer l'inclinaison physique de l'arrosoir de façon fluide
        Vector3 targetEuler = isTilted ? tiltedLocalRotation : uprightLocalRotation;
        targetRotation = Quaternion.Euler(targetEuler);

        if (Quaternion.Angle(transform.localRotation, targetRotation) > 0.05f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
        }
        else
        {
            transform.localEulerAngles = targetEuler;
        }

        // 2. Gérer la lévitation et le balancement si activé et qu'on arrose
        Vector3 targetLocalPos = baseLocalPosition;
        if (isWatering && enableHover)
        {
            hoverTimer += Time.deltaTime * hoverSpeed;
            float newY = baseLocalPosition.y + Mathf.Sin(hoverTimer) * hoverAmount;
            float newX = baseLocalPosition.x + Mathf.Cos(hoverTimer * 0.7f) * swayAmount;
            float newZ = baseLocalPosition.z + Mathf.Sin(hoverTimer * 0.9f) * swayAmount;
            targetLocalPos = new Vector3(newX, newY, newZ);
        }
        else
        {
            // Revenir doucement à la position de départ si on n'arrose plus
            hoverTimer = 0f;
        }

        // Interpoler doucement vers la position cible pour un effet très fluide
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * tiltSpeed);

        // 3. Gérer le flux d'eau (Particle System)
        if (waterParticles != null)
        {
            bool isCurrentlyPlaying = waterParticles.isPlaying;
            if (isWatering && !isCurrentlyPlaying)
            {
                waterParticles.Play();
            }
            else if (!isWatering && isCurrentlyPlaying)
            {
                // Stopper l'émission pour que les gouttes déjà émises terminent leur chute naturellement
                waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    /// <summary>
    /// Implémentation de la méthode d'interaction héritée de Interactable.
    /// Permet de basculer l'état d'arrosage et d'inclinaison en appuyant sur E.
    /// </summary>
    protected override void Interact()
    {
        isWatering = !isWatering;
        isTilted = !isTilted;
        Debug.Log($"[GiantWateringCan] Arrosage modifié par le joueur : isWatering={isWatering}, isTilted={isTilted}");
    }

    /// <summary>
    /// Initialise et configure le système de particules d'eau programmatiquement pour un rendu d'eau optimal.
    /// </summary>
    private void InitializeWaterParticles()
    {
        // 1. Résoudre le point d'émission (spoutPoint)
        if (spoutPoint == null)
        {
            // Essayer de trouver un enfant existant
            Transform foundSpout = transform.Find("SpoutPoint");
            if (foundSpout == null) foundSpout = transform.Find("spout");
            
            if (foundSpout == null)
            {
                GameObject spoutObj = new GameObject("SpoutPoint");
                spoutObj.transform.SetParent(this.transform);
                // Décalage par défaut vers le haut et l'avant pour l'embout
                spoutObj.transform.localPosition = new Vector3(0f, 0.5f, 1.5f);
                spoutObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                spoutPoint = spoutObj.transform;
                Debug.Log($"[GiantWateringCan] 'SpoutPoint' non spécifié. Création automatique d'un repère local à {spoutObj.transform.localPosition}.");
            }
            else
            {
                spoutPoint = foundSpout;
            }
        }

        // 2. Résoudre le système de particules
        if (waterParticles == null)
        {
            waterParticles = spoutPoint.GetComponentInChildren<ParticleSystem>();

            if (waterParticles == null)
            {
                GameObject particlesObj = new GameObject("WaterSpoutParticles");
                particlesObj.transform.SetParent(spoutPoint);
                particlesObj.transform.localPosition = Vector3.zero;
                particlesObj.transform.localRotation = Quaternion.identity;

                waterParticles = particlesObj.AddComponent<ParticleSystem>();
                Debug.Log("[GiantWateringCan] ParticleSystem non spécifié. Création automatique sur le SpoutPoint.");
            }
        }

        // 3. Configuration esthétique poussée du ParticleSystem
        var main = waterParticles.main;
        main.duration = 1.0f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.gravityModifier = 1.0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Traînée d'eau physique lors de mouvements
        main.maxParticles = 1500;

        // Couleur initiale bleue translucide
        main.startColor = new Color(0.5f, 0.8f, 1.0f, 0.6f);

        // Emission dense pour de l'eau
        var emission = waterParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 250f;

        // Shape de type Cône orienté vers le bas
        var shape = waterParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5f; // Jet concentré pour l'embout
        shape.radius = 0.15f;
        shape.scale = Vector3.one;

        // Évolution de la couleur de l'eau
        var colorOverLifetime = waterParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient waterGradient = new Gradient();
        waterGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.95f, 1.0f), 0.0f),  // Blanc/écume au départ
                new GradientColorKey(new Color(0.2f, 0.65f, 1.0f), 0.5f), // Bleu eau clair
                new GradientColorKey(new Color(0.1f, 0.4f, 0.85f), 1.0f)  // Bleu profond
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f), // Fade in rapide
                new GradientAlphaKey(0.7f, 0.1f), 
                new GradientAlphaKey(0.6f, 0.7f), 
                new GradientAlphaKey(0.0f, 1.0f) // Fade out complet
            }
        );
        colorOverLifetime.color = waterGradient;

        // Évolution de la taille des gouttes (effet d'étalement puis réduction)
        var sizeOverLifetime = waterParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.8f);
        sizeCurve.AddKey(0.2f, 1.1f);
        sizeCurve.AddKey(0.8f, 0.9f);
        sizeCurve.AddKey(1.0f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Activer la physique de collision 3D
        var collision = waterParticles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounce = 0.0f;
        collision.lifetimeLoss = 1.0f;
        collision.sendCollisionMessages = true;

        // Configuration du rendu avec les shaders URP
        var renderer = waterParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Recherche d'un shader de particules compatible URP ou standard
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

            if (particleShader != null)
            {
                Material waterMaterial = new Material(particleShader);
                waterMaterial.name = "WaterParticlesMaterial";

                if (particleShader.name.Contains("Universal Render Pipeline"))
                {
                    waterMaterial.SetFloat("_Surface", 1.0f); // Transparent
                    waterMaterial.SetFloat("_Blend", 0.0f);   // Alpha blend
                    waterMaterial.SetColor("_BaseColor", Color.white);
                }
                renderer.sharedMaterial = waterMaterial;
            }
        }
    }
}
