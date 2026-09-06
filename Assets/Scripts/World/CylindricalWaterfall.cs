using UnityEngine;

/// <summary>
/// Composant générateur et contrôleur de cascade cylindrique stylisée (Cylindrical Waterfall).
/// Génère un maillage cylindrique fluide mono-paroi (sans géométrie interne parasite) avec évasement naturel,
/// et gère 2 systèmes de particules (éclaboussures au sol et brume volumétrique montante).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("2.5D RPG/World/Cylindrical Waterfall")]
public class CylindricalWaterfall : MonoBehaviour
{
    [Header("Dimensions & Forme Organique")]
    [Tooltip("Rayon de base du cylindre d'eau en mètres.")]
    [Min(0.5f)]
    [SerializeField] private float radius = 5.0f;

    [Tooltip("Hauteur totale de la cascade cylindrique en mètres.")]
    [Min(1.0f)]
    [SerializeField] private float height = 20.0f;

    [Tooltip("Évasement au sommet (débordement de l'eau en haut).")]
    [Range(0.8f, 2.0f)]
    [SerializeField] private float topFlareMultiplier = 1.25f;

    [Tooltip("Évasement à la base (impact au sol).")]
    [Range(0.8f, 2.0f)]
    [SerializeField] private float bottomFlareMultiplier = 1.15f;

    [Tooltip("Ondulations géométriques organiques sur la silhouette du cylindre.")]
    [Range(0.0f, 0.4f)]
    [SerializeField] private float organicRadiusNoise = 0.08f;

    [Tooltip("Nombre de segments radiaux pour la circonférence du cylindre (fluidité géométrique).")]
    [Range(16, 128)]
    [SerializeField] private int radialSegments = 48;

    [Tooltip("Nombre de divisions sur la hauteur.")]
    [Range(4, 64)]
    [SerializeField] private int heightSegments = 24;

    [Header("Matériau et Shader d'Eau")]
    [Tooltip("Le matériau avec le shader '2.5D RPG/CylindricalWaterfall'. Si vide, sera recherché/créé automatiquement.")]
    [SerializeField] private Material waterfallMaterial;

    [Header("Brume et Éclaboussures (Base Splash & Mist)")]
    [Tooltip("Activer le cercle d'éclaboussures au pied de la cascade.")]
    [SerializeField] private bool enableSplashParticles = true;

    [Tooltip("Activer la brume volumétrique montante au sol.")]
    [SerializeField] private bool enableMistCloud = true;

    [Tooltip("Taux d'émission des particules d'éclaboussures.")]
    [SerializeField] private float splashEmissionRate = 100f;

    [Tooltip("Taux d'émission des nuages de brume montante.")]
    [SerializeField] private float mistEmissionRate = 30f;

    [Tooltip("Couleur des particules d'éclaboussures et de brume.")]
    [SerializeField] private Color splashColor = new Color(0.85f, 0.96f, 1.0f, 0.75f);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private ParticleSystem splashParticleSystem;
    private ParticleSystem mistParticleSystem;
    private Mesh proceduralMesh;

    private void OnValidate()
    {
        if (enabled)
        {
            GenerateWaterfallMesh();
            UpdateMaterialAndParticles();
        }
    }

    private void Awake()
    {
        InitComponents();
        GenerateWaterfallMesh();
        UpdateMaterialAndParticles();
    }

    private void Start()
    {
        if (Application.isPlaying && enableSplashParticles)
        {
            SetupSplashParticles();
            if (enableMistCloud)
            {
                SetupMistCloudParticles();
            }
        }
    }

    private void InitComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (waterfallMaterial == null)
        {
            Shader shader = Shader.Find("2.5D RPG/CylindricalWaterfall");
            if (shader != null)
            {
                waterfallMaterial = new Material(shader);
                waterfallMaterial.name = "CylindricalWaterfall_RuntimeMat";
            }
        }

        if (meshRenderer != null && waterfallMaterial != null)
        {
            meshRenderer.sharedMaterial = waterfallMaterial;
        }
    }

    /// <summary>
    /// Génère un maillage 3D cylindrique fluide mono-paroi parfait sans disques ni géométrie interne.
    /// </summary>
    [ContextMenu("⚡ Régénérer le Maillage Cascade")]
    public void GenerateWaterfallMesh()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        if (proceduralMesh == null)
        {
            proceduralMesh = new Mesh();
            proceduralMesh.name = "ProceduralOrganicWaterfallCylinder";
        }
        else
        {
            proceduralMesh.Clear();
        }

        int verticesPerRing = radialSegments + 1;
        int numRings = heightSegments + 1;
        int totalVertices = verticesPerRing * numRings;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] normals = new Vector3[totalVertices];
        Vector2[] uvs = new Vector2[totalVertices];
        int[] triangles = new int[radialSegments * heightSegments * 6];

        int vertIdx = 0;
        int triIdx = 0;

        float GetRadiusAtV(float v)
        {
            float flareProfile = Mathf.Lerp(bottomFlareMultiplier, topFlareMultiplier, v);
            float organicCurve = 1.0f + Mathf.Sin(v * Mathf.PI * 2.0f) * 0.04f;
            return radius * flareProfile * organicCurve;
        }

        for (int r = 0; r < numRings; r++)
        {
            float v = (float)r / heightSegments;
            float y = v * height;
            float currentRadius = GetRadiusAtV(v);

            for (int s = 0; s <= radialSegments; s++)
            {
                float u = (float)s / radialSegments;
                float angle = u * Mathf.PI * 2.0f;

                float organicNoise = Mathf.Sin(v * Mathf.PI * 6.0f + angle * 3.0f) * organicRadiusNoise * radius;
                float rWithNoise = currentRadius + organicNoise;

                float x = Mathf.Cos(angle) * rWithNoise;
                float z = Mathf.Sin(angle) * rWithNoise;

                vertices[vertIdx] = new Vector3(x, y, z);
                normals[vertIdx] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                uvs[vertIdx] = new Vector2(u, v);
                vertIdx++;
            }
        }

        for (int r = 0; r < heightSegments; r++)
        {
            for (int s = 0; s < radialSegments; s++)
            {
                int current = r * verticesPerRing + s;
                int next = current + verticesPerRing;

                triangles[triIdx++] = current;
                triangles[triIdx++] = next;
                triangles[triIdx++] = current + 1;

                triangles[triIdx++] = current + 1;
                triangles[triIdx++] = next;
                triangles[triIdx++] = next + 1;
            }
        }

        proceduralMesh.vertices = vertices;
        proceduralMesh.normals = normals;
        proceduralMesh.uv = uvs;
        proceduralMesh.triangles = triangles;

        proceduralMesh.RecalculateBounds();
        meshFilter.sharedMesh = proceduralMesh;
    }

    private void UpdateMaterialAndParticles()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && waterfallMaterial != null && meshRenderer.sharedMaterial != waterfallMaterial)
        {
            meshRenderer.sharedMaterial = waterfallMaterial;
        }
    }

    private void SetupSplashParticles()
    {
        Transform splashObj = transform.Find("WaterfallSplashRing");
        if (splashObj == null)
        {
            GameObject go = new GameObject("WaterfallSplashRing");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            splashObj = go.transform;
        }

        splashParticleSystem = splashObj.GetComponent<ParticleSystem>();
        if (splashParticleSystem == null)
        {
            splashParticleSystem = splashObj.gameObject.AddComponent<ParticleSystem>();
        }

        var main = splashParticleSystem.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startColor = splashColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = splashParticleSystem.emission;
        emission.rateOverTime = splashEmissionRate;

        var shape = splashParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * bottomFlareMultiplier;
        shape.radiusThickness = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sizeOverLifetime = splashParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1.3f));

        var colorOverLifetime = splashParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(splashColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = splashParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (!splashParticleSystem.isPlaying)
        {
            splashParticleSystem.Play();
        }
    }

    private void SetupMistCloudParticles()
    {
        Transform mistObj = transform.Find("WaterfallMistCloud");
        if (mistObj == null)
        {
            GameObject go = new GameObject("WaterfallMistCloud");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            mistObj = go.transform;
        }

        mistParticleSystem = mistObj.GetComponent<ParticleSystem>();
        if (mistParticleSystem == null)
        {
            mistParticleSystem = mistObj.gameObject.AddComponent<ParticleSystem>();
        }

        var main = mistParticleSystem.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 3.5f);
        main.startColor = new Color(splashColor.r, splashColor.g, splashColor.b, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = mistParticleSystem.emission;
        emission.rateOverTime = mistEmissionRate;

        var shape = mistParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * (bottomFlareMultiplier + 0.2f);
        shape.radiusThickness = 0.5f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sizeOverLifetime = mistParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, 2.5f));

        var colorOverLifetime = mistParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(splashColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.4f, 0.3f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = mistParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (!mistParticleSystem.isPlaying)
        {
            mistParticleSystem.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.15f, 0.85f, 1.0f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        
        int steps = 24;
        Vector3 lastTop = Vector3.zero;
        Vector3 lastBottom = Vector3.zero;

        for (int i = 0; i <= steps; i++)
        {
            float angle = i * (Mathf.PI * 2f / steps);
            Vector3 top = new Vector3(Mathf.Cos(angle) * (radius * topFlareMultiplier), height, Mathf.Sin(angle) * (radius * topFlareMultiplier));
            Vector3 bottom = new Vector3(Mathf.Cos(angle) * (radius * bottomFlareMultiplier), 0f, Mathf.Sin(angle) * (radius * bottomFlareMultiplier));

            if (i > 0)
            {
                Gizmos.DrawLine(lastTop, top);
                Gizmos.DrawLine(lastBottom, bottom);
            }
            Gizmos.DrawLine(bottom, top);

            lastTop = top;
            lastBottom = bottom;
        }
    }
}
