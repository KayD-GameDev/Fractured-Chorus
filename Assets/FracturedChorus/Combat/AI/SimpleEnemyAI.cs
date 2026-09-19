using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.AI
{
    public class SimpleEnemyAI
    {
        private static readonly List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat, BossNoteTier tier, int hits)> EmptyPicks = new();

        /// <summary>
        /// Plan notes for one phase. Does not clear existing telegraphs (Charlotte-pushed notes stay).
        /// Only fills empty beats up to the scaled attack count.
        /// </summary>
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
            if (enemies.Count == 0)
            {
                return;
            }

            var beatPicks = new List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat, BossNoteTier tier, int hits)>();
            var occupied = timeline.CollectImpactBeatsInRange(startBeat, slotCount);

            foreach (var enemy in enemies)
            {
                var skill = enemy.Skills.FirstOrDefault(s => s != null && s.IsAttack);
                if (skill == null)
                {
                    continue;
                }

                var attacks = ScaleAttacksForPhaseLength(enemy.TelegraphAttacksPerPhase, slotCount);
                for (var q = 0; q < attacks; q++)
                {
                    var impactPool = BuildImpactBeatPool(startBeat, slotCount, occupied, beatPicks);
                    if (impactPool.Count == 0)
                    {
                        break;
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
                    occupied.Add(impact);
                }
            }

            foreach (var (enemy, skill, impactBeat, tier, hits) in beatPicks)
            {
                timeline.AddTelegraph(enemy, skill, impactBeat, isWindupOnly: false, tier, hits);
            }

            Debug.Log(
                $"[EnemyAI] Phase {phaseIndex + 1}: +{beatPicks.Count} telegraph @ beats [{string.Join(", ", beatPicks.Select(p => p.impactBeat))}]");
        }

        public void ApplyMoodExtras(int phaseIndex, DualGrid grid, BeatTimelineEngine timeline)
        {
            if (grid == null || timeline == null || !AstraTvMoodState.CoversSegment(phaseIndex))
            {
                return;
            }

            TimelineConstants.GetPhaseBeatRange(phaseIndex, out var startBeat, out var slotCount);
            if (slotCount <= 0)
            {
                return;
            }

            var occupied = timeline.CollectImpactBeatsInRange(startBeat, slotCount);
            var added = 0;
            if (AstraTvMoodState.ExtraRedCoreNotes > 0)
            {
                var boss = grid.EnemyUnits.FirstOrDefault(u => u != null && u.IsAlive && u.Role == UnitRole.Boss);
                added += TryAddExtraAttacks(
                    boss, startBeat, slotCount, occupied, timeline,
                    AstraTvMoodState.ExtraRedCoreNotes, BossNoteTier.Red, 1);
            }

            if (AstraTvMoodState.ExtraMiniAttacks > 0)
            {
                foreach (var mini in grid.EnemyUnits.Where(IsMiniTarget))
                {
                    added += TryAddExtraAttacks(
                        mini, startBeat, slotCount, occupied, timeline,
                        AstraTvMoodState.ExtraMiniAttacks, BossNoteTier.Red, 1);
                }
            }

            if (added > 0)
            {
                Debug.Log($"[EnemyAI] Phase {phaseIndex + 1}: +{added} TV-mood extra telegraph(s) ({AstraTvMoodState.Mood})");
            }
        }

        private static bool IsMiniTarget(CombatUnit unit)
        {
            if (unit == null || !unit.IsAlive || unit.Role == UnitRole.Boss)
            {
                return false;
            }

            var id = unit.UnitId ?? string.Empty;
            var name = unit.DisplayName ?? string.Empty;
            return unit.Role == UnitRole.Elite
                   || id.IndexOf("eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || id.IndexOf("mic", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Mic", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int TryAddExtraAttacks(
            CombatUnit enemy,
            int startBeat,
            int slotCount,
            HashSet<int> occupied,
            BeatTimelineEngine timeline,
            int count,
            BossNoteTier tier,
            int hits)
        {
            if (enemy == null || count <= 0)
            {
                return 0;
            }

            var skill = enemy.Skills.FirstOrDefault(s => s != null && s.IsAttack);
            if (skill == null)
            {
                return 0;
            }

            var added = 0;
            for (var q = 0; q < count; q++)
            {
                var pool = BuildImpactBeatPool(startBeat, slotCount, occupied, EmptyPicks);
                if (pool.Count == 0)
                {
                    break;
                }

                var impact = pool[Random.Range(0, pool.Count)];
                timeline.AddTelegraph(enemy, skill, impact, isWindupOnly: false, tier, hits);
                occupied.Add(impact);
                added++;
            }

            return added;
        }

        private static List<int> BuildImpactBeatPool(
            int startBeat,
            int slotCount,
            HashSet<int> occupied,
            List<(CombatUnit enemy, SkillDefinitionSO skill, int impactBeat, BossNoteTier tier, int hits)> taken)
        {
            var takenBeats = new HashSet<int>(taken.Select(t => t.impactBeat));
            var pool = new List<int>(slotCount);
            var minImpact = TimelineConstants.GetMinEnemyImpactBeat(startBeat);

            for (var i = 0; i < slotCount; i++)
            {
                var impact = startBeat + i;
                if (impact < minImpact
                    || takenBeats.Contains(impact)
                    || (occupied != null && occupied.Contains(impact)))
                {
                    continue;
                }

                pool.Add(impact);
            }

            return pool;
        }

        /// <summary>~25% denser than the old 16-beat baseline (phase 22).</summary>
        private static int ScaleAttacksForPhaseLength(int baseAttacks, int slotCount)
        {
            const int referenceSlots = 16;
            const float densityBoost = 1.25f;
            if (slotCount <= 0 || baseAttacks <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(baseAttacks * (float)slotCount / referenceSlots * densityBoost));
        }
    }
}
