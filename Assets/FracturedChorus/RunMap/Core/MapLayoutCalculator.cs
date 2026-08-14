using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.Core
{
    public sealed class RunMapLayoutMetrics
    {
        private MapGenerationProfile _profile = MapGenerationProfile.Default;
        private RunMapLayoutConfigSO _config;

        public float NodeSpacingX { get; private set; } = MapLayoutConstants.NodeSpacingX;
        public float NodeSpacingY { get; private set; } = MapLayoutConstants.NodeSpacingY;
        public float NodeDiameter { get; private set; } = MapLayoutConstants.NodeDiameter;
        public float BossNodeDiameter { get; private set; } = MapLayoutConstants.BossNodeDiameter;

        public void SetConfig(RunMapLayoutConfigSO config)
        {
            _config = config;
            ResetToDefaults();
        }

        public void SetProfile(MapGenerationProfile profile)
        {
            _profile = profile ?? MapGenerationProfile.Default;
        }

        public float GridOriginX => -((_profile.ColumnCount - 1) * 0.5f * NodeSpacingX);

        private float StartNodeDiameter => NodeDiameter * StartNodeScale;

        private float StartNodeScale => _config != null ? _config.StartNodeScale : MapLayoutConstants.StartNodeScale;

        private float ContentPaddingBottom =>
            _config != null ? _config.ContentPaddingBottom : MapLayoutConstants.ContentPaddingBottom;

        private float ContentPaddingTop =>
            _config != null ? _config.ContentPaddingTop : MapLayoutConstants.ContentPaddingTop;

        private float StartBottomInset =>
            _config != null ? _config.StartBottomInset : MapLayoutConstants.StartBottomInset;

        private float StartToF1GapScale =>
            _config != null ? _config.StartToF1GapScale : MapLayoutConstants.StartToF1GapScale;

        private float BossYOffset => _config != null ? _config.BossYOffset : MapLayoutConstants.BossYOffset;

        public float ViewportBottomGutter =>
            _config != null ? _config.ViewportBottomGutter : MapLayoutConstants.ViewportBottomGutter;

        private float StartNodeCenterY =>
            ContentPaddingBottom + StartBottomInset + StartNodeDiameter * 0.5f;

        private float FloorOriginY =>
            StartNodeCenterY + StartNodeDiameter * 0.5f + NodeSpacingY * StartToF1GapScale;

        public float BossCenterY =>
            FloorOriginY + _profile.FloorCount * NodeSpacingY + BossYOffset;

        public void FitToViewport(ScrollRect scrollRect, bool enabled)
        {
            if (!enabled || scrollRect?.viewport == null)
            {
                ResetToDefaults();
                return;
            }

            if (_config != null && !_config.AllowViewportFit)
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

            var gridSpan = _profile.ColumnCount - 1;
            var labelGutter = _config != null ? _config.LabelGutter : 56f;
            var widthScale = _config != null ? _config.FitWidthScale : 0.94f;
            var minSpacingX = _config != null ? _config.MinSpacingX : 104f;
            var maxSpacingX = _config != null ? _config.MaxSpacingX : 168f;
            var minSpacingY = _config != null ? _config.MinSpacingY : 88f;
            var maxSpacingY = _config != null ? _config.MaxSpacingY : 128f;
            var yDivisor = _config != null ? _config.SpacingYViewportDivisor : 4.6f;
            var diameterRatio = _config != null ? _config.NodeDiameterSpacingRatio : 0.62f;
            var minDiameter = _config != null ? _config.MinNodeDiameter : 72f;
            var maxDiameter = _config != null ? _config.MaxNodeDiameter : 104f;
            var bossScale = _config != null ? _config.BossDiameterScale : 1.38f;
            var minBoss = _config != null ? _config.MinBossDiameter : 96f;
            var maxBoss = _config != null ? _config.MaxBossDiameter : 140f;

            var usableWidth = viewport.width * widthScale - labelGutter * 2f;
            NodeSpacingX = Mathf.Clamp(usableWidth / gridSpan, minSpacingX, maxSpacingX);
            NodeSpacingY = Mathf.Clamp(viewport.height / yDivisor, minSpacingY, maxSpacingY);
            NodeDiameter = Mathf.Clamp(NodeSpacingX * diameterRatio, minDiameter, maxDiameter);
            BossNodeDiameter = Mathf.Clamp(NodeDiameter * bossScale, minBoss, maxBoss);
        }

        public void ResetToDefaults()
        {
            NodeSpacingX = _config != null ? _config.NodeSpacingX : MapLayoutConstants.NodeSpacingX;
            NodeSpacingY = _config != null ? _config.NodeSpacingY : MapLayoutConstants.NodeSpacingY;
            NodeDiameter = _config != null ? _config.NodeDiameter : MapLayoutConstants.NodeDiameter;
            BossNodeDiameter = _config != null ? _config.BossNodeDiameter : MapLayoutConstants.BossNodeDiameter;
        }

        public Vector2 NodePosition(MapNodeData node)
        {
            if (node.Type == MapNodeType.Start)
            {
                var startColumn = (_profile.ColumnCount - 1) * 0.5f;
                return new Vector2(GridOriginX + startColumn * NodeSpacingX, StartNodeCenterY);
            }

            if (node.IsBoss)
            {
                var bossColumn = (_profile.ColumnCount - 1) * 0.5f;
                var bossY = FloorOriginY + _profile.FloorCount * NodeSpacingY + BossYOffset;
                return new Vector2(GridOriginX + bossColumn * NodeSpacingX, bossY);
            }

            return new Vector2(
                GridOriginX + node.Column * NodeSpacingX,
                FloorOriginY + (node.Floor - 1) * NodeSpacingY);
        }

        public Vector2 FloorPosition(int floor) =>
            new Vector2(GridOriginX, FloorOriginY + (floor - 1) * NodeSpacingY);

        public float NodeVisualDiameter(MapNodeData node)
        {
            if (node.IsBoss)
            {
                return BossNodeDiameter;
            }

            if (node.Type == MapNodeType.Start)
            {
                return NodeDiameter * StartNodeScale;
            }

            return NodeDiameter;
        }

        public void ComputeContentSize(out float width, out float height)
        {
            var gridWidth = (_profile.ColumnCount - 1) * NodeSpacingX;
            var labelGutter = NodeSpacingX * 0.6f;
            width = gridWidth + labelGutter * 2f;
            height = BossCenterY + BossNodeDiameter * 0.6f + ContentPaddingTop;
        }

        public float FloorLabelX => GridOriginX - NodeSpacingX * 0.58f;

        public int FloorLabelFontSize => Mathf.RoundToInt(Mathf.Clamp(NodeSpacingX * 0.14f, 13f, 18f));
    }
}
