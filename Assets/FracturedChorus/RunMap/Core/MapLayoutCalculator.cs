using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.Core
{
    /// <summary>Layout bottom-origin: F1 ở đáy content, F16 boss phía trên. Dùng chung UI + content size.</summary>
    public sealed class RunMapLayoutMetrics
    {
        public float NodeSpacingX { get; private set; } = MapLayoutConstants.NodeSpacingX;
        public float NodeSpacingY { get; private set; } = MapLayoutConstants.NodeSpacingY;
        public float NodeDiameter { get; private set; } = MapLayoutConstants.NodeDiameter;

        public float GridOriginX => -((MapLayoutConstants.ColumnCount - 1) * 0.5f * NodeSpacingX);

        public float BossCenterY =>
            MapLayoutConstants.ContentPaddingBottom +
            MapLayoutConstants.FloorCount * NodeSpacingY +
            MapLayoutConstants.BossYOffset;

        public void FitToViewport(ScrollRect scrollRect, bool enabled)
        {
            if (!enabled || scrollRect?.viewport == null)
            {
                ResetToDefaults();
                return;
            }

            Canvas.ForceUpdateCanvases();
            var viewport = scrollRect.viewport.rect;
            if (viewport.width <= 10f || viewport.height <= 10f)
            {
                ResetToDefaults();
                return;
            }

            var gridSpan = MapLayoutConstants.ColumnCount - 1;
            const float labelGutter = 52f;
            var usableWidth = viewport.width * 0.94f - labelGutter * 2f;
            NodeSpacingX = Mathf.Clamp(usableWidth / gridSpan, 78f, 148f);
            NodeSpacingY = Mathf.Clamp(viewport.height / 5.25f, 68f, 108f);
            NodeDiameter = Mathf.Clamp(NodeSpacingX * 0.36f, 34f, 50f);
        }

        public void ResetToDefaults()
        {
            NodeSpacingX = MapLayoutConstants.NodeSpacingX;
            NodeSpacingY = MapLayoutConstants.NodeSpacingY;
            NodeDiameter = MapLayoutConstants.NodeDiameter;
        }

        public Vector2 NodePosition(MapNodeData node)
        {
            var baseY = MapLayoutConstants.ContentPaddingBottom;

            if (node.IsBoss)
            {
                var bossColumn = (MapLayoutConstants.ColumnCount - 1) * 0.5f;
                var bossY = MapLayoutConstants.FloorCount * NodeSpacingY + MapLayoutConstants.BossYOffset;
                return new Vector2(GridOriginX + bossColumn * NodeSpacingX, baseY + bossY);
            }

            return new Vector2(
                GridOriginX + node.Column * NodeSpacingX,
                baseY + (node.Floor - 1) * NodeSpacingY);
        }

        public Vector2 FloorPosition(int floor) =>
            NodePosition(new MapNodeData { Floor = floor, Column = 0 });

        public float NodeVisualDiameter(MapNodeData node) =>
            node.IsBoss ? MapLayoutConstants.BossNodeDiameter : NodeDiameter;

        public void ComputeContentSize(out float width, out float height)
        {
            var gridWidth = (MapLayoutConstants.ColumnCount - 1) * NodeSpacingX;
            var labelGutter = NodeSpacingX * 0.6f;
            width = gridWidth + labelGutter * 2f;
            height = BossCenterY +
                     MapLayoutConstants.BossNodeDiameter * 0.6f +
                     MapLayoutConstants.ContentPaddingTop;
        }

        public float FloorLabelX => GridOriginX - NodeSpacingX * 0.58f;

        public int FloorLabelFontSize => Mathf.RoundToInt(Mathf.Clamp(NodeSpacingX * 0.16f, 12f, 16f));
    }
}
