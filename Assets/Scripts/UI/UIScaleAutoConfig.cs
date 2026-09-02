using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajuste automatiquement le CanvasScaler de n'importe quel Canvas du jeu
/// afin d'assurer un positionnement et une taille d'UI identiques quelle que soit la résolution d'écran.
/// </summary>
[RequireComponent(typeof(Canvas))]
[ExecuteAlways]
[AddComponentMenu("2.5D RPG/UI Scale Auto Config")]
public class UIScaleAutoConfig : MonoBehaviour
{
    [Header("Configuration Référence")]
    [Tooltip("Résolution de référence sur laquelle l'UI a été conçue (par défaut 1920x1080 Full HD).")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

    [Tooltip("Pondération entre largeur (0) et hauteur (1). 0.5 offre le meilleur équilibre sur tout écran.")]
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    private void Awake()
    {
        ApplyScalingSettings();
    }

    private void OnValidate()
    {
        ApplyScalingSettings();
    }

    /// <summary>
    /// Applique les réglages de dimensionnement réactif sur le CanvasScaler.
    /// </summary>
    public void ApplyScalingSettings()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
