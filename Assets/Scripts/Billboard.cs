using UnityEngine;

/// <summary>
/// Micro-script de Billboard pour aligner un sprite 2.5D avec la caméra ou une cible (ex: le Joueur).
/// </summary>
[ExecuteAlways]
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

    private void LateUpdate()
    {
        // --- 1. GESTION DES MODES LOOK AT CIBLE (JOUEUR / TRANSFORM) ---
        if (mode == BillboardMode.LookAtTarget || mode == BillboardMode.LookAtTargetY)
        {
            // Recherche automatique du joueur par son tag s'il n'est pas assigné
            if (targetTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    targetTransform = player.transform;
                }
            }

            if (targetTransform == null) return;

            if (mode == BillboardMode.LookAtTarget)
            {
                // Regarde directement la cible sur tous les axes (3D complet)
                transform.LookAt(targetTransform.position);
            }
            else // LookAtTargetY
            {
                // Regarde la cible uniquement en pivotant sur l'axe Y (conserve l'herbe/le sprite bien droit verticalement)
                Vector3 targetPos = targetTransform.position;
                targetPos.y = transform.position.y; // Aligne la hauteur sur celle du sprite
                transform.LookAt(targetPos);
            }
            return;
        }

        // --- 2. GESTION DES MODES CAMERA ---
        Camera activeCamera = null;
        if (useMainCamera)
        {
            activeCamera = Camera.main;
        }
        else
        {
            activeCamera = targetCamera;
        }

        // En mode édition dans Unity, si Camera.main n'est pas disponible,
        // on utilise la caméra de la vue Scène pour que le billboard réagisse en direct.
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
                // Aligne la rotation sur l'axe Y de la caméra uniquement
                transform.rotation = Quaternion.Euler(0f, activeCamera.transform.rotation.eulerAngles.y, 0f);
                break;

            case BillboardMode.CameraRotationFull:
                // Copie exactement la rotation de la caméra
                transform.rotation = activeCamera.transform.rotation;
                break;

            case BillboardMode.LookAtCamera:
                // Fait face directement à la caméra (tous les axes)
                transform.LookAt(transform.position + activeCamera.transform.rotation * Vector3.forward,
                                 activeCamera.transform.rotation * Vector3.up);
                break;
        }
    }
}
