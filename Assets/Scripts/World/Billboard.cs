using UnityEngine;

/// <summary>
/// Micro-script de Billboard pour aligner un sprite 2.5D avec la caméra ou une cible (ex: le Joueur).
/// Gère intelligemment les Rigidbodies pour préserver l'interpolation physique sans provoquer de saccades.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(9999)]
[AddComponentMenu("2.5D RPG/Billboard")]
public class Billboard : MonoBehaviour
{
    public enum BillboardMode
    {
        CameraRotationY,    // Aligne uniquement l'axe Y sur la rotation de la caméra (recommandé pour le RPG 2.5D)
        CameraRotationFull, // Copie exactement la rotation de la caméra
        LookAtCamera,       // Fait face directement à la position de la caméra (sphérique)
        LookAtTarget,       // Fait face directement à un Transform cible (ex: le Joueur) sur tous les axes
        LookAtTargetY       // Fait face au Transform cible en pivotant uniquement sur l'axe Y (recommandé pour préserver l'inclinaison)
    }

    [Header("Configuration")]
    [Tooltip("Mode d'alignement du Billboard")]
    [SerializeField] private BillboardMode mode = BillboardMode.CameraRotationY;

    [Header("Cible spécifique (Joueur / Transform)")]
    [Tooltip("Le Transform à regarder (utilisé si mode est LookAtTarget ou LookAtTargetY). Si laissé vide, cherchera automatiquement le joueur via le tag 'Player'.")]
    [SerializeField] private Transform targetTransform;

    [Header("Paramètres Caméra")]
    [Tooltip("Si activé, utilise la caméra principale (Camera.main) pour les modes caméra. Sinon, spécifiez une caméra ci-dessous.")]
    [SerializeField] private bool useMainCamera = true;

    [Tooltip("Caméra cible spécifique si 'Use Main Camera' est désactivé.")]
    [SerializeField] private Camera targetCamera;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // En mode jeu, si le GameObject possède un Rigidbody physique actif, la rotation doit être appliquée en FixedUpdate
        if (Application.isPlaying && rb != null && !rb.isKinematic)
        {
            UpdateRotation(true);
        }
    }

    private void LateUpdate()
    {
        // Si pas de Rigidbody physique actif ou en mode édition dans Unity Editor, utiliser LateUpdate
        if (!Application.isPlaying || rb == null || rb.isKinematic)
        {
            UpdateRotation(false);
        }
    }

    private void UpdateRotation(bool usePhysics)
    {
        Quaternion targetRotation = transform.rotation;
        bool hasTargetRotation = false;

        // --- 1. GESTION DES MODES LOOK AT CIBLE (JOUEUR / TRANSFORM) ---
        if (mode == BillboardMode.LookAtTarget || mode == BillboardMode.LookAtTargetY)
        {
            if (targetTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    targetTransform = player.transform;
                }
            }

            if (targetTransform == null) return;

            Vector3 dir = targetTransform.position - transform.position;
            if (mode == BillboardMode.LookAtTargetY)
            {
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(dir);
                hasTargetRotation = true;
            }
        }
        else
        {
            // --- 2. GESTION DES MODES CAMERA ---
            Camera activeCamera = useMainCamera ? Camera.main : targetCamera;

            #if UNITY_EDITOR
            if (activeCamera == null && !Application.isPlaying)
            {
                activeCamera = UnityEditor.SceneView.lastActiveSceneView != null 
                    ? UnityEditor.SceneView.lastActiveSceneView.camera 
                    : null;
            }
            #endif

            if (activeCamera == null) return;

            switch (mode)
            {
                case BillboardMode.CameraRotationY:
                    targetRotation = Quaternion.Euler(0f, activeCamera.transform.rotation.eulerAngles.y, 0f);
                    hasTargetRotation = true;
                    break;

                case BillboardMode.CameraRotationFull:
                    targetRotation = activeCamera.transform.rotation;
                    hasTargetRotation = true;
                    break;

                case BillboardMode.LookAtCamera:
                    Vector3 camForward = activeCamera.transform.rotation * Vector3.forward;
                    Vector3 camUp = activeCamera.transform.rotation * Vector3.up;
                    targetRotation = Quaternion.LookRotation(camForward, camUp);
                    hasTargetRotation = true;
                    break;
            }
        }

        if (hasTargetRotation)
        {
            if (usePhysics && rb != null && !rb.isKinematic)
            {
                if (Quaternion.Angle(rb.rotation, targetRotation) > 0.01f)
                {
                    rb.MoveRotation(targetRotation);
                }
            }
            else
            {
                if (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
                {
                    transform.rotation = targetRotation;
                }
            }
        }
    }
}
