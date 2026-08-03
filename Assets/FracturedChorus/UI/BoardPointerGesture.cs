using UnityEngine;

namespace FracturedChorus.UI
{
    public static class BoardPointerGesture
    {
        public static bool ShouldCommitDrag(Vector2 pointerDownScreen, Vector2 currentScreen, float thresholdPx)
        {
            return Vector2.Distance(pointerDownScreen, currentScreen) > thresholdPx;
        }

        public static bool IsClick(Vector2 pointerDownScreen, Vector2 releaseScreen, float thresholdPx)
        {
            return !ShouldCommitDrag(pointerDownScreen, releaseScreen, thresholdPx);
        }
    }
}
