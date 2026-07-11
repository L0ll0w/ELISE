using UnityEngine;

/// <summary>
/// Gère le comportement d'un coffre au trésor interactif.
/// Hérite d'Interactable pour s'intégrer au système d'interaction unifié.
/// </summary>
[AddComponentMenu("2.5D RPG/Chest")]
public class Chest : Interactable
{
    [Header("Contenu du Coffre")]
    [Tooltip("L'objet à donner au joueur.")]
    [SerializeField] private ItemData itemToGive;

    [Tooltip("La quantité de cet objet à donner.")]
    [SerializeField] private int quantityToGive = 1;

    [Header("Configuration visuelle et Animations")]
    [Tooltip("L'animateur du coffre (généré automatiquement s'il est sur le même GameObject).")]
    [SerializeField] private Animator animator;

    [Tooltip("Nom du trigger de l'animateur pour ouvrir le coffre.")]
    [SerializeField] private string openTriggerName = "Open";

    [Tooltip("Temps d'attente avant de donner l'objet (durée de l'animation d'ouverture en secondes).")]
    [SerializeField] private float openAnimationDelay = 1.0f;

    [Tooltip("Dialogue optionnel à jouer si le coffre est déjà ouvert et vide.")]
    [SerializeField] private DialogueData emptyChestDialogue;

    [Header("Paramètres de Disparition (Optionnel)")]
    [Tooltip("Indique si le coffre doit disparaître après obtention de l'objet.")]
    [SerializeField] private bool destroyAfterOpening = true;

    [Tooltip("Délai (en secondes) avant de commencer le fondu après la fermeture du dialogue.")]
    [SerializeField] private float fadeStartDelay = 0.5f;

    [Tooltip("Durée du fondu de disparition (en secondes).")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("Vitesse de rotation sur l'axe Y pendant la disparition (degrés par seconde).")]
    [SerializeField] private float fadeRotationSpeed = 360f;

    private bool isOpen = false;

    protected override void Start()
    {
        base.Start();

        // Récupération automatique de l'Animator si non assigné
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Implémentation de l'interaction (lorsque le joueur appuie sur E).
    /// </summary>
    protected override void Interact()
    {
        if (isOpen)
        {
            TriggerEmptyDialogue();
            return;
        }

        OpenChest();
    }

    /// <summary>
    /// Déclenche l'ouverture du coffre (animation) puis lance le délai avant d'attribuer l'objet.
    /// </summary>
    private void OpenChest()
    {
        if (itemToGive == null)
        {
            Debug.LogWarning("Ce coffre n'a pas d'objet assigné (itemToGive est nul).");
            return;
        }

        isOpen = true;

        // 1. Lancer l'animation d'ouverture
        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }

        // 2. Lancer la coroutine d'attente avant l'obtention réelle de l'objet
        StartCoroutine(GiveItemAfterDelay(openAnimationDelay));
    }

    /// <summary>
    /// Attend que l'animation d'ouverture soit terminée avant de distribuer l'objet et d'ouvrir le dialogue.
    /// </summary>
    private System.Collections.IEnumerator GiveItemAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 3. Ajouter l'objet à l'inventaire
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemToGive, quantityToGive);
        }
        else
        {
            Debug.LogError("InventoryManager.Instance est introuvable. Impossible d'ajouter l'objet.");
        }

        // 4. Afficher le dialogue d'obtention de l'objet (dynamique)
        TriggerAcquisitionDialogue();
    }

    /// <summary>
    /// Génère et lance un dialogue temporaire pour indiquer l'objet récupéré.
    /// Affiche l'icône de l'objet en portrait et laisse le nom du personnage vide.
    /// </summary>
    private void TriggerAcquisitionDialogue()
    {
        if (DialogueManager.Instance == null) return;

        // Génération d'un DialogueData factice à la volée
        DialogueData acquisitionData = ScriptableObject.CreateInstance<DialogueData>();
        
        DialogueNode node = new DialogueNode();
        node.nodeID = "chest_obtained_item";
        
        // Pas de nom de personnage comme demandé par l'utilisateur
        node.characterName = ""; 
        
        // Utilisation de l'icône de l'objet à la place du portrait
        node.portrait = itemToGive.icon;
        
        // Phrase dynamique en français
        node.sentence = quantityToGive > 1 
            ? $"Vous avez récupéré : {itemToGive.itemName} (x{quantityToGive}) !"
            : $"Vous avez récupéré : {itemToGive.itemName} !";

        acquisitionData.nodes = new DialogueNode[] { node };

        // Lancer le dialogue
        if (destroyAfterOpening)
        {
            DialogueManager.Instance.StartDialogue(acquisitionData, () =>
            {
                StartCoroutine(FadeOutAndDestroy());
            });
        }
        else
        {
            DialogueManager.Instance.StartDialogue(acquisitionData);
        }
    }

    /// <summary>
    /// Coroutine gérant la disparition progressive (fondu + rétrécissement) et la destruction du coffre.
    /// </summary>
    private System.Collections.IEnumerator FadeOutAndDestroy()
    {
        // 1. Attente initiale demandée (ex: 0.5s)
        yield return new WaitForSeconds(fadeStartDelay);

        // 2. Désactiver l'Animator pour l'empêcher d'écraser la couleur ou l'échelle chaque frame
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Récupérer tous les renderers actifs sur le coffre
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Renderer[] meshRenderers = GetComponentsInChildren<Renderer>();

        // Enregistrer l'échelle d'origine pour la faire rétrécir à zéro
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / fadeDuration;
            
            // Progression du fondu (alpha) et du rétrécissement (scale)
            float alpha = Mathf.Lerp(1f, 0f, normalizedTime);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, normalizedTime);

            // Faire tourner le coffre sur lui-même (axe Z)
            transform.Rotate(Vector3.forward, fadeRotationSpeed * Time.deltaTime);

            // Appliquer l'alpha aux SpriteRenderers
            foreach (var sr in spriteRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

            // Appliquer l'alpha aux matériaux des MeshRenderers (si supporté par le shader)
            foreach (var r in meshRenderers)
            {
                if (r != null && !(r is SpriteRenderer))
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            c.a = alpha;
                            mat.color = c;
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                    }
                }
            }

            yield return null;
        }

        // 3. Détruire l'objet pour nettoyer la scène
        Destroy(gameObject);
    }

    /// <summary>
    /// Déclenche le dialogue indiquant que le coffre est vide.
    /// </summary>
    private void TriggerEmptyDialogue()
    {
        if (DialogueManager.Instance == null) return;

        if (emptyChestDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(emptyChestDialogue);
        }
        else
        {
            // Dialogue par défaut si rien n'est configuré
            DialogueData defaultEmpty = ScriptableObject.CreateInstance<DialogueData>();
            
            DialogueNode node = new DialogueNode();
            node.nodeID = "chest_empty";
            node.characterName = "";
            node.portrait = null;
            node.sentence = "Le coffre est vide...";

            defaultEmpty.nodes = new DialogueNode[] { node };
            
            DialogueManager.Instance.StartDialogue(defaultEmpty);
        }
    }
}
