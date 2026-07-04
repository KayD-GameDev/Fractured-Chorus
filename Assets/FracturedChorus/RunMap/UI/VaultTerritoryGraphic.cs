using System.Collections.Generic;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class VaultTerritoryGraphic : MaskableGraphic,
        ICanvasRaycastFilter,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private CadenceMapLayoutSO.VaultFingerIdRef fingerId;
        [SerializeField] private Vector2[] normalizedVertices;
        [SerializeField] private Color baseTint = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f, 0.42f);
        [SerializeField] private Color lockedTint = new Color(0.2f, 0.2f, 0.2f, 0.12f);
        [SerializeField] private bool unlocked;

        private bool _hovered;
        private readonly List<Vector2> _localVertices = new List<Vector2>();

        public CadenceMapLayoutSO.VaultFingerIdRef FingerId => fingerId;
        public bool Unlocked => unlocked;

        public event System.Action<VaultTerritoryGraphic> TerritoryClicked;
        public event System.Action<VaultTerritoryGraphic> TerritoryHovered;

        public void ApplyEntry(CadenceMapLayoutSO.TerritoryEntry entry)
        {
            fingerId = entry.finger;
            normalizedVertices = entry.normalizedVertices;
            baseTint = entry.territoryColor;
            hoverTint = entry.highlightColor;
            unlocked = entry.unlocked;
            SetVerticesDirty();
        }

        public void SetUnlocked(bool value)
        {
            unlocked = value;
            SetVerticesDirty();
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!unlocked || normalizedVertices == null || normalizedVertices.Length < 3)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out var local))
            {
                return false;
            }

            var rect = rectTransform.rect;
            var normalized = new Vector2(
                (local.x - rect.xMin) / rect.width,
                (local.y - rect.yMin) / rect.height);
            return PointInPolygon(normalized, normalizedVertices);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!unlocked)
            {
                return;
            }

            _hovered = true;
            SetVerticesDirty();
            TerritoryHovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            SetVerticesDirty();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!unlocked)
            {
                return;
            }

            TerritoryClicked?.Invoke(this);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (normalizedVertices == null || normalizedVertices.Length < 3)
            {
                return;
            }

            _localVertices.Clear();
            var rect = rectTransform.rect;
            var centroid = Vector2.zero;

            for (var i = 0; i < normalizedVertices.Length; i++)
            {
                var local = new Vector2(
                    rect.xMin + normalizedVertices[i].x * rect.width,
                    rect.yMin + normalizedVertices[i].y * rect.height);
                _localVertices.Add(local);
                centroid += local;
            }

            centroid /= normalizedVertices.Length;

            var tint = unlocked && _hovered ? hoverTint : Color.clear;
            var color32 = (Color32)tint;

            for (var i = 0; i < _localVertices.Count; i++)
            {
                var next = (i + 1) % _localVertices.Count;
                AddTriangle(vh, centroid, _localVertices[i], _localVertices[next], color32);
            }
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            var index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }

        private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                var intersect = pi.y > point.y != pj.y > point.y &&
                                point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 1e-6f) + pi.x;
                if (intersect)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
