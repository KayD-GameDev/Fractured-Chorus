using System;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Units
{
    [Serializable]
    public class UnitStats
    {
        public const float AvConstant = 12000f;

        public HarmonyElement Element = HarmonyElement.Melody;
        public DamageType StrengthType = DamageType.Physical;
        public float Strength = 100f;
        public float Endurance = 10f;
        public int HeartBeat = 160;
        public float BaseLuck = 15f;
        public float CritMultiplier = 1.2f;
        public int MaxHp = 80;
        public int BaseSpeed = 12;

        /// <summary>Base Luck = % cơ hội crit mỗi lần skill gây sát thương (cap 100).</summary>
        public float CritChancePercent => Mathf.Clamp(BaseLuck, 0f, 100f);

        public float ResolveCritDamageMultiplier(bool isCritical)
        {
            if (!isCritical)
            {
                return 1f;
            }

            return ResolveCritMultiplierValue();
        }

        /// <summary>1.2 = 120% dmg · giá trị &gt; 10 được hiểu là % (120 → ×1.2).</summary>
        public float ResolveCritMultiplierValue()
        {
            if (CritMultiplier <= 0f)
            {
                return 1f;
            }

            return CritMultiplier > 10f ? CritMultiplier / 100f : CritMultiplier;
        }

        public bool RollCriticalHit()
        {
            return UnityEngine.Random.value * 100f < CritChancePercent;
        }

        /// <summary>Action priority on same beat — lower value acts first. Not spent as resource.</summary>
        public float BaseAv => HeartBeat > 0 ? AvConstant / HeartBeat : 0f;

        public float ActionPriority => BaseAv;

        public float AttackPower => Strength;

        public static UnitStats FromBlock(UnitStatBlockSO block)
        {
            return block != null ? block.ToRuntimeStats() : new UnitStats();
        }

        public static UnitStats CreateRenPreset()
        {
            return new UnitStats
            {
                Element = HarmonyElement.Melody,
                StrengthType = DamageType.Physical,
                Strength = 100f,
                Endurance = 10f,
                HeartBeat = 160,
                BaseLuck = 15f,
                CritMultiplier = 1.2f,
                MaxHp = 80,
                BaseSpeed = 12
            };
        }

        public static UnitStats CreateTankPreset()
        {
            return new UnitStats
            {
                Element = HarmonyElement.Rhythm,
                StrengthType = DamageType.Physical,
                Strength = 80f,
                Endurance = 15f,
                HeartBeat = 140,
                BaseLuck = 10f,
                CritMultiplier = 1.1f,
                MaxHp = 120,
                BaseSpeed = 8
            };
        }

        public static UnitStats CreateMagePreset()
        {
            return new UnitStats
            {
                Element = HarmonyElement.Harmony,
                StrengthType = DamageType.Magical,
                Strength = 100f,
                Endurance = 8f,
                HeartBeat = 150,
                BaseLuck = 12f,
                CritMultiplier = 1.25f,
                MaxHp = 70,
                BaseSpeed = 10
            };
        }

        public static UnitStats CreateGruntPreset()
        {
            return new UnitStats
            {
                Element = HarmonyElement.Rhythm,
                StrengthType = DamageType.Physical,
                Strength = 60f,
                Endurance = 8f,
                HeartBeat = 120,
                BaseLuck = 5f,
                CritMultiplier = 1.1f,
                MaxHp = 150,
                BaseSpeed = 9
            };
        }

        public UnitStats Clone()
        {
            return (UnitStats)MemberwiseClone();
        }
    }
}
