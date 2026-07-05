using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.AI;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.AI
{
    public class SimpleEnemyAI
    {
        public void PlanTelegraphsForPhase(int phaseIndex, DualGrid grid, BeatTimelineEngine timeline)
        {
            if (grid == null || timeline == null)
            {
                return;
            }

            TimelineConstants.GetPhaseBeatRange(phaseIndex, out var startBeat, out var slotCount);
            if (slotCount <= 0)
            {
                return;
            }

            var enemies = grid.EnemyUnits.Where(u => u.IsAlive).ToList();
            timeline.ClearTelegraphsInRange(startBeat, slotCount);

            if (enemies.Count == 0)
            {
                return;
            }

            var beatPicks = new List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat, BossNoteTier tier, int hits)>();

            foreach (var enemy in enemies)
            {
                var skill = enemy.Skills.FirstOrDefault(s => s != null && s.IsAttack);
                if (skill == null)
                {
                    continue;
                }

                for (var q = 0; q < enemy.TelegraphAttacksPerPhase; q++)
                {
                    var impactPool = BuildImpactBeatPool(startBeat, slotCount, beatPicks);
                    if (impactPool.Count == 0)
                    {
                        continue;
                    }

                    var impact = impactPool[Random.Range(0, impactPool.Count)];
                    var tier = enemy.Role switch
                    {
                        UnitRole.Boss => BossTelegraphPlanner.RollNoteTier(phaseIndex),
                        UnitRole.Elite => BossTelegraphPlanner.RollEliteNoteTier(),
                        _ => BossNoteTier.Red
                    };
                    var hits = enemy.Role == UnitRole.Boss || enemy.Role == UnitRole.Elite
                        ? BossTelegraphPlanner.HitsRequiredForTier(tier)
                        : 1;
                    beatPicks.Add((enemy, skill, impact, tier, hits));
                }
            }

            foreach (var (enemy, skill, impactBeat, tier, hits) in beatPicks)
            {
                timeline.AddTelegraph(enemy, skill, impactBeat, isWindupOnly: false, tier, hits);
            }

            Debug.Log(
                $"[EnemyAI] Phase {phaseIndex + 1}: {beatPicks.Count} impact telegraph @ beats [{string.Join(", ", beatPicks.Select(p => p.impactBeat))}]");
        }

        private static List<int> BuildImpactBeatPool(int startBeat, int slotCount,
            List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat, BossNoteTier tier, int hits)> taken)
        {
            var takenBeats = new HashSet<int>(taken.Select(t => t.impactBeat));
            var pool = new List<int>(slotCount);

            for (var i = 0; i < slotCount; i++)
            {
                var impact = startBeat + i;
                if (impact < TimelineConstants.EnemyFirstAttackBeat || takenBeats.Contains(impact))
                {
                    continue;
                }

                pool.Add(impact);
            }

            return pool;
        }
    }
}
