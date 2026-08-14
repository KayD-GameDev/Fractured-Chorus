using System;
using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
        public static class CombatPoolPlacement
        {
            public static int[] RollBattleSlots(int runSeed, int nodeId)
        {
            var rng = CreateRng(runSeed, nodeId, 37);
            var backRow = new List<int> { 0, 1, 2 };
            var frontRow = new List<int> { 3, 4, 5 };
            Shuffle(backRow, rng);
            Shuffle(frontRow, rng);
            return EnsureUniqueSlots(new[] { backRow[0], frontRow[0], frontRow[1] });
        }

        public static int[] RollEliteSlots(int runSeed, int nodeId)
        {
            var rng = CreateRng(runSeed, nodeId, 41);
            var backRow = new List<int> { 0, 1, 2 };
            var frontRow = new List<int> { 3, 4, 5 };
            Shuffle(backRow, rng);
            Shuffle(frontRow, rng);
            return EnsureUniqueSlots(new[] { backRow[0], frontRow[0], frontRow[1] });
        }

        public static int[] EnsureUniqueSlots(int[] slots, int count = 3)
        {
            var capacity = DualGrid.Rows * DualGrid.Columns;
            count = Mathf.Clamp(count, 1, capacity);
            var result = new int[count];
            var used = new bool[capacity];
            var write = 0;

            if (slots != null)
            {
                for (var i = 0; i < slots.Length && write < count; i++)
                {
                    var slot = NormalizeSlot(slots[i]);
                    if (used[slot])
                    {
                        continue;
                    }

                    used[slot] = true;
                    result[write++] = slot;
                }
            }

            for (var slot = 0; slot < capacity && write < count; slot++)
            {
                if (used[slot])
                {
                    continue;
                }

                used[slot] = true;
                result[write++] = slot;
            }

            return result;
        }

        public static int NormalizeSlot(int slot)
        {
            var size = DualGrid.Rows * DualGrid.Columns;
            var n = slot % size;
            return n < 0 ? n + size : n;
        }

        public static GridPosition SlotToGridPosition(int slot)
        {
            slot = NormalizeSlot(slot);
            var row = slot / DualGrid.Columns;
            var col = slot % DualGrid.Columns;
            return new GridPosition(GridSide.Enemy, row, col);
        }

        private static System.Random CreateRng(int runSeed, int nodeId, int salt)
        {
            unchecked
            {
                var mixed = runSeed ^ (nodeId * 73856093) ^ (salt * 19349663);
                return new System.Random(mixed);
            }
        }

        private static void Shuffle(List<int> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
