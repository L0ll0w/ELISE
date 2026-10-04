using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Bulle de texte en direct dans le monde 3D.
/// Affiche un texte écrit lettre par lettre au-dessus d'un personnage sans interrompre le jeu.
/// Gère le retour automatique à la ligne et permet la personnalisation de la police (TMP_FontAsset).
/// </summary>
public class LiveSpeechBubble : MonoBehaviour
{
    private TextMeshPro tmpText;
    private Coroutine typeCoroutine;
    private Transform followTarget;
    private Vector3 positionOffset;
    private bool isTyping = false;

    [Header("Réglages de la Bulle")]
    [Tooltip("Nombre maximum de mots par ligne avant retour à la ligne automatique.")]
    [SerializeField] private int maxWordsPerLine = 3;

    [Tooltip("Largeur maximale du rectangle de texte en unités 3D.")]
    [SerializeField] private float textRectWidth = 4.5f;

    public bool IsTyping => isTyping;

    /// <summary>
    /// Crée ou récupère une bulle de texte attachée à un personnage cible.
    /// </summary>
    public static LiveSpeechBubble GetOrCreate(Transform target, Vector3 offset)
    {
        if (target == null) return null;

        LiveSpeechBubble bubble = target.GetComponentInChildren<LiveSpeechBubble>();
        if (bubble == null)
        {
            GameObject bubbleObj = new GameObject("LiveSpeechBubble");
            bubbleObj.transform.SetParent(target, false);
            bubble = bubbleObj.AddComponent<LiveSpeechBubble>();
        }

        bubble.followTarget = target;
        bubble.positionOffset = offset;
        bubble.transform.localPosition = offset;

        return bubble;
    }

    private void Awake()
    {
        InitComponents();
    }

    private void InitComponents()
    {
        if (tmpText == null)
        {
            tmpText = GetComponent<TextMeshPro>();
            if (tmpText == null)
            {
                tmpText = gameObject.AddComponent<TextMeshPro>();
            }

            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 4.2f;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = Color.black; // Texte en noir par défaut
            
            // Retour à la ligne automatique & dimensions du conteneur
            tmpText.enableWordWrapping = true;
            tmpText.rectTransform.sizeDelta = new Vector2(textRectWidth, 4.0f);
            tmpText.overflowMode = TextOverflowModes.Overflow;

            // Contour clair pour la lisibilité sur tout fond
            tmpText.outlineColor = new Color(1f, 1f, 1f, 0.85f);
            tmpText.outlineWidth = 0.18f;
            
            tmpText.sortingOrder = 100;
        }
    }

    private void LateUpdate()
    {
        // Billboarding : toujours faire face à la caméra
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }

        // Suivi de la cible
        if (followTarget != null)
        {
            transform.position = followTarget.position + positionOffset;
        }
    }

    /// <summary>
    /// Lance l'affichage du texte lettre par lettre avec option de police personnalisée (TMP_FontAsset).
    /// </summary>
    public void Speak(string text, float typingSpeed = 0.04f, Color? textColor = null, float fontSize = 4.2f, int maxWords = 3, TMP_FontAsset fontAsset = null)
    {
        InitComponents();

        this.maxWordsPerLine = maxWords;

        // Attribuer la police personnalisée si spécifiée
        if (fontAsset != null)
        {
            tmpText.font = fontAsset;
        }

        if (textColor.HasValue)
        {
            tmpText.color = textColor.Value;
        }

        tmpText.fontSize = fontSize;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
        }

        // Formater le texte pour ajouter les retours à la ligne au bon endroit
        string formattedText = FormatWordWrap(text, maxWordsPerLine);

        typeCoroutine = StartCoroutine(TypeTextRoutine(formattedText, typingSpeed));
    }

    /// <summary>
    /// Découpe intelligemment le texte pour aller à la ligne après ~3 mots ou 2 mots longs.
    /// </summary>
    private string FormatWordWrap(string input, int maxWords)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.Contains("\n")) return input; // Conserver les saut de lignes explicites

        string[] words = input.Split(' ');
        if (words.Length <= maxWords) return input;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int wordsInCurrentLine = 0;
        int charsInCurrentLine = 0;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            bool needLineBreak = (wordsInCurrentLine >= maxWords) || 
                                 (wordsInCurrentLine >= 2 && (charsInCurrentLine + word.Length) > 13);

            if (i > 0)
            {
                if (needLineBreak)
                {
                    sb.Append("\n");
                    wordsInCurrentLine = 0;
                    charsInCurrentLine = 0;
                }
                else
                {
                    sb.Append(" ");
                }
            }

            sb.Append(word);
            wordsInCurrentLine++;
            charsInCurrentLine += word.Length;
        }

        return sb.ToString();
    }

    private IEnumerator TypeTextRoutine(string formattedText, float typingSpeed)
    {
        isTyping = true;
        gameObject.SetActive(true);

        tmpText.text = formattedText;
        tmpText.maxVisibleCharacters = 0;
        tmpText.ForceMeshUpdate();

        int totalChars = tmpText.textInfo.characterCount;
        if (totalChars == 0 && !string.IsNullOrEmpty(formattedText))
        {
            totalChars = formattedText.Length;
        }

        int visibleCount = 0;
        while (visibleCount < totalChars)
        {
            visibleCount++;
            tmpText.maxVisibleCharacters = visibleCount;

            char currentChar = (visibleCount - 1 < formattedText.Length) ? formattedText[visibleCount - 1] : ' ';
            float pause = typingSpeed;
            if (currentChar == '.' || currentChar == '!' || currentChar == '?') pause *= 2.5f;
            else if (currentChar == ',' || currentChar == ';') pause *= 1.8f;
            else if (currentChar == '\n') pause *= 1.2f;

            yield return new WaitForSeconds(pause);
        }

        isTyping = false;
    }

    /// <summary>
    /// Efface le texte et masque la bulle.
    /// </summary>
    public void Clear()
    {
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        isTyping = false;
        if (tmpText != null)
        {
            tmpText.text = "";
            tmpText.maxVisibleCharacters = 0;
        }
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        isTyping = false;
    }
}
