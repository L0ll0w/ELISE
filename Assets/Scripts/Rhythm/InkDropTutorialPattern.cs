using UnityEngine;

[CreateAssetMenu(fileName = "InkDropTutorialPattern", menuName = "2.5D RPG/Rhythm/Beat Patterns/Ink Drop Tutorial Pattern", order = 5)]
public class InkDropTutorialPattern : EnemyBeatPattern
{
    [Tooltip("Intervalle en battements entre chaque alerte de goutte d'encre (ex: toutes les 4 pulsations).")]
    [SerializeField] private int attackInterval = 4;

    [Tooltip("Durée de l'alerte en battements (ex: 2 pour donner beaucoup de temps au joueur pour esquiver).")]
    [Range(1, 4)]
    [SerializeField] private int warningDuration = 2;

    [Tooltip("Si vrai, cible la case actuelle du joueur pour le forcer à se déplacer. Sinon, cible une case aléatoire.")]
    [SerializeField] private bool targetPlayerDirectly = true;

    public override void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player)
    {
        if (beatIndex % attackInterval != 0) return;

        int targetImpactBeat = beatIndex + warningDuration;

        if (targetPlayerDirectly && player != null)
        {
            // Cibler la case exacte du joueur pour lui apprendre à bouger
            manager.TelegraphCell(player.CurrentRing, player.CurrentSector, targetImpactBeat);
        }
        else
        {
            // Cibler une case aléatoire de la grille
            int randomRing = Random.Range(0, grid.RingsCount);
            int randomSector = Random.Range(0, grid.SectorsCount);
            manager.TelegraphCell(randomRing, randomSector, targetImpactBeat);
        }
    }
}
