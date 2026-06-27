using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Thứ tự thẻ party: cùng hàng → cột giảm dần (Mage→Ren→Tank); cùng cột → thứ tự đặt lên lưới.
    /// </summary>
    public static class PartyCardDisplayOrder
    {
        public static int Compare(UnitView a, UnitView b)
        {
            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            var posA = a.GridPosition;
            var posB = b.GridPosition;

            if (posA.Column == posB.Column)
            {
                return ComparePlacementOrder(a, b);
            }

            if (posA.Row == posB.Row)
            {
                return posB.Column.CompareTo(posA.Column);
            }

            var columnCompare = posB.Column.CompareTo(posA.Column);
            if (columnCompare != 0)
            {
                return columnCompare;
            }

            var rowCompare = posA.Row.CompareTo(posB.Row);
            if (rowCompare != 0)
            {
                return rowCompare;
            }

            return ComparePlacementOrder(a, b);
        }

        public static int CompareUnits(CombatUnit a, CombatUnit b)
        {
            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            var posA = a.GridPosition;
            var posB = b.GridPosition;

            if (posA.Column == posB.Column)
            {
                return a.PartyBarOrder.CompareTo(b.PartyBarOrder);
            }

            if (posA.Row == posB.Row)
            {
                return posB.Column.CompareTo(posA.Column);
            }

            var columnCompare = posB.Column.CompareTo(posA.Column);
            if (columnCompare != 0)
            {
                return columnCompare;
            }

            var rowCompare = posA.Row.CompareTo(posB.Row);
            if (rowCompare != 0)
            {
                return rowCompare;
            }

            return a.PartyBarOrder.CompareTo(b.PartyBarOrder);
        }

        public static void SortUnitViews(List<UnitView> views)
        {
            views.Sort(Compare);
        }

        private static int ComparePlacementOrder(UnitView a, UnitView b)
        {
            var orderA = a.Unit?.PartyBarOrder ?? int.MaxValue;
            var orderB = b.Unit?.PartyBarOrder ?? int.MaxValue;
            return orderA.CompareTo(orderB);
        }
    }
}
