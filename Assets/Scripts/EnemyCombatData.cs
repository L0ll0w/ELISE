using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyCombatData", menuName = "2.5D RPG/Rhythm/Enemy Combat Data", order = 1)]
public class EnemyCombatData : ScriptableObject
{
    [Header("Identité & Stats")]
    [Tooltip("Nom de l'ennemi affiché dans le HUD.")]
    [SerializeField] private string enemyName = "Monstre";
    
    [Tooltip("Points de vie maximaux de l'ennemi.")]
    [SerializeField] private int maxHP = 300;

    [Header("Configuration Rythmique")]
    [Tooltip("Musique de combat (Clip audio).")]
    [SerializeField] private AudioClip musicTrack;

    [Tooltip("BPM (Tempo) de la musique de combat.")]
    [SerializeField] private float bpm = 120f;

    [Header("Visualisation & Comportement")]
    [Tooltip("Préfabricateur visuel optionnel (le pantin/sprite animé) pour l'ennemi au centre.")]
    [SerializeField] private GameObject visualPrefab;

    [Tooltip("Le pattern d'attaque rythmique associé à cet ennemi.")]
    [SerializeField] private EnemyBeatPattern beatPattern;

    [Header("Effets Visuels d'Attaques (Spécifiques à l'ennemi)")]
    [Tooltip("Préfab du projectile tombant du ciel pour les alertes de cet ennemi.")]
    [SerializeField] private GameObject warningProjectilePrefab;

    [Tooltip("Préfab de l'impact au sol (éclaboussure/explosion) pour les alertes de cet ennemi.")]
    [SerializeField] private GameObject impactVisualPrefab;

    [Header("Phase de Discussion & Verdict")]
    [Tooltip("Durée de la phase d'esquive en nombre de battements (beats) (ex: 16).")]
    [SerializeField] private int dodgePhaseDuration = 16;

    [Tooltip("Dialogues successifs lorsque le joueur choisit d'engager la discussion (dans l'ordre).")]
    [TextArea(2, 5)]
    [SerializeField] private List<string> talkDialogues = new List<string>();

    // Propriétés d'accès en lecture seule
    public string EnemyName => enemyName;
    public int MaxHP => maxHP;
    public AudioClip MusicTrack => musicTrack;
    public float Bpm => bpm;
    public GameObject VisualPrefab => visualPrefab;
    public EnemyBeatPattern BeatPattern => beatPattern;
    public GameObject WarningProjectilePrefab => warningProjectilePrefab;
    public GameObject ImpactVisualPrefab => impactVisualPrefab;
    public int DodgePhaseDuration => dodgePhaseDuration;
    public List<string> TalkDialogues => talkDialogues;
}
