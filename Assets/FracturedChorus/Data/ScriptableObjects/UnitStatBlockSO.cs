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
        [Tooltip("Element badge icon on party card UI — if empty, uses the default icon for the element.")]
        public Sprite elementBadgeIcon;

        [Header("Attack channels")]
        [Tooltip("Physical skills use Strength.")]
        public float strength = 100f;
        [Tooltip("Magical skills use Magic.")]
        public float magic = 10f;

        [Header("Other core stats")]
        public float endurance = 10f;
        public int heartBeat = 160;
        [Tooltip("Design name: Base Luck — mechanic: crit % per skill damage roll (0–100).")]
        public float baseLuck = 15f;
        [Tooltip("Applied on crit only. No crit = ×1. Enter 1.2 or 120 (=120% damage).")]
        public float critMultiplier = 1.2f;
        public int maxHp = 80;
        public int baseSpeed = 12;

        public UnitStats ToRuntimeStats()
        {
            return new UnitStats
            {
                Element = element,
                Strength = strength,
                Magic = magic,
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
