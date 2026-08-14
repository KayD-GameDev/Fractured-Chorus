using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public static class CombatBackgroundPool
    {
        private static readonly string[] ResourcePaths =
        {
            "Backgrounds/CombatPool/combat_bg_01_music_stage",
            "Backgrounds/CombatPool/combat_bg_02_neon_hall",
            "Backgrounds/CombatPool/combat_bg_03_industrial",
            "Backgrounds/CombatPool/combat_bg_04_luxe_bridge"
        };

        public static int Count => ResourcePaths.Length;

        public static Sprite LoadSprite(int index)
        {
            if (ResourcePaths.Length == 0)
            {
                return null;
            }

            var clamped = Mathf.Clamp(index, 0, ResourcePaths.Length - 1);
            var path = ResourcePaths[clamped];

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"[CombatBackgroundPool] Missing background at Resources/{path}");
                return null;
            }

            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
