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
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

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
    public bool CanStartDialogue => !isDialogueActive && Time.frameCount != dialogueEndFrame;

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

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);
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
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) interact = true;
        #else
        if (Input.GetKeyDown(KeyCode.E)) interact = true;
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

        activeDialogue = data;
        isDialogueActive = true;
        areChoicesActive = false;
        onDialogueCompleteCallback = onComplete;
        dialogueStartFrame = Time.frameCount; // Enregistre la frame de départ

        TogglePlayerMovement(false);

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

        if (portraitImage != null)
        {
            if (node.portrait != null)
            {
                portraitImage.sprite = node.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

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
            if (gp.buttonSouth.wasPressedThisFrame) confirm = true;
        }
        #else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) up = true;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) down = true;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) confirm = true;
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

        TogglePlayerMovement(true);

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
            cachedPlayerMovement.enabled = enable;
        }
    }
}
