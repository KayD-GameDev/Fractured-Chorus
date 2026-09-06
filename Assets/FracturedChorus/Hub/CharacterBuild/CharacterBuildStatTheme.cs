using UnityEngine;

namespace FracturedChorus.Hub.CharacterBuild
{
    public static class CharacterBuildStatTheme
    {
        public static readonly Color IconTint = new Color(0.227f, 0.259f, 0.400f, 1f);
        public static readonly Color IconImageColor = Color.white;
        public static readonly Color LabelColor = IconTint;
        public static readonly Color SubLabelColor = new Color(0.541f, 0.541f, 0.620f, 1f);
        public static readonly Color AccentColor = new Color(0f, 0.651f, 0.820f, 1f);
        public static readonly Color HandleTint = Color.white;

        public readonly struct RowMeta
        {
            public RowMeta(string title, string subtitle)
            {
                Title = title;
                Subtitle = subtitle;
            }

            public string Title { get; }
            public string Subtitle { get; }
        }

        public static RowMeta ForKind(CharacterBuildStatKind kind)
        {
            switch (kind)
            {
                case CharacterBuildStatKind.Strength:
                    return new RowMeta("STR", "\u529B");
                case CharacterBuildStatKind.Magic:
                    return new RowMeta("MA", "\u9B54\u529B");
                case CharacterBuildStatKind.Endurance:
                    return new RowMeta("EN", "\u30A8\u30CA\u30B8\u30FC");
                case CharacterBuildStatKind.HeartBeat:
                    return new RowMeta("HB", "\u30CF\u30FC\u30E2\u30CB\u30FC");
                case CharacterBuildStatKind.Luck:
                    return new RowMeta("LUCK", "\u30E9\u30C3\u30AF");
                default:
                    return new RowMeta(string.Empty, string.Empty);
            }
        }

        public static bool TryForRowName(string rowName, out RowMeta meta)
        {
            switch (rowName)
            {
                case "StatRow_Strength":
                    meta = ForKind(CharacterBuildStatKind.Strength);
                    return true;
                case "StatRow_Magic":
                    meta = ForKind(CharacterBuildStatKind.Magic);
                    return true;
                case "StatRow_Endurance":
                    meta = ForKind(CharacterBuildStatKind.Endurance);
                    return true;
                case "StatRow_HeartBeat":
                    meta = ForKind(CharacterBuildStatKind.HeartBeat);
                    return true;
                case "StatRow_Luck":
                    meta = ForKind(CharacterBuildStatKind.Luck);
                    return true;
                default:
                    meta = default;
                    return false;
            }
        }
    }
}
