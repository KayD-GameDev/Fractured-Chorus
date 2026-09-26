using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public static class TutorialQtePolicy
    {
        public static bool IsActive { get; private set; }

        private static bool _introShown;

        public static void Begin()
        {
            IsActive = true;
            _introShown = false;
        }

        public static void End()
        {
            IsActive = false;
            _introShown = false;
        }

        public static bool TryDecide(int phaseIndex, float chance, out bool run)
        {
            run = false;
            if (!IsActive)
            {
                return false;
            }

            if (phaseIndex <= 0)
            {
                return true;
            }

            if (!_introShown)
            {
                run = true;
                return true;
            }

            run = Random.value <= Mathf.Clamp01(chance);
            return true;
        }

        public static void NotifyIntroShown()
        {
            if (IsActive)
            {
                _introShown = true;
            }
        }
    }
}
