using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Units;

namespace FracturedChorus.Combat.Grid
{
    /// <summary>
    /// Enemy targeting: cột đầu tiên (C1 / index 0) trước → C2 → C3.
    /// Tank chết thì chuyển sang cột Ren, rồi Mage.
    /// </summary>
    public static class CombatTargetPicker
    {
        public static CombatUnit PickEnemyAttackTarget(DualGrid grid)
        {
            if (grid == null)
            {
                return null;
            }

            var alive = grid.PlayerUnits.Where(u => u.IsAlive).ToList();
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

                return PickPrimaryInColumn(inColumn);
            }

            return alive[0];
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

        private static CombatUnit PickPrimaryInColumn(IReadOnlyList<CombatUnit> inColumn)
        {
            return inColumn
                .OrderBy(u => u.Role == UnitRole.Tank ? 0 : u.Role == UnitRole.Dps ? 1 : 2)
                .ThenBy(u => u.GridPosition.Row)
                .First();
        }
    }
}
