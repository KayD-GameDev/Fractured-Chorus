using System;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "LuxeArenaAudienceLayout",
        menuName = "Fractured Chorus/Luxe Arena/Audience Layout")]
    public sealed class LuxeArenaAudienceLayout : ScriptableObject
    {
        [Serializable]
        public sealed class PanelLayout
        {
            public Vector2 AnchorMin = new(0f, 0f);
            public Vector2 AnchorMax = new(1f, 1f);
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Rect UvRect = new(0f, 0.55f, 1f, 0.3f);
        }

        public Vector2 BandAnchorMin = new(0f, 0.56f);
        public Vector2 BandAnchorMax = new(1f, 0.82f);
        public Vector2 BandAnchoredPosition;
        public Vector2 BandSizeDelta;

        public PanelLayout Left = new()
        {
            AnchorMin = new Vector2(0f, 0f),
            AnchorMax = new Vector2(0.38f, 1f),
            AnchoredPosition = new Vector2(0f, 52.1f),
            SizeDelta = new Vector2(0f, 65.61f),
            UvRect = new Rect(0f, 0.55f, 0.38f, 0.3f)
        };

        public PanelLayout Center = new()
        {
            AnchorMin = new Vector2(0.38f, 0f),
            AnchorMax = new Vector2(0.62f, 1f),
            AnchoredPosition = new Vector2(0f, 8.68f),
            SizeDelta = new Vector2(0f, -125.43f),
            UvRect = new Rect(0.38f, 0.55f, 0.24f, 0.3f)
        };

        public PanelLayout Right = new()
        {
            AnchorMin = new Vector2(0.62f, 0f),
            AnchorMax = new Vector2(1f, 1f),
            AnchoredPosition = new Vector2(0f, 52.1f),
            SizeDelta = new Vector2(0f, 65.61f),
            UvRect = new Rect(0.62f, 0.55f, 0.38f, 0.3f)
        };
    }
}
