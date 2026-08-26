using UnityEngine;

/// <summary>
/// Déclenche automatiquement le combat associé à cet ennemi dès que le joueur le touche (collision ou trigger).
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Combat/Enemy Touch Trigger")]
public class EnemyTouchTrigger : MonoBehaviour
{
    [Tooltip("Tag recherché sur l'objet joueur pour déclencher le combat.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Si vrai, le collider doit être configuré en 'Is Trigger'.")]
    [SerializeField] private bool useTriggerOnly = true;

    private bool hasTriggered = false;

    private void Start()
    {
        // S'assurer que le collider est bien configuré
        Collider col = GetComponent<Collider>();
        if (col != null && useTriggerOnly)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerOnly || hasTriggered) return;
        
        if (IsPlayer(other.gameObject))
        {
            TriggerCombat();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTriggerOnly || hasTriggered) return;

        if (IsPlayer(collision.gameObject))
        {
            TriggerCombat();
        }
    }

    private bool IsPlayer(GameObject go)
    {
        // Vérifier par le tag ou par la présence du contrôleur de mouvements d'exploration du joueur
        return go.CompareTag(playerTag) || 
               go.GetComponent<PlayerMovement>() != null || 
               go.GetComponentInParent<PlayerMovement>() != null || 
               go.GetComponentInChildren<PlayerMovement>() != null;
    }

    private void TriggerCombat()
    {
        if (CombatManager.Instance != null)
        {
            hasTriggered = true;
            Debug.Log($"[EnemyTouchTrigger] Joueur détecté ! Lancement du combat avec {gameObject.name}...");
            CombatManager.Instance.StartCombat(gameObject);
        }
        else
        {
            Debug.LogError("[EnemyTouchTrigger] CombatManager.Instance est introuvable dans la scène !");
        }
    }

    /// <summary>
    /// Permet de réinitialiser le trigger si besoin (ex: si le combat est annulé ou pour des tests).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
