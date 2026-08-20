using UnityEngine;

namespace FracturedChorus.UI
{
    public static class TimelineLayoutLock
    {
        public const float SlotWidth = 73.85f;
        public const float MinSlotWidth = 14f;
        public const float SlotHeight = 64f;
        public const float LaneMarkerSize = 26f;
        public const float ActiveFootprintDotSize = 30f;
        public const float FootprintDotSize = 16f;
        public const float SkillNoteActiveSize = 50.6f;
        public const float SkillNoteStandingSize = 34.32f;
        public const float CodaCharlotteNoteScale = 1.1f;
        public const float ScanBarWidth = 6f;
        public const float ScanBarVerticalInset = -4f;
        public const float TrackLineY = 6f;
        public const float TrackLineHeight = 2f;

        public const float TimelineAnchorMinX = 0.02f;
        public const float TimelineAnchorMinY = 0.02f;
        public const float TimelineAnchorMaxX = 0.98f;
        public const float TimelineAnchorMaxY = 0.22277778f;
        public const float TimelineAnchoredPosY = 69.400024f;
        public const float TimelineSizeDeltaY = 138.8f;

        public static float ClampSlotWidth(float width)
        {
            if (width <= 0.01f)
            {
                return SlotWidth;
            }

            return Mathf.Max(SlotWidth, width);
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
                return Mathf.Max(serializedWidth, SlotWidth);
            }

            return SlotWidth;
        }
    }
}
