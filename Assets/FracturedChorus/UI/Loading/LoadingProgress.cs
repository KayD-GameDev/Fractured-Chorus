using UnityEngine;

namespace FracturedChorus.UI.Loading
{
    public static class LoadingProgress
    {
        public const float UnityActivationCap = 0.9f;
        public const float FadeInSec = 0.20f;
        public const float FadeOutSec = 0.25f;
        public const float MinHoldSec = 0.80f;
        public const float SmoothTime = 0.12f;
        public const float ActivateFill = 0.99f;
        public const float PercentVisibleMin = 0.02f;

        public static float MapAsyncProgress(float unityProgress)
        {
            if (unityProgress <= 0f)
            {
                return 0f;
            }

            var mapped = unityProgress / UnityActivationCap;
            return Mathf.Clamp01(mapped);
        }

        public static bool CanActivate(float displayedFill, float holdElapsedSec)
        {
            return displayedFill >= ActivateFill && holdElapsedSec >= MinHoldSec;
        }
    }
}
