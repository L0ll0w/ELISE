using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Aide à la configuration et au contrôle d'une caméra 2.5D avec Cinemachine 3.x.
/// Ce script s'applique sur le GameObject de la Cinemachine Camera.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[AddComponentMenu("2.5D RPG/Cinemachine 2.5D Helper")]
public class CinemachineHelper : MonoBehaviour
{
    [Header("Cible à suivre")]
    [SerializeField] private Transform targetPlayer;

    [Header("Réglages de la Perspective 2.5D")]
    [Tooltip("Distance de recul (Z) de la caméra par rapport au joueur.")]
    [SerializeField] private float distance = 10f;

    [Tooltip("Hauteur (Y) de la caméra par rapport au joueur.")]
    [SerializeField] private float height = 4f;

    [Tooltip("Inclinaison verticale de la caméra (X Axis Rotation) en degrés.")]
    [Range(0f, 85f)]
    [SerializeField] private float pitchAngle = 20f;

    private CinemachineCamera cinemachineCamera;

    private void OnValidate()
    {
        UpdateCameraSettings();
    }

    private void Start()
    {
        UpdateCameraSettings();
    }

    public void UpdateCameraSettings()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (cinemachineCamera == null) return;

        // Assigne le joueur en tant que cible de suivi (Follow)
        if (targetPlayer == null)
        {
            // Recherche automatique du joueur persistant
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerObj = pm.gameObject;
            }

            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
        }

        if (targetPlayer != null)
        {
            cinemachineCamera.Follow = targetPlayer;
        }

        // Pour un jeu 2.5D avec des sprites plats (style Octopath Traveler / Paper Mario),
        // il est CRITIQUE de NE PAS utiliser de cible "LookAt" (laisser à None/Null).
        // Si la caméra s'oriente pour regarder le joueur, les sprites plats subiront
        // une distorsion de perspective. La caméra doit rester parallèle aux axes du monde.
        cinemachineCamera.LookAt = null;

        // Applique l'angle fixe (Pitch) sur la rotation de la caméra virtuelle
        transform.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);

        // Récupère ou configure le composant CinemachineFollow (le remplaçant du Transposer en v3)
        CinemachineFollow followComponent = GetComponent<CinemachineFollow>();
        if (followComponent == null)
        {
            followComponent = GetComponentInChildren<CinemachineFollow>();
        }

        if (followComponent != null)
        {
            // Force le mode de liaison en World Space pour que les décalages soient absolus par rapport au monde
            followComponent.TrackerSettings.BindingMode = BindingMode.WorldSpace;

            // Applique l'offset de positionnement (X = 0 pour rester centré, Y = hauteur, Z = -distance)
            followComponent.FollowOffset = new Vector3(0f, height, -distance);
        }
    }
}
