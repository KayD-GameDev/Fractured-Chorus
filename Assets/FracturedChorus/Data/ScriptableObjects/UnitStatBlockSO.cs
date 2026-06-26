using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Data
{
    /// <summary>
    /// Chỉ số gốc — chỉnh tay trong Inspector, nhiều Unit Preset có thể dùng chung hoặc kế thừa bản copy.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitStatBlock", menuName = "Fractured Chorus/Unit Stat Block")]
    public class UnitStatBlockSO : ScriptableObject
    {
        [Header("Identity")]
        public string blockId;

        [Header("Pre-condition (element)")]
        public HarmonyElement element = HarmonyElement.Melody;
        [Tooltip("Icon hệ trên thẻ party UI — nếu trống dùng icon mặc định theo element.")]
        public Sprite elementBadgeIcon;

        [Header("Strength — chọn Physical/Magical, rồi nhập chỉ số")]
        public DamageType strengthType = DamageType.Physical;
        public float strength = 100f;

        [Header("Other core stats")]
        public float endurance = 10f;
        public int heartBeat = 160;
        [Tooltip("Tên thiết kế: Base Luck — cơ chế: % crit mỗi lần skill gây dmg (0–100).")]
        public float baseLuck = 15f;
        [Tooltip("Chỉ áp khi crit. Không crit = ×1. Nhập 1.2 hoặc 120 (=120% dmg).")]
        public float critMultiplier = 1.2f;
        public int maxHp = 80;
        public int baseSpeed = 12;

        public UnitStats ToRuntimeStats()
        {
            return new UnitStats
            {
                Element = element,
                StrengthType = strengthType,
                Strength = strength,
                Endurance = endurance,
                HeartBeat = heartBeat,
                BaseLuck = baseLuck,
                CritMultiplier = critMultiplier,
                MaxHp = maxHp,
                BaseSpeed = baseSpeed
            };
        }
    }
}
