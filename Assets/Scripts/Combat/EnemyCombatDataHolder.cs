using UnityEngine;

/// <summary>
/// Conteneur permettant de lier les données de combat rythmique (ScriptableObject)
/// à un GameObject ennemi présent dans le monde de jeu.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Enemy Combat Data Holder")]
public class EnemyCombatDataHolder : MonoBehaviour
{
    [Tooltip("Les données de combat rythmique pour cet ennemi.")]
    [SerializeField] private EnemyCombatData combatData;

    public EnemyCombatData CombatData
    {
        get => combatData;
        set => combatData = value;
    }
}
