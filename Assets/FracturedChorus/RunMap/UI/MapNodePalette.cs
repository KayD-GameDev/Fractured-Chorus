using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.RunMap.UI
{
    public static class MapNodePalette
    {
        // Brand stroke colors (legend + map node ring) — 2026 spec
        private static readonly Color BattleStroke = Hex("#AA4E49");
        private static readonly Color EventStroke = Hex("#82B366");
        private static readonly Color EliteStroke = Hex("#795F86");
        private static readonly Color CampStroke = Hex("#D6B657");
        private static readonly Color RelayStroke = Hex("#D79B00");
        private static readonly Color TreasureStroke = Hex("#7091C0");
        private static readonly Color BossStroke = Hex("#C0463E");
        private static readonly Color BossFill = new Color(0.12f, 0.12f, 0.2f);

        public static Color FillColor(MapNodeType type)
        {
            if (type == MapNodeType.Boss)
            {
                return BossFill;
            }

            return LightenFill(StrokeColor(type));
        }

        public static Color StrokeColor(MapNodeType type) => type switch
        {
            MapNodeType.Battle => BattleStroke,
            MapNodeType.Event => EventStroke,
            MapNodeType.Elite => EliteStroke,
            MapNodeType.Camp => CampStroke,
            MapNodeType.Relay => RelayStroke,
            MapNodeType.Treasure => TreasureStroke,
            MapNodeType.Boss => BossStroke,
            _ => Color.gray
        };

        public static string Label(MapNodeType type) => type switch
        {
            MapNodeType.Battle => "⚔",
            MapNodeType.Event => "?",
            MapNodeType.Elite => "★",
            MapNodeType.Camp => "⛺",
            MapNodeType.Relay => "$",
            MapNodeType.Treasure => "◆",
            MapNodeType.Boss => "♪",
            _ => "·"
        };

        public static string DisplayName(MapNodeType type) => type switch
        {
            MapNodeType.Battle => "Battle",
            MapNodeType.Event => "Event",
            MapNodeType.Elite => "Elite",
            MapNodeType.Camp => "Camp",
            MapNodeType.Relay => "Relay",
            MapNodeType.Treasure => "Treasure",
            MapNodeType.Boss => "Oni",
            _ => type.ToString()
        };

        private static Color LightenFill(Color stroke, float whiteMix = 0.52f) =>
            Color.Lerp(stroke, Color.white, whiteMix);

        private static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.white;
        }
    }
}
