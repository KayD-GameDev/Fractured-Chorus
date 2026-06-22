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
        public SkillDefinitionSO[] Skills { get; }
        public Color PlaceholderColor { get; }
        public int CurrentHp { get; private set; }
        public int CurrentDelay { get; set; }

        /// <summary>Read-only mirror of BaseAv for UI — not spent when using skills.</summary>
        public float CurrentAv => Stats.BaseAv;

        public float ActionPriority => Stats.ActionPriority;

        public bool IsAlive => CurrentHp > 0;

        public event Action<CombatUnit> OnHpChanged;
        public event Action<CombatUnit> OnDied;

        public CombatUnit(UnitPresetSO preset, GridSide side)
        {
            UnitId = string.IsNullOrEmpty(preset.unitId) ? Guid.NewGuid().ToString("N") : preset.unitId;
            DisplayName = preset.displayName;
            Role = preset.role;
            Side = side;
            Stats = preset.stats?.Clone() ?? new UnitStats();
            Skills = preset.skills ?? Array.Empty<SkillDefinitionSO>();
            PlaceholderColor = preset.placeholderColor;
            CurrentHp = Stats.MaxHp;
        }

        public void SetGridPosition(GridPosition position)
        {
            GridPosition = position;
            Side = position.Side;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHp = Mathf.Max(0, CurrentHp - Mathf.RoundToInt(amount));
            OnHpChanged?.Invoke(this);

            if (!IsAlive)
            {
                OnDied?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHp = Mathf.Min(Stats.MaxHp, CurrentHp + Mathf.RoundToInt(amount));
            OnHpChanged?.Invoke(this);
        }

        public float GetOrderScore(int skillSpeedMod = 0)
        {
            return ActionPriority + skillSpeedMod - CurrentDelay * 0.1f;
        }
    }
}
