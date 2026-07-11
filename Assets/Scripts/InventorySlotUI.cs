using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'affichage d'un emplacement (slot) d'objet dans l'UI d'inventaire.
/// </summary>
[AddComponentMenu("2.5D RPG/Inventory Slot UI")]
public class InventorySlotUI : MonoBehaviour
{
    [Header("Composants UI")]
    [Tooltip("Zone de texte pour afficher le nom de l'objet.")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Zone de texte pour afficher la description de l'objet.")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("Composant image pour afficher l'icône de l'objet.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Zone de texte pour afficher la quantité.")]
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Paramètres de Sélection")]
    [Tooltip("L'image d'arrière-plan du slot qui changera de couleur lors de la sélection.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Couleur par défaut du slot.")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("Couleur du slot lorsqu'il est sélectionné.")]
    [SerializeField] private Color selectedColor = Color.yellow;

    private void Start()
    {
        // Récupération automatique de l'image de fond si non assignée
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
    }

    /// <summary>
    /// Initialise graphiquement le slot avec les données d'un objet.
    /// </summary>
    /// <param name="item">Les données ScriptableObject de l'objet.</param>
    /// <param name="quantity">La quantité possédée.</param>
    public void Setup(ItemData item, int quantity)
    {
        if (item == null) return;

        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = item.description;
        }

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
        }

        if (quantityText != null)
        {
            // Affiche la quantité formatée (ex: "x15" ou "15")
            quantityText.text = $"x{quantity}";
            // Masque la quantité si c'est un équipement ou si la quantité est nulle
            quantityText.gameObject.SetActive(item.itemType != ItemType.Equipment && quantity > 0);
        }

        // Réinitialise la couleur par défaut
        SetSelected(false);
    }

    /// <summary>
    /// Change l'état visuel de sélection de cette case en modifiant sa couleur.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
    }
}
