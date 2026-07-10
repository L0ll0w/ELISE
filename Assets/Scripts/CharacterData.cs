using UnityEngine;

/// <summary>
/// ScriptableObject contenant toutes les données statistiques et d'équipement d'un personnage de RPG.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "2.5D RPG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Informations Générales")]
    [Tooltip("Nom complet du personnage.")]
    public string characterName = "Nom";
    
    [Tooltip("Portrait de haute résolution du personnage.")]
    public Sprite portrait;

    [Tooltip("Icône ou sprite de petite taille pour l'affichage dans la liste du groupe.")]
    public Sprite menuIcon;

    [Header("Statistiques de Combat")]
    [Tooltip("Niveau actuel.")]
    public int level = 1;

    [Tooltip("Points de Vie actuels (PV).")]
    public int currentHP = 100;
    [Tooltip("Points de Vie maximum (PV).")]
    public int maxHP = 100;

    [Tooltip("Points de Capacité actuels (PC / MP).")]
    public int currentMP = 50;
    [Tooltip("Points de Capacité maximum (PC / MP).")]
    public int maxMP = 50;

    [Tooltip("Force d'attaque physique.")]
    public int strength = 10;
    
    [Tooltip("Défense aux attaques.")]
    public int defense = 10;
    
    [Tooltip("Vitesse de déplacement/d'action.")]
    public int speed = 10;

    [Header("Équipements Équipés")]
    [Tooltip("Équipement offensif (arme, etc.).")]
    public string offensiveEquipment = "Aucun";

    [Tooltip("Équipement défensif (armure, bouclier, etc.).")]
    public string defensiveEquipment = "Aucun";

    [Tooltip("Équipement bonus (accessoires, anneaux, etc.).")]
    public string bonusEquipment = "Aucun";
}
