using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Units;

namespace FracturedChorus.Combat.Grid
{
    /// <summary>
    /// Enemy targeting: cột Tank (C1–C3) trước → cột giữa (C2) → cột Mage.
    /// Bỏ qua cột Tank khi không còn Tank sống.
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

            var tankColumn = FindRoleColumn(alive, UnitRole.Tank);
            var mageColumn = FindRoleColumn(alive, UnitRole.Mage);
            var tankAlive = alive.Any(u => u.Role == UnitRole.Tank);

            var columnOrder = BuildColumnOrder(tankColumn, mageColumn, tankAlive);

            foreach (var column in columnOrder)
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

        private static int? FindRoleColumn(IReadOnlyList<CombatUnit> units, UnitRole role)
        {
            var unit = units.FirstOrDefault(u => u.Role == role);
            return unit != null ? unit.GridPosition.Column : (int?)null;
        }

        private static IEnumerable<int> BuildColumnOrder(int? tankColumn, int? mageColumn, bool tankAlive)
        {
            var order = new List<int>();

            if (tankAlive && tankColumn.HasValue)
            {
                order.Add(tankColumn.Value);
            }

            if (!order.Contains(1))
            {
                order.Add(1);
            }

            if (mageColumn.HasValue && !order.Contains(mageColumn.Value))
            {
                order.Add(mageColumn.Value);
            }

            for (var col = 0; col < DualGrid.Columns; col++)
            {
                if (!order.Contains(col))
                {
                    order.Add(col);
                }
            }

            return order;
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
