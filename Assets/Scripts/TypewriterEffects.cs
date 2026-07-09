using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gère l'écriture lettre par lettre (Typewriter) avec détection de la ponctuation,
/// et applique des effets visuels (Shake, Glitch, Wave) sur des mots balisés.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[AddComponentMenu("2.5D RPG/Typewriter Effects")]
public class TypewriterEffects : MonoBehaviour
{
    public enum EffectType { None, Shake, Glitch, Wave }

    public struct CharacterEffect
    {
        public int charIndex;
        public EffectType effect;
    }

    [Header("Paramètres d'Écriture")]
    [Tooltip("Temps d'attente de base entre chaque lettre (en secondes).")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Tooltip("Temps d'attente supplémentaire lors d'une virgule ou double point.")]
    [SerializeField] private float commaPauseTime = 0.15f;

    [Tooltip("Temps d'attente supplémentaire lors d'un point, exclamation ou interrogation.")]
    [SerializeField] private float periodPauseTime = 0.35f;

    [Header("Réglages Effet : Secousse (Shake)")]
    [SerializeField] private float shakeStrength = 1.5f;

    [Header("Réglages Effet : Glitch")]
    [SerializeField] private float glitchStrength = 3f;
    [SerializeField] private float glitchFrequency = 8f;

    [Header("Réglages Effet : Vague (Wave)")]
    [SerializeField] private float waveHeight = 4f;
    [SerializeField] private float waveSpeed = 8f;
    [SerializeField] private float waveSpread = 0.2f;

    private TextMeshProUGUI textComponent;
    private List<CharacterEffect> activeEffects = new List<CharacterEffect>();
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Lance l'écriture animée d'un texte.
    /// </summary>
    public void StartTyping(string rawText, System.Action onComplete = null)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeTextCoroutine(rawText, onComplete));
    }

    /// <summary>
    /// Affiche instantanément l'intégralité du texte en cours d'écriture.
    /// </summary>
    public void Skip()
    {
        if (!isTyping) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        textComponent.maxVisibleCharacters = textComponent.textInfo.characterCount;
        isTyping = false;
    }

    private IEnumerator TypeTextCoroutine(string rawText, System.Action onComplete)
    {
        isTyping = true;

        // 1. Analyse et retrait des balises d'effets personnalisées
        string cleanText = ParseCustomEffects(rawText, out List<CharacterEffect> parsedEffects);
        activeEffects = parsedEffects;

        textComponent.text = cleanText;
        textComponent.maxVisibleCharacters = 0;

        // Force la mise à jour pour calculer le nombre réel de caractères
        textComponent.ForceMeshUpdate();
        int totalCharacters = textComponent.textInfo.characterCount;

        // Si c'est la toute première activation du composant, TextMeshPro peut renvoyer 0 caractères
        // car ses structures internes ne sont pas encore prêtes. On attend une frame pour corriger cela.
        if (totalCharacters == 0 && !string.IsNullOrEmpty(cleanText))
        {
            yield return null;
            textComponent.ForceMeshUpdate();
            totalCharacters = textComponent.textInfo.characterCount;
        }

        // Si après l'attente c'est toujours 0 (bug persistant d'initialisation de Canvas/Mesh),
        // on calcule nous-mêmes la longueur du texte épurée de ses balises.
        string rawString = GetRawStringWithoutTags(cleanText);
        bool useFallback = (totalCharacters == 0 && rawString.Length > 0);
        if (useFallback)
        {
            totalCharacters = rawString.Length;
        }

        int visibleCount = 0;

        while (visibleCount < totalCharacters)
        {
            visibleCount++;
            textComponent.maxVisibleCharacters = visibleCount;

            // Analyse le dernier caractère pour ajuster le rythme
            char lastChar = ' ';
            if (!useFallback && textComponent.textInfo != null && 
                textComponent.textInfo.characterInfo != null && 
                visibleCount - 1 < textComponent.textInfo.characterInfo.Length)
            {
                lastChar = textComponent.textInfo.characterInfo[visibleCount - 1].character;
            }
            else if (visibleCount - 1 < rawString.Length)
            {
                // Fallback direct sur la chaîne nettoyée si le mesh n'est pas encore prêt
                lastChar = rawString[visibleCount - 1];
            }

            float waitTime = GetPauseTime(lastChar);
            yield return new WaitForSeconds(waitTime);
        }

        isTyping = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Extrait la chaîne brute débarrassée des balises riches de TMPro (ex: <color=...>)
    /// pour calculer la taille et analyser la ponctuation en cas d'un maillage non initialisé.
    /// </summary>
    private string GetRawStringWithoutTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool inTag = false;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<') inTag = true;
            else if (text[i] == '>') inTag = false;
            else if (!inTag) sb.Append(text[i]);
        }
        return sb.ToString();
    }

    private float GetPauseTime(char c)
    {
        if (c == '.' || c == '?' || c == '!')
        {
            return periodPauseTime;
        }
        else if (c == ',' || c == ';' || c == ':')
        {
            return commaPauseTime;
        }
        else
        {
            return typingSpeed;
        }
    }

    private void LateUpdate()
    {
        // On n'anime les sommets que si des effets sont actifs
        if (activeEffects == null || activeEffects.Count == 0) return;

        // Recalcule le mesh de base pour écraser les modifications de la frame précédente
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        float time = Time.time;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            CharacterEffect fx = activeEffects[i];
            int charIdx = fx.charIndex;

            // N'anime pas les caractères non encore révélés par le Typewriter
            if (charIdx >= textInfo.characterCount || charIdx >= textComponent.maxVisibleCharacters) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIdx];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            // Modifie les positions des 4 sommets (quad) formant le caractère
            for (int v = 0; v < 4; v++)
            {
                Vector3 orig = destinationVertices[vertexIndex + v];
                Vector3 offset = Vector3.zero;

                switch (fx.effect)
                {
                    case EffectType.Shake:
                        offset.x = Random.Range(-shakeStrength, shakeStrength);
                        offset.y = Random.Range(-shakeStrength, shakeStrength);
                        break;

                    case EffectType.Glitch:
                        // Le glitch produit une secousse brusque intermittente
                        if (Mathf.Repeat(time * glitchFrequency, 10f) < 0.15f)
                        {
                            offset.x = Random.Range(-glitchStrength, glitchStrength);
                            offset.y = Random.Range(-glitchStrength, glitchStrength);
                        }
                        break;

                    case EffectType.Wave:
                        // Effet de vague sinusoïdale fluide
                        offset.y = Mathf.Sin(time * waveSpeed + charIdx * waveSpread) * waveHeight;
                        break;
                }

                destinationVertices[vertexIndex + v] = orig + offset;
            }
        }

        // Met à jour la géométrie du composant TextMeshPro
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    /// <summary>
    /// Analyse le texte source pour extraire les balises d'effets personnalisées (ex: <shake>mot</shake>).
    /// Retourne une chaîne nettoyée compatible avec TextMeshPro, tout en listant les effets par index de lettre.
    /// </summary>
    private string ParseCustomEffects(string sourceText, out List<CharacterEffect> effects)
    {
        effects = new List<CharacterEffect>();
        System.Text.StringBuilder cleanText = new System.Text.StringBuilder();

        Stack<EffectType> activeEffectsStack = new Stack<EffectType>();
        activeEffectsStack.Push(EffectType.None);

        int charIndex = 0;
        int i = 0;
        int length = sourceText.Length;

        while (i < length)
        {
            if (sourceText[i] == '<')
            {
                int tagEnd = sourceText.IndexOf('>', i);
                if (tagEnd != -1)
                {
                    string tag = sourceText.Substring(i, tagEnd - i + 1);
                    string tagContent = tag.ToLower();

                    if (tagContent == "<shake>")
                    {
                        activeEffectsStack.Push(EffectType.Shake);
                        i = tagEnd + 1;
                        continue;
                    }
                    else if (tagContent == "</shake>")
                    {
                        if (activeEffectsStack.Peek() == EffectType.Shake) activeEffectsStack.Pop();
                        i = tagEnd + 1;
                        continue;
                    }
                    else if (tagContent == "<glitch>")
                    {
                        activeEffectsStack.Push(EffectType.Glitch);
                        i = tagEnd + 1;
                        continue;
                    }
                    else if (tagContent == "</glitch>")
                    {
                        if (activeEffectsStack.Peek() == EffectType.Glitch) activeEffectsStack.Pop();
                        i = tagEnd + 1;
                        continue;
                    }
                    else if (tagContent == "<wave>")
                    {
                        activeEffectsStack.Push(EffectType.Wave);
                        i = tagEnd + 1;
                        continue;
                    }
                    else if (tagContent == "</wave>")
                    {
                        if (activeEffectsStack.Peek() == EffectType.Wave) activeEffectsStack.Pop();
                        i = tagEnd + 1;
                        continue;
                    }
                    else
                    {
                        // C'est une balise standard de TextMeshPro (ex: <color=yellow> ou <b>)
                        // On la conserve dans le texte final, mais sans l'analyser comme caractère
                        cleanText.Append(tag);
                        i = tagEnd + 1;
                        continue;
                    }
                }
            }

            // Caractère standard
            cleanText.Append(sourceText[i]);

            EffectType currentEffect = activeEffectsStack.Peek();
            if (currentEffect != EffectType.None)
            {
                effects.Add(new CharacterEffect { charIndex = charIndex, effect = currentEffect });
            }

            charIndex++;
            i++;
        }

        return cleanText.ToString();
    }
}
