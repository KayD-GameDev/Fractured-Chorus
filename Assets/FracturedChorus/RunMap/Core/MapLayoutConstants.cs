namespace FracturedChorus.RunMap.Core
{
    public static class MapLayoutConstants
    {
        public const int ColumnCount = 7;
        public const int FloorCount = 15;
        public const int BossFloor = 16;
        public const int DefaultPathCount = 4;
        public const int MinStartNodes = 2;
        public const int MaxStartNodes = 4;
        public const int ExclusivePrefixFloors = 3;
        public const int MaxColumnConnectDelta = 1;
        public const int MaxDriftFromCenter = 2;
        public const int CenterColumnBiasWeight = 10;

        public const float NodeSpacingX = 118f;
        public const float NodeSpacingY = 96f;
        public const float NodeDiameter = 80f;
        public const float BossNodeDiameter = 112f;
        public const float BossYOffset = 112f;
        public const float ContentPaddingX = 48f;
        public const float ContentPaddingBottom = 48f;
        public const float ContentPaddingTop = 120f;
        public const float StartBottomInset = 36f;
        public const float StartToF1GapScale = 0.65f;
        public const float StartNodeScale = 1.12f;
        public const float ViewportBottomGutter = 88f;

        /// <summary>Icon glyph scale vs base 14px / boss 22px.</summary>
        public const float NodeIconFontScale = 1.75f;
        /// <summary>Camp (nghỉ) icon +25% so với node thường.</summary>
        public const float CampIconFontScaleBonus = 1.25f;
        public const int NodeIconFontSizeBase = 14;
        public const int NodeIconFontSizeBoss = 22;

        public static int NodeLabelFontSize(MapNodeType type, bool isBoss)
        {
            if (isBoss)
            {
                return (int)System.Math.Round(NodeIconFontSizeBoss * NodeIconFontScale);
            }

            var scale = NodeIconFontScale;
            if (type == MapNodeType.Camp)
            {
                scale *= CampIconFontScaleBonus;
            }

            return (int)System.Math.Round(NodeIconFontSizeBase * scale);
        }

        // Legend panel (RunMapPrototype)
        public const int LegendTitleFontSize = 24;
        public const int LegendDescFontSize = 20;
        public const int LegendHintFontSize = 18;
        /// <summary>VLG spacing — base 12 + 1.5px (not +2).</summary>
        public const float LegendVerticalSpacing = 13.5f;
        /// <summary>HLG dot↔text — base 16 + 1.5px.</summary>
        public const float LegendRowHorizontalSpacing = 17.5f;
        public const float LegendDotSize = 56f;
        public const float LegendRowMinHeight = 56f;
        public const float LegendTitleHeight = 32f;
        public const float LegendHintMinHeight = 72f;
        public const float LegendHintLineSpacing = 0.75f;
    }
}
