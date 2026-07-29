using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Formation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Grid
{
    public static class CombatTargetPicker
    {
        public static CombatUnit PickEnemyAttackTargetForBeat(DualGrid grid, BeatTimelineEngine timeline, int beatIndex)
        {
            if (grid == null)
            {
                return null;
            }

            var standingUnits = GetStandingUnitsOnBeat(grid, timeline, beatIndex);
            if (standingUnits.Count > 0)
            {
                return standingUnits.OrderByDescending(u => u.Stats.BaseAv).First();
            }

            return PickEnemyAttackTarget(grid, BossFormationRuntime.Active);
        }

        public static List<CombatUnit> GetStandingUnitsOnBeat(DualGrid grid, BeatTimelineEngine timeline, int beatIndex)
        {
            var result = new List<CombatUnit>();
            if (grid == null || timeline == null)
            {
                return result;
            }

            foreach (var entry in timeline.Agenda)
            {
                if (entry.Unit == null || entry.Unit.Side != GridSide.Player || !entry.Unit.IsAlive || entry.Skill == null)
                {
                    continue;
                }

                foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(entry.Skill, entry.BeatIndex))
                {
                    if (info.BeatIndex != beatIndex)
                    {
                        continue;
                    }

                    if (info.Role == FootprintBeatRole.StandingBefore || info.Role == FootprintBeatRole.StandingAfter)
                    {
                        if (!result.Contains(entry.Unit))
                        {
                            result.Add(entry.Unit);
                        }
                    }
                }
            }

            return result;
        }

        public static CombatUnit PickHighestBaseAvAlive(IEnumerable<CombatUnit> units)
        {
            return units.Where(u => u != null && u.IsAlive).OrderByDescending(u => u.Stats.BaseAv).FirstOrDefault();
        }

        public static CombatUnit PickEnemyAttackTarget(DualGrid grid) =>
            PickEnemyAttackTarget(grid, BossFormationRuntime.Active);

        public static CombatUnit PickEnemyAttackTarget(DualGrid grid, BossFormationProfileSO profile)
        {
            if (grid == null)
            {
                return null;
            }

            var alive = grid.PlayerUnits.Where(u => u != null && u.IsAlive).ToList();
            if (alive.Count == 0)
            {
                return null;
            }

            if (profile == null)
            {
                return PickHighestBaseAvAlive(alive);
            }

            var candidates = alive;
            if (profile.backPierceChance > 0f && Random.value < profile.backPierceChance)
            {
                var backOnly = alive
                    .Where(u => u.GridPosition.Column == PositionalModifiers.BackColumnIndex)
                    .ToList();
                if (backOnly.Count > 0)
                {
                    candidates = backOnly;
                }
            }

            var frontWeight = Mathf.Max(0.01f, profile.frontTargetWeight);
            var totalWeight = 0f;
            var weights = new float[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                var weight = 1f;
                if (candidates[i].GridPosition.Column == PositionalModifiers.FrontColumnIndex)
                {
                    weight *= frontWeight;
                }

                weights[i] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return PickHighestBaseAvAlive(candidates);
            }

            var roll = Random.value * totalWeight;
            var cumulative = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        public static CombatUnit PickPlayerAttackTarget(DualGrid grid)
        {
            if (grid == null)
            {
                return null;
            }

            var alive = grid.EnemyUnits.Where(u => u.IsAlive).ToList();
            if (alive.Count == 0)
            {
                return null;
            }

            for (var column = 0; column < DualGrid.Columns; column++)
            {
                var inColumn = alive.Where(u => u.GridPosition.Column == column).ToList();
                if (inColumn.Count == 0)
                {
                    continue;
                }

                return inColumn.OrderBy(u => u.GridPosition.Row).First();
            }

            return alive[0];
        }
    }
}
