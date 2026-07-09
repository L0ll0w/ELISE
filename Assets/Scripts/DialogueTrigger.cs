using UnityEngine;

[System.Serializable]
public struct ConditionalDialogue
{
    [Tooltip("Le flag requis dans le StoryStateManager pour jouer ce dialogue (laisser vide si pas de condition).")]
    public string requiredFlag;

    [Tooltip("La valeur attendue pour ce flag (True ou False).")]
    public bool expectedValue;

    [Tooltip("Les données du dialogue à jouer (Asset ScriptableObject DialogueData).")]
    public DialogueData dialogue;

    [Tooltip("Le flag à définir à True dans le StoryStateManager une fois le dialogue terminé.")]
    public string flagToSetOnComplete;
}

[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("2.5D RPG/Dialogue Trigger")]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogues Conditionnels")]
    [Tooltip("Ordre de priorité : du haut vers le bas. Le premier dialogue dont la condition est remplie sera joué.")]
    [SerializeField] private ConditionalDialogue[] conditionalDialogues;

    private bool isPlayerInRange = false;
    private BoxCollider triggerCollider;

    private void Reset()
    {
        // Configure automatiquement la case isTrigger lorsque le script est ajouté dans l'éditeur
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (isPlayerInRange && DialogueManager.Instance.CanStartDialogue)
        {
            bool interact = false;
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                interact = true;
            }
            #else
            if (Input.GetKeyDown(KeyCode.E))
            {
                interact = true;
            }
            #endif

            if (interact)
            {
                TriggerDialogue();
            }
        }
    }

    private void TriggerDialogue()
    {
        if (conditionalDialogues == null || conditionalDialogues.Length == 0) return;

        foreach (var cond in conditionalDialogues)
        {
            bool conditionMet = false;

            if (string.IsNullOrEmpty(cond.requiredFlag))
            {
                conditionMet = true;
            }
            else
            {
                bool flagValue = StoryStateManager.Instance.GetFlag(cond.requiredFlag);
                if (flagValue == cond.expectedValue)
                {
                    conditionMet = true;
                }
            }

            if (conditionMet && cond.dialogue != null)
            {
                DialogueManager.Instance.StartDialogue(cond.dialogue, () =>
                {
                    if (!string.IsNullOrEmpty(cond.flagToSetOnComplete))
                    {
                        StoryStateManager.Instance.SetFlag(cond.flagToSetOnComplete, true);
                    }
                });
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = false;
            if (DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.EndDialogue();
            }
        }
    }
}
