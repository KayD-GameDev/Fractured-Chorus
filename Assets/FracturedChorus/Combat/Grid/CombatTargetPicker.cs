using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;

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

            return PickHighestBaseAvAlive(grid.PlayerUnits);
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
            PickHighestBaseAvAlive(grid?.PlayerUnits);

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
