using UnityEngine;

/// <summary>
/// Déclencheur de zone pour la musique.
/// À placer sur un GameObject doté d'un Collider (Configuré en IsTrigger).
/// Lorsque le joueur pénètre dans la zone, communique avec l'AudioManager
/// pour lancer la musique de zone associée avec un fondu croisé (crossfade) fluide.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("2.5D RPG/Audio/Zone Music Trigger")]
public class ZoneMusicTrigger : MonoBehaviour
{
    [Header("Configuration de la Musique de Zone")]
    [Tooltip("Le morceau de musique (AudioClip) associé à cette zone.")]
    [SerializeField] private AudioClip zoneMusicTrack;

    [Tooltip("Durée de la transition en fondu croisé (en secondes).")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("Nom indicatif de la zone (pour les logs et le débogage).")]
    [SerializeField] private string zoneName = "Nouvelle Zone";

    [Tooltip("Si vrai, déclenche la musique automatiquement dès le Start si le joueur se trouve déjà dans le Trigger.")]
    [SerializeField] private bool triggerOnStartIfPlayerInside = true;

    private void Awake()
    {
        // S'assurer que le Collider est bien un Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (triggerOnStartIfPlayerInside && zoneMusicTrack != null)
        {
            // Vérifier si le joueur est déjà à l'intérieur du Trigger lors de l'initialisation
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
                if (player != null && col.bounds.Contains(player.transform.position))
                {
                    Debug.Log($"[ZoneMusicTrigger] Le joueur commence dans la zone '{zoneName}'. Lancement de la musique.");
                    AudioManager.Instance.PlayZoneMusic(zoneMusicTrack, fadeDuration);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (zoneMusicTrack == null) return;

        // Détection du joueur (soit par le Tag "Player", soit par la présence du composant PlayerMovement)
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null)
        {
            Debug.Log($"[ZoneMusicTrigger] Entrée du joueur dans la zone '{zoneName}'. Transistion vers la musique '{zoneMusicTrack.name}'.");
            AudioManager.Instance.PlayZoneMusic(zoneMusicTrack, fadeDuration);
        }
    }

    private void OnDrawGizmos()
    {
        // Dessiner une boîte ou sphère verte dans la vue Scène pour repérer facilement les zones de musique
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.9f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.9f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}
