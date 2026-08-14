using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.RunMap.UI
{
    public static class MapNodePalette
    {
        public static Color FillColor(MapNodeType type)
        {
            if (type == MapNodeType.Boss)
            {
                return FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.96f);
            }

            return FcColorTokens.LerpSurface(StrokeColor(type));
        }

        public static Color StrokeColor(MapNodeType type) => type switch
        {
            MapNodeType.Battle => FcColorTokens.RunMap.BattleStroke,
            MapNodeType.Event => FcColorTokens.RunMap.EventStroke,
            MapNodeType.Elite => FcColorTokens.RunMap.EliteStroke,
            MapNodeType.Camp => FcColorTokens.RunMap.CampStroke,
            MapNodeType.Relay => FcColorTokens.RunMap.RelayStroke,
            MapNodeType.Treasure => FcColorTokens.RunMap.TreasureStroke,
            MapNodeType.Boss => FcColorTokens.RunMap.BossStroke,
            MapNodeType.Start => FcColorTokens.Brand.CyanNeonCore,
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
            MapNodeType.Start => "⚑",
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
            MapNodeType.Start => "Start",
            _ => type.ToString()
        };
    }
}
