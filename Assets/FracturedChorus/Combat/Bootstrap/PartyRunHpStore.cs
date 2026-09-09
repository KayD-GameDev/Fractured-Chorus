using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    /// <summary>
    /// HP party giữa các trận. Vẫn là cache trong RAM cho nhanh, nhưng mọi thay đổi đều được
    /// ghi thẳng sang GameMetaState.PartyVitals nên save file luôn có số mới nhất
    /// mà không cần ai nhớ gọi "chụp trước khi save".
    /// </summary>
    public static class PartyRunHpStore
    {
        private static readonly List<string> Order = new List<string>();
        private static readonly Dictionary<string, int> HpByUnitId = new();
        private static readonly Dictionary<string, int> MaxHpByUnitId = new();

        private static bool s_hooked;

        public static bool HasData => HpByUnitId.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSession()
        {
            if (s_hooked)
            {
                return;
            }

            s_hooked = true;
            GameMetaSession.SessionStateChanged += OnSessionStateChanged;
        }

        private static void OnSessionStateChanged(GameMetaState state)
        {
            Order.Clear();
            HpByUnitId.Clear();
            MaxHpByUnitId.Clear();

            if (state?.PartyVitals == null)
            {
                return;
            }

            foreach (var entry in state.PartyVitals.Entries)
            {
                if (string.IsNullOrEmpty(entry.UnitId))
                {
                    continue;
                }

                var max = Mathf.Max(1, entry.MaxHp);
                Order.Add(entry.UnitId);
                MaxHpByUnitId[entry.UnitId] = max;
                HpByUnitId[entry.UnitId] = Mathf.Clamp(entry.Hp, 0, max);
            }
        }

        /// <summary>Đẩy toàn bộ cache sang state đang mở để lần ghi file kế tiếp có số đúng.</summary>
        private static void SyncToSession()
        {
            if (!GameMetaSession.HasSession)
            {
                return;
            }

            var vitals = GameMetaSession.Current.PartyVitals;
            vitals.Clear();

            for (var i = 0; i < Order.Count; i++)
            {
                var id = Order[i];
                if (!HpByUnitId.TryGetValue(id, out var hp))
                {
                    continue;
                }

                MaxHpByUnitId.TryGetValue(id, out var max);
                vitals.Set(id, hp, max);
            }
        }

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
            SyncToSession();
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

            if (healed > 0)
            {
                SyncToSession();
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
                SyncToSession();
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
            SyncToSession();
        }
    }
}
