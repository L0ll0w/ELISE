using UnityEngine;

/// <summary>
/// Contient les paramètres centralisés pour l'indicateur visuel d'interaction.
/// Permet de configurer le comportement de tous les indicateurs à un seul endroit.
/// </summary>
[CreateAssetMenu(fileName = "DefaultInteractionPromptSettings", menuName = "ELISE/Interaction Prompt Settings")]
public class InteractionPromptSettings : ScriptableObject
{
    [Header("Visual Prompt Configuration")]
    [Tooltip("The sprite to display when the player is in range.")]
    public Sprite indicatorSprite;
    
    [Tooltip("Visual offset of the indicator relative to this object's center.")]
    public Vector3 indicatorOffset = new Vector3(0.5f, 1f, 0f);
    
    [Tooltip("Local scale of the indicator sprite.")]
    public Vector3 indicatorScale = new Vector3(0.5f, 0.5f, 0.5f);
    
    [Tooltip("Maximum angle of rotation / swaying (in degrees).")]
    public float maxRotationAngle = 15f;
    
    [Tooltip("Speed of the rotation / swaying oscillation.")]
    public float rotationSpeed = 3f;
    
    [Tooltip("Speed of the fade in/out animation.")]
    public float fadeSpeed = 5f;

    [Tooltip("Should the indicator billboard (always face the camera)?")]
    public bool billboard = true;

    [Tooltip("Use the top-right of the collider/renderer bounds as the starting position before offset?")]
    public bool useBoundsTopRight = true;

    [Tooltip("Delay in seconds before the prompt reappears after interaction if no dialogue is active.")]
    public float reappearDelay = 1f;
}
