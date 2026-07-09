using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gère la téléportation du joueur vers le camp (ZoneDePoche) en pressant 'H'
/// et le retour à sa position et scène d'origine lors d'un second appui.
/// </summary>
[AddComponentMenu("2.5D RPG/Camp Teleporter")]
public class CampTeleporter : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Nom de la scène de camp.")]
    [SerializeField] private string campSceneName = "ZoneDePoche";

    [Tooltip("Nom de l'objet Point de Spawn dans la scène de camp.")]
    [SerializeField] private string campSpawnPointName = "SpawnPoint";

    // Singleton pour éviter les doublons lors du rechargement des scènes
    private static CampTeleporter instance;

    // Variables pour mémoriser l'endroit d'origine
    private string savedSceneName;
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Système de Singleton pour la persistance du joueur
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si un doublon du joueur est créé en rechargeant la scène, on le détruit
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        bool pressH = false;

        #if ENABLE_INPUT_SYSTEM
        // Détection avec le nouvel Input System
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            pressH = true;
        }
        #else
        // Détection classique
        if (Input.GetKeyDown(KeyCode.H))
        {
            pressH = true;
        }
        #endif

        if (pressH)
        {
            OnPressH();
        }
    }

    private void OnPressH()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == campSceneName)
        {
            // Retour au monde d'origine
            if (!string.IsNullOrEmpty(savedSceneName))
            {
                StartCoroutine(TransitionToScene(savedSceneName, savedPosition, savedRotation));
            }
            else
            {
                Debug.LogWarning("Aucun point de retour mémorisé. Impossible de quitter le camp.");
            }
        }
        else
        {
            // Sauvegarde de l'emplacement actuel (Monde d'origine)
            savedSceneName = currentScene;
            savedPosition = transform.position;
            savedRotation = transform.rotation;

            // Téléportation vers la Zone de Poche
            StartCoroutine(TransitionToCamp());
        }
    }

    private IEnumerator TransitionToCamp()
    {
        isTransitioning = true;

        // Chargement asynchrone de la scène de camp
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(campSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Positionnement du joueur sur le point de spawn de la ZoneDePoche
        GameObject spawnPoint = GameObject.Find(campSpawnPointName);
        if (spawnPoint != null)
        {
            Teleport(spawnPoint.transform.position, spawnPoint.transform.rotation);
            Debug.Log($"[CampTeleporter] Joueur téléporté au point de spawn dans {campSceneName}.");
        }
        else
        {
            // Positionnement par défaut s'il n'y a pas de spawnpoint
            Teleport(new Vector3(0f, 0.5f, -1.8f), Quaternion.identity);
            Debug.LogWarning($"[CampTeleporter] SpawnPoint '{campSpawnPointName}' manquant. Position par défaut appliquée.");
        }

        UpdateCinemachineFollow();
        isTransitioning = false;
    }

    private IEnumerator TransitionToScene(string sceneName, Vector3 targetPos, Quaternion targetRot)
    {
        isTransitioning = true;

        // Chargement asynchrone de la scène d'origine
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Téléportation du joueur à son emplacement de départ
        Teleport(targetPos, targetRot);
        Debug.Log($"[CampTeleporter] Joueur de retour dans {sceneName} à son point d'origine.");

        UpdateCinemachineFollow();
        isTransitioning = false;
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        // Désactive temporairement le CharacterController s'il existe
        // pour forcer le déplacement du transform
        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        transform.position = position;
        transform.rotation = rotation;

        if (controller != null) controller.enabled = true;
    }

    /// <summary>
    /// Met à jour la cible de suivi (Follow) de toutes les caméras virtuelles Cinemachine de la scène active
    /// pour pointer vers l'instance de ce joueur persistant.
    /// </summary>
    private void UpdateCinemachineFollow()
    {
        CinemachineCamera[] vcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera vcam in vcams)
        {
            vcam.Follow = transform;
            
            // Si le helper 2.5D est attaché, on met à jour ses calculs d'angles/offsets
            CinemachineHelper helper = vcam.GetComponent<CinemachineHelper>();
            if (helper != null)
            {
                helper.UpdateCameraSettings();
            }
        }
    }
}
