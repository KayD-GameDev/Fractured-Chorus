using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>Sprites UI procedural — Unity 6+ không dùng built-in Knob.psd.</summary>
    public static class UiCircleSpriteUtil
    {
        private static Sprite _circleSprite;
        private static Sprite _whiteSprite;
        private static Sprite _capsuleSprite;

        public static Sprite Circle => _circleSprite ??= CreateCircleSprite(64);

        public static Sprite White => _whiteSprite ??= CreateSolidSprite(4);

        public static Sprite Capsule => _capsuleSprite ??= CreateCapsuleSprite(64);

        private static Sprite CreateSolidSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = new Color(1f, 1f, 1f, 1f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, fill);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = dist <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCapsuleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = dist <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            var border = Mathf.Floor(size * 0.5f) - 1f;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }
    }
}
