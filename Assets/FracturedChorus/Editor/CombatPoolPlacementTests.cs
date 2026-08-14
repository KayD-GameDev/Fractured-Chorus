using System.Collections.Generic;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class CombatPoolPlacementTests
    {
        [Test]
        public void RollBattleSlots_AreUniqueAndMixRows()
        {
            var slots = CombatPoolPlacement.RollBattleSlots(7, 3);
            AssertUniqueSlots(slots, 3);
            Assert.AreEqual(1, CountInRange(slots, 0, 2));
            Assert.AreEqual(2, CountInRange(slots, 3, 5));
        }

        [Test]
        public void RollEliteSlots_AreUniqueAndMixRows()
        {
            var slots = CombatPoolPlacement.RollEliteSlots(11, 5);
            AssertUniqueSlots(slots, 3);
            Assert.AreEqual(1, CountInRange(slots, 0, 2));
            Assert.AreEqual(2, CountInRange(slots, 3, 5));
        }

        [Test]
        public void SlotToGridPosition_MapsSixSlotsToDistinctCells()
        {
            var seen = new HashSet<GridPosition>();
            for (var slot = 0; slot < 6; slot++)
            {
                Assert.IsTrue(seen.Add(CombatPoolPlacement.SlotToGridPosition(slot)), slot.ToString());
            }
        }

        [Test]
        public void EnsureUniqueSlots_DedupesAndFills()
        {
            var slots = CombatPoolPlacement.EnsureUniqueSlots(new[] { 4, 4, 4 }, 3);
            AssertUniqueSlots(slots, 3);
        }

        [Test]
        public void TryPlaceUnitOrEmptyCell_RelocatesWhenOccupied()
        {
            var grid = new DualGrid();
            var first = CreateEnemy("a");
            var second = CreateEnemy("b");
            var pos = new GridPosition(GridSide.Enemy, 1, 1);
            Assert.IsTrue(grid.TryPlaceUnit(first, pos));
            Assert.IsTrue(grid.TryPlaceUnitOrEmptyCell(second, ref pos));
            Assert.IsFalse(first.GridPosition.Equals(second.GridPosition));
        }

        private static CombatUnit CreateEnemy(string id)
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = id;
            preset.displayName = id;
            return new CombatUnit(preset, GridSide.Enemy);
        }

        private static void AssertUniqueSlots(int[] slots, int count)
        {
            Assert.AreEqual(count, slots.Length);
            var seen = new HashSet<int>();
            foreach (var slot in slots)
            {
                Assert.IsTrue(slot >= 0 && slot < 6, slot.ToString());
                Assert.IsTrue(seen.Add(slot), slot.ToString());
            }
        }

        private static int CountInRange(int[] slots, int min, int max)
        {
            var n = 0;
            foreach (var slot in slots)
            {
                if (slot >= min && slot <= max)
                {
                    n++;
                }
            }

            return n;
        }
    }
}
