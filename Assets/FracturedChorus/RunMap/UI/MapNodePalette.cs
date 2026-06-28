using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.RunMap.UI
{
    public static class MapNodePalette
    {
        public static Color FillColor(MapNodeType type) => type switch
        {
            MapNodeType.Battle => new Color(0.973f, 0.808f, 0.8f),
            MapNodeType.Event => new Color(0.835f, 0.91f, 0.831f),
            MapNodeType.Elite => new Color(0.882f, 0.835f, 0.906f),
            MapNodeType.Camp => new Color(1f, 0.949f, 0.8f),
            MapNodeType.Relay => new Color(1f, 0.902f, 0.8f),
            MapNodeType.Treasure => new Color(0.855f, 0.91f, 0.988f),
            MapNodeType.Boss => new Color(0.102f, 0.102f, 0.18f),
            _ => Color.gray
        };

        public static Color StrokeColor(MapNodeType type) => type switch
        {
            MapNodeType.Battle => new Color(0.722f, 0.329f, 0.314f),
            MapNodeType.Event => new Color(0.51f, 0.702f, 0.4f),
            MapNodeType.Elite => new Color(0.588f, 0.451f, 0.651f),
            MapNodeType.Camp => new Color(0.839f, 0.714f, 0.337f),
            MapNodeType.Relay => new Color(0.843f, 0.608f, 0f),
            MapNodeType.Treasure => new Color(0.424f, 0.557f, 0.749f),
            MapNodeType.Boss => new Color(0.722f, 0.329f, 0.314f),
            _ => Color.white
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
    }
}
