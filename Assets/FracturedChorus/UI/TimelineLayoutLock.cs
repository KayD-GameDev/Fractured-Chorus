using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Canonical timeline beat layout locked from CombatTutorial.unity (Beat_0 / BeatTimelineUI).
    /// Runtime and rebuild menus must not shrink below these values.
    /// </summary>
    public static class TimelineLayoutLock
    {
        public const float SlotWidth = 73.85f;
        public const float MinSlotWidth = 14f;
        public const float SlotHeight = 64f;
        public const float LaneMarkerSize = 26f;
        public const float ActiveFootprintDotSize = 30f;
        public const float FootprintDotSize = 16f;
        public const float ScanBarWidth = 6f;
        public const float ScanBarVerticalInset = -4f;
        public const float TrackLineY = 6f;
        public const float TrackLineHeight = 2f;
        public const float BossTrackFrameHeight = 56f;

        public static float ClampSlotWidth(float width)
        {
            return Mathf.Max(MinSlotWidth, width > 0.01f ? width : SlotWidth);
        }

        public static float ResolveSlotWidth(float templateWidth, float serializedWidth, bool preserveScene)
        {
            if (preserveScene)
            {
                var sceneWidth = templateWidth > MinSlotWidth ? templateWidth : SlotWidth;
                return Mathf.Max(sceneWidth, SlotWidth);
            }

            if (serializedWidth > MinSlotWidth)
            {
                return serializedWidth;
            }

            return SlotWidth;
        }
    }
}
