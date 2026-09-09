using UnityEngine;

namespace FracturedChorus.UI
{
    public static class DamageNumberDigitAtlas
    {
        public const string TitleSheetArtPath =
            "Assets/FracturedChorus/Art/UI/Combat/DamageNumbers/Digits/fc_title_digits_0-9_v1.png";
        public const string TitleDigitArtFolder =
            "Assets/FracturedChorus/Art/UI/Combat/DamageNumbers/Digits";
        public const string TitleSheetResourcePath = "UI/Combat/DamageNumbers/fc_title_digits_0-9_v1";
        public const string LegacyDmgSheetPath = "UI/Combat/DamageNumbers/combat_dmg_digits_holo_v2";
        public const string LegacyHealSheetPath = "UI/Combat/DamageNumbers/combat_heal_digits_holo_v1";
        public const string CritBadgePath = "UI/Combat/DamageNumbers/combat_crit_badge_holo_v1";

        private static readonly int[] TitleSliceX =
            { 0, 244, 330, 551, 765, 968, 1190, 1404, 1596, 1813 };
        private static readonly int[] TitleSliceW =
            { 239, 81, 216, 209, 198, 217, 209, 187, 212, 212 };
        private const int TitleSliceH = 200;

        private static Sprite[] _titleDigits;
        private static Sprite[] _legacyDmgDigits;
        private static Sprite[] _legacyHealDigits;
        private static Sprite _critBadge;
        private static bool _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _titleDigits = null;
            _legacyDmgDigits = null;
            _legacyHealDigits = null;
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
            return HasValidSheet(ResolveSheet(heal));
        }

        public static Sprite GetDigit(int digit, bool heal)
        {
            EnsureLoaded();
            digit = Mathf.Clamp(digit, 0, 9);
            var sheet = ResolveSheet(heal);
            if (sheet == null || digit >= sheet.Length || sheet[digit] == null)
            {
                return null;
            }

            return sheet[digit];
        }

        public static void EnsureLoaded()
        {
            if (_loaded && HasValidSheet(_titleDigits))
            {
                return;
            }

            _loaded = true;
            _titleDigits = LoadTitleDigits();
            _legacyDmgDigits = SliceSheet(LegacyDmgSheetPath);
            _legacyHealDigits = SliceSheet(LegacyHealSheetPath);
            _critBadge = Resources.Load<Sprite>(CritBadgePath);

            if (!HasValidSheet(_titleDigits) && !HasValidSheet(_legacyDmgDigits))
            {
                Debug.LogError("[DamageNumbers] Failed to load title or legacy digit sheets.");
            }
        }

        private static Sprite[] ResolveSheet(bool heal)
        {
            if (HasValidSheet(_titleDigits))
            {
                return _titleDigits;
            }

            return heal ? _legacyHealDigits : _legacyDmgDigits;
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

        private static Sprite[] LoadTitleDigits()
        {
            var fromArtFiles = LoadTitleDigitsFromArtFiles();
            if (HasValidSheet(fromArtFiles))
            {
                return fromArtFiles;
            }

            var fromArtSheet = SliceSheetFromAssetPath(TitleSheetArtPath);
            if (HasValidSheet(fromArtSheet))
            {
                return fromArtSheet;
            }

            var fromResources = SliceSheet(TitleSheetResourcePath);
            if (HasValidSheet(fromResources))
            {
                return fromResources;
            }

            var tex = Resources.Load<Texture2D>(TitleSheetResourcePath);
            return SliceTitleTexture(tex, "fc_title_digits_0-9_v1");
        }

        private static Sprite[] LoadTitleDigitsFromArtFiles()
        {
#if UNITY_EDITOR
            var sorted = new Sprite[10];
            var filled = 0;
            for (var i = 0; i < 10; i++)
            {
                var path = TitleDigitArtFolder + "/fc_title_digit_" + i + ".png";
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                sorted[i] = sprite;
                filled++;
            }

            return filled >= 10 ? sorted : null;
#else
            return null;
#endif
        }

        private static Sprite[] SliceSheetFromAssetPath(string assetPath)
        {
#if UNITY_EDITOR
            var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var sorted = SortLoadedSprites(loaded);
            if (HasValidSheet(sorted))
            {
                return sorted;
            }

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return SliceTitleTexture(tex, "fc_title_digits_0-9_v1");
#else
            return null;
#endif
        }

        private static Sprite[] SliceSheet(string resourcePath)
        {
            var loaded = Resources.LoadAll<Sprite>(resourcePath);
            var sorted = SortLoadedSprites((System.Collections.IEnumerable)loaded);
            if (HasValidSheet(sorted))
            {
                return sorted;
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
                return null;
            }

            if (resourcePath == TitleSheetResourcePath)
            {
                return SliceTitleTexture(tex, tex.name);
            }

            return SliceTextureEqual(tex, 100f, tex.name);
        }

        private static Sprite[] SortLoadedSprites(System.Collections.IEnumerable loaded)
        {
            if (loaded == null)
            {
                return null;
            }

            var sorted = new Sprite[10];
            var filled = 0;
            foreach (var obj in loaded)
            {
                if (obj is not Sprite sprite || string.IsNullOrEmpty(sprite.name))
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

            return filled >= 10 ? sorted : null;
        }

        private static Sprite[] SliceTitleTexture(Texture2D tex, string baseName)
        {
            if (tex == null || tex.width < 10 || tex.height < 10)
            {
                return null;
            }

            var sprites = new Sprite[10];
            for (var i = 0; i < 10; i++)
            {
                var rect = new Rect(TitleSliceX[i], 0f, TitleSliceW[i], TitleSliceH);
                try
                {
                    sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprites[i].name = baseName + "_" + i;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[DamageNumbers] Title Sprite.Create failed for " + baseName + "_" + i + ": " + e.Message);
                    return null;
                }
            }

            return sprites;
        }

        private static Sprite[] SliceTextureEqual(Texture2D tex, float ppu, string baseName)
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
