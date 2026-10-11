using UnityEngine;

/// <summary>
/// Effet visuel procédural d'un nuage de poussière magique féerique (Dust Puff / Disappear effect).
/// Configure automatiquement un ParticleSystem double couche (poussière vaporeuse + étincelles dorées)
/// avec des matériaux compatibles URP/Sprites et s'auto-détruit.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ProceduralDustEffect : MonoBehaviour
{
    private Color dustColor = new Color(0.92f, 0.85f, 0.70f); // Teinte beige sableuse / poussière chaude
    private Color sparkleColor = new Color(1.0f, 0.95f, 0.60f); // Poussière d'étoile dorée

    /// <summary>
    /// Fait apparaître un nuage de poussière magique à la position spécifiée.
    /// </summary>
    public static ProceduralDustEffect Spawn(Vector3 position, Color? customColor = null)
    {
        GameObject dustObj = new GameObject("ProceduralDustEffect");
        dustObj.transform.position = position;
        ProceduralDustEffect effect = dustObj.AddComponent<ProceduralDustEffect>();
        if (customColor.HasValue && customColor.Value != Color.clear && customColor.Value != Color.black)
        {
            effect.SetDustColor(customColor.Value);
        }
        return effect;
    }

    /// <summary>
    /// Personnalise la teinte principale de la poussière.
    /// </summary>
    public void SetDustColor(Color customColor)
    {
        dustColor = Color.Lerp(dustColor, customColor, 0.65f);
        sparkleColor = Color.Lerp(sparkleColor, customColor, 0.5f);
    }

    [Header("Paramètres de Légèreté de la Poussière")]
    [Tooltip("Taille minimale de la poussière douce")]
    [SerializeField] private float dustMinSize = 0.06f;

    [Tooltip("Taille maximale de la poussière douce")]
    [SerializeField] private float dustMaxSize = 0.14f;

    [Tooltip("Opacité maximale de la poussière (transparence légère et vaporeuse)")]
    [Range(0f, 1f)]
    [SerializeField] private float dustMaxAlpha = 0.30f;

    [Tooltip("Vitesse minimale de dérive de la poussière")]
    [SerializeField] private float dustMinSpeed = 0.2f;

    [Tooltip("Vitesse maximale de dérive de la poussière")]
    [SerializeField] private float dustMaxSpeed = 0.55f;

    [Tooltip("Nombre de particules de poussière")]
    [SerializeField] private int dustParticleCount = 14;

    [Header("Grains de Poussière Féerique / Étincelles")]
    [Tooltip("Taille minimale des petits grains scintillants")]
    [SerializeField] private float sparkleMinSize = 0.015f;

    [Tooltip("Taille maximale des petits grains scintillants")]
    [SerializeField] private float sparkleMaxSize = 0.045f;

    [Tooltip("Vitesse des petits grains scintillants")]
    [SerializeField] private float sparkleMinSpeed = 0.25f;

    [Tooltip("Vitesse maximale des petits grains scintillants")]
    [SerializeField] private float sparkleMaxSpeed = 0.75f;

    [Tooltip("Nombre de grains scintillants")]
    [SerializeField] private int sparkleParticleCount = 12;

    private void Start()
    {
        // --- COUCHE 1 : NUAGE DE POUSSIÈRE DOUCE & LÉGÈRE ---
        ParticleSystem ps = GetComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(dustMinSpeed, dustMaxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(dustMinSize, dustMaxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = -0.02f; // Légère lévitation lente
        main.stopAction = ParticleSystemStopAction.Destroy; // S'auto-détruit à la fin

        // Rafale d'émission (Burst) légère
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var burst = new ParticleSystem.Burst(0f, dustParticleCount);
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        // Forme de sphère compacte
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        // Gradient de couleur : voile diaphane, très doux et subtil
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient dustGradient = new Gradient();
        dustGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(dustColor, 0.0f),
                new GradientColorKey(Color.Lerp(dustColor, Color.white, 0.35f), 0.4f),
                new GradientColorKey(dustColor * 0.9f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(dustMaxAlpha, 0.2f),
                new GradientAlphaKey(dustMaxAlpha * 0.5f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorModule.color = dustGradient;

        // Taille : la poussière s'épanouit très subtilement sans grossir excessivement
        var sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.85f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Rotation douce
        var rotModule = ps.rotationOverLifetime;
        rotModule.enabled = true;
        rotModule.z = new ParticleSystem.MinMaxCurve(-30f, 30f);

        // Matériau URP/Transparent
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.material = CreateParticleMaterial(false);
            psRenderer.sortingOrder = 50;
        }

        // --- COUCHE 2 : GRAINS DE POUSSIÈRE FÉERIQUE FINS ET SCINTILLANTS ---
        GameObject sparksObj = new GameObject("DustMotes");
        sparksObj.transform.SetParent(transform, false);
        ParticleSystem sparksPS = sparksObj.AddComponent<ParticleSystem>();

        var sparksMain = sparksPS.main;
        sparksMain.duration = 1.0f;
        sparksMain.loop = false;
        sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(sparkleMinSpeed, sparkleMaxSpeed);
        sparksMain.startSize = new ParticleSystem.MinMaxCurve(sparkleMinSize, sparkleMaxSize);
        sparksMain.gravityModifier = -0.04f;

        var sparksEmission = sparksPS.emission;
        sparksEmission.rateOverTime = 0f;
        var sparksBurst = new ParticleSystem.Burst(0f, sparkleParticleCount);
        sparksEmission.SetBursts(new ParticleSystem.Burst[] { sparksBurst });

        var sparksShape = sparksPS.shape;
        sparksShape.shapeType = ParticleSystemShapeType.Sphere;
        sparksShape.radius = 0.12f;

        var sparksColor = sparksPS.colorOverLifetime;
        sparksColor.enabled = true;
        Gradient sparkGrad = new Gradient();
        sparkGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkleColor, 0.0f),
                new GradientColorKey(Color.white, 0.4f),
                new GradientColorKey(sparkleColor * 0.8f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.7f, 0.2f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        sparksColor.color = sparkGrad;

        var sparksSize = sparksPS.sizeOverLifetime;
        sparksSize.enabled = true;
        AnimationCurve sparkSizeCurve = new AnimationCurve();
        sparkSizeCurve.AddKey(0f, 0.7f);
        sparkSizeCurve.AddKey(0.4f, 1.0f);
        sparkSizeCurve.AddKey(1.0f, 0.0f);
        sparksSize.size = new ParticleSystem.MinMaxCurve(1f, sparkSizeCurve);

        var sparksRenderer = sparksPS.GetComponent<ParticleSystemRenderer>();
        if (sparksRenderer != null)
        {
            sparksRenderer.material = CreateParticleMaterial(true);
            sparksRenderer.sortingOrder = 51;
        }

        // Lancer la lecture
        ps.Play();
        sparksPS.Play();

        // Sécurité auto-destruction
        Destroy(gameObject, 1.8f);
    }

    private Material CreateParticleMaterial(bool isAdditive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool isURP = true;

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
            isURP = false;
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
            isURP = false;
        }

        Material mat = new Material(shader);

        Texture2D defaultParticleTex = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");
        if (defaultParticleTex != null)
        {
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", defaultParticleTex);
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", defaultParticleTex);
            }
        }

        if (isURP)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", isAdditive ? 1f : 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", isAdditive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_AlphaClip", 0f);
        }
        else
        {
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_ZWrite", 0f);
            if (isAdditive)
            {
                Shader additiveShader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Particles/Additive");
                if (additiveShader != null)
                {
                    mat.shader = additiveShader;
                    if (defaultParticleTex != null) mat.mainTexture = defaultParticleTex;
                }
            }
        }

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }
}
