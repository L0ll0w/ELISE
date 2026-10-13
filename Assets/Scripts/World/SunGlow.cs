using UnityEngine;

/// <summary>
/// À poser sur l'objet Soleil (sphère). Rend le soleil lumineux sans post-processing :
/// noyau unlit sans fog + halo additif face caméra qui pulse doucement.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/World/Sun Glow")]
public class SunGlow : MonoBehaviour
{
    [Header("Noyau")]
    [SerializeField] private Color coreColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color rimColor = new Color(1f, 0.55f, 0.15f);
    [SerializeField, Range(0.5f, 4f)] private float coreIntensity = 1.3f;

    [Header("Halo")]
    [SerializeField] private Color haloColor = new Color(1f, 0.75f, 0.3f);
    [SerializeField, Range(0f, 4f)] private float haloIntensity = 1.2f;
    [SerializeField, Range(1f, 8f)] private float haloScale = 3f;
    [SerializeField, Range(0.5f, 8f)] private float haloFalloff = 2.2f;

    [Header("Pulsation")]
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 1.2f;

    public Color CoreColor => coreColor;
    public Color RimColor => rimColor;
    public float CoreIntensity => coreIntensity;

    private Material coreMat, haloMat;
    private Transform halo;

    private void Awake() => Setup();
    private void Start() => Setup();
    private void OnEnable() => Setup();

    private void OnValidate()
    {
        if (isActiveAndEnabled) Setup();
    }

    private void Setup()
    {
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            if (coreMat == null)
            {
                var sh = Shader.Find("ELISE/SunCore");
                if (sh == null) { Debug.LogWarning("[SunGlow] Shader ELISE/SunCore introuvable."); return; }
                coreMat = new Material(sh) { name = "SunCore (runtime)", hideFlags = HideFlags.DontSave };
            }
            coreMat.SetColor("_Color", coreColor);
            coreMat.SetColor("_RimColor", rimColor);
            coreMat.SetFloat("_Intensity", coreIntensity);
            rend.sharedMaterials = new Material[] { coreMat };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        if (halo == null)
        {
            var existing = transform.Find("SunHalo");
            if (existing != null) halo = existing;
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "SunHalo";
                go.hideFlags = HideFlags.DontSave;
                var col = go.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                go.transform.SetParent(transform, false);
                halo = go.transform;
            }
        }

        // IMPORTANT : Hérite du calque du Soleil (ex: Grounds/Layer 3) pour éviter d'être masqué par le Layer Culling de la caméra à distance !
        if (halo != null)
        {
            halo.gameObject.layer = gameObject.layer;
        }

        if (haloMat == null)
        {
            var sh = Shader.Find("ELISE/SunHalo");
            if (sh == null) { Debug.LogWarning("[SunGlow] Shader ELISE/SunHalo introuvable."); return; }
            haloMat = new Material(sh) { name = "SunHalo (runtime)", hideFlags = HideFlags.DontSave };
        }
        haloMat.SetColor("_Color", haloColor);
        haloMat.SetFloat("_Falloff", haloFalloff);

        var hr = halo.GetComponent<MeshRenderer>();
        hr.sharedMaterial = haloMat;
        hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hr.receiveShadows = false;

        // Détecte ou attache le contrôleur de clignement des yeux sur l'oeil du Soleil
        var eyeBlinker = GetComponentInChildren<SunEyeBlinker>(true);
        if (eyeBlinker == null)
        {
            Transform eye = transform.Find("EYE");
            if (eye == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child.name.IndexOf("eye", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        eye = child;
                        break;
                    }
                }
            }
            if (eye != null && eye.GetComponent<SunEyeBlinker>() == null)
            {
                eye.gameObject.AddComponent<SunEyeBlinker>();
            }
        }
    }

    private void LateUpdate()
    {
        if (halo == null)
        {
            Setup();
            if (halo == null) return;
        }

        if (haloMat == null)
        {
            Setup();
            if (haloMat == null) return;
        }

        Camera cam = OcclusionCullingManager.MainCamera;
        if (cam == null) cam = Camera.main;

#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
            cam = UnityEditor.SceneView.lastActiveSceneView.camera;
#endif

        if (cam != null)
        {
            // Oriente le billboard face au plan de la caméra pour un rendu parfait dans tous les angles
            halo.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }

        float t = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
        float pulse = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
        halo.localScale = Vector3.one * haloScale * pulse;
        haloMat.SetFloat("_Intensity", haloIntensity * pulse);
    }

    private void OnDisable()
    {
        if (halo != null && halo.gameObject.hideFlags == HideFlags.DontSave && !Application.isPlaying)
        {
            DestroyImmediate(halo.gameObject);
            halo = null;
        }
    }
}
