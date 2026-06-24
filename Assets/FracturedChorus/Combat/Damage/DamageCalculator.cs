using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Damage
{
    public struct DamageResult
    {
        public float SkillRandomRoll;
        public float RawDamage;
        public float EnduranceFactor;
        public float FinalDamage;
        public bool IsCritical;
        /// <summary>1 khi không crit · CritMultiplier khi crit.</summary>
        public float CritDamageMultiplier;
    }

    public static class DamageCalculator
    {
        /// <summary>raw = random(tier) × strength × StrengthDamageConstant (A = 10).</summary>
        public const float StrengthDamageConstant = 10f;
        public const float EnduranceConstant = 4f;

        /// <summary>100 / (100 × C × √EN) — C = 4.</summary>
        public static float GetEnduranceFactor(float defenderEndurance)
        {
            var endurance = Mathf.Max(defenderEndurance, 1f);
            var denominator = 100f * EnduranceConstant * Mathf.Sqrt(endurance);
            return 100f / denominator;
        }

        public static DamageResult Calculate(
            UnitStats attacker,
            UnitStats defender,
            int skillTier,
            DamageType damageType = DamageType.Physical,
            BeatTiming beatTiming = BeatTiming.OnBeat,
            HarmonyRelation harmony = HarmonyRelation.Neutral,
            float coverModifier = 1f,
            float exposedModifier = 1f,
            float buffModifier = 1f)
        {
            var randomMultiplier = RollSkillRandom(skillTier);
            var attackPower = attacker.AttackPower;
            var rawDamage = randomMultiplier * attackPower * StrengthDamageConstant;

            var enduranceFactor = GetEnduranceFactor(defender.Endurance);
            var beatCondition = beatTiming.GetMultiplier();
            var preCondition = harmony.GetPreCondition();

            var damageBeforeCrit = rawDamage * enduranceFactor * beatCondition * preCondition
                                   * coverModifier * exposedModifier * buffModifier;

            var isCritical = attacker.RollCriticalHit();
            var critDamageMultiplier = attacker.ResolveCritDamageMultiplier(isCritical);
            var finalDamage = damageBeforeCrit * critDamageMultiplier;

            return new DamageResult
            {
                SkillRandomRoll = randomMultiplier,
                RawDamage = rawDamage,
                EnduranceFactor = enduranceFactor,
                FinalDamage = Mathf.Max(1f, finalDamage),
                IsCritical = isCritical,
                CritDamageMultiplier = critDamageMultiplier
            };
        }

        /// <summary>Basic 0.80–1.05 · Skill 0.90–1.10 · Ultimate 1.10–1.50</summary>
        public static float RollSkillRandom(int skillTier)
        {
            return skillTier switch
            {
                1 => Random.Range(0.80f, 1.05f),
                2 => Random.Range(0.90f, 1.10f),
                3 => Random.Range(1.10f, 1.50f),
                _ => Random.Range(0.90f, 1.10f)
            };
        }
    }
}
