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
public class DialogueTrigger : Interactable
{
    [Header("Dialogues Conditionnels")]
    [Tooltip("Ordre de priorité : du haut vers le bas. Le premier dialogue dont la condition est remplie sera joué.")]
    [SerializeField] private ConditionalDialogue[] conditionalDialogues;

    protected override void Interact()
    {
        TriggerDialogue();
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

    protected override void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);
        
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.EndDialogue();
            }
        }
    }
}
