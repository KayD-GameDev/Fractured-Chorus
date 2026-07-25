using UnityEngine;

namespace FracturedChorus.UI
{
    public static class DamageNumberDigitAtlas
    {
        private const string DmgSheetPath = "UI/Combat/DamageNumbers/combat_dmg_digits_holo_v2";
        private const string HealSheetPath = "UI/Combat/DamageNumbers/combat_heal_digits_holo_v1";
        private const string CritBadgePath = "UI/Combat/DamageNumbers/combat_crit_badge_holo_v1";

        private static Sprite[] _dmgDigits;
        private static Sprite[] _healDigits;
        private static Sprite _critBadge;
        private static bool _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _dmgDigits = null;
            _healDigits = null;
            _critBadge = null;
            _loaded = false;
        }

        public static void ForceReload()
        {
            ResetStatics();
            EnsureLoaded();
        }

        public static Sprite CritBadge
        {
            get
            {
                EnsureLoaded();
                return _critBadge;
            }
        }

        public static bool HasDigits(bool heal)
        {
            EnsureLoaded();
            var sheet = heal ? _healDigits : _dmgDigits;
            return sheet != null && sheet.Length > 0 && sheet[0] != null;
        }

        public static Sprite GetDigit(int digit, bool heal)
        {
            EnsureLoaded();
            digit = Mathf.Clamp(digit, 0, 9);
            var sheet = heal ? _healDigits : _dmgDigits;
            if (sheet == null || digit >= sheet.Length || sheet[digit] == null)
            {
                return null;
            }

            return sheet[digit];
        }

        public static void EnsureLoaded()
        {
            if (_loaded && HasValidSheet(_dmgDigits))
            {
                return;
            }

            _loaded = true;
            _dmgDigits = SliceSheet(DmgSheetPath);
            _healDigits = SliceSheet(HealSheetPath);
            _critBadge = Resources.Load<Sprite>(CritBadgePath);

            if (!HasValidSheet(_dmgDigits))
            {
                Debug.LogError("[DamageNumbers] Failed to load damage digit sheet: " + DmgSheetPath);
            }
        }

        private static bool HasValidSheet(Sprite[] sheet)
        {
            if (sheet == null || sheet.Length < 10)
            {
                return false;
            }

            for (var i = 0; i < 10; i++)
            {
                if (sheet[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Sprite[] SliceSheet(string resourcePath)
        {
            var loaded = Resources.LoadAll<Sprite>(resourcePath);
            if (loaded != null && loaded.Length >= 10)
            {
                var sorted = new Sprite[10];
                var filled = 0;
                foreach (var sprite in loaded)
                {
                    if (sprite == null || string.IsNullOrEmpty(sprite.name))
                    {
                        continue;
                    }

                    var underscore = sprite.name.LastIndexOf('_');
                    if (underscore < 0)
                    {
                        continue;
                    }

                    if (!int.TryParse(sprite.name.Substring(underscore + 1), out var index) ||
                        index < 0 || index > 9 || sorted[index] != null)
                    {
                        continue;
                    }

                    sorted[index] = sprite;
                    filled++;
                }

                if (filled >= 10)
                {
                    return sorted;
                }
            }

            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                var full = Resources.Load<Sprite>(resourcePath);
                if (full != null)
                {
                    tex = full.texture;
                }
            }

            if (tex == null)
            {
                Debug.LogWarning("[DamageNumbers] Missing sheet: " + resourcePath);
                return null;
            }

            return SliceTexture(tex, 100f, tex.name);
        }

        private static Sprite[] SliceTexture(Texture2D tex, float ppu, string baseName)
        {
            if (tex == null || tex.width < 10 || tex.height < 10)
            {
                return null;
            }

            var sprites = new Sprite[10];
            for (var i = 0; i < 10; i++)
            {
                var x0 = Mathf.RoundToInt(i * (tex.width / 10f));
                var x1 = Mathf.RoundToInt((i + 1) * (tex.width / 10f));
                var width = Mathf.Max(1, x1 - x0);
                var rect = new Rect(x0, 0f, width, tex.height);
                try
                {
                    sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
                    sprites[i].name = baseName + "_" + i;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[DamageNumbers] Sprite.Create failed for " + baseName + "_" + i + ": " + e.Message);
                    return null;
                }
            }

            return sprites;
        }
    }
}
