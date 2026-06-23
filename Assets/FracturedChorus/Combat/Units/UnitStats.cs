using System;
using UnityEngine;

namespace FracturedChorus.Combat.Units
{
    [Serializable]
    public class UnitStats
    {
        public const float AvConstant = 12000f;

        public float Strength = 100f;
        public float Endurance = 10f;
        public int HeartBeat = 160;
        public float BaseLuck = 15f;
        public float CritMultiplier = 1.2f;
        public int MaxHp = 80;
        public int BaseSpeed = 12;

        /// <summary>Action priority on same beat — lower value acts first (Ren ≈ 75). Not spent as resource.</summary>
        public float BaseAv => HeartBeat > 0 ? AvConstant / HeartBeat : 0f;

        public float ActionPriority => BaseAv;

        public static UnitStats CreateRenPreset()
        {
            return new UnitStats
            {
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
                Strength = 80f,
                Endurance = 15f,
                HeartBeat = 140,
                BaseLuck = 10f,
                CritMultiplier = 1.1f,
                MaxHp = 5000,
                BaseSpeed = 8
            };
        }

        public static UnitStats CreateMagePreset()
        {
            return new UnitStats
            {
                Strength = 90f,
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
