using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gère la centralisation de l'affichage UI, du typewriter et de la sélection de réponses.
/// </summary>
[AddComponentMenu("2.5D RPG/Dialogue Manager")]
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;
    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DialogueManager>();
            }
            return instance;
        }
    }

    [Header("UI Réf - Fenêtre")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject portraitContainer; // Cadre/Placeholder du portrait
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject namePanel; // Cadre/Background du nom du personnage
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Positionnement du Texte Sans Portrait")]
    [Tooltip("Conteneur global du texte (contient nameText et dialogueText). Si laissé vide, décale dialogueText, nameText et namePanel individuellement.")]
    [SerializeField] private RectTransform textContentContainer;
    [Tooltip("Ajuster automatiquement la position du texte s'il n'y a pas de portrait?")]
    [SerializeField] private bool autoAdjustTextPosition = true;
    [Tooltip("Marge / Décalage à gauche du texte sans portrait (ex: 30px = commence sur le bord gauche du cadre).")]
    [SerializeField] private float textLeftMarginWithoutPortrait = 30f;

    [Header("UI Réf - Choix")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceTextPrefab;

    [Header("Couleurs")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private DialogueData activeDialogue;
    private DialogueNode currentNode;
    private TypewriterEffects typewriter;
    private bool isDialogueActive = false;
    private bool areChoicesActive = false;

    private List<TextMeshProUGUI> choiceInstances = new List<TextMeshProUGUI>();
    private ChoiceData[] activeChoices;
    private int selectedChoiceIndex = 0;

    private PlayerMovement cachedPlayerMovement;
    private System.Action onDialogueCompleteCallback;
    private int dialogueEndFrame = -1;
    private int dialogueStartFrame = -1;

    public bool IsDialogueActive => isDialogueActive;
    public bool CanStartDialogue
    {
        get
        {
            return !isDialogueActive;
        }
    }
    public RectTransform DialoguePanelRect => dialoguePanel != null ? dialoguePanel.GetComponent<RectTransform>() : null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Rend la racine entière (le Canvas) persistante entre les scènes
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else if (instance != this)
        {
            // Détruit le doublon de la racine pour éviter d'avoir plusieurs Canvas
            Destroy(transform.root.gameObject);
            return;
        }

        if (dialogueText != null)
        {
            typewriter = dialogueText.GetComponent<TypewriterEffects>();
            if (typewriter == null)
            {
                typewriter = dialogueText.gameObject.AddComponent<TypewriterEffects>();
            }
        }

        if (dialoguePanel != null)
        {
            // Pré-échauffement des composants UI et des polices TextMeshPro pour éviter le micro-lag au premier dialogue
            dialoguePanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            dialoguePanel.SetActive(false);
        }
        if (choicesPanel != null) choicesPanel.SetActive(false);
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
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 99999;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 0.5f;
            }
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (areChoicesActive)
        {
            HandleChoicesInput();
            return;
        }

        bool interact = false;
        #if ENABLE_INPUT_SYSTEM
        if ((Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame))) interact = true;
        #else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton2)) interact = true;
        #endif

        // Empêche de consommer la touche d'interaction sur la même frame que l'ouverture
        if (interact && Time.frameCount != dialogueStartFrame)
        {
            OnInteractPressed();
        }
    }

    public void StartDialogue(DialogueData data, System.Action onComplete = null)
    {
        if (data == null || data.nodes == null || data.nodes.Length == 0) return;
        if (!CanStartDialogue) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 99999;
            if (Camera.main != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 0.5f;
            }
        }

        activeDialogue = data;
        isDialogueActive = true;
        areChoicesActive = false;
        onDialogueCompleteCallback = onComplete;
        dialogueStartFrame = Time.frameCount; // Enregistre la frame de départ

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.RequestPause(PauseManager.PauseSource.Dialogue);
        }
        else
        {
            TogglePlayerMovement(false);
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (choicesPanel != null) choicesPanel.SetActive(false);

        DisplayNode(data.GetStartNode());
    }

    private void DisplayNode(DialogueNode node)
    {
        currentNode = node;

        if (nameText != null)
        {
            nameText.text = node.characterName;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(node.characterName));
        }

        if (namePanel != null)
        {
            namePanel.SetActive(!string.IsNullOrEmpty(node.characterName));
        }

        bool hasPortrait = node.portrait != null;

        if (portraitImage != null)
        {
            if (hasPortrait)
            {
                portraitImage.sprite = node.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        // Gérer le conteneur/cadre du portrait s'il est spécifié ou s'il s'agit d'un objet parent dédié
        GameObject containerToHide = portraitContainer;
        if (containerToHide == null && portraitImage != null && portraitImage.transform.parent != null && portraitImage.transform.parent.gameObject != dialoguePanel)
        {
            containerToHide = portraitImage.transform.parent.gameObject;
        }

        if (containerToHide != null && containerToHide != portraitImage.gameObject)
        {
            containerToHide.SetActive(hasPortrait);
        }

        // Ajuster la position/marge du texte selon la présence du portrait
        ApplyTextPosition(hasPortrait);

        // Récupération tardive si le composant typewriter n'avait pas pu être chargé au Awake
        if (typewriter == null && dialogueText != null)
        {
            typewriter = dialogueText.GetComponent<TypewriterEffects>();
            if (typewriter == null)
            {
                typewriter = dialogueText.gameObject.AddComponent<TypewriterEffects>();
            }
        }

        if (typewriter != null)
        {
            typewriter.StartTyping(node.sentence, OnTextFinished);
        }
        else if (dialogueText != null)
        {
            dialogueText.text = node.sentence;
            OnTextFinished();
        }
    }

    private struct RectState
    {
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 anchoredPos;
        public Vector4 tmpMargin;
    }

    private Dictionary<RectTransform, RectState> originalRectStates = new Dictionary<RectTransform, RectState>();
    private bool hasRecordedTextOffsets = false;

    private void RecordOriginalRectState(RectTransform rt, TextMeshProUGUI tmpText)
    {
        if (rt != null && !originalRectStates.ContainsKey(rt))
        {
            originalRectStates[rt] = new RectState
            {
                offsetMin = rt.offsetMin,
                offsetMax = rt.offsetMax,
                anchoredPos = rt.anchoredPosition,
                tmpMargin = tmpText != null ? tmpText.margin : Vector4.zero
            };
        }
    }

    private void ApplyTextPosition(bool hasPortrait)
    {
        if (!autoAdjustTextPosition) return;

        List<RectTransform> targets = new List<RectTransform>();

        if (textContentContainer != null)
        {
            targets.Add(textContentContainer);
        }
        else
        {
            if (dialogueText != null) targets.Add(dialogueText.rectTransform);
            if (nameText != null) targets.Add(nameText.rectTransform);
            if (namePanel != null)
            {
                RectTransform namePanelRt = namePanel.GetComponent<RectTransform>();
                if (namePanelRt != null && !targets.Contains(namePanelRt)) targets.Add(namePanelRt);
            }
        }

        if (targets.Count == 0) return;

        // Enregistrer la configuration originale du prefab lors de la première réplique
        if (!hasRecordedTextOffsets)
        {
            foreach (var rt in targets)
            {
                TextMeshProUGUI tmp = rt == dialogueText?.rectTransform ? dialogueText : (rt == nameText?.rectTransform ? nameText : null);
                RecordOriginalRectState(rt, tmp);
            }
            hasRecordedTextOffsets = true;
        }

        foreach (var rt in targets)
        {
            if (rt == null || !originalRectStates.TryGetValue(rt, out RectState state)) continue;

            TextMeshProUGUI tmp = rt == dialogueText?.rectTransform ? dialogueText : (rt == nameText?.rectTransform ? nameText : null);

            if (hasPortrait)
            {
                // Restaurer la position d'origine du prefab (laissant l'espace pour le portrait)
                rt.offsetMin = state.offsetMin;
                rt.offsetMax = state.offsetMax;
                rt.anchoredPosition = state.anchoredPos;
                if (tmp != null) tmp.margin = state.tmpMargin;
            }
            else
            {
                // Repositionner sur le bord gauche du cadre de dialogue !
                if (tmp != null)
                {
                    // Annuler les marges internes TMP qui poussent le texte vers la droite
                    tmp.margin = new Vector4(0, state.tmpMargin.y, state.tmpMargin.z, state.tmpMargin.w);
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                }

                if (rt.anchorMin.x == 0f && rt.anchorMax.x == 1f)
                {
                    // Ancres horizontales en mode Stretch (Stretch-Stretch)
                    rt.offsetMin = new Vector2(textLeftMarginWithoutPortrait, state.offsetMin.y);
                }
                else
                {
                    // Ancres fixes (Left, Center, etc.)
                    // Calculer le décalage depuis la position initiale du prefab
                    float originalLeft = state.offsetMin.x != 0 ? state.offsetMin.x : state.anchoredPos.x;
                    float deltaX = textLeftMarginWithoutPortrait - originalLeft;
                    rt.anchoredPosition = new Vector2(state.anchoredPos.x + deltaX, state.anchoredPos.y);
                }
            }
        }
    }

    private void OnTextFinished()
    {
        if (currentNode.choices != null && currentNode.choices.Length > 0)
        {
            DisplayChoices(currentNode.choices);
        }
    }

    private void OnInteractPressed()
    {
        if (typewriter != null && typewriter.IsTyping)
        {
            typewriter.Skip();
            OnTextFinished();
            return;
        }

        if (!string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            if (activeDialogue.TryGetNode(currentNode.nextNodeID, out DialogueNode nextNode))
            {
                DisplayNode(nextNode);
            }
            else
            {
                EndDialogue();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    private void DisplayChoices(ChoiceData[] choices)
    {
        ClearChoices();
        activeChoices = choices;
        areChoicesActive = true;

        if (choicesPanel != null) choicesPanel.SetActive(true);

        for (int i = 0; i < choices.Length; i++)
        {
            GameObject choiceObj;
            if (choiceTextPrefab != null)
            {
                choiceObj = Instantiate(choiceTextPrefab, choicesContainer);
            }
            else
            {
                GameObject g = new GameObject("ChoiceText");
                g.transform.SetParent(choicesContainer, false);
                choiceObj = g;
                choiceObj.AddComponent<TextMeshProUGUI>();
            }

            TextMeshProUGUI text = choiceObj.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = choices[i].text;
                choiceInstances.Add(text);
            }
        }
        SelectChoice(0);
    }

    private void ClearChoices()
    {
        foreach (var c in choiceInstances)
        {
            if (c != null) Destroy(c.gameObject);
        }
        choiceInstances.Clear();
        activeChoices = null;
        areChoicesActive = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void HandleChoicesInput()
    {
        bool up = false;
        bool down = false;
        bool confirm = false;

        #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) up = true;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) down = true;
            if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) confirm = true;
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame) up = true;
            if (gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame) down = true;
            if (gp.buttonWest.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame) confirm = true;
        }
        #else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) up = true;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) down = true;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton2)) confirm = true;
        #endif

        if (up) SelectChoice(selectedChoiceIndex - 1);
        else if (down) SelectChoice(selectedChoiceIndex + 1);
        else if (confirm) SubmitChoice();
    }

    private void SelectChoice(int index)
    {
        if (choiceInstances.Count == 0) return;

        if (index < 0) index = choiceInstances.Count - 1;
        else if (index >= choiceInstances.Count) index = 0;

        selectedChoiceIndex = index;

        for (int i = 0; i < choiceInstances.Count; i++)
        {
            if (i == selectedChoiceIndex)
            {
                choiceInstances[i].color = selectedColor;
                choiceInstances[i].text = "► " + activeChoices[i].text;
            }
            else
            {
                choiceInstances[i].color = normalColor;
                choiceInstances[i].text = "  " + activeChoices[i].text;
            }
        }
    }

    private void SubmitChoice()
    {
        ChoiceData chosen = activeChoices[selectedChoiceIndex];
        ClearChoices();

        if (!string.IsNullOrEmpty(chosen.nextNodeID) && activeDialogue.TryGetNode(chosen.nextNodeID, out DialogueNode nextNode))
        {
            DisplayNode(nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        areChoicesActive = false;
        ClearChoices();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.RequestUnpause(PauseManager.PauseSource.Dialogue);
        }
        else
        {
            TogglePlayerMovement(true);
        }

        // Enregistre la frame de fin de dialogue pour éviter la propagation d'input
        dialogueEndFrame = Time.frameCount;

        var callback = onDialogueCompleteCallback;
        onDialogueCompleteCallback = null;
        callback?.Invoke();
    }

    private void TogglePlayerMovement(bool enable)
    {
        if (cachedPlayerMovement == null)
        {
            cachedPlayerMovement = FindFirstObjectByType<PlayerMovement>();
        }
        if (cachedPlayerMovement != null)
        {
            if (enable && ((RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive) ||
                           (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive)))
            {
                return;
            }
            cachedPlayerMovement.enabled = enable;
        }
    }
}
