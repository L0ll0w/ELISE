using UnityEngine;

/// <summary>
/// Contient les informations d'un compagnon du groupe à afficher dans le menu UI (nom, portrait).
/// </summary>
[AddComponentMenu("2.5D RPG/Group Member Info")]
public class GroupMemberInfo : MonoBehaviour
{
    [Header("Données ScriptableObject")]
    [Tooltip("La référence vers le ScriptableObject de données de ce personnage.")]
    [SerializeField] private CharacterData characterData;

    [Header("Fallbacks (Si pas de ScriptableObject)")]
    [Tooltip("Le nom affiché pour ce personnage dans le menu du groupe.")]
    [SerializeField] private string characterName = "Nom du Personnage";

    [Tooltip("Le portrait de ce personnage à afficher dans le menu du groupe.")]
    [SerializeField] private Sprite portrait;

    [Tooltip("L'icône ou sprite de ce personnage à afficher dans la liste (ScrollView).")]
    [SerializeField] private Sprite menuIcon;

    public CharacterData CharacterData
    {
        get => characterData;
        set => characterData = value;
    }

    public string CharacterName
    {
        get => characterData != null ? characterData.characterName : characterName;
        set
        {
            if (characterData != null) characterData.characterName = value;
            else characterName = value;
        }
    }

    public Sprite Portrait
    {
        get => characterData != null ? characterData.portrait : portrait;
        set
        {
            if (characterData != null) characterData.portrait = value;
            else portrait = value;
        }
    }

    public Sprite MenuIcon
    {
        get => characterData != null ? characterData.menuIcon : menuIcon;
        set
        {
            if (characterData != null) characterData.menuIcon = value;
            else menuIcon = value;
        }
    }
}
