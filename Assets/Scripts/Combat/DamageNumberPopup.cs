using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Pop-up de dégâts gribouillé (ex: "-33") sans texte BAM/OUCH.
/// Utilise la police personnalisée du projet et applique un effet de gribouillage / jitter crayomné.
/// </summary>
public class DamageNumberPopup : MonoBehaviour
{
    private TextMeshPro tmpText;
    private float lifeTime = 0.8f;
    private Vector3 floatVelocity;

    /// <summary>
    /// Spawne un nombre de dégâts dans le monde 3D avec la police choisie et l'effet gribouillage.
    /// </summary>
    public static DamageNumberPopup Create(Vector3 position, int amount, bool isPlayerDamage = false, TMP_FontAsset fontAsset = null)
    {
        GameObject popupObj = new GameObject("DamageNumberPopup");
        popupObj.transform.position = position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.4f), Random.Range(-0.1f, 0.1f));

        DamageNumberPopup popup = popupObj.AddComponent<DamageNumberPopup>();
        popup.Setup(amount, isPlayerDamage, fontAsset);

        return popup;
    }

    private void Setup(int amount, bool isPlayerDamage, TMP_FontAsset fontAsset)
    {
        tmpText = gameObject.AddComponent<TextMeshPro>();

        // Police personnalisée du jeu
        if (fontAsset != null)
        {
            tmpText.font = fontAsset;
        }

        // Juste le chiffre (ex: "-33"), sans BAM ni OUCH
        tmpText.text = $"-{amount}";
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 5.2f; // Plus petit et discret
        tmpText.fontStyle = FontStyles.Bold;

        // Blanc pur avec contour noir net
        tmpText.color = Color.white;
        tmpText.outlineColor = Color.black;
        tmpText.outlineWidth = 0.35f;

        // Angle oblique style dessin à la main
        float randomAngle = Random.Range(-12f, 12f);
        transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        // Vitesse d'envol
        floatVelocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(2.0f, 2.8f), 0f);

        StartCoroutine(PopAndFloatRoutine());
    }

    private IEnumerator PopAndFloatRoutine()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation * Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
        }

        Vector3 targetScale = Vector3.one * (tmpText.fontSize / 5.2f);
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float popDuration = 0.10f;

        // 1. Pop-scale élastique (0 -> 1.35 -> 1.0)
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float scaleBounce = t < 0.7f 
                ? Mathf.Lerp(0f, 1.35f, t / 0.7f) 
                : Mathf.Lerp(1.35f, 1.0f, (t - 0.7f) / 0.3f);
            
            transform.localScale = targetScale * scaleBounce;
            yield return null;
        }

        transform.localScale = targetScale;

        // 2. Envol parabolique avec micro-gribouillage (jitter crayonnique) + fondu
        elapsed = 0f;
        Vector3 currentPos = transform.position;

        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifeTime);

            currentPos += floatVelocity * Time.deltaTime;
            floatVelocity.y -= Time.deltaTime * 3.8f; // Gravité d'envol

            // Effet de tremblotement / gribouillage (jitter à la main)
            Vector3 scribbleJitter = new Vector3(Random.Range(-0.025f, 0.025f), Random.Range(-0.025f, 0.025f), 0f);
            transform.position = currentPos + scribbleJitter;

            // Garder face à la caméra
            if (mainCam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position) * Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
            }

            // Fondu progressif vers la fin
            if (t > 0.55f)
            {
                float fadeT = (t - 0.55f) / 0.45f;
                Color c = tmpText.color;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                tmpText.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
