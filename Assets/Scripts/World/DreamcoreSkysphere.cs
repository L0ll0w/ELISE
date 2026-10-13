using UnityEngine;

/// <summary>
/// Gestionnaire de Skysphere Dreamcore & Ambiance Céleste pour le Jardin et le Soleil.
/// - Génère ou contrôle une Skysphere procédurale (ou s'applique au Skybox RenderSettings).
/// - Suit la caméra principale en temps réel (fond infini sans parallaxe parasite).
/// - Synchronise en temps réel la direction du Soleil 3D vers le shader pour un halo solaire majestueux.
/// - Harmonise l'éclairage ambiant (RenderSettings Trilight) et le brouillard (Fog) avec les couleurs du ciel.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/World/Dreamcore Skysphere")]
public class DreamcoreSkysphere : MonoBehaviour
{
    public enum PresetTheme
    {
        DreamcorePastelGarden,  // Rose pêche / Lavande / Violet rêve
        FairytaleSunburst,      // Or éclatant / Pêche rose / Lilas
        EtherealTwilight,       // Violet nuit / Rose crépuscule / Indigo
        SoftSpringDawn          // Aube printanière / Menthe pastel / Rose poudré
    }

    [Header("Matériau & Rendu Céleste")]
    [Tooltip("Matériau utilisant le shader 'ELISE/DreamcoreSkysphere'.")]
    [SerializeField] private Material skyMaterial;

    [Tooltip("Appliquer également ce matériau au Skybox natif de la scène (RenderSettings.skybox).")]
    [SerializeField] private bool applyToRenderSettingsSkybox = true;

    [Tooltip("Créer et afficher un dôme 3D inversé (Skysphere) qui suit la caméra. Décoché par défaut pour utiliser le Skybox natif de scène sans collision de profondeur.")]
    [SerializeField] private bool createPhysicalDome = false;

    [Tooltip("Rayon du dôme céleste en mètres.")]
    [SerializeField] private float domeRadius = 450f;

    [Header("Alignement Solaire & Cible")]
    [Tooltip("Cible du Soleil physique (ex: objet SUN). Si vide, recherche automatiquement l'objet 'SUN'.")]
    [SerializeField] private Transform sunTarget;

    [Tooltip("Lumière directionnelle principale liée au soleil. Synchronise sa rotation avec le soleil.")]
    [SerializeField] private Light mainDirectionalLight;

    [Header("Rotation & Dynamique Céleste")]
    [Tooltip("Faire tourner lentement le dôme céleste pour faire défiler les nuages et étoiles.")]
    [SerializeField] private bool autoRotate = true;

    [Tooltip("Vitesse de rotation du dôme céleste en degrés par seconde.")]
    [SerializeField] private float rotationSpeed = 0.25f;

    [Header("Harmonisation de la Lumière Globale")]
    [Tooltip("Harmonise automatiquement RenderSettings (Fog & Ambient Trilight) avec la palette du ciel.")]
    [SerializeField] private bool harmonizeSceneLighting = true;

    [SerializeField] private Color horizonFogColor = new Color(0.40f, 0.38f, 0.58f, 1f);
    [SerializeField] private float fogDensity = 0.003f;

    private MeshFilter domeMeshFilter;
    private MeshRenderer domeMeshRenderer;

    private void Reset()
    {
        FindAssetsAndTargets();
    }

    private void Awake()
    {
        FindAssetsAndTargets();
        SetupDome();
        ApplyLightingAndSkybox();
    }

    private void OnEnable()
    {
        FindAssetsAndTargets();
        SetupDome();
        ApplyLightingAndSkybox();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            ApplyLightingAndSkybox();
        }
    }

    private void Start()
    {
        FindAssetsAndTargets();
        SetupDome();
        ApplyLightingAndSkybox();
    }

    private void LateUpdate()
    {
        // 1. Suivi de la caméra principale : le dôme reste toujours centré sur le point de vue
        Camera cam = Camera.main;
#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
        {
            cam = UnityEditor.SceneView.lastActiveSceneView.camera;
        }
#endif
        if (cam != null && createPhysicalDome)
        {
            transform.position = cam.transform.position;
        }

        // 2. Rotation douce du dôme céleste
        if (autoRotate && Application.isPlaying)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        // 3. Mise à jour de la direction du soleil dans le shader
        UpdateSunVectorInShader();
    }

    private void FindAssetsAndTargets()
    {
        if (skyMaterial == null)
        {
            skyMaterial = Resources.Load<Material>("DreamcoreSkybox");
            if (skyMaterial == null)
            {
#if UNITY_EDITOR
                skyMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DreamcoreSkybox.mat");
#endif
            }
        }

        if (sunTarget == null)
        {
            GameObject sunObj = GameObject.Find("SUN");
            if (sunObj == null) sunObj = GameObject.Find("Sun");
            if (sunObj == null)
            {
                SunGlow glow = Object.FindFirstObjectByType<SunGlow>();
                if (glow != null) sunObj = glow.gameObject;
            }
            if (sunObj != null) sunTarget = sunObj.transform;
        }

        if (mainDirectionalLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    mainDirectionalLight = l;
                    break;
                }
            }
        }
    }

    private void SetupDome()
    {
        domeMeshRenderer = GetComponent<MeshRenderer>();
        domeMeshFilter = GetComponent<MeshFilter>();

        if (!createPhysicalDome)
        {
            if (domeMeshRenderer != null) domeMeshRenderer.enabled = false;
            return;
        }

        domeMeshFilter = GetComponent<MeshFilter>();
        if (domeMeshFilter == null) domeMeshFilter = gameObject.AddComponent<MeshFilter>();

        domeMeshRenderer = GetComponent<MeshRenderer>();
        if (domeMeshRenderer == null) domeMeshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (domeMeshFilter.sharedMesh == null)
        {
            domeMeshFilter.sharedMesh = GenerateInvertedSphereMesh(domeRadius, 32, 24);
        }

        domeMeshRenderer.enabled = true;
        domeMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        domeMeshRenderer.receiveShadows = false;

        if (skyMaterial != null)
        {
            domeMeshRenderer.sharedMaterial = skyMaterial;
        }
    }

    /// <summary>
    /// Génère un maillage de sphère inversée (les normales et triangles pointent vers l'intérieur).
    /// </summary>
    private Mesh GenerateInvertedSphereMesh(float radius, int longitudeSegments, int latitudeSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Inverted_Skysphere_Mesh";

        int numVertices = (latitudeSegments + 1) * (longitudeSegments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector3[] normals = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];

        int vertIndex = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = lat * Mathf.PI / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = lon * 2f * Mathf.PI / longitudeSegments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 normal = new Vector3(cosPhi * sinTheta, cosTheta, sinPhi * sinTheta);
                // Inverser les normales pour regarder vers le centre de la sphère
                normals[vertIndex] = -normal;
                vertices[vertIndex] = normal * radius;
                uvs[vertIndex] = new Vector2((float)lon / longitudeSegments, 1f - (float)lat / latitudeSegments);

                vertIndex++;
            }
        }

        int numIndices = latitudeSegments * longitudeSegments * 6;
        int[] triangles = new int[numIndices];
        int triIndex = 0;

        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                // Triangles inversés pour le rendu interne (Cull Front / Back inversé)
                triangles[triIndex++] = current;
                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next + 1;
                triangles[triIndex++] = next;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (radius * 2.5f));

        return mesh;
    }

    /// <summary>
    /// Calcule et transmet la direction précise du soleil au shader et oriente la lumière directionnelle.
    /// </summary>
    private void UpdateSunVectorInShader()
    {
        Vector3 sunDirection;

        if (sunTarget != null)
        {
            Camera cam = Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : transform.position;
            sunDirection = (sunTarget.position - origin).normalized;
        }
        else if (mainDirectionalLight != null)
        {
            sunDirection = -mainDirectionalLight.transform.forward;
        }
        else
        {
            sunDirection = new Vector3(-0.6f, 0.75f, -0.3f).normalized;
        }

        if (skyMaterial != null)
        {
            skyMaterial.SetVector("_SunDirection", sunDirection);
        }

        // Aligner la lumière directionnelle avec le Soleil pour des ombres 100% réalistes
        if (mainDirectionalLight != null && sunTarget != null)
        {
            Camera cam = Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 toSun = (sunTarget.position - origin).normalized;
            // La lumière se propage DU soleil VERS la scène
            mainDirectionalLight.transform.rotation = Quaternion.LookRotation(-toSun, Vector3.up);
        }
    }

    /// <summary>
    /// Applique les couleurs et l'harmonie céleste à la scène.
    /// </summary>
    [ContextMenu("⚡ Appliquer Ciel & Éclairage")]
    public void ApplyLightingAndSkybox()
    {
        FindAssetsAndTargets();

        if (applyToRenderSettingsSkybox && skyMaterial != null)
        {
            RenderSettings.skybox = skyMaterial;
        }

        UpdateSunVectorInShader();

        if (harmonizeSceneLighting)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.32f, 0.54f, 1f);     // Lilas doux
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.30f, 0.44f, 1f); // Rose lavande
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.12f, 0.22f, 1f);  // Indigo doux jardin

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = horizonFogColor;
            RenderSettings.fogDensity = fogDensity;

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = new Color(1.0f, 0.94f, 0.84f, 1f);
                mainDirectionalLight.intensity = 1.9f;
                mainDirectionalLight.shadows = LightShadows.Soft;
                mainDirectionalLight.shadowStrength = 0.82f;
            }
        }
    }

    #region Presets Stylistiques
    [ContextMenu("🌸 Preset : Jardin Pastel Dreamcore (Défaut)")]
    public void PresetPastelGarden()
    {
        if (skyMaterial != null)
        {
            skyMaterial.SetColor("_ZenithColor", new Color(0.24f, 0.18f, 0.44f, 1f));
            skyMaterial.SetColor("_MiddleColor", new Color(0.52f, 0.36f, 0.58f, 1f));
            skyMaterial.SetColor("_HorizonColor", new Color(0.84f, 0.56f, 0.48f, 1f));
            skyMaterial.SetColor("_NadirColor", new Color(0.12f, 0.10f, 0.22f, 1f));

            skyMaterial.SetColor("_SunDiscColor", new Color(1.5f, 1.3f, 0.9f, 1f));
            skyMaterial.SetFloat("_SunDiscIntensity", 1.2f);
            skyMaterial.SetColor("_SunCoronaColor", new Color(1.4f, 0.95f, 0.6f, 1f));
            skyMaterial.SetFloat("_SunCoronaIntensity", 1.4f);
            skyMaterial.SetColor("_SunOuterGlowColor", new Color(1.1f, 0.6f, 0.7f, 1f));
            skyMaterial.SetFloat("_SunOuterGlowIntensity", 0.75f);

            skyMaterial.SetColor("_CloudColor", new Color(0.88f, 0.78f, 0.85f, 1f));
            skyMaterial.SetFloat("_CloudOpacity", 0.45f);
        }

        horizonFogColor = new Color(0.40f, 0.38f, 0.58f, 1f);
        fogDensity = 0.003f;
        ApplyLightingAndSkybox();
    }

    [ContextMenu("✨ Preset : Fairytale Sunburst (Or & Rose Céleste)")]
    public void PresetFairytaleSunburst()
    {
        if (skyMaterial != null)
        {
            skyMaterial.SetColor("_ZenithColor", new Color(0.28f, 0.38f, 0.68f, 1f));
            skyMaterial.SetColor("_MiddleColor", new Color(0.75f, 0.52f, 0.68f, 1f));
            skyMaterial.SetColor("_HorizonColor", new Color(1.0f, 0.82f, 0.62f, 1f));
            skyMaterial.SetColor("_NadirColor", new Color(0.15f, 0.18f, 0.32f, 1f));

            skyMaterial.SetColor("_SunCoronaColor", new Color(2.8f, 2.0f, 1.2f, 1f));
            skyMaterial.SetFloat("_SunCoronaIntensity", 3.2f);
            skyMaterial.SetColor("_SunOuterGlowColor", new Color(1.8f, 0.85f, 0.72f, 1f));
            skyMaterial.SetFloat("_SunOuterGlowIntensity", 2.2f);

            skyMaterial.SetColor("_CloudColorHigh", new Color(1.1f, 0.98f, 0.90f, 1f));
            skyMaterial.SetColor("_CloudColorLow", new Color(0.72f, 0.55f, 0.70f, 1f));
        }

        horizonFogColor = new Color(0.96f, 0.82f, 0.72f, 1f);
        fogDensity = 0.005f;
        ApplyLightingAndSkybox();
    }

    [ContextMenu("🌙 Preset : Crépuscule Éthéré (Violet & Pêche Dorée)")]
    public void PresetEtherealTwilight()
    {
        if (skyMaterial != null)
        {
            skyMaterial.SetColor("_ZenithColor", new Color(0.22f, 0.15f, 0.42f, 1f));
            skyMaterial.SetColor("_MiddleColor", new Color(0.55f, 0.32f, 0.58f, 1f));
            skyMaterial.SetColor("_HorizonColor", new Color(0.92f, 0.58f, 0.55f, 1f));
            skyMaterial.SetColor("_NadirColor", new Color(0.10f, 0.08f, 0.20f, 1f));

            skyMaterial.SetColor("_SunCoronaColor", new Color(2.6f, 1.4f, 0.9f, 1f));
            skyMaterial.SetFloat("_SunCoronaIntensity", 2.8f);
            skyMaterial.SetColor("_SunOuterGlowColor", new Color(1.6f, 0.60f, 0.75f, 1f));
            skyMaterial.SetFloat("_SunOuterGlowIntensity", 2.0f);

            skyMaterial.SetColor("_CloudColorHigh", new Color(1.0f, 0.82f, 0.88f, 1f));
            skyMaterial.SetColor("_CloudColorLow", new Color(0.52f, 0.35f, 0.62f, 1f));
        }

        horizonFogColor = new Color(0.85f, 0.58f, 0.62f, 1f);
        fogDensity = 0.0065f;
        ApplyLightingAndSkybox();
    }
    #endregion
}
