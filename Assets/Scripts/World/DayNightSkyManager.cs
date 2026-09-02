using UnityEngine;

/// <summary>
/// Contrôle et anime le ciel divisé Jour / Nuit (DayNightSplitSky shader) en temps réel,
/// tout en adaptant dynamiquement l'intensité, la couleur et l'orientation de la lumière de la scène.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/Day Night Sky Manager")]
public class DayNightSkyManager : MonoBehaviour
{
    [Header("Matériau de Ciel")]
    [Tooltip("Le matériau utilisant le shader Custom/DayNightSplitSky. Si laissé vide, utilise la Skybox actuelle du projet.")]
    [SerializeField] private Material skyboxMaterial;

    [Header("Combat Jour / Nuit")]
    [Range(0f, 1f)]
    [Tooltip("Ratio de contrôle du ciel : 0 = Nuit Totale, 0.5 = 50/50, 1 = Jour Total.")]
    [SerializeField] private float battleRatio = 0.5f;

    [Header("Positionnement dans le Monde")]
    [Tooltip("Activer la transition spatiale. Le ciel change selon la position du joueur/caméra sur la carte par rapport au centre.")]
    [SerializeField] private bool useWorldCenter = true;

    [Tooltip("Le point central de la carte (Transform) où la séparation est à 50/50.")]
    [SerializeField] private Transform worldCenterTarget;

    [Tooltip("Position centrale manuelle si aucun Transform n'est assigné.")]
    [SerializeField] private Vector3 manualWorldCenter = Vector3.zero;

    [Tooltip("Sensibilité du déplacement : ajuste la vitesse de transition du ciel quand le joueur traverse la carte (recommandé: 0.01 pour 100m).")]
    [SerializeField] private float positionSensitivity = 0.01f;

    [Header("Rotation de la Séparation")]
    [Tooltip("Faire pivoter la ligne de séparation en continu.")]
    [SerializeField] private bool rotateSeparation = true;

    [Tooltip("Vitesse de rotation de la séparation (degrés par seconde).")]
    [SerializeField] private float rotationSpeed = 2f;

    [Tooltip("Axe de rotation de la séparation.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Ajustements de la Frontière")]
    [Range(0.01f, 2f)]
    [Tooltip("Épaisseur du fondu de transition entre le Jour et la Nuit.")]
    [SerializeField] private float transitionWidth = 0.2f;

    [Range(0f, 5f)]
    [Tooltip("Intensité de la lueur de néon/rêve à la frontière.")]
    [SerializeField] private float glowIntensity = 1.5f;

    [Header("Réglages de la Lumière (Soleil / Lune)")]
    [Tooltip("La lumière directionnelle principale (si vide, cherche la première lumière directionnelle de la scène).")]
    [SerializeField] private Light directionalLight;

    [Tooltip("Orienter automatiquement la direction de la lumière pour qu'elle provienne du côté ensoleillé du ciel.")]
    [SerializeField] private bool autoOrientLight = true;

    [Range(0.1f, 1.5f)]
    [Tooltip("Inclinaison vers le bas de la lumière (0.1 = rasante, 1.5 = zénithale).")]
    [SerializeField] private float lightDownwardTilt = 0.7f;

    [Header("Intensités & Couleurs (Directional Light)")]
    [SerializeField] private float dayLightIntensity = 1.2f;
    [SerializeField] private float nightLightIntensity = 0.15f;
    [SerializeField] private Color dayLightColor = new Color(1f, 0.95f, 0.85f); // Chaud ensoleillé
    [SerializeField] private Color nightLightColor = new Color(0.15f, 0.2f, 0.4f); // Bleu nuit lunaire

    [Header("Lumière Ambiante (Global)")]
    [Tooltip("Ajuster automatiquement la couleur ambiante globale de la scène (RenderSettings).")]
    [SerializeField] private bool controlAmbientLight = true;
    [SerializeField] private Color dayAmbientColor = new Color(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color nightAmbientColor = new Color(0.03f, 0.03f, 0.07f);

    [Header("Ajustements Herbe (Grass Shader)")]
    [Tooltip("La teinte de couleur appliquée à l'herbe du côté Nuit.")]
    [SerializeField] private Color grassNightTint = new Color(0.25f, 0.15f, 0.5f); // Violet nuit dreamcore

    private float currentAngle = 0f;
    private static readonly int SplitDirectionId = Shader.PropertyToID("_SplitDirection");
    private static readonly int SplitOffsetId = Shader.PropertyToID("_SplitOffset");
    private static readonly int TransitionWidthId = Shader.PropertyToID("_TransitionWidth");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_BoundaryGlowIntensity");

    private void Start()
    {
        InitializeMaterial();
        FindDirectionalLight();
        UpdateSkySettings();
    }

    private void Update()
    {
        if (skyboxMaterial == null) return;

        // Fait tourner la séparation uniquement en mode Play
        if (Application.isPlaying && rotateSeparation)
        {
            currentAngle += rotationSpeed * Time.deltaTime;
            currentAngle %= 360f;
            UpdateSkySettings();
        }
        else if (Application.isPlaying && useWorldCenter)
        {
            // Même sans rotation, on met à jour si la caméra bouge sur la carte
            UpdateSkySettings();
        }
    }

    private void OnValidate()
    {
        InitializeMaterial();
        FindDirectionalLight();
        UpdateSkySettings();
    }

    /// <summary>
    /// Initialise le matériau en ciblant la Skybox de la scène si aucune n'est assignée.
    /// </summary>
    private void InitializeMaterial()
    {
        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
    }

    /// <summary>
    /// Recherche automatique de la lumière directionnelle principale si non assignée.
    /// </summary>
    private void FindDirectionalLight()
    {
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Met à jour les paramètres du shader et des lumières de la scène.
    /// </summary>
    [ContextMenu("Forcer la mise à jour du ciel")]
    public void UpdateSkySettings()
    {
        if (skyboxMaterial == null) return;

        // 1. Calcul du vecteur de direction de séparation en rotation
        Vector3 baseDir = Vector3.right;
        Vector3 rotatedDir = Quaternion.AngleAxis(currentAngle, rotationAxis.normalized) * baseDir;
        skyboxMaterial.SetVector(SplitDirectionId, new Vector4(rotatedDir.x, rotatedDir.y, rotatedDir.z, 0f));

        // 2. Calcul du décalage de position de la caméra par rapport au centre du monde
        float positionOffset = 0f;
        Vector3 centerPos = (worldCenterTarget != null) ? worldCenterTarget.position : manualWorldCenter;
        Camera mainCam = Camera.main;
        Vector3 camPos = mainCam != null ? mainCam.transform.position : Vector3.zero;

        if (useWorldCenter && mainCam != null)
        {
            Vector3 toCam = camPos - centerPos;
            positionOffset = Vector3.Dot(toCam, rotatedDir) * positionSensitivity;
        }

        // 3. Calcul de l'offset de séparation final
        float baseOffset = Mathf.Lerp(-1.5f, 1.5f, battleRatio);
        float splitOffset = Mathf.Clamp(baseOffset + positionOffset, -2.0f, 2.0f);
        skyboxMaterial.SetFloat(SplitOffsetId, splitOffset);

        // 4. Ajustement de la frontière dans le shader
        skyboxMaterial.SetFloat(TransitionWidthId, transitionWidth);
        skyboxMaterial.SetFloat(GlowIntensityId, glowIntensity);

        // 5. Calcul de l'intensité locale au niveau de la caméra pour adapter la lumière
        // splitOffset > 0 signifie que la caméra est dans la zone Jour, < 0 dans la zone Nuit
        float localDayWeight = Mathf.InverseLerp(-transitionWidth * 0.5f, transitionWidth * 0.5f, splitOffset);

        // 6. Mise à jour de la lumière directionnelle
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(nightLightIntensity, dayLightIntensity, localDayWeight);
            directionalLight.color = Color.Lerp(nightLightColor, dayLightColor, localDayWeight);

            if (autoOrientLight)
            {
                // rotatedDir pointe vers le Jour. On oriente la lumière pour qu'elle provienne du Jour et brille vers le bas.
                Vector3 lightForward = -rotatedDir - Vector3.up * lightDownwardTilt;
                directionalLight.transform.rotation = Quaternion.LookRotation(lightForward.normalized);
            }
        }

        // 7. Mise à jour de la lumière ambiante globale de la scène (RenderSettings)
        if (controlAmbientLight)
        {
            RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, localDayWeight);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        // 8. Transmission des variables globales pour affecter l'herbe et d'autres shaders
        Shader.SetGlobalVector("_DayNightSplitDirection", rotatedDir);
        Shader.SetGlobalVector("_DayNightWorldCenter", centerPos);
        Shader.SetGlobalFloat("_DayNightBaseOffset", baseOffset);
        Shader.SetGlobalFloat("_DayNightPositionSensitivity", positionSensitivity);
        Shader.SetGlobalFloat("_DayNightTransitionWidth", transitionWidth);
        Shader.SetGlobalColor("_DayNightGrassNightTint", grassNightTint);

        // Force le rafraîchissement visuel de la Skybox dans l'éditeur
        #if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
        #endif
    }
}
