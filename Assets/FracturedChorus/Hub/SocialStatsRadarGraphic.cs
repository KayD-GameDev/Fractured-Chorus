using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocialStatsRadarGraphic : MaskableGraphic
    {
        private const int AxisCount = 5;
        private static readonly float[] RingScales = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };

        [SerializeField] private int maxRank = 10;
        [SerializeField] private Color axisColor = new Color(0f, 0.83f, 1f, 0.45f);
        [SerializeField] private Color ringColor = new Color(0f, 0.83f, 1f, 0.22f);
        [SerializeField] private Color fillColor = new Color(0f, 0.75f, 1f, 0.28f);
        [SerializeField] private Color strokeColor = new Color(0.4f, 0.95f, 1f, 0.9f);
        [SerializeField] private float strokeWidth = 2.5f;

        private readonly int[] _ranks = { 1, 1, 1, 1, 1 };
        private readonly Vector2[] _axisDirs = new Vector2[AxisCount];
        private readonly Vector2[] _dataPoints = new Vector2[AxisCount];

        public int MaxRank => maxRank;

        public void SetRanks(IReadOnlyList<int> ranks)
        {
            for (var i = 0; i < AxisCount; i++)
            {
                var v = ranks != null && i < ranks.Count ? ranks[i] : 1;
                _ranks[i] = Mathf.Clamp(v, 1, maxRank);
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var center = rect.center;
            var radius = 0.48f * Mathf.Min(rect.width, rect.height);
            if (radius <= 0.01f || maxRank < 1)
            {
                return;
            }

            CacheAxisDirections();

            var ring32 = (Color32)ringColor;
            for (var r = 0; r < RingScales.Length; r++)
            {
                DrawClosedStroke(vh, center, radius * RingScales[r], strokeWidth, ring32);
            }

            var axis32 = (Color32)axisColor;
            for (var i = 0; i < AxisCount; i++)
            {
                AddLineQuad(vh, center, center + _axisDirs[i] * radius, strokeWidth, axis32);
            }

            for (var i = 0; i < AxisCount; i++)
            {
                var t = _ranks[i] / (float)maxRank;
                _dataPoints[i] = center + _axisDirs[i] * (radius * t);
            }

            var fill32 = (Color32)fillColor;
            for (var i = 0; i < AxisCount; i++)
            {
                var next = (i + 1) % AxisCount;
                AddTriangle(vh, center, _dataPoints[i], _dataPoints[next], fill32);
            }

            var stroke32 = (Color32)strokeColor;
            for (var i = 0; i < AxisCount; i++)
            {
                var next = (i + 1) % AxisCount;
                AddLineQuad(vh, _dataPoints[i], _dataPoints[next], strokeWidth, stroke32);
            }
        }

        private void CacheAxisDirections()
        {
            for (var i = 0; i < AxisCount; i++)
            {
                var angleDeg = -90f + (i - 2) * 72f;
                var rad = angleDeg * Mathf.Deg2Rad;
                _axisDirs[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
        }

        private void DrawClosedStroke(VertexHelper vh, Vector2 center, float ringRadius, float width, Color32 color)
        {
            for (var i = 0; i < AxisCount; i++)
            {
                var next = (i + 1) % AxisCount;
                var a = center + _axisDirs[i] * ringRadius;
                var b = center + _axisDirs[next] * ringRadius;
                AddLineQuad(vh, a, b, width, color);
            }
        }

        private static void AddLineQuad(VertexHelper vh, Vector2 from, Vector2 to, float width, Color32 color)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < 1e-8f)
            {
                return;
            }

            var dir = delta.normalized;
            var perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            var v0 = from + perp;
            var v1 = from - perp;
            var v2 = to - perp;
            var v3 = to + perp;

            var index = vh.currentVertCount;
            vh.AddVert(v0, color, Vector2.zero);
            vh.AddVert(v1, color, Vector2.zero);
            vh.AddVert(v2, color, Vector2.zero);
            vh.AddVert(v3, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            var index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }
    }
}
