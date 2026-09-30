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

    private GameObject instantiatedLeftPortrait;
    private GameObject instantiatedRightPortrait;

    /// <summary>
    /// Initialise les composants graphiques du membre du groupe.
    /// </summary>
    /// <param name="characterName">Nom à afficher.</param>
    /// <param name="portrait">Sprite de portrait.</param>
    /// <param name="isEven">Vrai si l'index est pair (portrait à gauche), Faux si impair (portrait à droite).</param>
    /// <param name="portraitPrefab">Prefab GameObject animé optionnel.</param>
    /// <param name="portraitAnimator">Animator Controller optionnel.</param>
    public void Setup(string characterName, Sprite portrait, bool isEven, GameObject portraitPrefab = null, RuntimeAnimatorController portraitAnimator = null)
    {
        if (nameText != null)
        {
            nameText.text = characterName;
        }

        // Nettoyer les précédents prefabs instanciés si existants
        if (instantiatedLeftPortrait != null) { Destroy(instantiatedLeftPortrait); instantiatedLeftPortrait = null; }
        if (instantiatedRightPortrait != null) { Destroy(instantiatedRightPortrait); instantiatedRightPortrait = null; }

        if (isEven)
        {
            // Activer portrait gauche, désactiver portrait droit
            if (rightPortraitImage != null)
            {
                rightPortraitImage.enabled = false;
                rightPortraitImage.gameObject.SetActive(false);
            }

            if (leftPortraitImage != null)
            {
                if (portraitPrefab != null)
                {
                    leftPortraitImage.enabled = false;
                    leftPortraitImage.gameObject.SetActive(false);
                    instantiatedLeftPortrait = Instantiate(portraitPrefab, leftPortraitImage.transform.parent, false);
                    RectTransform rt = instantiatedLeftPortrait.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        rt.anchoredPosition = Vector2.zero;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    }

                    SpriteRenderer sr = instantiatedLeftPortrait.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = instantiatedLeftPortrait.GetComponentInChildren<SpriteRenderer>(true);
                    Image img = instantiatedLeftPortrait.GetComponent<Image>();
                    if (img == null && sr != null) img = instantiatedLeftPortrait.AddComponent<Image>();
                    if (img != null && sr != null)
                    {
                        UIAnimatedPortraitAdapter adapter = instantiatedLeftPortrait.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter == null) adapter = instantiatedLeftPortrait.AddComponent<UIAnimatedPortraitAdapter>();
                        adapter.SetTargets(img, sr);
                        adapter.SyncSprite();
                    }
                }
                else
                {
                    leftPortraitImage.gameObject.SetActive(true);
                    leftPortraitImage.sprite = portrait;

                    SpriteRenderer sr = leftPortraitImage.GetComponent<SpriteRenderer>();
                    Animator anim = leftPortraitImage.GetComponent<Animator>();
                    if (portraitAnimator != null)
                    {
                        if (sr == null) sr = leftPortraitImage.gameObject.AddComponent<SpriteRenderer>();
                        sr.enabled = false;

                        if (anim == null) anim = leftPortraitImage.gameObject.AddComponent<Animator>();
                        anim.runtimeAnimatorController = portraitAnimator;
                        anim.enabled = true;
                        anim.Rebind();
                        anim.Update(0f);

                        UIAnimatedPortraitAdapter adapter = leftPortraitImage.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter == null) adapter = leftPortraitImage.gameObject.AddComponent<UIAnimatedPortraitAdapter>();
                        adapter.SetTargets(leftPortraitImage, sr);
                        adapter.SyncSprite();
                    }
                    else
                    {
                        if (anim != null) anim.enabled = false;
                        UIAnimatedPortraitAdapter adapter = leftPortraitImage.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter != null) adapter.enabled = false;
                        leftPortraitImage.enabled = portrait != null;
                    }
                }
            }
        }
        else
        {
            // Désactiver portrait gauche, activer portrait droit
            if (leftPortraitImage != null)
            {
                leftPortraitImage.enabled = false;
                leftPortraitImage.gameObject.SetActive(false);
            }

            if (rightPortraitImage != null)
            {
                if (portraitPrefab != null)
                {
                    rightPortraitImage.enabled = false;
                    rightPortraitImage.gameObject.SetActive(false);
                    instantiatedRightPortrait = Instantiate(portraitPrefab, rightPortraitImage.transform.parent, false);
                    RectTransform rt = instantiatedRightPortrait.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        rt.anchoredPosition = Vector2.zero;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    }

                    SpriteRenderer sr = instantiatedRightPortrait.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = instantiatedRightPortrait.GetComponentInChildren<SpriteRenderer>(true);
                    Image img = instantiatedRightPortrait.GetComponent<Image>();
                    if (img == null && sr != null) img = instantiatedRightPortrait.AddComponent<Image>();
                    if (img != null && sr != null)
                    {
                        UIAnimatedPortraitAdapter adapter = instantiatedRightPortrait.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter == null) adapter = instantiatedRightPortrait.AddComponent<UIAnimatedPortraitAdapter>();
                        adapter.SetTargets(img, sr);
                        adapter.SyncSprite();
                    }
                }
                else
                {
                    rightPortraitImage.gameObject.SetActive(true);
                    rightPortraitImage.sprite = portrait;

                    SpriteRenderer sr = rightPortraitImage.GetComponent<SpriteRenderer>();
                    Animator anim = rightPortraitImage.GetComponent<Animator>();
                    if (portraitAnimator != null)
                    {
                        if (sr == null) sr = rightPortraitImage.gameObject.AddComponent<SpriteRenderer>();
                        sr.enabled = false;

                        if (anim == null) anim = rightPortraitImage.gameObject.AddComponent<Animator>();
                        anim.runtimeAnimatorController = portraitAnimator;
                        anim.enabled = true;
                        anim.Rebind();
                        anim.Update(0f);

                        UIAnimatedPortraitAdapter adapter = rightPortraitImage.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter == null) adapter = rightPortraitImage.gameObject.AddComponent<UIAnimatedPortraitAdapter>();
                        adapter.SetTargets(rightPortraitImage, sr);
                        adapter.SyncSprite();
                    }
                    else
                    {
                        if (anim != null) anim.enabled = false;
                        UIAnimatedPortraitAdapter adapter = rightPortraitImage.GetComponent<UIAnimatedPortraitAdapter>();
                        if (adapter != null) adapter.enabled = false;
                        rightPortraitImage.enabled = portrait != null;
                    }
                }
            }
        }
    }
}
