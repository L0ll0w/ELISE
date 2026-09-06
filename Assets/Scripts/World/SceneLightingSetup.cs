using UnityEngine;

/// <summary>
/// Contrôleur et configurateur d'éclairage atmosphérique professionnel pour scène 2.5D / 3D.
/// Génère et gère automatiquement la lumière directionnelle principale, la couleur ambiante tricolore (Ciel/Équateur/Sol),
/// la brume atmosphérique (Fog), la lumière d'accentuation de la cascade et la lueur du joueur/statues.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/Scene Lighting Setup")]
public class SceneLightingSetup : MonoBehaviour
{
    public enum PresetStyle
    {
        DreamcoreFairy,    // Rêve Féerique 2.5D (Cyan/Or/Indigo)
        MagicalTwilight,   // Crépuscule Magique (Violet/Rose/Or)
        MoonlightNight,    // Clair de Lune Nuit (Bleu Nuit/Argent)
        VibrantDaylight    // Plein Soleil Éclatant (Solaire/Ciel Pur)
    }

    [Header("Style Atmosphérique Actuel")]
    [SerializeField] private PresetStyle activePreset = PresetStyle.DreamcoreFairy;

    [Header("Lumière Principale (Directional Sun)")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float sunIntensity = 1.8f;
    [SerializeField] private Color sunColor = new Color(1.0f, 0.94f, 0.84f);
    [SerializeField] private Vector3 sunRotation = new Vector3(45f, -35f, 0f);

    [Header("Éclairage Ambiant Tricolore (Sky / Equator / Ground)")]
    [SerializeField] private Color skyColor = new Color(0.35f, 0.68f, 0.92f);
    [SerializeField] private Color equatorColor = new Color(0.25f, 0.35f, 0.52f);
    [SerializeField] private Color groundColor = new Color(0.1f, 0.14f, 0.22f);

    [Header("Brouillard d Atmosphère (Fog)")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogColor = new Color(0.28f, 0.52f, 0.72f);
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
    [SerializeField] private float fogDensity = 0.006f;

    [Header("Lumière d Accentuation de la Cascade (Waterfall Light)")]
    [SerializeField] private bool enableWaterfallGlow = true;
    [SerializeField] private Transform waterfallTarget;
    [SerializeField] private Color waterfallGlowColor = new Color(0.25f, 0.92f, 1.0f);
    [SerializeField] private float waterfallGlowIntensity = 3.8f;
    [SerializeField] private float waterfallGlowRange = 16.0f;

    [Header("Lumière d Accentuation du Joueur / Centre (Player Light)")]
    [SerializeField] private bool enablePlayerFillLight = true;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Color playerFillColor = new Color(1.0f, 0.88f, 0.72f);
    [SerializeField] private float playerFillIntensity = 2.0f;
    [SerializeField] private float playerFillRange = 8.0f;

    private Light waterfallGlowLight;
    private Light playerFillLight;

    private void OnValidate()
    {
        if (enabled)
        {
            ApplyLightingSetup();
        }
    }

    private void Awake()
    {
        ApplyLightingSetup();
    }

    private void Start()
    {
        ApplyLightingSetup();
    }

    /// <summary>
    /// Applique l'ensemble de la configuration d'éclairage atmosphérique à la scène.
    /// </summary>
    [ContextMenu("⚡ Appliquer l Éclairage Scène")]
    public void ApplyLightingSetup()
    {
        // 1. Configuration de la Lumière Directionnelle (Soleil / Clarté)
        SetupDirectionalLight();

        // 2. Configuration de l'Ambiance Globale Tricolore (RenderSettings)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = skyColor;
        RenderSettings.ambientEquatorColor = equatorColor;
        RenderSettings.ambientGroundColor = groundColor;

        // 3. Configuration du Brouillard Atmosphérique (Fog)
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
        }

        // 4. Lumière d'accentuation de la Cascade
        if (enableWaterfallGlow)
        {
            SetupWaterfallLight();
        }
        else if (waterfallGlowLight != null)
        {
            waterfallGlowLight.gameObject.SetActive(false);
        }

        // 5. Lumière d'accentuation du Joueur / Centre
        if (enablePlayerFillLight)
        {
            SetupPlayerFillLight();
        }
        else if (playerFillLight != null)
        {
            playerFillLight.gameObject.SetActive(false);
        }
    }

    private void SetupDirectionalLight()
    {
        if (directionalLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }
        }

        if (directionalLight == null)
        {
            GameObject sunObj = new GameObject("MainDirectionalSun");
            directionalLight = sunObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.intensity = sunIntensity;
        directionalLight.color = sunColor;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.82f;
        directionalLight.transform.rotation = Quaternion.Euler(sunRotation);
    }

    private void SetupWaterfallLight()
    {
        if (waterfallTarget == null)
        {
            CylindricalWaterfall wf = Object.FindFirstObjectByType<CylindricalWaterfall>();
            if (wf != null) waterfallTarget = wf.transform;
            else
            {
                GameObject g = GameObject.Find("Waterfall");
                if (g == null) g = GameObject.Find("CylindricalWaterfall");
                if (g != null) waterfallTarget = g.transform;
            }
        }

        Transform lightObj = transform.Find("WaterfallGlowPointLight");
        if (lightObj == null)
        {
            GameObject go = new GameObject("WaterfallGlowPointLight");
            go.transform.SetParent(transform, false);
            lightObj = go.transform;
        }

        if (waterfallTarget != null)
        {
            lightObj.position = waterfallTarget.position + new Vector3(0f, 3.0f, 0f);
        }

        waterfallGlowLight = lightObj.GetComponent<Light>();
        if (waterfallGlowLight == null)
        {
            waterfallGlowLight = lightObj.gameObject.AddComponent<Light>();
        }

        waterfallGlowLight.gameObject.SetActive(true);
        waterfallGlowLight.type = LightType.Point;
        waterfallGlowLight.color = waterfallGlowColor;
        waterfallGlowLight.intensity = waterfallGlowIntensity;
        waterfallGlowLight.range = waterfallGlowRange;
    }

    private void SetupPlayerFillLight()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        Transform lightObj = transform.Find("PlayerFillPointLight");
        if (lightObj == null)
        {
            GameObject go = new GameObject("PlayerFillPointLight");
            go.transform.SetParent(transform, false);
            lightObj = go.transform;
        }

        if (playerTarget != null)
        {
            lightObj.position = playerTarget.position + new Vector3(0f, 1.8f, -1.0f);
        }

        playerFillLight = lightObj.GetComponent<Light>();
        if (playerFillLight == null)
        {
            playerFillLight = lightObj.gameObject.AddComponent<Light>();
        }

        playerFillLight.gameObject.SetActive(true);
        playerFillLight.type = LightType.Point;
        playerFillLight.color = playerFillColor;
        playerFillLight.intensity = playerFillIntensity;
        playerFillLight.range = playerFillRange;
    }

    [ContextMenu("🎨 Style : Rêve Féerique 2.5D RPG (Cyan / Or / Indigo)")]
    public void PresetDreamcoreFairy()
    {
        activePreset = PresetStyle.DreamcoreFairy;
        sunIntensity = 1.9f;
        sunColor = new Color(1.0f, 0.94f, 0.82f);
        sunRotation = new Vector3(46f, -38f, 0f);

        skyColor = new Color(0.38f, 0.72f, 0.94f);
        equatorColor = new Color(0.28f, 0.38f, 0.58f);
        groundColor = new Color(0.12f, 0.16f, 0.25f);

        enableFog = true;
        fogColor = new Color(0.32f, 0.58f, 0.78f);
        fogDensity = 0.007f;

        waterfallGlowColor = new Color(0.2f, 0.95f, 1.0f);
        waterfallGlowIntensity = 4.2f;
        waterfallGlowRange = 18.0f;

        playerFillColor = new Color(1.0f, 0.92f, 0.76f);
        playerFillIntensity = 2.2f;
        playerFillRange = 9.0f;

        ApplyLightingSetup();
    }

    [ContextMenu("🎨 Style : Crépuscule Magique (Violet / Rose / Or)")]
    public void PresetMagicalTwilight()
    {
        activePreset = PresetStyle.MagicalTwilight;
        sunIntensity = 1.6f;
        sunColor = new Color(1.0f, 0.75f, 0.52f);
        sunRotation = new Vector3(25f, -50f, 0f);

        skyColor = new Color(0.62f, 0.38f, 0.75f);
        equatorColor = new Color(0.42f, 0.25f, 0.52f);
        groundColor = new Color(0.15f, 0.1f, 0.25f);

        enableFog = true;
        fogColor = new Color(0.48f, 0.28f, 0.58f);
        fogDensity = 0.008f;

        waterfallGlowColor = new Color(0.3f, 0.85f, 1.0f);
        waterfallGlowIntensity = 3.5f;

        playerFillColor = new Color(1.0f, 0.85f, 0.65f);
        playerFillIntensity = 1.8f;

        ApplyLightingSetup();
    }

    [ContextMenu("🎨 Style : Clair de Lune Nuit Étoilée (Bleu Nuit / Argent)")]
    public void PresetMoonlightNight()
    {
        activePreset = PresetStyle.MoonlightNight;
        sunIntensity = 0.9f;
        sunColor = new Color(0.45f, 0.65f, 1.0f);
        sunRotation = new Vector3(60f, -20f, 0f);

        skyColor = new Color(0.15f, 0.25f, 0.48f);
        equatorColor = new Color(0.1f, 0.15f, 0.32f);
        groundColor = new Color(0.04f, 0.06f, 0.15f);

        enableFog = true;
        fogColor = new Color(0.12f, 0.2f, 0.38f);
        fogDensity = 0.01f;

        waterfallGlowColor = new Color(0.1f, 0.98f, 1.0f);
        waterfallGlowIntensity = 5.0f;

        playerFillColor = new Color(0.7f, 0.88f, 1.0f);
        playerFillIntensity = 2.5f;

        ApplyLightingSetup();
    }

    private void OnDrawGizmosSelected()
    {
        if (waterfallTarget != null)
        {
            Gizmos.color = waterfallGlowColor;
            Gizmos.DrawWireSphere(waterfallTarget.position + new Vector3(0f, 3f, 0f), waterfallGlowRange * 0.5f);
        }

        if (playerTarget != null)
        {
            Gizmos.color = playerFillColor;
            Gizmos.DrawWireSphere(playerTarget.position + new Vector3(0f, 1.8f, -1f), playerFillRange * 0.5f);
        }
    }
}

