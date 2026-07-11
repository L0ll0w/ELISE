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

    [Header("Configuration Inventaire UI")]
    [Tooltip("Conteneur pour les objets de type Équipement.")]
    [SerializeField] private Transform equipmentContainer;

    [Tooltip("Conteneur pour les objets de type Standard (Items).")]
    [SerializeField] private Transform itemsContainer;

    [Tooltip("Conteneur pour les objets de type Clé.")]
    [SerializeField] private Transform keysContainer;

    [Tooltip("Prefab d'un slot d'inventaire (contenant le script InventorySlotUI).")]
    [SerializeField] private GameObject inventorySlotPrefab;

    [Tooltip("Zone de texte globale de description d'objet (ailleurs dans le menu d'inventaire).")]
    [SerializeField] private TextMeshProUGUI inventoryDescriptionText;

    [Tooltip("Zone de texte globale du nom d'objet (dans le panneau de description de l'inventaire).")]
    [SerializeField] private TextMeshProUGUI inventoryItemNameText;

    [Header("Scroll Views de l'Inventaire")]
    [Tooltip("La Scroll View contenant les items consommables.")]
    [SerializeField] private GameObject scrollViewItems;

    [Tooltip("La Scroll View contenant les équipements.")]
    [SerializeField] private GameObject scrollViewEquip;

    [Tooltip("La Scroll View contenant les clés.")]
    [SerializeField] private GameObject scrollViewKeys;

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

    [Header("UI Equipement boutons (Optionnels)")]
    [SerializeField] private Button offensiveEquipButton;
    [SerializeField] private Button defensiveEquipButton;
    [SerializeField] private Button bonusEquipButton;

    [Header("Configuration Equip Panel")]
    [Tooltip("Le panneau EquipPanel à activer lors du choix d'un équipement.")]
    [SerializeField] private GameObject equipPanel;

    [Tooltip("Conteneur des éléments de la liste des équipements dans l'EquipPanel.")]
    [SerializeField] private Transform equipItemsContainer;

    [Tooltip("Prefab d'un bouton d'équipement dans l'EquipPanel (doit avoir un Button et un TextMeshProUGUI).")]
    [SerializeField] private GameObject equipItemButtonPrefab;

    [Header("Controller Navigation Configuration")]
    [Tooltip("Bouton de l'onglet Inventaire pour y placer le focus.")]
    [SerializeField] private Button inventoryTabButton;
    [Tooltip("Bouton de l'onglet Groupe pour y placer le focus.")]
    [SerializeField] private Button groupTabButton;
    [Tooltip("Bouton de l'onglet Paramètres pour y placer le focus.")]
    [SerializeField] private Button settingsTabButton;

    private CharacterData selectedCharacterData;
    private bool isMenuOpen = false;
    private InventorySlotUI selectedSlotUI;

    private int currentTab = 0; // 0: Inventaire, 1: Groupe, 2: Paramètres
    private GameObject lastSelectedSlotButton;
    private GameObject lastSelectedGroupMemberButton;

    private GameObject previousSelectedObject;
    private Color previousObjectOriginalColor = Color.white;
    private bool hasOriginalColorStored = false;

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

        // Auto-détection des boutons sur les textes d'équipement s'ils ne sont pas assignés
        if (offensiveEquipButton == null && offensiveEquipText != null) offensiveEquipButton = offensiveEquipText.GetComponent<Button>();
        if (offensiveEquipButton == null && offensiveEquipText != null) offensiveEquipButton = offensiveEquipText.GetComponentInParent<Button>();

        if (defensiveEquipButton == null && defensiveEquipText != null) defensiveEquipButton = defensiveEquipText.GetComponent<Button>();
        if (defensiveEquipButton == null && defensiveEquipText != null) defensiveEquipButton = defensiveEquipText.GetComponentInParent<Button>();

        if (bonusEquipButton == null && bonusEquipText != null) bonusEquipButton = bonusEquipText.GetComponent<Button>();
        if (bonusEquipButton == null && bonusEquipText != null) bonusEquipButton = bonusEquipText.GetComponentInParent<Button>();

        // Affectation des listeners
        if (offensiveEquipButton != null) offensiveEquipButton.onClick.AddListener(() => OpenEquipPanel(EquipmentType.Offensive));
        if (defensiveEquipButton != null) defensiveEquipButton.onClick.AddListener(() => OpenEquipPanel(EquipmentType.Defensive));
        if (bonusEquipButton != null) bonusEquipButton.onClick.AddListener(() => OpenEquipPanel(EquipmentType.Bonus));

        // Masquer l'EquipPanel au départ
        if (equipPanel != null)
        {
            equipPanel.SetActive(false);
        }
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
        bool toggle = false;

        // Écoute de la touche Tab (Clavier)
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            toggle = true;
        }

        // Écoute du bouton Y / Triangle (Manette)
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame)
        {
            toggle = true;
        }

        if (toggle)
        {
            // Vérification si un dialogue est en cours (si DialogueManager existe)
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                Debug.Log("Impossible d'ouvrir/fermer le menu : un dialogue est en cours.");
                return;
            }

            ToggleMenu();
        }

        // Contrôles manette quand le menu est ouvert
        if (isMenuOpen)
        {
            UpdateSelectionHighlight();

            if (gamepad != null)
            {
                // Navigation par onglets avec LB/RB
                if (gamepad.leftShoulder.wasPressedThisFrame)
                {
                    CycleTab(-1);
                }
                else if (gamepad.rightShoulder.wasPressedThisFrame)
                {
                    CycleTab(1);
                }

                // Bouton Retour (B / Cercle)
                if (gamepad.buttonEast.wasPressedThisFrame)
                {
                    if (equipPanel != null && equipPanel.activeSelf)
                    {
                        CloseEquipPanel();
                    }
                    else if (charProfilePanel != null && charProfilePanel.activeSelf)
                    {
                        HideCharacterDetails();
                    }
                    else
                    {
                        CloseMenu();
                    }
                }
            }
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

        // Placer le focus EventSystem sur le bouton d'onglet Inventaire pour la navigation manette
        if (UnityEngine.EventSystems.EventSystem.current != null && inventoryTabButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(inventoryTabButton.gameObject);
        }
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

        CloseEquipPanel();
        ResetSelectionHighlight();
    }

    #region Navigation Onglets

    /// <summary>
    /// Active l'onglet Inventaire et désactive les autres.
    /// </summary>
    public void ShowInventory()
    {
        currentTab = 0;
        SetPanelActive(inventoryPanel);
        if (inventoryItemNameText != null)
        {
            inventoryItemNameText.text = "";
        }
        if (inventoryDescriptionText != null)
        {
            inventoryDescriptionText.text = "Sélectionnez un objet pour voir sa description.";
        }
        PopulateInventory();
        ShowInventoryItemsTab(); // Par défaut, on affiche le scroll view des items
    }

    /// <summary>
    /// Active l'onglet Groupe, désactive les autres et rafraîchit la liste des membres.
    /// </summary>
    public void ShowGroup()
    {
        currentTab = 1;
        SetPanelActive(groupPanel);
        HideCharacterDetails(); // Réinitialise l'affichage sur la liste principale au clic sur l'onglet
        PopulateGroupList();
    }

    /// <summary>
    /// Active l'onglet Paramètres et désactive les autres.
    /// </summary>
    public void ShowSettings()
    {
        currentTab = 2;
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
        CloseEquipPanel();
    }

    /// <summary>
    /// Cycle entre les onglets du menu principal (LB / RB).
    /// </summary>
    private void CycleTab(int direction)
    {
        currentTab += direction;
        if (currentTab < 0) currentTab = 2;
        else if (currentTab > 2) currentTab = 0;

        if (currentTab == 0) ShowInventory();
        else if (currentTab == 1) ShowGroup();
        else if (currentTab == 2) ShowSettings();

        // Placer le focus EventSystem sur le bouton d'onglet actif
        GameObject tabToSelect = null;
        if (currentTab == 0 && inventoryTabButton != null) tabToSelect = inventoryTabButton.gameObject;
        else if (currentTab == 1 && groupTabButton != null) tabToSelect = groupTabButton.gameObject;
        else if (currentTab == 2 && settingsTabButton != null) tabToSelect = settingsTabButton.gameObject;

        if (UnityEngine.EventSystems.EventSystem.current != null && tabToSelect != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(tabToSelect);
        }
    }

    #endregion

    #region Gestion de l'Inventaire UI

    /// <summary>
    /// Vide et repeuple dynamiquement l'UI d'inventaire par catégories d'objets.
    /// </summary>
    public void PopulateInventory()
    {
        // Réinitialise la case sélectionnée avant de repeupler
        selectedSlotUI = null;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance est introuvable. Impossible d'afficher l'inventaire.");
            return;
        }

        if (inventorySlotPrefab == null)
        {
            Debug.LogWarning("Le prefab 'Inventory Slot Prefab' n'est pas assigné dans le MenuManager.");
            return;
        }

        // 1. Remplir la catégorie Équipements
        PopulateCategory(ItemType.Equipment, equipmentContainer);

        // 2. Remplir la catégorie Items
        PopulateCategory(ItemType.Item, itemsContainer);

        // 3. Remplir la catégorie Clés
        PopulateCategory(ItemType.Key, keysContainer);
    }

    /// <summary>
    /// Helper pour vider et repeupler un conteneur d'inventaire spécifique.
    /// </summary>
    private void PopulateCategory(ItemType type, Transform container)
    {
        if (container == null) return;

        // Vider le conteneur
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Récupérer les objets de ce type
        List<InventoryEntry> entries = InventoryManager.Instance.GetItemsByType(type);

        // Instancier les slots
        foreach (var entry in entries)
        {
            if (entry == null || entry.item == null) continue;

            GameObject slotObj = Instantiate(inventorySlotPrefab, container);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(entry.item, entry.quantity);
            }
            else
            {
                Debug.LogWarning($"Le prefab '{inventorySlotPrefab.name}' ne possède pas le script InventorySlotUI.");
            }

            // Liaison du clic sur le bouton pour sélectionner la case et afficher la description globale
            Button button = slotObj.GetComponent<Button>();
            if (button != null && slotUI != null)
            {
                ItemData item = entry.item;
                InventorySlotUI targetSlot = slotUI;
                button.onClick.AddListener(() => OnSlotClicked(targetSlot, item));
            }
        }
    }

    /// <summary>
    /// Gère la sélection visuelle d'un slot et affiche son nom et sa description.
    /// </summary>
    private void OnSlotClicked(InventorySlotUI clickedSlot, ItemData item)
    {
        // 1. Désélectionner le précédent slot
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
        }

        // 2. Sélectionner le nouveau slot
        selectedSlotUI = clickedSlot;
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(true);
        }

        // 3. Afficher la description globale
        ShowItemDescription(item);
    }

    /// <summary>
    /// Affiche la description d'un objet sélectionné dans la zone de description globale de l'inventaire.
    /// </summary>
    public void ShowItemDescription(ItemData item)
    {
        if (item != null)
        {
            if (inventoryItemNameText != null)
            {
                inventoryItemNameText.text = item.itemName;
            }
            if (inventoryDescriptionText != null)
            {
                inventoryDescriptionText.text = item.description;
            }
        }
        else
        {
            if (inventoryItemNameText != null)
            {
                inventoryItemNameText.text = "";
            }
            if (inventoryDescriptionText != null)
            {
                inventoryDescriptionText.text = "Sélectionnez un objet pour voir sa description.";
            }
        }
    }

    /// <summary>
    /// Affiche la Scroll View des items standard (consommables) et masque les autres.
    /// </summary>
    public void ShowInventoryItemsTab()
    {
        if (scrollViewItems != null) scrollViewItems.SetActive(true);
        if (scrollViewEquip != null) scrollViewEquip.SetActive(false);
        if (scrollViewKeys != null) scrollViewKeys.SetActive(false);
    }

    /// <summary>
    /// Affiche la Scroll View des équipements et masque les autres.
    /// </summary>
    public void ShowInventoryEquipmentsTab()
    {
        if (scrollViewItems != null) scrollViewItems.SetActive(false);
        if (scrollViewEquip != null) scrollViewEquip.SetActive(true);
        if (scrollViewKeys != null) scrollViewKeys.SetActive(false);
    }

    /// <summary>
    /// Affiche la Scroll View des clés / objets de quête et masque les autres.
    /// </summary>
    public void ShowInventoryKeysTab()
    {
        if (scrollViewItems != null) scrollViewItems.SetActive(false);
        if (scrollViewEquip != null) scrollViewEquip.SetActive(false);
        if (scrollViewKeys != null) scrollViewKeys.SetActive(true);
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
                GameObject targetBtnObj = button.gameObject;
                if (info != null && info.CharacterData != null)
                {
                    CharacterData data = info.CharacterData;
                    button.onClick.AddListener(() => {
                        lastSelectedGroupMemberButton = targetBtnObj;
                        ShowCharacterDetails(data);
                    });
                }
                else
                {
                    // Fallback si pas de ScriptableObject : on génère des données virtuelles temporaires
                    CharacterData fallbackData = ScriptableObject.CreateInstance<CharacterData>();
                    fallbackData.characterName = name;
                    fallbackData.portrait = portrait;
                    fallbackData.menuIcon = listIcon;
                    button.onClick.AddListener(() => {
                        lastSelectedGroupMemberButton = targetBtnObj;
                        ShowCharacterDetails(fallbackData);
                    });
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

        selectedCharacterData = data;

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
        if (offensiveEquipText != null) offensiveEquipText.text = $"Arme : {(data.offensiveEquipment != null ? data.offensiveEquipment.itemName : "Aucun")}";
        if (defensiveEquipText != null) defensiveEquipText.text = $"Armure : {(data.defensiveEquipment != null ? data.defensiveEquipment.itemName : "Aucun")}";
        if (bonusEquipText != null) bonusEquipText.text = $"Accessoire : {(data.bonusEquipment != null ? data.bonusEquipment.itemName : "Aucun")}";

        // Placer le focus EventSystem sur le bouton d'équipement offensif pour manette
        if (UnityEngine.EventSystems.EventSystem.current != null && offensiveEquipButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(offensiveEquipButton.gameObject);
        }
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
        CloseEquipPanel();

        // Restaurer le focus EventSystem sur le compagnon précédemment sélectionné
        if (UnityEngine.EventSystems.EventSystem.current != null && lastSelectedGroupMemberButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(lastSelectedGroupMemberButton);
        }
    }

    #region Gestion Equipement
    
    /// <summary>
    /// Récupère la liste de tous les CharacterData du groupe (leader + compagnons).
    /// </summary>
    public List<CharacterData> GetAllGroupMembersData()
    {
        List<CharacterData> list = new List<CharacterData>();
        
        if (GroupManager.Instance != null)
        {
            // Leader
            if (GroupManager.Instance.Leader != null)
            {
                GroupMemberInfo info = GroupManager.Instance.Leader.GetComponent<GroupMemberInfo>();
                if (info == null) info = GroupManager.Instance.Leader.GetComponentInChildren<GroupMemberInfo>();
                if (info != null && info.CharacterData != null)
                {
                    list.Add(info.CharacterData);
                }
            }
            
            // Followers
            if (GroupManager.Instance.ActiveFollowers != null)
            {
                foreach (var follower in GroupManager.Instance.ActiveFollowers)
                {
                    if (follower != null)
                    {
                        GroupMemberInfo info = follower.GetComponent<GroupMemberInfo>();
                        if (info == null) info = follower.GetComponentInChildren<GroupMemberInfo>();
                        if (info != null && info.CharacterData != null)
                        {
                            list.Add(info.CharacterData);
                        }
                    }
                }
            }
        }
        
        return list;
    }

    /// <summary>
    /// Ouvre le panneau EquipPanel pour choisir un équipement de la catégorie donnée.
    /// </summary>
    public void OpenEquipPanel(EquipmentType category)
    {
        if (selectedCharacterData == null || equipPanel == null || equipItemsContainer == null) return;

        // Enregistrer le bouton de slot actuellement sélectionné pour pouvoir y revenir
        lastSelectedSlotButton = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;

        equipPanel.SetActive(true);

        // Vider le conteneur
        foreach (Transform child in equipItemsContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject firstSelected = null;

        // 1. Bouton "Aucun" (pour déséquiper)
        if (equipItemButtonPrefab != null)
        {
            GameObject noneButtonObj = Instantiate(equipItemButtonPrefab, equipItemsContainer);
            firstSelected = noneButtonObj;
            Button btn = noneButtonObj.GetComponent<Button>();
            TextMeshProUGUI txt = noneButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = "Aucun";
                txt.color = Color.white;
            }
            if (btn != null)
            {
                btn.onClick.AddListener(() => EquipItem(null, category, false));
            }

            // Désactiver toute image enfant sur le bouton Aucun
            Image[] images = noneButtonObj.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != noneButtonObj)
                {
                    img.enabled = false;
                }
            }
        }

        // 2. Parcourir les équipements dans l'inventaire
        if (InventoryManager.Instance != null)
        {
            List<InventoryEntry> allEquipments = InventoryManager.Instance.GetItemsByType(ItemType.Equipment);
            List<CharacterData> allMembers = GetAllGroupMembersData();

            // Créer une liste de tous les objets de cette catégorie équipés par les membres du groupe
            List<ItemData> equippedItemsList = new List<ItemData>();
            foreach (var member in allMembers)
            {
                ItemData equipped = null;
                if (category == EquipmentType.Offensive) equipped = member.offensiveEquipment;
                else if (category == EquipmentType.Defensive) equipped = member.defensiveEquipment;
                else if (category == EquipmentType.Bonus) equipped = member.bonusEquipment;

                if (equipped != null)
                {
                    equippedItemsList.Add(equipped);
                }
            }

            foreach (var entry in allEquipments)
            {
                if (entry == null || entry.item == null || entry.item.equipmentType != category) continue;

                ItemData item = entry.item;

                // Trouver si cet item est équipé par un membre du groupe
                CharacterData equippingMember = null;
                if (equippedItemsList.Contains(item))
                {
                    // Trouver le premier membre du groupe qui l'équipe dans cette catégorie
                    foreach (var member in allMembers)
                    {
                        if (category == EquipmentType.Offensive && member.offensiveEquipment == item)
                        {
                            equippingMember = member;
                            break;
                        }
                        else if (category == EquipmentType.Defensive && member.defensiveEquipment == item)
                        {
                            equippingMember = member;
                            break;
                        }
                        else if (category == EquipmentType.Bonus && member.bonusEquipment == item)
                        {
                            equippingMember = member;
                            break;
                        }
                    }

                    // On retire l'objet de la liste temporaire pour que les autres doublons éventuels restent disponibles
                    equippedItemsList.Remove(item);
                }

                if (equipItemButtonPrefab != null)
                {
                    GameObject itemButtonObj = Instantiate(equipItemButtonPrefab, equipItemsContainer);
                    Button btn = itemButtonObj.GetComponent<Button>();
                    TextMeshProUGUI txt = itemButtonObj.GetComponentInChildren<TextMeshProUGUI>();

                    bool isEquippedByAnyone = (equippingMember != null);

                    if (txt != null)
                    {
                        txt.text = item.itemName;
                        txt.color = isEquippedByAnyone ? Color.red : Color.white;
                    }

                    // Gérer l'affichage de l'icône du personnage qui l'équipe
                    Image iconImg = null;
                    Image[] images = itemButtonObj.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (img.gameObject != itemButtonObj)
                        {
                            iconImg = img;
                            break;
                        }
                    }

                    if (isEquippedByAnyone)
                    {
                        // S'il n'y a pas d'image enfant existante, on la crée programmatiquement
                        if (iconImg == null)
                        {
                            GameObject iconGo = new GameObject("EquippedCharIcon");
                            iconGo.transform.SetParent(itemButtonObj.transform, false);
                            iconImg = iconGo.AddComponent<Image>();
                            RectTransform rect = iconGo.GetComponent<RectTransform>();
                            if (rect != null)
                            {
                                rect.anchorMin = new Vector2(1f, 0.5f);
                                rect.anchorMax = new Vector2(1f, 0.5f);
                                rect.pivot = new Vector2(1f, 0.5f);
                                rect.anchoredPosition = new Vector2(-10f, 0f);
                                rect.sizeDelta = new Vector2(30f, 30f);
                            }
                        }

                        if (iconImg != null)
                        {
                            iconImg.sprite = equippingMember.menuIcon;
                            iconImg.enabled = equippingMember.menuIcon != null;
                        }
                    }
                    else
                    {
                        // Désactiver les images enfants si l'objet n'est pas équipé
                        if (iconImg != null)
                        {
                            iconImg.enabled = false;
                        }
                    }

                    if (btn != null)
                    {
                        ItemData targetItem = item;
                        bool isStealing = isEquippedByAnyone;
                        btn.onClick.AddListener(() => EquipItem(targetItem, category, isStealing));
                    }
                }
            }
        }

        // Placer le focus EventSystem sur le premier élément (bouton Aucun) pour la manette
        if (UnityEngine.EventSystems.EventSystem.current != null && firstSelected != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    /// <summary>
    /// Ferme l'EquipPanel.
    /// </summary>
    public void CloseEquipPanel()
    {
        if (equipPanel != null)
        {
            equipPanel.SetActive(false);
        }

        // Restaurer le focus EventSystem sur le slot d'équipement qui avait ouvert le panneau
        if (UnityEngine.EventSystems.EventSystem.current != null && lastSelectedSlotButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(lastSelectedSlotButton);
        }
    }

    /// <summary>
    /// Équipe un objet dans une catégorie sur le personnage sélectionné.
    /// Gère également le transfert d'équipement d'un personnage à un autre.
    /// </summary>
    private void EquipItem(ItemData item, EquipmentType category, bool stealFromOther)
    {
        if (selectedCharacterData == null) return;

        // Si l'objet à équiper est non nul et que stealFromOther est vrai, on le retire d'abord de quiconque le porte
        if (item != null && stealFromOther)
        {
            List<CharacterData> allMembers = GetAllGroupMembersData();
            foreach (var member in allMembers)
            {
                if (category == EquipmentType.Offensive && member.offensiveEquipment == item)
                {
                    member.offensiveEquipment = null;
                    break;
                }
                else if (category == EquipmentType.Defensive && member.defensiveEquipment == item)
                {
                    member.defensiveEquipment = null;
                    break;
                }
                else if (category == EquipmentType.Bonus && member.bonusEquipment == item)
                {
                    member.bonusEquipment = null;
                    break;
                }
            }
        }

        // Équiper sur le personnage sélectionné
        if (category == EquipmentType.Offensive)
        {
            selectedCharacterData.offensiveEquipment = item;
        }
        else if (category == EquipmentType.Defensive)
        {
            selectedCharacterData.defensiveEquipment = item;
        }
        else if (category == EquipmentType.Bonus)
        {
            selectedCharacterData.bonusEquipment = item;
        }

        // Rafraîchir l'affichage
        ShowCharacterDetails(selectedCharacterData);
        CloseEquipPanel();
    }

    /// <summary>
    /// Met à jour la surbrillance jaune du texte du bouton actuellement sélectionné par l'EventSystem.
    /// </summary>
    private void UpdateSelectionHighlight()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;

        GameObject currentSelected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        if (currentSelected != previousSelectedObject)
        {
            // 1. Restaurer la couleur de l'ancien objet sélectionné
            if (previousSelectedObject != null && hasOriginalColorStored)
            {
                TextMeshProUGUI txt = previousSelectedObject.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.color = previousObjectOriginalColor;
                }
                else
                {
                    UnityEngine.UI.Text legacyText = previousSelectedObject.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (legacyText != null)
                    {
                        legacyText.color = previousObjectOriginalColor;
                    }
                }
            }

            // 2. Enregistrer et appliquer la couleur jaune sur le nouvel objet sélectionné
            previousSelectedObject = currentSelected;
            hasOriginalColorStored = false;

            if (currentSelected != null)
            {
                TextMeshProUGUI txt = currentSelected.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    previousObjectOriginalColor = txt.color;
                    hasOriginalColorStored = true;
                    txt.color = Color.yellow;
                }
                else
                {
                    UnityEngine.UI.Text legacyText = currentSelected.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (legacyText != null)
                    {
                        previousObjectOriginalColor = legacyText.color;
                        hasOriginalColorStored = true;
                        legacyText.color = Color.yellow;
                    }
                }

                // Ajuster dynamiquement les couleurs du composant Button s'il existe et utilise ColorTint
                Button btn = currentSelected.GetComponent<Button>();
                if (btn != null && btn.transition == Selectable.Transition.ColorTint)
                {
                    ColorBlock cb = btn.colors;
                    if (cb.highlightedColor != Color.yellow || cb.selectedColor != Color.yellow)
                    {
                        cb.highlightedColor = Color.yellow;
                        cb.selectedColor = Color.yellow;
                        btn.colors = cb;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Réinitialise la couleur jaune de l'élément sélectionné lorsque le menu se ferme.
    /// </summary>
    private void ResetSelectionHighlight()
    {
        if (previousSelectedObject != null && hasOriginalColorStored)
        {
            TextMeshProUGUI txt = previousSelectedObject.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = previousObjectOriginalColor;
            }
            else
            {
                UnityEngine.UI.Text legacyText = previousSelectedObject.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacyText != null)
                {
                    legacyText.color = previousObjectOriginalColor;
                }
            }
        }
        previousSelectedObject = null;
        hasOriginalColorStored = false;
    }

    #endregion

    #endregion
}
