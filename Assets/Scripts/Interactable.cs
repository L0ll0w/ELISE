using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Classe de base pour tous les objets interactifs du jeu (PNJ, Dialogues, Coffres, Portes, etc.).
/// Gère la détection de proximité avec le joueur et écoute l'input d'interaction.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    protected bool isPlayerInRange = false;
    private Collider interactionCollider;

    protected virtual void Reset()
    {
        // Configure automatiquement le collider en Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    protected virtual void Start()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
        }
    }

    protected virtual void Update()
    {
        if (isPlayerInRange && CanInteract())
        {
            bool interact = false;
            
            // Écoute de l'input avec l'Input System ou fallback classique
            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
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
                Interact();
            }
        }
    }

    /// <summary>
    /// Vérifie si l'interaction est possible (ex: jeu non en pause).
    /// </summary>
    protected virtual bool CanInteract()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Action à exécuter lors de l'interaction. Doit être implémentée par les classes enfants.
    /// </summary>
    protected abstract void Interact();

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = true;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = false;
        }
    }
}
