using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.AI
{
    public class SimpleEnemyAI
    {
        /// <summary>
        /// Mỗi phase: mỗi quái một đòn 2 pha (S1 wind-up + S impact) trên timeline.
        /// </summary>
        public void PlanTelegraphsForPhase(int phaseIndex, DualGrid grid, BeatTimelineEngine timeline)
        {
            if (grid == null || timeline == null)
            {
                return;
            }

            TimelineConstants.GetPhaseBeatRange(phaseIndex, out var startBeat, out var slotCount);

            var enemies = grid.EnemyUnits.Where(u => u.IsAlive).ToList();
            timeline.ClearTelegraphsInRange(startBeat, slotCount);

            if (enemies.Count == 0)
            {
                return;
            }

            var beatPicks = new List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat)>();

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var skill = enemy.Skills.FirstOrDefault(s => s != null && s.IsAttack);
                if (skill == null)
                {
                    continue;
                }

                var impactPool = BuildImpactBeatPool(startBeat, slotCount, skill, beatPicks);
                if (impactPool.Count == 0)
                {
                    continue;
                }

                var impact = impactPool[Random.Range(0, impactPool.Count)];
                beatPicks.Add((enemy, skill, impact));
            }

            foreach (var (enemy, skill, impactBeat) in beatPicks)
            {
                var s1 = SkillFootprintUtil.GetStandingBefore(skill);
                for (var w = s1; w >= 1; w--)
                {
                    timeline.AddTelegraph(enemy, skill, impactBeat - w, isWindupOnly: true);
                }

                timeline.AddTelegraph(enemy, skill, impactBeat, isWindupOnly: false);
            }

            Debug.Log(
                $"[EnemyAI] Phase {phaseIndex + 1}: {beatPicks.Count} impact telegraph @ beats [{string.Join(", ", beatPicks.Select(p => p.impactBeat))}]");
        }

        private static List<int> BuildImpactBeatPool(int startBeat, int slotCount, SkillDefinitionSO skill,
            List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat)> taken)
        {
            var s1 = SkillFootprintUtil.GetStandingBefore(skill);
            var takenBeats = new HashSet<int>(taken.Select(t => t.impactBeat));
            var pool = new List<int>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                var impact = startBeat + i;
                if (impact < TimelineConstants.EnemyFirstAttackBeat || takenBeats.Contains(impact))
                {
                    continue;
                }

                var firstWindup = impact - s1;
                if (firstWindup < startBeat)
                {
                    continue;
                }

                pool.Add(impact);
            }

            return pool;
        }
    }
}
