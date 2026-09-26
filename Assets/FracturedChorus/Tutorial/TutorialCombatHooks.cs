using System;
using FracturedChorus.Combat.Qte;
using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public static class TutorialCombatHooks
    {
        public static event Action SkillPanelOpened;
        public static event Action SkillPlacedOnTimeline;
        public static event Action<CombatQteGrade> QteResolved;

        public static bool ForceNextCounterQte { get; set; }

        private static float? _forcedQteChance;

        public static void NotifySkillPanelOpened()
        {
            SkillPanelOpened?.Invoke();
        }

        public static void NotifySkillPlacedOnTimeline()
        {
            SkillPlacedOnTimeline?.Invoke();
        }

        public static void NotifyQteResolved(CombatQteGrade grade)
        {
            QteResolved?.Invoke(grade);
        }

        public static void SetForcedQteChance(float chance)
        {
            _forcedQteChance = Mathf.Clamp01(chance);
        }

        public static bool TryTakeForcedQteChance(out float chance)
        {
            if (!_forcedQteChance.HasValue)
            {
                chance = 0f;
                return false;
            }

            chance = _forcedQteChance.Value;
            _forcedQteChance = null;
            return true;
        }

        public static void ResetCadenceIntroOverrides()
        {
            ForceNextCounterQte = false;
            _forcedQteChance = null;
        }
    }
}
