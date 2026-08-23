using UnityEngine;

/// <summary>
/// Effet visuel procédural de fumée et d'étincelles magiques.
/// Configure automatiquement un ParticleSystem double couche avec des matériaux compatibles URP et s'auto-détruit.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ProceduralSmokeEffect : MonoBehaviour
{
    private void Start()
    {
        // --- COUCHE 1 : FUMÉE ÉPAISSE ---
        ParticleSystem ps = GetComponent<ParticleSystem>();
        
        // Configuration générale
        var main = ps.main;
        main.duration = 2.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 3.0f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = -0.08f; // Légère lévitation de la fumée
        main.stopAction = ParticleSystemStopAction.Destroy; // S'auto-détruit quand c'est fini

        // Émission : Rafale (Burst)
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var burst = new ParticleSystem.Burst(0f, 35f); // 35 particules d'un coup
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        // Forme d'émission : Sphère pour que la fumée se propage dans toutes les directions
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        // Couleur au cours du temps : Violet sombre -> Gris -> Disparition
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient smokeGradient = new Gradient();
        smokeGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.15f, 0.08f, 0.25f), 0.0f), // Violet mystique sombre
                new GradientColorKey(new Color(0.3f, 0.3f, 0.35f), 0.5f),    // Gris fumée
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.15f), // Pic d'opacité rapide
                new GradientAlphaKey(0.4f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f) // Disparition en douceur
            }
        );
        colorModule.color = smokeGradient;

        // Taille au cours du temps : La fumée gonfle en s'élevant
        var sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.25f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(1.0f, 2.2f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Rotation au cours du temps : Rend la fumée plus vivante et tourbillonnante
        var rotationModule = ps.rotationOverLifetime;
        rotationModule.enabled = true;
        rotationModule.z = new ParticleSystem.MinMaxCurve(-60f, 60f);

        // Assigner le matériau de particules pour la fumée (fusion alpha classique)
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.material = CreateParticleMaterial(false);
        }


        // --- COUCHE 2 : ÉTINCELLES MAGIQUES CYANS/BLEUES (ENFANT) ---
        GameObject sparksObj = new GameObject("MagicSparks");
        sparksObj.transform.SetParent(transform, false);
        ParticleSystem sparksPS = sparksObj.AddComponent<ParticleSystem>();

        var sparksMain = sparksPS.main;
        sparksMain.duration = 2.0f;
        sparksMain.loop = false;
        sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        sparksMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
        sparksMain.gravityModifier = -0.15f; // S'élève plus vite

        var sparksEmission = sparksPS.emission;
        sparksEmission.rateOverTime = 0f;
        var sparksBurst = new ParticleSystem.Burst(0f, 25f);
        sparksEmission.SetBursts(new ParticleSystem.Burst[] { sparksBurst });

        var sparksShape = sparksPS.shape;
        sparksShape.shapeType = ParticleSystemShapeType.Cone;
        sparksShape.radius = 0.2f;
        sparksShape.angle = 20f;

        var sparksColor = sparksPS.colorOverLifetime;
        sparksColor.enabled = true;
        Gradient sparksGradient = new Gradient();
        sparksGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.cyan, 0.0f), 
                new GradientColorKey(new Color(0f, 0.4f, 1f), 0.7f), // Bleu magique
                new GradientColorKey(Color.black, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.5f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        sparksColor.color = sparksGradient;

        // Vitesse par rapport à la taille
        var sparksSize = sparksPS.sizeOverLifetime;
        sparksSize.enabled = true;
        AnimationCurve sparksSizeCurve = new AnimationCurve();
        sparksSizeCurve.AddKey(0f, 1.0f);
        sparksSizeCurve.AddKey(0.8f, 0.8f);
        sparksSizeCurve.AddKey(1.0f, 0.0f);
        sparksSize.size = new ParticleSystem.MinMaxCurve(1f, sparksSizeCurve);

        // Assigner le matériau de particules pour les étincelles (mode additif)
        var sparksRenderer = sparksPS.GetComponent<ParticleSystemRenderer>();
        if (sparksRenderer != null)
        {
            sparksRenderer.material = CreateParticleMaterial(true);
        }

        // Lancer les deux systèmes de particules
        ps.Play();
        sparksPS.Play();
    }

    /// <summary>
    /// Crée un matériau de particule URP ou Sprite/Default compatible à la volée avec la texture de rond flou intégrée à Unity.
    /// </summary>
    private Material CreateParticleMaterial(bool isAdditive)
    {
        // 1. Chercher un shader de particules compatible avec le pipeline de rendu actif (URP ou Standard)
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

        // 2. Charger la texture par défaut de rond flou de Unity (présente dans tous les projets)
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

        // 3. Configurer la transparence et les modes de fusion selon le pipeline détecté
        if (isURP)
        {
            // Mode surface : 1 = Transparent
            mat.SetFloat("_Surface", 1f);
            
            // Mode de fusion : 0 = Alpha Blend, 1 = Additive
            mat.SetFloat("_Blend", isAdditive ? 1f : 0f);

            // Facteurs de blend manuels
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", isAdditive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            
            // Désactiver l'écriture dans le buffer de profondeur (ZWrite = Off) pour éviter les carrés opaques
            mat.SetFloat("_ZWrite", 0f);
            mat.SetInt("_ZWrite", 0);
            
            // Désactiver le clipping d'alpha
            mat.SetFloat("_AlphaClip", 0f);
        }
        else
        {
            // Configuration de secours (Standard Pipeline ou simple Sprite)
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_ZWrite", 0f);
            
            if (isAdditive)
            {
                Shader additiveShader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Particles/Additive");
                if (additiveShader != null)
                {
                    mat.shader = additiveShader;
                    if (defaultParticleTex != null)
                    {
                        mat.mainTexture = defaultParticleTex;
                    }
                }
            }
        }

        // Forcer la queue de rendu transparent (s'affiche au-dessus de la géométrie 3D opaque)
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }
}
