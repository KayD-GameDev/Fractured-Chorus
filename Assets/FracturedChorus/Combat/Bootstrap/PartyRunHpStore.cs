using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class PartyRunHpStore
    {
        private static readonly Dictionary<string, int> HpByUnitId = new();

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

                HpByUnitId[unit.UnitId] = Mathf.Clamp(unit.CurrentHp, 0, unit.Stats.MaxHp);
            }
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

        public static void Clear() => HpByUnitId.Clear();
    }
}
