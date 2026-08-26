using UnityEngine;

[CreateAssetMenu(fileName = "ColumnShotBeatPattern", menuName = "2.5D RPG/Rhythm/Beat Patterns/Column Shot Pattern", order = 3)]
public class ColumnShotBeatPattern : EnemyBeatPattern
{
    [Tooltip("Intervalle de battements entre chaque tir (ex: toutes les 4 pulsations).")]
    [SerializeField] private int shotInterval = 4;

    [Tooltip("Durée de l'alerte en battements avant l'impact (ex: alerte à beat N, dégâts à beat N + warningDuration).")]
    [SerializeField] private int warningDuration = 1;

    [Tooltip("Si vrai, cible également les 2 secteurs adjacents pour forcer un changement de cercle (et pas juste tourner).")]
    [SerializeField] private bool splashDamage = false;

    public override void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player)
    {
        if (beatIndex % shotInterval != 0) return;

        int targetImpactBeat = beatIndex + warningDuration;
        int targetSector = player.CurrentSector;

        // Télégraphier toute la colonne (les 2 cercles du secteur du joueur)
        for (int r = 0; r < grid.RingsCount; r++)
        {
            manager.TelegraphCell(r, targetSector, targetImpactBeat);
        }

        // Si Splash, on télégraphie aussi les secteurs adjacents sur l'anneau intérieur
        if (splashDamage)
        {
            int leftSector = (targetSector - 1 + grid.SectorsCount) % grid.SectorsCount;
            int rightSector = (targetSector + 1) % grid.SectorsCount;

            manager.TelegraphCell(0, leftSector, targetImpactBeat);
            manager.TelegraphCell(0, rightSector, targetImpactBeat);
        }
    }
}
