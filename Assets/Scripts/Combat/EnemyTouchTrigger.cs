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

    [Header("Point de Repère de Combat")]
    [Tooltip("Transform / GameObject repère optionnel placé sur la carte pour définir le centre exact de l'arène de combat. Si vide, la position de l'ennemi sera utilisée.")]
    [SerializeField] private Transform combatCenterMarker;

    [Tooltip("Si vrai et que le point de repère est un enfant de cet ennemi, il sera automatiquement détaché au lancement du jeu pour rester fixe dans la scène pendant la ronde.")]
    [SerializeField] private bool lockMarkerInWorldSpace = true;

    public Transform CombatCenterMarker
    {
        get => combatCenterMarker;
        set => combatCenterMarker = value;
    }

    private bool hasTriggered = false;

    private void Awake()
    {
        if (combatCenterMarker != null && lockMarkerInWorldSpace)
        {
            // Si le marqueur est un enfant de l'ennemi, le détacher pour qu'il reste fixe dans le monde
            if (combatCenterMarker.IsChildOf(transform))
            {
                combatCenterMarker.SetParent(null, true);
            }
        }
    }

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

            // Mettre en pause le script de déplacement wandering immédiatement
            EnemyWander wander = GetComponent<EnemyWander>();
            if (wander == null) wander = GetComponentInParent<EnemyWander>();
            if (wander != null)
            {
                wander.PauseWander();
            }

            Debug.Log($"[EnemyTouchTrigger] Joueur détecté ! Lancement du combat avec {gameObject.name} (Centre: {(combatCenterMarker != null ? combatCenterMarker.name : "Position Ennemi")})...");
            CombatManager.Instance.StartCombat(gameObject, combatCenterMarker);
        }
        else
        {
            Debug.LogError("[EnemyTouchTrigger] CombatManager.Instance est introuvable dans la scène !");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (combatCenterMarker != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(combatCenterMarker.position, 0.6f);
            Gizmos.DrawLine(transform.position, combatCenterMarker.position);
        }
    }

    /// <summary>
    /// Permet de réinitialiser le trigger si besoin (ex: si le combat est annulé ou pour des tests).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;

        EnemyWander wander = GetComponent<EnemyWander>();
        if (wander == null) wander = GetComponentInParent<EnemyWander>();
        if (wander != null)
        {
            wander.ResumeWander();
        }
    }
}
