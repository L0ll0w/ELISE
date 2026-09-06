using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Zone de déclencheur de caméra orbitale orientée vers la cascade.
/// À placer sur un GameObject doté d'un Collider (configuré en IsTrigger) autour de la zone de la cascade.
/// Lorsque le joueur pénètre dans la zone, force la caméra à pivoter continuellement pour rester toujours face à la cascade.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/World/Waterfall Camera Zone")]
public class WaterfallCameraZone : MonoBehaviour
{
    [Header("Cible de la Cascade")]
    [Tooltip("Transform du centre de la cascade cylindrique. Si laissé vide, sera recherché automatiquement dans la scène.")]
    [SerializeField] private Transform waterfallTarget;

    [Header("Réglages Caméra")]
    [Tooltip("Caméra Cinemachine de la zone (si laissée vide, cherchera automatiquement la caméra principale de la scène).")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Tooltip("Inverser la direction du regard à 180° si besoin.")]
    [SerializeField] private bool invertDirection = false;

    [Tooltip("Restaurer l'orientation standard de la caméra lors de la sortie de la zone.")]
    [SerializeField] private bool restoreCameraOnExit = true;

    private CinemachineHelper cameraHelper;

    private void Awake()
    {
        // S'assurer que le Collider est bien un Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        // Recherche automatique de la cascade si non assignée dans l'inspecteur
        if (waterfallTarget == null)
        {
            CylindricalWaterfall waterfall = FindFirstObjectByType<CylindricalWaterfall>();
            if (waterfall != null)
            {
                waterfallTarget = waterfall.transform;
            }
            else
            {
                GameObject wfObj = GameObject.Find("Waterfall");
                if (wfObj == null) wfObj = GameObject.Find("CylindricalWaterfall");
                if (wfObj != null) waterfallTarget = wfObj.transform;
            }
        }
    }

    private void EnsureCameraHelper()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (virtualCamera != null && cameraHelper == null)
        {
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
            if (cameraHelper == null) cameraHelper = virtualCamera.GetComponentInChildren<CinemachineHelper>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Détecter le joueur
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null)
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm == null) pm = other.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                pm.MoveRelativeToCamera = true;
            }

            EnsureCameraHelper();

            if (cameraHelper != null && waterfallTarget != null)
            {
                Debug.Log($"[WaterfallCameraZone] Le joueur entre dans la zone. Orientation dynamique de la caméra vers '{waterfallTarget.name}'.");
                cameraHelper.SetFocalPointTarget(waterfallTarget, true);
            }
            else
            {
                Debug.LogWarning("[WaterfallCameraZone] Impossible de cibler la cascade : CinemachineHelper ou waterfallTarget introuvable.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!restoreCameraOnExit) return;

        // Détecter la sortie du joueur
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null)
        {
            EnsureCameraHelper();

            if (cameraHelper != null)
            {
                Debug.Log("[WaterfallCameraZone] Le joueur sort de la zone. Restauration de l'orientation standard de la caméra.");
                cameraHelper.ClearFocalPointTarget();
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Dessiner le volume du Trigger en bleu translucide dans la vue Scène
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.1f, 0.7f, 1.0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.1f, 0.7f, 1.0f, 0.8f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(0.1f, 0.7f, 1.0f, 0.8f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}
