using System;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "LuxeArenaBackgroundConfig",
        menuName = "Fractured Chorus/Luxe Arena/Background Config")]
    public sealed class LuxeArenaBackgroundConfig : ScriptableObject
    {
        [Serializable]
        public sealed class SpotlightSeed
        {
            [Range(0f, 1f)] public float AnchorX = 0.5f;
            [Range(0f, 1f)] public float AnchorY = 0.95f;
            public float Angle;
            public float Scale = 1f;
            public Vector2 Size = new(220f, 520f);
            public Color Color = new(0.72f, 0.45f, 1f, 0.18f);
        }

        [Header("Plates")]
        public Sprite BasePlate;
        public Sprite Floor;
        public Sprite Grandstand;
        public Sprite TvFrame;
        public Sprite EmotionScreen;
        public Sprite SoftCone;

        [Header("Audience FX")]
        public Texture2D[] AudienceFrames;

        [Header("Layer Placement")]
        public Vector2 FloorAnchorMin = new(0f, 0f);
        public Vector2 FloorAnchorMax = new(1f, 1f);
        public Vector2 GrandstandAnchorMin = new(0f, 0f);
        public Vector2 GrandstandAnchorMax = new(1f, 1f);
        public Vector2 AudienceAnchorMin = new(0f, 0.52f);
        public Vector2 AudienceAnchorMax = new(1f, 0.88f);
        public Vector2 TvAnchorMin = new(0.04f, 0.735f);
        public Vector2 TvAnchorMax = new(0.96f, 0.995f);
        public Vector2 TvContentInsetMin = new(0.117f, 0.063f);
        public Vector2 TvContentInsetMax = new(0.883f, 0.647f);
        public Rect TvContentUvRect = new(0.079f, 0.079f, 0.842f, 0.842f);

        [Header("Spotlights")]
        public SpotlightSeed[] Spotlights =
        {
            new() { AnchorX = 0.12f, AnchorY = 0.93f, Angle = -18f, Scale = 0.95f },
            new() { AnchorX = 0.22f, AnchorY = 0.95f, Angle = -10f, Scale = 1.05f },
            new() { AnchorX = 0.34f, AnchorY = 0.96f, Angle = -4f, Scale = 0.9f },
            new() { AnchorX = 0.66f, AnchorY = 0.96f, Angle = 4f, Scale = 0.9f },
            new() { AnchorX = 0.78f, AnchorY = 0.95f, Angle = 10f, Scale = 1.05f },
            new() { AnchorX = 0.88f, AnchorY = 0.93f, Angle = 18f, Scale = 0.95f },
        };

        [Header("Audience Motion")]
        [Range(0.1f, 1f)] public float AudienceAlpha = 0.7f;
        [Range(0.2f, 8f)] public float FrameMorphSpeed = 1.1f;
        [Range(0f, 0.03f)] public float ShimmerAmount = 0.006f;
        [Range(0f, 0.4f)] public float PulseAmount = 0.12f;
        [Range(0.05f, 2f)] public float PulseSpeed = 0.4f;

        [Header("Spotlight Motion")]
        public bool EnableSpotlightRig = true;
        [Range(0.05f, 0.6f)] public float SpotlightMaxAlpha = 0.28f;
        [Range(0.05f, 2f)] public float SpotlightPulseSpeed = 0.35f;
        [Range(0f, 12f)] public float SpotlightSwayDegrees = 4.5f;
        [Range(0.05f, 1f)] public float SpotlightSwaySpeed = 0.22f;
    }
}
