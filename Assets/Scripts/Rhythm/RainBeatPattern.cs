using UnityEngine;

[CreateAssetMenu(fileName = "RainBeatPattern", menuName = "2.5D RPG/Rhythm/Beat Patterns/Rain Pattern", order = 2)]
public class RainBeatPattern : EnemyBeatPattern
{
    [Tooltip("Intervalle de battements entre chaque pluie (ex: toutes les 2 pulsations).")]
    [SerializeField] private int beatInterval = 2;

    [Tooltip("Si vrai, alterne en damier parfait à chaque intervalle de pluie.")]
    [SerializeField] private bool checkerboardAlternation = true;

    public override void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player)
    {
        if (beatIndex % beatInterval != 0) return;

        int targetImpactBeat = beatIndex + 1;
        bool oddCycle = (beatIndex / beatInterval) % 2 == 1;

        for (int s = 0; s < grid.SectorsCount; s++)
        {
            // Damier : si oddCycle, on cible Ring 0 sur les secteurs pairs, Ring 1 sur les impairs. Sinon l'inverse.
            bool targetRing0 = (s % 2 == 0) ^ (checkerboardAlternation && oddCycle);
            
            if (targetRing0)
            {
                manager.TelegraphCell(0, s, targetImpactBeat);
            }
            else
            {
                manager.TelegraphCell(1, s, targetImpactBeat);
            }
        }
    }
}
