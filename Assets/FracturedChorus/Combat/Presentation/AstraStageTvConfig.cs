using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "AstraStageTvConfig",
        menuName = "Fractured Chorus/Luxe Arena/Astra Stage TV Config")]
    public sealed class AstraStageTvConfig : ScriptableObject
    {
        public const string ResourcePath = "UI/Combat/Boss/Astra/AstraStageTvConfig";
        public const string SpriteResourceRoot = "UI/Combat/Boss/Astra/StageTv/";

        [Header("Art — separate sprites, never atlased")]
        public Sprite FrameSprite;
        public Sprite[] FaceSprites = new Sprite[AstraStageTvSequence.FaceCount];

        [Header("Layout fallback — scene RectTransform wins when authored")]
        [Tooltip("Normalized anchor of the purple square (bottom-left origin). Seeds a rect nobody authored yet.")]
        public Vector2 RestNormalizedPos = new Vector2(0.5f, 0.74f);
        [Tooltip("Rest size for an unauthored rect. Tune the scene rect instead once the TV is placed.")]
        public Vector2 RestSizePx = new Vector2(210f, 210f);
        [Tooltip("Screen hole inset, used only when the Screen child has no authored rect.")]
        [Range(0.04f, 0.25f)] public float ScreenInsetNormalized = 0.12f;

        [Header("Timing")]
        public float DropDurationSec = 1f;
        public float SpinDurationSec = 2.2f;
        public float SpinSpeedFacesPerSec = 6f;
        public float DecelDurationSec = 0.45f;

        public static AstraStageTvConfig Load()
        {
            var config = Resources.Load<AstraStageTvConfig>(ResourcePath);
            if (config != null)
            {
                config.EnsureSpritesLoaded();
            }

            return config;
        }

        public void EnsureSpritesLoaded()
        {
            if (FrameSprite == null)
            {
                FrameSprite = Resources.Load<Sprite>(SpriteResourceRoot + "astra_stage_tv_frame_v1");
            }

            if (FaceSprites == null || FaceSprites.Length != AstraStageTvSequence.FaceCount)
            {
                FaceSprites = new Sprite[AstraStageTvSequence.FaceCount];
            }

            TryLoadFace(0, "astra_tv_face_joy_v1");
            TryLoadFace(1, "astra_tv_face_anger_v1");
            TryLoadFace(2, "astra_tv_face_love_v1");
            TryLoadFace(3, "astra_tv_face_hate_v1");
            TryLoadFace(4, "astra_tv_face_sorrow_v1");
        }

        public Sprite[] ResolveFaces()
        {
            EnsureSpritesLoaded();
            return FaceSprites;
        }

        private void TryLoadFace(int index, string fileName)
        {
            if (index < 0 || index >= FaceSprites.Length)
            {
                return;
            }

            if (FaceSprites[index] == null)
            {
                FaceSprites[index] = Resources.Load<Sprite>(SpriteResourceRoot + fileName);
            }
        }
    }
}
