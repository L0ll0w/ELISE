using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SequencerBeatPattern", menuName = "2.5D RPG/Rhythm/Beat Patterns/Sequencer Pattern", order = 4)]
public class SequencerBeatPattern : EnemyBeatPattern
{
    public enum AttackTargetType
    {
        SpecificCell,
        EntireRing,
        PlayerColumn,
        PlayerCell,
        CheckerboardEven,
        CheckerboardOdd,
        AllGrid
    }

    [System.Serializable]
    public struct BeatAction
    {
        [Tooltip("Le battement relatif dans la boucle (ex: de 0 à 15 pour une boucle de 16 temps).")]
        public int beatOffset;

        [Tooltip("Le type de cible pour cette attaque.")]
        public AttackTargetType targetType;

        [Tooltip("L'anneau ciblé (si applicable).")]
        [Range(0, 1)]
        public int ringIndex;

        [Tooltip("Le secteur ciblé (si applicable).")]
        [Range(0, 7)]
        public int sectorIndex;

        [Tooltip("Nombre de battements d'alerte avant l'impact.")]
        [Range(1, 4)]
        public int warningDuration;
    }

    [Header("Configuration de la Séquence")]
    [Tooltip("Longueur totale de la boucle en battements (ex: 16 pour une mesure de 16 temps).")]
    [SerializeField] private int loopLength = 16;

    [Tooltip("La liste des actions rythmiques composant la chorégraphie du combat.")]
    [SerializeField] private List<BeatAction> sequence = new List<BeatAction>();

    public override void ProcessBeat(int beatIndex, RhythmCombatManager manager, RadialCombatGrid grid, RhythmPlayerController player)
    {
        if (loopLength <= 0) return;

        // Calculer l'index local dans la boucle
        int localBeat = beatIndex % loopLength;

        // Trouver toutes les actions qui se déclenchent à ce beat local
        foreach (var action in sequence)
        {
            if (action.beatOffset == localBeat)
            {
                int targetImpactBeat = beatIndex + action.warningDuration;

                switch (action.targetType)
                {
                    case AttackTargetType.SpecificCell:
                        manager.TelegraphCell(action.ringIndex, action.sectorIndex, targetImpactBeat);
                        break;

                    case AttackTargetType.EntireRing:
                        manager.TelegraphEntireRing(action.ringIndex, targetImpactBeat);
                        break;

                    case AttackTargetType.PlayerColumn:
                        for (int r = 0; r < grid.RingsCount; r++)
                        {
                            manager.TelegraphCell(r, player.CurrentSector, targetImpactBeat);
                        }
                        break;

                    case AttackTargetType.PlayerCell:
                        manager.TelegraphCell(player.CurrentRing, player.CurrentSector, targetImpactBeat);
                        break;

                    case AttackTargetType.CheckerboardEven:
                        for (int s = 0; s < grid.SectorsCount; s++)
                        {
                            int r = s % 2;
                            manager.TelegraphCell(r, s, targetImpactBeat);
                        }
                        break;

                    case AttackTargetType.CheckerboardOdd:
                        for (int s = 0; s < grid.SectorsCount; s++)
                        {
                            int r = (s + 1) % 2;
                            manager.TelegraphCell(r, s, targetImpactBeat);
                        }
                        break;

                    case AttackTargetType.AllGrid:
                        for (int r = 0; r < grid.RingsCount; r++)
                        {
                            for (int s = 0; s < grid.SectorsCount; s++)
                            {
                                manager.TelegraphCell(r, s, targetImpactBeat);
                            }
                        }
                        break;
                }
            }
        }
    }
}
