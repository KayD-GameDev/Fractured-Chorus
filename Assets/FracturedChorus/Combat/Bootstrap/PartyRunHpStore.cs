using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class PartyRunHpStore
    {
        private static readonly List<string> Order = new List<string>();
        private static readonly Dictionary<string, int> HpByUnitId = new();
        private static readonly Dictionary<string, int> MaxHpByUnitId = new();

        public static bool HasData => HpByUnitId.Count > 0;

        public static void CaptureFromSession(CombatSession session)
        {
            if (session?.Grid == null)
            {
                return;
            }

            foreach (var unit in session.Grid.PlayerUnits)
            {
                if (unit == null || string.IsNullOrEmpty(unit.UnitId))
                {
                    continue;
                }

                Write(unit.UnitId, unit.CurrentHp, unit.Stats.MaxHp);
            }
        }

        public static void Write(string unitId, int hp, int maxHp)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return;
            }

            var max = Mathf.Max(1, maxHp);
            if (!HpByUnitId.ContainsKey(unitId))
            {
                Order.Add(unitId);
            }

            MaxHpByUnitId[unitId] = max;
            HpByUnitId[unitId] = Mathf.Clamp(hp, 0, max);
        }

        public static bool TryGet(string unitId, out int hp, out int maxHp)
        {
            hp = 0;
            maxHp = 0;
            if (string.IsNullOrEmpty(unitId) || !HpByUnitId.TryGetValue(unitId, out hp))
            {
                return false;
            }

            MaxHpByUnitId.TryGetValue(unitId, out maxHp);
            return true;
        }

        public static bool CanHealLiving()
        {
            for (var i = 0; i < Order.Count; i++)
            {
                var id = Order[i];
                if (!HpByUnitId.TryGetValue(id, out var hp) || hp <= 0)
                {
                    continue;
                }

                if (MaxHpByUnitId.TryGetValue(id, out var max) && hp < max)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanRevive()
        {
            for (var i = 0; i < Order.Count; i++)
            {
                if (HpByUnitId.TryGetValue(Order[i], out var hp) && hp <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static int HealLivingPercent(float percent)
        {
            var healed = 0;
            var ratio = Mathf.Max(0f, percent);
            for (var i = 0; i < Order.Count; i++)
            {
                var id = Order[i];
                if (!HpByUnitId.TryGetValue(id, out var hp) || hp <= 0)
                {
                    continue;
                }

                if (!MaxHpByUnitId.TryGetValue(id, out var max))
                {
                    continue;
                }

                var next = Mathf.Min(max, hp + Mathf.RoundToInt(max * ratio));
                if (next == hp)
                {
                    continue;
                }

                HpByUnitId[id] = next;
                healed++;
            }

            return healed;
        }

        public static bool ReviveOne(int hp = 1)
        {
            var reviveHp = Mathf.Max(1, hp);
            for (var i = 0; i < Order.Count; i++)
            {
                var id = Order[i];
                if (!HpByUnitId.TryGetValue(id, out var current) || current > 0)
                {
                    continue;
                }

                var max = MaxHpByUnitId.TryGetValue(id, out var storedMax) ? storedMax : reviveHp;
                HpByUnitId[id] = Mathf.Min(reviveHp, max);
                return true;
            }

            return false;
        }

        public static void ApplyToUnit(CombatUnit unit)
        {
            if (unit == null || unit.Side != GridSide.Player || string.IsNullOrEmpty(unit.UnitId))
            {
                return;
            }

            unit.ResetPrep();

            if (!HpByUnitId.TryGetValue(unit.UnitId, out var hp))
            {
                return;
            }

            unit.SetCurrentHp(hp);
        }

        public static void ApplyToSession(CombatSession session)
        {
            if (session?.Grid == null)
            {
                return;
            }

            foreach (var unit in session.Grid.PlayerUnits)
            {
                ApplyToUnit(unit);
            }
        }

        public static void RestoreFullAtCamp() => Clear();

        public static void Clear()
        {
            Order.Clear();
            HpByUnitId.Clear();
            MaxHpByUnitId.Clear();
        }
    }
}
