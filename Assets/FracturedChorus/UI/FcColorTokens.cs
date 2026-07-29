using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>UI color source of truth. See docs/ui/COLOR_TOKENS.md.</summary>
    public static class FcColorTokens
    {
        public static class Brand
        {
            public static readonly Color Cyan = Rgb(0f, 0.831f, 1f);
            public static readonly Color CyanDim = FromHex("#008CB3");
            public static readonly Color CyanHover = Rgb(0.55f, 0.85f, 1f);
            public static readonly Color CyanSoft = Rgb(0.2f, 0.75f, 1f);
            public static readonly Color CyanNeonBody = FromHex("#22D3EE");
            public static readonly Color CyanNeonCore = FromHex("#8CF3FF");
            public static readonly Color MagentaAccent = FromHex("#FF3DA6");
            public static readonly Color RedSelection = FromHex("#FF4757");
            public static readonly Color TextPrimary = FromHex("#EAFBFF");
            public static readonly Color TextMuted = Rgb(0.72f, 0.82f, 0.92f, 0.85f);
            public static readonly Color TextIdle = Rgb(0.75f, 0.9f, 1f, 0.85f);
            public static readonly Color RadarStroke = Rgb(0.4f, 0.95f, 1f, 0.9f);
            public static readonly Color SaturdayLabel = Rgb(0.55f, 0.92f, 1f);
        }

        public static class Surface
        {
            public static readonly Color Dim = Rgb(0.02f, 0.04f, 0.12f, 0.75f);
            public static readonly Color Panel = Rgb(0.03f, 0.05f, 0.14f, 0.92f);
            public static readonly Color Modal = Rgb(0.039f, 0.059f, 0.18f, 0.94f);
            public static readonly Color Track = Rgb(0.08f, 0.12f, 0.22f, 0.95f);
            public static readonly Color Row = Rgb(0.06f, 0.08f, 0.24f, 0.92f);
            public static readonly Color RowSelected = Rgb(0.1f, 0.14f, 0.34f, 0.96f);
            public static readonly Color Detail = Rgb(0.039f, 0.039f, 0.18f, 0.72f);
            public static readonly Color Chip = Rgb(0f, 0.75f, 0.92f, 0.92f);
            public static readonly Color DimmerBlack = Rgb(0f, 0f, 0f, 0.72f);
        }

        public static class Semantic
        {
            public static readonly Color ElementRhythm = Rgb(0.92f, 0.28f, 0.22f);
            public static readonly Color ElementMelody = Rgb(0.58f, 0.28f, 0.88f);
            public static readonly Color ElementHarmony = Rgb(0.95f, 0.82f, 0.18f);
            public static readonly Color Damage = FromHex("#FF61C7");
            public static readonly Color Heal = FromHex("#40FF8C");
            public static readonly Color Crit = FromHex("#FFE033");
            public static readonly Color Warning = FromHex("#F27D22");
            public static readonly Color EventGold = FromHex("#FFD633");
            public static readonly Color CalendarPink = Rgb(1f, 0f, 0.4f);
            public static readonly Color CalendarSunday = Rgb(1f, 0.28f, 0.35f);
        }

        public static class RunMap
        {
            public static readonly Color BattleStroke = FromHex("#C04A55");
            public static readonly Color EventStroke = FromHex("#5BA88A");
            public static readonly Color EliteStroke = FromHex("#7A5E9E");
            public static readonly Color CampStroke = FromHex("#C9A84E");
            public static readonly Color RelayStroke = FromHex("#E8A830");
            public static readonly Color TreasureStroke = FromHex("#4A9FD4");
            public static readonly Color BossStroke = FromHex("#D43840");
        }

        public static class Selection
        {
            public static Color Accent => WithAlpha(Brand.RedSelection, 0.95f);
            public static Color RowBackground => Color.Lerp(Surface.Row, WithAlpha(Brand.RedSelection, 0.32f), 0.45f);
            public static Color TabIconTint => Color.Lerp(Color.white, Brand.RedSelection, 0.22f);
            public static Color VnChoiceHighlight => WithAlpha(Rgb(0.35f, 0.72f, 1f), 0.92f);
        }

        public static Color FromHex(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return Color.white;
            }

            var value = html.StartsWith("#") ? html : "#" + html;
            return ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color LerpSurface(Color stroke, float panelMix = 0.55f)
        {
            var panel = Surface.Panel;
            panel.a = 1f;
            return Color.Lerp(stroke, panel, panelMix);
        }

        private static Color Rgb(float r, float g, float b, float a = 1f)
        {
            return new Color(r, g, b, a);
        }
    }
}
