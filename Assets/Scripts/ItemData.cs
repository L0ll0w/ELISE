using UnityEngine;

/// <summary>
/// Définit les trois types d'emplacements/catégories d'objets.
/// </summary>
public enum ItemType
{
    Equipment, // Équipements (armes, armures, accessoires)
    Item,      // Consommables (potions, parchemins)
    Key        // Objets de quête et clés
}

/// <summary>
/// Définit les types d'équipements pour le profil de personnage.
/// </summary>
public enum EquipmentType
{
    None,
    Offensive,
    Defensive,
    Bonus
}

/// <summary>
/// ScriptableObject représentant les données d'un objet dans le jeu.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "2.5D RPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Informations d'Identification")]
    [Tooltip("ID unique interne de l'objet (ex: potion_hp_01).")]
    public string itemID;

    [Tooltip("Nom complet de l'objet affiché dans l'UI.")]
    public string itemName = "Nom de l'objet";

    [Tooltip("Description textuelle de l'objet.")]
    [TextArea(2, 5)]
    public string description = "Description de l'objet...";

    [Tooltip("Image/Sprite affiché dans l'inventaire.")]
    public Sprite icon;

    [Header("Configuration")]
    [Tooltip("Catégorie de l'objet.")]
    public ItemType itemType = ItemType.Item;

    [Tooltip("Type d'équipement (actif uniquement si itemType est Equipment).")]
    public EquipmentType equipmentType = EquipmentType.None;
}
