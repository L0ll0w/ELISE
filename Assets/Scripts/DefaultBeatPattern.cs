using UnityEngine;

[CreateAssetMenu(fileName = "DefaultBeatPattern", menuName = "2.5D RPG/Rhythm/Beat Patterns/Default Pattern", order = 1)]
public class DefaultBeatPattern : EnemyBeatPattern
{
    public override void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player)
    {
        // On prévient 1 beat à l'avance (Telegraph à beat N, dégâts à beat N+1)
        int targetImpactBeat = beatIndex + 1;

        // Pattern simple alterné
        int patternIndex = beatIndex % 8;

        switch (patternIndex)
        {
            case 0:
                // 1. Attaque sur tout le cercle intérieur (Ring 0)
                manager.TelegraphEntireRing(0, targetImpactBeat);
                break;
            case 2:
                // 2. Attaque sur tout le cercle extérieur (Ring 1)
                manager.TelegraphEntireRing(1, targetImpactBeat);
                break;
            case 4:
                // 3. Attaque sur les secteurs pairs (0, 2, 4, 6)
                for (int s = 0; s < grid.SectorsCount; s += 2)
                {
                    manager.TelegraphCell(0, s, targetImpactBeat);
                    manager.TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 6:
                // 4. Attaque sur les secteurs impairs (1, 3, 5, 7)
                for (int s = 1; s < grid.SectorsCount; s += 2)
                {
                    manager.TelegraphCell(0, s, targetImpactBeat);
                    manager.TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 7:
                // 5. Attaque ciblée sur le secteur actuel du joueur
                manager.TelegraphCell(0, player.CurrentSector, targetImpactBeat);
                manager.TelegraphCell(1, player.CurrentSector, targetImpactBeat);
                break;
        }
    }
}
