using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestionnaire centralisé des fondus d'écran (Fade In / Fade Out au noir).
/// Évite la duplication de la création de Canvas de transition dans chaque manager.
/// </summary>
[AddComponentMenu("2.5D RPG/Core/UI Fade Manager")]
public class UIFadeManager : MonoBehaviour
{
    private static UIFadeManager instance;

    public static UIFadeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("[UIFadeManager]");
                instance = obj.AddComponent<UIFadeManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private CanvasGroup fadeCanvasGroup;
    private Image blackImage;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeCanvas();
    }

    private void InitializeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObj = new GameObject("BlackOverlay");
        imageObj.transform.SetParent(transform, false);

        blackImage = imageObj.AddComponent<Image>();
        blackImage.color = Color.black;

        RectTransform rect = blackImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Coroutine pour fondu progressif vers une opacité donnée (0 = transparent, 1 = noir complet).
    /// </summary>
    public IEnumerator FadeRoutine(float targetAlpha, float duration = 0.5f)
    {
        InitializeCanvas();
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, duration > 0f ? elapsed / duration : 1f);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
    }

    /// <summary>
    /// Lancer un fondu au noir instantané ou progressif.
    /// </summary>
    public void SetFadeInstant(float alpha)
    {
        InitializeCanvas();
        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = alpha > 0.01f;
    }
}
