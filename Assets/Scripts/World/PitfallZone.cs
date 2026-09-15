using UnityEngine;

/// <summary>
/// Zone de déclenchement (Trigger) pour les fosses, étendues d'eau profondes ou zones de vide.
/// Lorsqu'un joueur entre dans ce collider trigger, il déclenche immédiatement la réapparition
/// style Paper Mario via son composant PlayerPitfallRespawn.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Pitfall Zone")]
public class PitfallZone : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Détecter le joueur directement ou via ses parents/enfants
        PlayerPitfallRespawn respawn = other.GetComponent<PlayerPitfallRespawn>();
        if (respawn == null)
        {
            respawn = other.GetComponentInParent<PlayerPitfallRespawn>();
        }

        if (respawn != null)
        {
            PlayerMovement movement = respawn.GetComponent<PlayerMovement>();
            if (movement != null && movement.IsSliding)
            {
                return;
            }

            respawn.TriggerPitfallRespawn();
        }
    }
}
