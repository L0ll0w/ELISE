using UnityEngine;

/// <summary>
/// Micro-script de Billboard pour aligner un sprite 2.5D avec la caméra.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/Billboard")]
public class Billboard : MonoBehaviour
{
    public enum BillboardMode
    {
        CameraRotationY,    // Aligne uniquement l'axe Y sur la rotation de la caméra (recommandé pour le RPG 2.5D)
        CameraRotationFull, // Copie exactement la rotation de la caméra
        LookAtCamera        // Fait face directement à la position de la caméra (sphérique)
    }

    [Header("Configuration")]
    [Tooltip("Mode d'alignement du Billboard")]
    [SerializeField] private BillboardMode mode = BillboardMode.CameraRotationY;

    [Tooltip("Si activé, utilise la caméra principale (Camera.main). Sinon, spécifiez une caméra ci-dessous.")]
    [SerializeField] private bool useMainCamera = true;

    [Tooltip("Caméra cible spécifique si 'Use Main Camera' est désactivé.")]
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        // Récupération de la caméra active
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
        // on peut utiliser la caméra de la vue Scène pour que le billboard réagisse en direct.
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
