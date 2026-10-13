using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Zone trigger : quand le joueur entre, la caméra s'oriente vers une nouvelle cible (ex : le soleil),
/// y compris verticalement (elle lève la tête). Quand il sort, elle revient sur le point focal précédent.
/// Garde la priorité sur WaterfallCameraZone tant que le joueur est dedans.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Cinematics/Camera Focal Trigger")]
public class CameraFocalTrigger : MonoBehaviour
{
    [Tooltip("Cible à regarder dans la zone (ex : le Soleil).")]
    [SerializeField] private Transform focusTarget;

    [Tooltip("La caméra lève/baisse aussi la tête pour cadrer la cible (indispensable pour un soleil en hauteur).")]
    [SerializeField] private bool lookVertically = true;

    [Tooltip("Inverser la direction du regard pendant la zone (laisser décoché normalement).")]
    [SerializeField] private bool invertDirection = false;

    [Tooltip("Caméra à contrôler. Laisser vide = trouvée automatiquement.")]
    [SerializeField] private CinemachineHelper cameraHelper;

    [SerializeField] private string playerTag = "Player";

    private Transform previousTarget;
    private bool previousEnabled;
    private bool previousInvert;
    private bool previousVertical;
    private bool isActive;
    private Transform aimProxy;

    /// <summary>Centre visible de la cible (centre du mesh), même si son pivot est ailleurs.</summary>
    private Vector3 GetAimPoint()
    {
        if (focusTarget == null) return Vector3.zero;
        Bounds b = default;
        bool has = false;
        foreach (var r in focusTarget.GetComponentsInChildren<Renderer>())
        {
            if (r.name == "SunHalo" || r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        return has ? b.center : focusTarget.position;
    }

    private Transform GetAimProxy()
    {
        if (aimProxy == null)
        {
            aimProxy = new GameObject("CameraFocalTrigger_AimPoint").transform;
            aimProxy.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        }
        aimProxy.position = GetAimPoint();
        return aimProxy;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        FindHelper();
    }

    private void FindHelper()
    {
        if (cameraHelper != null) return;
        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null) cameraHelper = vcam.GetComponentInChildren<CinemachineHelper>();
        if (cameraHelper == null) cameraHelper = FindFirstObjectByType<CinemachineHelper>();
    }

    private bool IsPlayer(Collider other) =>
        other.CompareTag(playerTag) || other.GetComponentInParent<PlayerMovement>() != null;

    private void OnTriggerEnter(Collider other)
    {
        if (isActive || !IsPlayer(other)) return;
        FindHelper();
        if (focusTarget == null || cameraHelper == null)
        {
            Debug.LogWarning($"[CameraFocalTrigger] {name} : focusTarget ou CinemachineHelper manquant.");
            return;
        }

        previousTarget = cameraHelper.FocalPointTarget;
        previousEnabled = cameraHelper.FaceFocalPoint;
        previousInvert = cameraHelper.InvertFocalDirection;
        previousVertical = cameraHelper.FocalLookVertical;

        isActive = true;
        Apply();
        Debug.Log($"[CameraFocalTrigger] Caméra orientée vers '{focusTarget.name}' (point visé : {GetAimPoint()}, pivot : {focusTarget.position}).");
    }

    private void Update()
    {
        if (!isActive) return;
        if (TeleportFlower.IsAnyFlowerTeleporting)
        {
            Restore();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null && Vector3.Distance(transform.position, player.transform.position) > 25f)
        {
            Restore();
        }
    }

    // Si une autre zone (ex : cascade) réécrit la cible pendant qu'on est dedans, on reprend la main.
    private void LateUpdate()
    {
        if (!isActive || cameraHelper == null) return;
        if (TeleportFlower.IsAnyFlowerTeleporting) return;
        if (aimProxy != null) aimProxy.position = GetAimPoint(); // suit le soleil s'il bouge
        if (cameraHelper.FocalPointTarget != aimProxy) Apply();
    }

    private void Apply()
    {
        cameraHelper.InvertFocalDirection = invertDirection;
        cameraHelper.FocalLookVertical = lookVertically;
        cameraHelper.SetFocalPointTarget(GetAimProxy(), true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isActive || !IsPlayer(other)) return;
        Restore();
    }

    private void OnDisable()
    {
        if (isActive) Restore();
    }

    private void Restore()
    {
        isActive = false;
        if (cameraHelper == null) return;
        cameraHelper.InvertFocalDirection = previousInvert;
        cameraHelper.FocalLookVertical = previousVertical;
        if (previousTarget != null) cameraHelper.SetFocalPointTarget(previousTarget, previousEnabled);
        else cameraHelper.ClearFocalPointTarget();
        Debug.Log($"[CameraFocalTrigger] Retour vers '{(previousTarget ? previousTarget.name : "orientation standard")}'.");
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        if (focusTarget == null) return;
        Vector3 aim = GetAimPoint();
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, aim);
        Gizmos.DrawWireSphere(aim, 1f);
        if ((aim - focusTarget.position).sqrMagnitude > 1f)
        {
            // Ligne rouge = pivot de l'objet (différent du centre visible)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawLine(transform.position, focusTarget.position);
        }
    }

    private void OnDestroy()
    {
        if (aimProxy != null) Destroy(aimProxy.gameObject);
    }
}
