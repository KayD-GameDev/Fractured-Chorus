using FracturedChorus.Combat.Damage;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>Màu khung / badge hệ (Nhịp · Giai điệu · Hòa âm) cho UI party card.</summary>
    public static class HarmonyElementPalette
    {
        private static Sprite _rhythmIcon;
        private static Sprite _melodyIcon;
        private static Sprite _harmonyIcon;

        public static Color GetBorderColor(HarmonyElement element)
        {
            return element switch
            {
                HarmonyElement.Rhythm => new Color(0.92f, 0.28f, 0.22f, 1f),
                HarmonyElement.Melody => new Color(0.58f, 0.28f, 0.88f, 1f),
                HarmonyElement.Harmony => new Color(0.95f, 0.82f, 0.18f, 1f),
                _ => new Color(0.55f, 0.55f, 0.6f, 1f)
            };
        }

        public static Color GetBadgeRingColor(HarmonyElement element)
        {
            return GetBorderColor(element);
        }

        public static Color GetBadgeFill(HarmonyElement element)
        {
            var baseColor = GetBorderColor(element);
            return Color.Lerp(baseColor, Color.white, 0.15f);
        }

        public static Sprite ResolveElementIcon(HarmonyElement element, UnitStatBlockSO statBlock)
        {
            if (statBlock?.elementBadgeIcon != null)
            {
                return statBlock.elementBadgeIcon;
            }

            return GetDefaultElementIcon(element);
        }

        public static Sprite GetDefaultElementIcon(HarmonyElement element)
        {
            return element switch
            {
                HarmonyElement.Rhythm => _rhythmIcon ??= CreateColoredDisc(new Color(0.95f, 0.35f, 0.28f)),
                HarmonyElement.Melody => _melodyIcon ??= CreateColoredDisc(new Color(0.65f, 0.35f, 0.95f)),
                HarmonyElement.Harmony => _harmonyIcon ??= CreateColoredDisc(new Color(0.98f, 0.88f, 0.25f)),
                _ => null
            };
        }

        private static Sprite CreateColoredDisc(Color fill)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, dist <= radius ? fill : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
