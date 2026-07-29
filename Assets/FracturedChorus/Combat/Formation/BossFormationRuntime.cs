using UnityEngine;

namespace FracturedChorus.Combat.Formation
{
    public static class BossFormationRuntime
    {
        private static BossFormationProfileSO s_active;
        private static float s_baseFrontTargetWeight = 1f;
        private static float s_baseBackPierceChance;

        public static BossFormationProfileSO Active => s_active;

        public static void Initialize(BossFormationProfileSO profile)
        {
            s_active = profile;
            if (s_active == null)
            {
                s_baseFrontTargetWeight = 1f;
                s_baseBackPierceChance = 0f;
                return;
            }

            s_baseFrontTargetWeight = s_active.frontTargetWeight;
            s_baseBackPierceChance = s_active.backPierceChance;
        }

        public static void ApplyDifficultyScale(float pierceFrontBiasMult)
        {
            if (s_active == null)
            {
                return;
            }

            var mult = Mathf.Max(0.01f, pierceFrontBiasMult);
            s_active.frontTargetWeight = s_baseFrontTargetWeight * mult;
            s_active.backPierceChance = Mathf.Clamp01(s_baseBackPierceChance * mult);
        }

        public static void Clear()
        {
            s_active = null;
            s_baseFrontTargetWeight = 1f;
            s_baseBackPierceChance = 0f;
        }
    }
}
