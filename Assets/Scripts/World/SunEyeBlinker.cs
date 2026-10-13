using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère l'animation de clignement des yeux et l'alignement procédural des paupières solaires
/// sur le soleil d'ELISE.
/// Gère également :
/// 1. Le suivi du joueur du regard en 3D (la pupille s'oriente vers le joueur en temps réel).
/// 2. La rétraction et disparition de la pupille enfant sous les paupières lors du clignement.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/World/Sun Eye Blinker")]
public class SunEyeBlinker : MonoBehaviour
{
    [Header("Clignement Automatique")]
    [Tooltip("Active le clignement procédural autonome")]
    [SerializeField] private bool autoBlink = true;

    [Tooltip("Intervalle minimum entre deux clignements (secondes)")]
    [SerializeField, Range(1f, 10f)] private float minInterval = 2.5f;

    [Tooltip("Intervalle maximum entre deux clignements (secondes)")]
    [SerializeField, Range(1f, 15f)] private float maxInterval = 5.5f;

    [Tooltip("Durée de fermeture de la paupière (secondes)")]
    [SerializeField, Range(0.04f, 0.3f)] private float closeDuration = 0.08f;

    [Tooltip("Durée pendant laquelle l'oeil reste complètement fermé (secondes)")]
    [SerializeField, Range(0.01f, 0.2f)] private float closedHoldDuration = 0.03f;

    [Tooltip("Durée de réouverture de la paupière (secondes)")]
    [SerializeField, Range(0.04f, 0.4f)] private float openDuration = 0.12f;

    [Tooltip("Chance d'effectuer un double-clignement rapide (expressif et vivant)")]
    [SerializeField, Range(0f, 1f)] private float doubleBlinkChance = 0.25f;

    [Header("Contrôle Manuel / Prévisualisation")]
    [Range(0f, 1f)]
    [SerializeField] private float manualBlink = 0f;

    [Header("Suivi du Joueur (Regard 3D)")]
    [Tooltip("La pupille suit du regard le joueur lorsqu'il se déplace dans le monde")]
    [SerializeField] private bool lookAtPlayer = true;

    [Tooltip("Cible à suivre (auto-assignée au GameObject avec le Tag 'Player' si vide)")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("Permet au regard de suivre le joueur partout dans le monde sans se bloquer en butée")]
    [SerializeField] private bool unconstrainedGaze = true;

    [Tooltip("Angle maximal de rotation du regard si unconstrainedGaze est désactivé")]
    [SerializeField, Range(15f, 90f)] private float maxGazeAngle = 80f;

    [Tooltip("Recentre instantanément le regard lors d'une téléportation (évite tout décrochage)")]
    [SerializeField] private bool snapOnTeleport = true;

    [Tooltip("Vitesse de suivi et fluidité d'amorti du regard")]
    [SerializeField, Range(0.5f, 15f)] private float gazeDampingSpeed = 4.0f;

    [Tooltip("Micro-saccades réalistes du regard pour rendre l'oeil vivant et conscient")]
    [SerializeField, Range(0f, 0.05f)] private float microSaccadeAmount = 0.015f;

    [Header("Gestion de la Pupille (Objet Enfant)")]
    [Tooltip("Transform de la pupille enfant (auto-détecté si non renseigné)")]
    [SerializeField] private Transform pupilTransform;

    [Tooltip("MeshRenderer de la pupille enfant (auto-détecté si non renseigné)")]
    [SerializeField] private Renderer pupilRenderer;

    [Tooltip("Rétracte et aplatit la pupille sous les paupières pendant la fermeture")]
    [SerializeField] private bool shrinkPupilOnBlink = true;

    [Tooltip("Seuil de fermeture où la pupille disparaît totalement (ex: 0.85 = 85% fermé)")]
    [SerializeField, Range(0.5f, 1f)] private float pupilDisappearThreshold = 0.85f;

    [Tooltip("Léger recul de la pupille vers le centre de l'oeil (évite tout relief sous la peau solaire)")]
    [SerializeField, Range(0f, 0.3f)] private float pupilRetractDepth = 0.06f;

    [Header("Paramètres Géométriques")]
    [Tooltip("Rayon de l'oeil dans l'espace monde pour calibrer la courbure des paupières")]
    [SerializeField] private float eyeRadius = 5.0f;

    [Tooltip("Oriente les paupières automatiquement par rapport à la caméra active")]
    [SerializeField] private bool alignWithCamera = true;

    [Header("Animation du Regard")]
    [Tooltip("Léger micro-mouvement de texture au repos")]
    [SerializeField] private bool idlePupilDrift = true;
    [SerializeField, Range(0f, 0.1f)] private float driftAmount = 0.02f;
    [SerializeField, Range(0.1f, 2f)] private float driftSpeed = 0.5f;

    [Header("Synchronisation Solaire")]
    [Tooltip("Copie automatiquement les couleurs du soleil (SunGlow) pour une parfaite continuité")]
    [SerializeField] private bool syncWithSunGlow = true;

    // Identifiants de propriétés shader
    private static readonly int propBlinkAmount = Shader.PropertyToID("_BlinkAmount");
    private static readonly int propEyeRadius = Shader.PropertyToID("_EyeRadius");
    private static readonly int propEyeCenterWS = Shader.PropertyToID("_EyeCenterWS");
    private static readonly int propEyeUpWS = Shader.PropertyToID("_EyeUpWS");
    private static readonly int propEyeRightWS = Shader.PropertyToID("_EyeRightWS");
    private static readonly int propPupilOffset = Shader.PropertyToID("_PupilLookOffset");
    private static readonly int propSunColor = Shader.PropertyToID("_SunColor");
    private static readonly int propSunRimColor = Shader.PropertyToID("_SunRimColor");
    private static readonly int propSunIntensity = Shader.PropertyToID("_SunIntensity");

    private Renderer[] childRenderers;
    private MaterialPropertyBlock propertyBlock;
    private SunGlow cachedSunGlow;

    // Cache des transformations initiales de la pupille enfant
    private Vector3 initialPupilScale = Vector3.one;
    private Vector3 initialPupilLocalPos = Vector3.zero;
    private Quaternion initialPupilLocalRotation = Quaternion.identity;
    private Vector3 initialForwardLS = Vector3.forward;
    private Vector3 currentGazeDirWS = Vector3.forward;
    private bool hasCachedPupilInitial;
    private bool hasCachedRotation;
    private Vector3 lastPlayerPos;
    private bool hasLastPlayerPos;

    // État du clignement
    private float nextBlinkTimer;
    private float blinkProgress; // 0 à 1
    private bool isBlinking;
    private int blinkPhase; // 0=closing, 1=holding, 2=opening
    private float phaseTimer;
    private bool isSecondaryBlink;

    private void Awake()
    {
        CacheRenderersAndPupil();
        ResetBlinkTimer();
    }

    private void OnEnable()
    {
        CacheRenderersAndPupil();
        ResetBlinkTimer();
    }

    private void OnDisable()
    {
        RestorePupil();
    }

    private void CacheRenderersAndPupil()
    {
        childRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedSunGlow == null)
        {
            cachedSunGlow = GetComponentInParent<SunGlow>();
            if (cachedSunGlow == null)
            {
                cachedSunGlow = Object.FindFirstObjectByType<SunGlow>();
            }
        }

        // Auto-détection de la pupille enfant si non assignée
        if (pupilTransform == null && childRenderers != null)
        {
            for (int i = 0; i < childRenderers.Length; i++)
            {
                var r = childRenderers[i];
                if (r == null) continue;

                string n = r.name.ToLowerInvariant();
                string parentName = r.transform.parent != null ? r.transform.parent.name.ToLowerInvariant() : "";

                if (n.Contains("pupil") || n.Contains("polysurface1") || parentName.Contains("polysurface1"))
                {
                    pupilRenderer = r;
                    pupilTransform = r.transform;
                    break;
                }
            }
        }

        // Enregistrer la taille, position et rotation de base de la pupille
        if (pupilTransform != null && (!hasCachedPupilInitial || initialPupilScale == Vector3.zero))
        {
            if (pupilTransform.localScale != Vector3.zero)
            {
                initialPupilScale = pupilTransform.localScale;
                initialPupilLocalPos = pupilTransform.localPosition;
                initialPupilLocalRotation = pupilTransform.localRotation;
                hasCachedPupilInitial = true;
            }
        }

        if (pupilTransform != null && !hasCachedRotation)
        {
            Transform parentT = pupilTransform.parent != null ? pupilTransform.parent : transform;
            Vector3 eyeCenterWS = parentT.position;
            Vector3 pupilCenterWS = pupilRenderer != null ? pupilRenderer.bounds.center : pupilTransform.position;
            Vector3 dirWS = (pupilCenterWS - eyeCenterWS).normalized;
            if (dirWS.sqrMagnitude < 0.01f) dirWS = transform.forward;

            currentGazeDirWS = dirWS;
            initialForwardLS = parentT.InverseTransformDirection(dirWS);
            hasCachedRotation = true;
        }

        // Auto-détection du joueur si non assigné
        if (playerTarget == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) playerTarget = pObj.transform;
        }
    }

    private void RestorePupil()
    {
        if (pupilTransform != null && hasCachedPupilInitial)
        {
            pupilTransform.localScale = initialPupilScale;
            pupilTransform.localPosition = initialPupilLocalPos;
            pupilTransform.localRotation = initialPupilLocalRotation;
            if (pupilRenderer != null) pupilRenderer.enabled = true;
        }
    }

    private void ResetBlinkTimer()
    {
        nextBlinkTimer = Random.Range(minInterval, maxInterval);
        isBlinking = false;
        blinkProgress = 0f;
    }

    private void Update()
    {
        if (childRenderers == null || childRenderers.Length == 0)
        {
            CacheRenderersAndPupil();
            if (childRenderers == null || childRenderers.Length == 0) return;
        }

        float dt = Application.isPlaying ? Time.deltaTime : 0.02f;

        // 1. Gestion du regard vers le joueur
        UpdateGazeTracking(dt);

        // 2. Gestion de l'animation temporelle de clignement
        if (Application.isPlaying && autoBlink)
        {
            UpdateBlinkAnimation(dt);
        }
        else if (!Application.isPlaying)
        {
            blinkProgress = manualBlink;
        }

        // 3. Application des états à la géométrie et aux shaders
        ApplyBlinkState();
    }

    private void UpdateGazeTracking(float dt)
    {
        if (pupilTransform == null) return;

        Transform parentT = pupilTransform.parent != null ? pupilTransform.parent : transform;
        Vector3 eyeCenterWS = parentT.position;
        Vector3 restingForwardWS = parentT.TransformDirection(initialForwardLS);

        Vector3 desiredDirWS = restingForwardWS;

        if (lookAtPlayer)
        {
            if (playerTarget == null)
            {
                var pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) playerTarget = pObj.transform;
            }

            if (playerTarget != null)
            {
                Vector3 targetPos = playerTarget.position + Vector3.up * 1.1f; // Hauteur buste
                Vector3 toPlayerWS = (targetPos - eyeCenterWS).normalized;

                // Limitation angulaire si activee
                if (!unconstrainedGaze)
                {
                    float angle = Vector3.Angle(restingForwardWS, toPlayerWS);
                    if (angle > maxGazeAngle)
                    {
                        desiredDirWS = Vector3.Slerp(restingForwardWS, toPlayerWS, maxGazeAngle / angle);
                    }
                    else
                    {
                        desiredDirWS = toPlayerWS;
                    }
                }
                else
                {
                    desiredDirWS = toPlayerWS;
                }

                // Detection de teleportation (deplacement soudain > 4m en 1 frame) : recadrage immediat sans trainer
                if (snapOnTeleport && hasLastPlayerPos && Vector3.Distance(targetPos, lastPlayerPos) > 4.0f)
                {
                    currentGazeDirWS = desiredDirWS;
                }
                lastPlayerPos = targetPos;
                hasLastPlayerPos = true;

                // Micro-saccades realistes
                if (microSaccadeAmount > 0.001f)
                {
                    float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
                    float jitterX = (Mathf.PerlinNoise(t * 1.8f, 0f) - 0.5f) * microSaccadeAmount;
                    float jitterY = (Mathf.PerlinNoise(0f, t * 1.8f) - 0.5f) * microSaccadeAmount;
                    Vector3 jitterOffset = (parentT.right * jitterX) + (parentT.up * jitterY);
                    desiredDirWS = (desiredDirWS + jitterOffset).normalized;
                }
            }
        }

        // Amortissement fluide de la rotation
        float smoothFactor = Mathf.Clamp01(dt * gazeDampingSpeed);
        currentGazeDirWS = Vector3.Slerp(currentGazeDirWS, desiredDirWS, smoothFactor);

        // Application de l'orbite de la pupille sur la sphère
        Quaternion deltaRotWS = Quaternion.FromToRotation(restingForwardWS, currentGazeDirWS);
        Quaternion deltaRotLS = Quaternion.Inverse(parentT.rotation) * deltaRotWS * parentT.rotation;

        pupilTransform.localRotation = deltaRotLS * initialPupilLocalRotation;
    }

    private void UpdateBlinkAnimation(float dt)
    {
        if (!isBlinking)
        {
            nextBlinkTimer -= dt;
            if (nextBlinkTimer <= 0f)
            {
                StartBlink();
            }
            blinkProgress = 0f;
            return;
        }

        phaseTimer += dt;
        switch (blinkPhase)
        {
            case 0: // Fermeture rapide
                float tClose = Mathf.Clamp01(phaseTimer / Mathf.Max(0.001f, closeDuration));
                blinkProgress = Mathf.SmoothStep(0f, 1f, tClose);
                if (phaseTimer >= closeDuration)
                {
                    blinkPhase = 1;
                    phaseTimer = 0f;
                    blinkProgress = 1f;
                }
                break;

            case 1: // Maintien fermé
                blinkProgress = 1f;
                if (phaseTimer >= closedHoldDuration)
                {
                    blinkPhase = 2;
                    phaseTimer = 0f;
                }
                break;

            case 2: // Réouverture fluide
                float tOpen = Mathf.Clamp01(phaseTimer / Mathf.Max(0.001f, openDuration));
                blinkProgress = 1f - Mathf.SmoothStep(0f, 1f, tOpen);
                if (phaseTimer >= openDuration)
                {
                    blinkProgress = 0f;
                    isBlinking = false;

                    // Possibilité de double clignement rapide
                    if (!isSecondaryBlink && Random.value < doubleBlinkChance)
                    {
                        isSecondaryBlink = true;
                        nextBlinkTimer = Random.Range(0.08f, 0.18f);
                    }
                    else
                    {
                        isSecondaryBlink = false;
                        ResetBlinkTimer();
                    }
                }
                break;
        }
    }

    [ContextMenu("Forcer un Clignement")]
    public void StartBlink()
    {
        isBlinking = true;
        blinkPhase = 0;
        phaseTimer = 0f;
        blinkProgress = 0f;
    }

    private void ApplyBlinkState()
    {
        float currentBlink = Application.isPlaying && autoBlink ? blinkProgress : manualBlink;

        // 1. Gestion de la pupille enfant (mise à l'échelle & rétraction pour ne pas dépasser)
        if (pupilTransform != null && shrinkPupilOnBlink && hasCachedPupilInitial)
        {
            if (currentBlink >= pupilDisappearThreshold)
            {
                if (pupilRenderer != null) pupilRenderer.enabled = false;
                pupilTransform.localScale = Vector3.zero;
            }
            else
            {
                if (pupilRenderer != null) pupilRenderer.enabled = true;
                float openRatio = Mathf.Clamp01(1f - (currentBlink / pupilDisappearThreshold));
                float scaleFactor = Mathf.SmoothStep(0f, 1f, openRatio);
                pupilTransform.localScale = initialPupilScale * scaleFactor;

                // Rétraction en profondeur dans l'axe de son regard pour passer sous la paupière
                Transform parentT = pupilTransform.parent != null ? pupilTransform.parent : transform;
                Vector3 gazeDirLS = parentT.InverseTransformDirection(currentGazeDirWS);
                Vector3 retractOffset = -gazeDirLS * (currentBlink * pupilRetractDepth);
                pupilTransform.localPosition = initialPupilLocalPos + retractOffset;
            }
        }

        // 2. Application des propriétés aux shaders
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Vector3 centerWS = transform.position;

        // Calcul des axes d'orientation des paupières
        Vector3 upWS = transform.up;
        Vector3 rightWS = transform.right;

        Camera cam = Camera.main;
#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
        {
            var svCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            if (svCam != null) cam = svCam;
        }
#endif
        if (alignWithCamera && cam != null)
        {
            upWS = cam.transform.up;
            rightWS = cam.transform.right;
        }

        // Micro-mouvement de la pupille au repos
        Vector4 lookOffset = Vector4.zero;
        if (idlePupilDrift)
        {
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            float offsetX = Mathf.Sin(time * driftSpeed * 1.3f) * driftAmount;
            float offsetY = Mathf.Cos(time * driftSpeed * 0.9f) * (driftAmount * 0.7f);
            lookOffset = new Vector4(offsetX, offsetY, 0f, 0f);
        }

        for (int i = 0; i < childRenderers.Length; i++)
        {
            var rend = childRenderers[i];
            if (rend == null) continue;

            rend.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(propBlinkAmount, currentBlink);
            propertyBlock.SetFloat(propEyeRadius, eyeRadius);
            propertyBlock.SetVector(propEyeCenterWS, centerWS);
            propertyBlock.SetVector(propEyeUpWS, upWS);
            propertyBlock.SetVector(propEyeRightWS, rightWS);
            propertyBlock.SetVector(propPupilOffset, lookOffset);

            if (syncWithSunGlow && cachedSunGlow != null)
            {
                propertyBlock.SetColor(propSunColor, cachedSunGlow.CoreColor);
                propertyBlock.SetColor(propSunRimColor, cachedSunGlow.RimColor);
                propertyBlock.SetFloat(propSunIntensity, cachedSunGlow.CoreIntensity);
            }

            rend.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnValidate()
    {
        CacheRenderersAndPupil();
        if (eyeRadius <= 0.1f)
        {
            eyeRadius = 5f;
        }
        ApplyBlinkState();
    }
}
