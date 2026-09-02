using UnityEngine;

/// <summary>
/// Classe de base abstraite pour définir le comportement d'attaque rythmique d'un ennemi.
/// </summary>
public abstract class EnemyBeatPattern : ScriptableObject
{
    /// <summary>
    /// Traite la logique rythmique à chaque pulsation (beat).
    /// </summary>
    /// <param name="beatIndex">L'index du battement actuel.</param>
    /// <param name="manager">Le gestionnaire de combat rythmique.</param>
    /// <param name="grid">La grille de combat circulaire.</param>
    /// <param name="player">Le contrôleur du joueur sur la grille.</param>
    public abstract void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player);
}
