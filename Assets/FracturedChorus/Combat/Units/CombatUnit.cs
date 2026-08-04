using System;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Units
{
    public class CombatUnit
    {
        public string UnitId { get; }
        public string DisplayName { get; }
        public UnitRole Role { get; }
        public GridSide Side { get; private set; }
        public GridPosition GridPosition { get; private set; }
        public UnitStats Stats { get; }
        public SkillDefinitionSO[] Skills { get; private set; }
        public Color PlaceholderColor { get; }
        public Sprite TimelineAvatarSprite { get; }
        public const int PrepCap = 3;

        public int CurrentHp { get; private set; }
        public int CurrentDelay { get; set; }
        public int Prep { get; private set; }
        public int Shield { get; private set; }
        public int PendingReduceS2 { get; private set; }

        private int _timedShieldPool;
        private int _timedShieldExpireBeatExclusive = int.MaxValue;

        /// <summary>Thứ tự đặt lên lưới — dùng sắp xếp thẻ party khi cùng cột/hàng.</summary>
        public int PartyBarOrder { get; internal set; }

        /// <summary>Read-only mirror of BaseAv for UI — not spent when using skills.</summary>
        public float CurrentAv => Stats.BaseAv;

        public float ActionPriority => Stats.ActionPriority;

        public int TelegraphAttacksPerPhase { get; }

        public bool IsAlive => CurrentHp > 0;

        public HpChangeInfo LastHpChange { get; private set; } = HpChangeInfo.Silent;

        public event Action<CombatUnit> OnHpChanged;
        public event Action<CombatUnit> OnPrepChanged;
        public event Action<CombatUnit> OnPendingReduceS2Changed;
        public event Action<CombatUnit> OnDied;

        public CombatUnit(UnitPresetSO preset, GridSide side)
        {
            UnitId = string.IsNullOrEmpty(preset.unitId) ? Guid.NewGuid().ToString("N") : preset.unitId;
            DisplayName = preset.displayName;
            Role = preset.role;
            Side = side;
            Stats = preset.ResolveStats();
            Skills = preset.skills ?? Array.Empty<SkillDefinitionSO>();
            PlaceholderColor = preset.placeholderColor;
            TimelineAvatarSprite = preset.timelineAvatarSprite;
            TelegraphAttacksPerPhase = Mathf.Max(1, preset.telegraphAttacksPerPhase);
            CurrentHp = Stats.MaxHp;
        }

        public void SetGridPosition(GridPosition position)
        {
            GridPosition = position;
            Side = position.Side;
        }

        public void ReplaceSkills(SkillDefinitionSO[] skills)
        {
            Skills = skills ?? Array.Empty<SkillDefinitionSO>();
        }

        public void SetCurrentHp(int hp)
        {
            LastHpChange = HpChangeInfo.Silent;
            CurrentHp = Mathf.Clamp(hp, 0, Stats.MaxHp);
            OnHpChanged?.Invoke(this);
            if (!IsAlive)
            {
                OnDied?.Invoke(this);
            }
        }

        public void TakeDamage(float amount, bool isCritical = false)
        {
            if (!IsAlive)
            {
                return;
            }

            var display = Mathf.Max(0, Mathf.RoundToInt(amount));
            var remaining = display;
            if (Shield > 0 && remaining > 0)
            {
                var absorbed = Mathf.Min(Shield, remaining);
                Shield -= absorbed;
                remaining -= absorbed;
                if (_timedShieldPool > 0)
                {
                    _timedShieldPool = Mathf.Min(_timedShieldPool, Shield);
                }
            }

            if (remaining > 0)
            {
                CurrentHp = Mathf.Max(0, CurrentHp - remaining);
            }

            LastHpChange = display > 0
                ? new HpChangeInfo(HpChangeKind.Damage, display, isCritical)
                : HpChangeInfo.Silent;
            OnHpChanged?.Invoke(this);

            if (!IsAlive)
            {
                OnDied?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            Heal(amount, convertOverhealToShield: false, overhealShieldCap: 0);
        }

        public int Heal(float amount, bool convertOverhealToShield, int overhealShieldCap)
        {
            if (!IsAlive)
            {
                return 0;
            }

            var heal = Mathf.Max(0, Mathf.RoundToInt(amount));
            var room = Mathf.Max(0, Stats.MaxHp - CurrentHp);
            var applied = Mathf.Min(room, heal);
            CurrentHp += applied;
            LastHpChange = applied > 0
                ? new HpChangeInfo(HpChangeKind.Heal, applied, false)
                : HpChangeInfo.Silent;
            OnHpChanged?.Invoke(this);

            var overheal = heal - applied;
            if (convertOverhealToShield && overheal > 0 && overhealShieldCap > 0)
            {
                AddShield(Mathf.Min(overheal, overhealShieldCap));
            }

            return applied;
        }

        public void AddShield(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            Shield += amount;
            LastHpChange = HpChangeInfo.Silent;
            OnHpChanged?.Invoke(this);
        }

        public void GrantTimedShield(int amount, int expireAtBeatExclusive)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            AddShield(amount);
            _timedShieldPool += amount;
            if (_timedShieldExpireBeatExclusive == int.MaxValue)
            {
                _timedShieldExpireBeatExclusive = expireAtBeatExclusive;
            }
            else
            {
                _timedShieldExpireBeatExclusive = Mathf.Max(
                    _timedShieldExpireBeatExclusive,
                    expireAtBeatExclusive);
            }
        }

        public void TickTimedShieldExpiry(int currentBeat)
        {
            if (_timedShieldPool <= 0 || currentBeat < _timedShieldExpireBeatExclusive)
            {
                return;
            }

            var remove = Mathf.Min(Shield, _timedShieldPool);
            Shield -= remove;
            _timedShieldPool = 0;
            _timedShieldExpireBeatExclusive = int.MaxValue;
            LastHpChange = HpChangeInfo.Silent;
            OnHpChanged?.Invoke(this);
        }

        public int GainPrep(int amount = 1)
        {
            if (amount <= 0)
            {
                return Prep;
            }

            var next = Mathf.Min(PrepCap, Prep + amount);
            if (next == Prep)
            {
                return Prep;
            }

            Prep = next;
            OnPrepChanged?.Invoke(this);
            return Prep;
        }

        public bool TrySpendPrep(int amount)
        {
            if (amount <= 0 || Prep < amount)
            {
                return false;
            }

            Prep -= amount;
            OnPrepChanged?.Invoke(this);
            return true;
        }

        public void ResetPrep()
        {
            if (Prep == 0)
            {
                return;
            }

            Prep = 0;
            OnPrepChanged?.Invoke(this);
        }

        public void SetPrepAbsolute(int value)
        {
            var next = Mathf.Clamp(value, 0, PrepCap);
            if (next == Prep)
            {
                return;
            }

            Prep = next;
            OnPrepChanged?.Invoke(this);
        }

        public void SetPendingReduceS2(int amount)
        {
            var next = Mathf.Max(0, amount);
            if (next == PendingReduceS2)
            {
                return;
            }

            PendingReduceS2 = next;
            OnPendingReduceS2Changed?.Invoke(this);
        }

        public float GetOrderScore(int skillSpeedMod = 0)
        {
            return ActionPriority + skillSpeedMod - CurrentDelay * 0.1f;
        }
    }
}
