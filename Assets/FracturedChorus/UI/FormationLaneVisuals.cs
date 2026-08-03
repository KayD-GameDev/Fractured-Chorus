using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.UI
{
    public static class FormationLaneVisuals
    {
        public const int MidColumnIndex = 1;

        private const string ResourcesFolder = "UI/Combat/Formation";
        private const string ArtFolder = "Assets/FracturedChorus/Art/UI/Combat/Formation";

        public static readonly Color Front = new Color(0.22f, 0.48f, 1f, 0.42f);
        public static readonly Color Mid = new Color(0.95f, 0.22f, 0.24f, 0.42f);
        public static readonly Color Back = new Color(0.22f, 0.78f, 0.38f, 0.42f);

        public static readonly Color FrontBadge = new Color(0.35f, 0.65f, 1f, 1f);
        public static readonly Color MidBadge = new Color(1f, 0.35f, 0.38f, 1f);
        public static readonly Color BackBadge = new Color(0.35f, 0.9f, 0.5f, 1f);

        public static Color FloorColor(int column)
        {
            if (column <= PositionalModifiers.FrontColumnIndex)
            {
                return Front;
            }

            if (column >= PositionalModifiers.BackColumnIndex)
            {
                return Back;
            }

            return Mid;
        }

        public static Color BadgeColor(int column)
        {
            if (column <= PositionalModifiers.FrontColumnIndex)
            {
                return FrontBadge;
            }

            if (column >= PositionalModifiers.BackColumnIndex)
            {
                return BackBadge;
            }

            return MidBadge;
        }

        public static Sprite LoadLaneIcon(int column)
        {
            var file = column <= PositionalModifiers.FrontColumnIndex
                ? "formation_icon_front_shield_v1"
                : column >= PositionalModifiers.BackColumnIndex
                    ? "formation_icon_back_flame_v1"
                    : "formation_icon_mid_sword_v1";

            var fromResources = Resources.Load<Sprite>($"{ResourcesFolder}/{file}");
            if (fromResources != null)
            {
                return fromResources;
            }

#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{file}.png");
            if (sprite != null)
            {
                return sprite;
            }

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/{file}.png");
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
#endif
            return null;
        }
    }
}
