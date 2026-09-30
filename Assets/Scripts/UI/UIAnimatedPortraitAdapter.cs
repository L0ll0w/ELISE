using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Synchronise automatiquement les animations de SpriteRenderer (classID 212)
/// avec le composant UI Image (classID 114) pour éviter le carré blanc dans l'UI Canvas Unity.
/// </summary>
[AddComponentMenu("2.5D RPG/UI Animated Portrait Adapter")]
public class UIAnimatedPortraitAdapter : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        InitComponents();
    }

    private void OnEnable()
    {
        InitComponents();
        SyncSprite();
    }

    public void InitComponents()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void SetTargets(Image image, SpriteRenderer sr)
    {
        targetImage = image;
        spriteRenderer = sr;
        SyncSprite();
    }

    private void Update()
    {
        SyncSprite();
    }

    private void LateUpdate()
    {
        SyncSprite();
    }

    public void SyncSprite()
    {
        if (spriteRenderer != null && targetImage != null)
        {
            Sprite currentSprite = spriteRenderer.sprite;
            if (targetImage.sprite != currentSprite)
            {
                targetImage.sprite = currentSprite;
            }
            targetImage.enabled = (currentSprite != null);
        }
        else if (targetImage != null && spriteRenderer == null)
        {
            targetImage.enabled = (targetImage.sprite != null);
        }
    }
}
