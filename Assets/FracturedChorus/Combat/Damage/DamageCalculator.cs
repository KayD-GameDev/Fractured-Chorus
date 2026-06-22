using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Damage
{
    public struct DamageResult
    {
        public float RawDamage;
        public float FinalDamage;
        public bool IsCritical;
    }

    public static class DamageCalculator
    {
        private const float StrengthConstant = 10f;
        private const float EnduranceConstant = 4f;

        public static DamageResult Calculate(
            UnitStats attacker,
            UnitStats defender,
            int skillTier,
            BeatTiming beatTiming = BeatTiming.OnBeat,
            HarmonyRelation harmony = HarmonyRelation.Neutral,
            float coverModifier = 1f,
            float exposedModifier = 1f,
            float buffModifier = 1f)
        {
            var randomMultiplier = RollSkillRandom(skillTier);
            var rawDamage = randomMultiplier * attacker.Strength * StrengthConstant;

            var enduranceFactor = 100f / (100f + EnduranceConstant * defender.Endurance);
            var beatCondition = beatTiming.GetMultiplier();
            var preCondition = harmony.GetPreCondition();

            var finalDamage = rawDamage * enduranceFactor * beatCondition * preCondition
                              * coverModifier * exposedModifier * buffModifier;

            var isCritical = RollCritical(attacker.BaseLuck);
            if (isCritical)
            {
                finalDamage *= attacker.CritMultiplier;
            }

            return new DamageResult
            {
                RawDamage = rawDamage,
                FinalDamage = Mathf.Max(1f, finalDamage),
                IsCritical = isCritical
            };
        }

        public static float RollSkillRandom(int skillTier)
        {
            return skillTier switch
            {
                1 => Random.Range(0.80f, 1.05f),
                2 => Random.Range(0.9f, 1.1f),
                3 => Random.Range(1.1f, 1.5f),
                _ => Random.Range(0.9f, 1.1f)
            };
        }

        private static bool RollCritical(float baseLuckPercent)
        {
            return Random.Range(0f, 100f) < baseLuckPercent;
        }
    }
}
