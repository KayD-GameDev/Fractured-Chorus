using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Thứ tự logic thẻ (thẻ 1 → thẻ N) theo formation:
    /// 1) Cột: C1/front (Tank) = thẻ 1 → C2 → C3 (Mage);
    /// 2) Cùng cột: H2 → H1 (trên) → H3 (dưới) — khớp số hàng đỏ trên board;
    /// 3) Hòa hàng: PartyBarOrder.
    /// Layout UI: thẻ 1 neo ngoài cùng phải (PartyCardLayout).
    /// </summary>
    public static class PartyCardDisplayOrder
    {
        public const float BarSlotSpacing = PartyCardLayout.CardStepX;

        /// <summary>Hàng hiển thị 2 trên honeycomb (Board margin.drawio).</summary>
        public const int PriorityDisplayRow = 2;

        public static int PriorityRowIndex => HoneycombIndex.ToIndex(PriorityDisplayRow);

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

            return ComparePositions(
                ResolvePosition(a),
                a.Unit?.PartyBarOrder ?? int.MaxValue,
                ResolvePosition(b),
                b.Unit?.PartyBarOrder ?? int.MaxValue);
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

            return ComparePositions(
                a.GridPosition,
                a.PartyBarOrder,
                b.GridPosition,
                b.PartyBarOrder);
        }

        public static void SortUnitViews(List<UnitView> views)
        {
            views.Sort(Compare);
        }

        private static int ComparePositions(
            GridPosition posA,
            int orderA,
            GridPosition posB,
            int orderB)
        {
            if (posA.Column == posB.Column)
            {
                return CompareSameColumn(posA, orderA, posB, orderB);
            }

            var columnCompare = CompareColumnsForBarOrder(posA.Column, posB.Column);
            if (columnCompare != 0)
            {
                return columnCompare;
            }

            return orderA.CompareTo(orderB);
        }

        /// <summary>C1 (front/Tank) = thẻ 1, rồi C2, C3.</summary>
        private static int CompareColumnsForBarOrder(int columnA, int columnB)
        {
            return columnA.CompareTo(columnB);
        }

        private static int CompareSameColumn(
            GridPosition posA,
            int orderA,
            GridPosition posB,
            int orderB)
        {
            var rowRankCompare = GetWithinColumnRowRank(posA.Row).CompareTo(GetWithinColumnRowRank(posB.Row));
            if (rowRankCompare != 0)
            {
                return rowRankCompare;
            }

            return orderA.CompareTo(orderB);
        }

        /// <summary>
        /// Số hàng đỏ trên board: 1=trên (index 2), 2=giữa/H2 (index 1), 3=dưới (index 0).
        /// Thứ tự thẻ trong cột: hàng 2 → hàng 1 → hàng 3.
        /// </summary>
        private static int GetWithinColumnRowRank(int rowIndex)
        {
            var userRow = DualGrid.Rows - rowIndex;
            return userRow switch
            {
                2 => 0,
                1 => 1,
                3 => 2,
                _ => userRow + 10
            };
        }

        private static GridPosition ResolvePosition(UnitView view)
        {
            if (view?.Unit != null)
            {
                return view.Unit.GridPosition;
            }

            return view?.GridPosition ?? default;
        }
    }
}
