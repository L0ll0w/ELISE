using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Gère l'affichage, l'ouverture/fermeture du menu avec la touche Tab,
/// la navigation entre onglets (Inventaire, Groupe, Paramètres),
/// et le peuplement dynamique du groupe.
/// </summary>
[AddComponentMenu("2.5D RPG/Menu Manager")]
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panneaux UI")]
    [Tooltip("L'objet racine contenant tout le menu (Canvas ou Panel global).")]
    [SerializeField] private GameObject menuRoot;

    [Tooltip("Panneau de l'onglet Inventaire.")]
    [SerializeField] private GameObject inventoryPanel;

    [Tooltip("Panneau de l'onglet Groupe.")]
    [SerializeField] private GameObject groupPanel;

    [Tooltip("Panneau de l'onglet Paramètres.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Configuration Groupe UI")]
    [Tooltip("Conteneur (Content de ScrollView) où seront instanciés les éléments de groupe.")]
    [SerializeField] private Transform groupMembersContainer;

    [Tooltip("Le GameObject contenant la liste des personnages (ScrollView) à masquer lors du profil.")]
    [SerializeField] private GameObject groupListContainer;

    [Tooltip("Le panneau de notes/infos générales à masquer lors du profil.")]
    [SerializeField] private GameObject notePanel;

    [Tooltip("Le panneau de profil de personnage (Char profil) à afficher.")]
    [SerializeField] private GameObject charProfilePanel;

    [Tooltip("Le panneau de statistiques (Stat panel) à afficher.")]
    [SerializeField] private GameObject statPanel;

    [Tooltip("Prefab d'un élément UI de compagnon (contenant le script GroupMemberUIItem et un composant Button).")]
    [SerializeField] private GameObject groupMemberUIItemPrefab;

    [Header("UI Fiche Personnage Details")]
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private Image detailPortraitImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText; // PC
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI offensiveEquipText;
    [SerializeField] private TextMeshProUGUI defensiveEquipText;
    [SerializeField] private TextMeshProUGUI bonusEquipText;

    private bool isMenuOpen = false;

    /// <summary>
    /// Indique si le menu est actuellement ouvert.
    /// </summary>
    public bool IsMenuOpen => isMenuOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Masquer le menu au démarrage
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
        isMenuOpen = false;
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 1. Recibler la caméra principale si le Canvas est en mode Screen Space Camera
        if (menuRoot != null)
        {
            Canvas canvas = menuRoot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = Camera.main;
            }
        }

        // 2. S'assurer qu'il y a un EventSystem actif dans la nouvelle scène
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem (Spawned)");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();

            #if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            #endif

            DontDestroyOnLoad(es);
            Debug.Log("EventSystem recréé automatiquement après changement de scène.");
        }
    }

    private void Update()
    {
        // Écoute de la touche Tab du nouveau système d'input
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            // Vérification si un dialogue est en cours (si DialogueManager existe)
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                Debug.Log("Impossible d'ouvrir le menu : un dialogue est en cours.");
                return;
            }

            ToggleMenu();
        }
    }

    /// <summary>
    /// Alterne l'état d'ouverture du menu.
    /// </summary>
    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// Ouvre le menu, applique la pause et affiche l'onglet par défaut (Inventaire).
    /// </summary>
    public void OpenMenu()
    {
        if (isMenuOpen) return;

        isMenuOpen = true;
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        // Requête de pause globale
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.RequestPause(PauseManager.PauseSource.Menu);
        }

        // Affiche le premier onglet par défaut
        ShowInventory();
    }

    /// <summary>
    /// Ferme le menu et retire la pause.
    /// </summary>
    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        isMenuOpen = false;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        // Retrait de la pause globale
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.RequestUnpause(PauseManager.PauseSource.Menu);
        }
    }

    #region Navigation Onglets

    /// <summary>
    /// Active l'onglet Inventaire et désactive les autres.
    /// </summary>
    public void ShowInventory()
    {
        SetPanelActive(inventoryPanel);
    }

    /// <summary>
    /// Active l'onglet Groupe, désactive les autres et rafraîchit la liste des membres.
    /// </summary>
    public void ShowGroup()
    {
        SetPanelActive(groupPanel);
        HideCharacterDetails(); // Réinitialise l'affichage sur la liste principale au clic sur l'onglet
        PopulateGroupList();
    }

    /// <summary>
    /// Active l'onglet Paramètres et désactive les autres.
    /// </summary>
    public void ShowSettings()
    {
        SetPanelActive(settingsPanel);
    }

    /// <summary>
    /// Helper pour activer un onglet spécifique.
    /// </summary>
    private void SetPanelActive(GameObject panelToActivate)
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(inventoryPanel == panelToActivate);
        if (groupPanel != null) groupPanel.SetActive(groupPanel == panelToActivate);
        if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == panelToActivate);
    }

    #endregion

    #region Gestion du Groupe UI

    /// <summary>
    /// Vide et repeuple dynamique le ScrollView du groupe en incluant le joueur (leader) et ses compagnons.
    /// </summary>
    private void PopulateGroupList()
    {
        if (groupMembersContainer == null || groupMemberUIItemPrefab == null)
        {
            Debug.LogWarning("Les références UI pour l'affichage du groupe ne sont pas configurées dans le MenuManager.");
            return;
        }

        // 1. Vider le conteneur actuel
        foreach (Transform child in groupMembersContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Vérifier si GroupManager est disponible
        if (GroupManager.Instance == null)
        {
            Debug.LogWarning("GroupManager.Instance est introuvable. Impossible d'afficher le groupe.");
            return;
        }

        // 3. Récupérer tous les membres actifs (Leader + Compagnons)
        List<GameObject> allMembers = new List<GameObject>();

        // Ajouter le joueur principal (Leader) en premier
        if (GroupManager.Instance.Leader != null)
        {
            allMembers.Add(GroupManager.Instance.Leader.gameObject);
        }

        // Ajouter ensuite les compagnons qui suivent
        var followers = GroupManager.Instance.ActiveFollowers;
        if (followers != null)
        {
            foreach (var follower in followers)
            {
                if (follower != null)
                {
                    allMembers.Add(follower.gameObject);
                }
            }
        }

        if (allMembers.Count == 0)
        {
            Debug.Log("Aucun membre actif dans le groupe (y compris le joueur principal).");
            return;
        }

        // 4. Instancier et configurer les éléments UI
        for (int i = 0; i < allMembers.Count; i++)
        {
            GameObject memberGo = allMembers[i];
            if (memberGo == null) continue;

            // Récupération des informations du personnage (sur le parent ou un enfant)
            GroupMemberInfo info = memberGo.GetComponent<GroupMemberInfo>();
            if (info == null) info = memberGo.GetComponentInChildren<GroupMemberInfo>();

            string name = info != null ? info.CharacterName : memberGo.name;
            Sprite portrait = info != null ? info.Portrait : null;
            Sprite listIcon = info != null ? info.MenuIcon : null;

            // Sécurité / Fallback : Si aucune icône n'est assignée pour la liste,
            // on tente de récupérer le sprite du SpriteRenderer du personnage (parent ou enfant)
            if (listIcon == null)
            {
                SpriteRenderer sr = memberGo.GetComponent<SpriteRenderer>();
                if (sr == null) sr = memberGo.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) listIcon = sr.sprite;
            }

            // Instanciation de l'item UI
            GameObject itemObj = Instantiate(groupMemberUIItemPrefab, groupMembersContainer);
            GroupMemberUIItem uiItem = itemObj.GetComponent<GroupMemberUIItem>();

            if (uiItem != null)
            {
                // Alternance : pair (0, 2, 4...) -> portrait à gauche (isEven = true)
                // impair (1, 3, 5...) -> portrait à droite (isEven = false)
                bool isEven = (i % 2 == 0);
                uiItem.Setup(name, listIcon, isEven);
            }
            else
            {
                Debug.LogWarning($"Le prefab '{groupMemberUIItemPrefab.name}' ne possède pas le script GroupMemberUIItem.");
            }

            // Configuration du clic de bouton pour afficher les détails du personnage
            Button button = itemObj.GetComponent<Button>();
            if (button != null)
            {
                if (info != null && info.CharacterData != null)
                {
                    CharacterData data = info.CharacterData;
                    button.onClick.AddListener(() => ShowCharacterDetails(data));
                }
                else
                {
                    // Fallback si pas de ScriptableObject : on génère des données virtuelles temporaires
                    CharacterData fallbackData = ScriptableObject.CreateInstance<CharacterData>();
                    fallbackData.characterName = name;
                    fallbackData.portrait = portrait;
                    fallbackData.menuIcon = listIcon;
                    button.onClick.AddListener(() => ShowCharacterDetails(fallbackData));
                }
            }
        }
    }

    /// <summary>
    /// Affiche les détails du personnage cliqué (fiche profil et statistiques) et masque la liste principale.
    /// </summary>
    public void ShowCharacterDetails(CharacterData data)
    {
        if (data == null) return;

        // 1. Activer/Désactiver les panneaux correspondants
        if (groupListContainer != null) groupListContainer.SetActive(false);
        if (notePanel != null) notePanel.SetActive(false);
        if (charProfilePanel != null) charProfilePanel.SetActive(true);
        if (statPanel != null) statPanel.SetActive(true);

        // 2. Remplir les données graphiques et textuelles du profil
        if (detailNameText != null) detailNameText.text = data.characterName;
        if (detailPortraitImage != null)
        {
            detailPortraitImage.sprite = data.portrait;
            detailPortraitImage.enabled = data.portrait != null;
        }

        // Statistiques
        if (levelText != null) levelText.text = $"Niveau : {data.level}";
        if (hpText != null) hpText.text = $"PV : {data.currentHP}/{data.maxHP}";
        if (mpText != null) mpText.text = $"PC : {data.currentMP}/{data.maxMP}";
        if (strengthText != null) strengthText.text = $"Force : {data.strength}";
        if (defenseText != null) defenseText.text = $"Défense : {data.defense}";
        if (speedText != null) speedText.text = $"Vitesse : {data.speed}";

        // Équipements
        if (offensiveEquipText != null) offensiveEquipText.text = $"Arme : {data.offensiveEquipment}";
        if (defensiveEquipText != null) defensiveEquipText.text = $"Armure : {data.defensiveEquipment}";
        if (bonusEquipText != null) bonusEquipText.text = $"Accessoire : {data.bonusEquipment}";
    }

    /// <summary>
    /// Revient à l'affichage de la liste des personnages du groupe (utilisé par le bouton retour).
    /// </summary>
    public void HideCharacterDetails()
    {
        if (groupListContainer != null) groupListContainer.SetActive(true);
        if (notePanel != null) notePanel.SetActive(true);
        if (charProfilePanel != null) charProfilePanel.SetActive(false);
        if (statPanel != null) statPanel.SetActive(false);
    }

    #endregion
}
