using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public static class UiSpriteUtil
    {
        private static Sprite _white;

        public static Sprite White()
        {
            if (_white != null)
            {
                return _white;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }

        public static void EnsureSprite(Image image)
        {
            if (image != null && image.sprite == null)
            {
                image.sprite = White();
            }
        }
    }
}
