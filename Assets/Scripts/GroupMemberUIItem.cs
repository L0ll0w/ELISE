using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'affichage individuel d'un compagnon dans l'onglet Groupe du menu.
/// Alterne le portrait à gauche ou à droite selon la parité de l'index dans la liste.
/// </summary>
[AddComponentMenu("2.5D RPG/Group Member UI Item")]
public class GroupMemberUIItem : MonoBehaviour
{
    [Header("Composants UI")]
    [Tooltip("Zone de texte pour afficher le nom du compagnon.")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Conteneur d'image du portrait à gauche.")]
    [SerializeField] private Image leftPortraitImage;

    [Tooltip("Conteneur d'image du portrait à droite.")]
    [SerializeField] private Image rightPortraitImage;

    /// <summary>
    /// Initialise les composants graphiques du membre du groupe.
    /// </summary>
    /// <param name="characterName">Nom à afficher.</param>
    /// <param name="portrait">Sprite de portrait.</param>
    /// <param name="isEven">Vrai si l'index est pair (portrait à gauche), Faux si impair (portrait à droite).</param>
    public void Setup(string characterName, Sprite portrait, bool isEven)
    {
        if (nameText != null)
        {
            nameText.text = characterName;
        }

        if (isEven)
        {
            // Activer portrait gauche, désactiver portrait droit
            if (leftPortraitImage != null)
            {
                leftPortraitImage.gameObject.SetActive(true);
                leftPortraitImage.sprite = portrait;
                // Si pas de portrait, on peut cacher ou mettre un placeholder
                leftPortraitImage.enabled = portrait != null;
            }
            if (rightPortraitImage != null)
            {
                rightPortraitImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // Désactiver portrait gauche, activer portrait droit
            if (leftPortraitImage != null)
            {
                leftPortraitImage.gameObject.SetActive(false);
            }
            if (rightPortraitImage != null)
            {
                rightPortraitImage.gameObject.SetActive(true);
                rightPortraitImage.sprite = portrait;
                rightPortraitImage.enabled = portrait != null;
            }
        }
    }
}
