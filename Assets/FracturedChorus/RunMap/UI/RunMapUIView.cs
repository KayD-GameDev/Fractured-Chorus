using System.Collections;
using System.Collections.Generic;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    public class RunMapUIView : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private RectTransform connectionsLayer;
        [SerializeField] private RectTransform nodesLayer;
        [SerializeField] private RectTransform floorLabelsLayer;
        [SerializeField] private MapNodeView nodeTemplate;
        [SerializeField] private MapConnectionLineView connectionTemplate;

        [Header("Layout")]
        [SerializeField] private float nodeSpacingX = MapLayoutConstants.NodeSpacingX;
        [SerializeField] private float nodeSpacingY = MapLayoutConstants.NodeSpacingY;
        [SerializeField] private float nodeDiameter = MapLayoutConstants.NodeDiameter;
        [SerializeField] private bool fitToViewport = true;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Path visuals")]
        [SerializeField] private float baseLineThickness = 4f;
        [SerializeField] private float visitedLineThickness = 7f;
        [SerializeField] private float previewLineThickness = 5.5f;

        private float _contentWidth;
        private float _contentHeight;
        private readonly Dictionary<int, MapNodeView> _nodeViews = new Dictionary<int, MapNodeView>();
        private readonly List<MapConnectionLineView> _connectionViews = new List<MapConnectionLineView>();
        private readonly List<Text> _floorLabels = new List<Text>();

        public IReadOnlyDictionary<int, MapNodeView> NodeViews => _nodeViews;
        public event System.Action<MapNodeView> NodeClicked;

        public void ApplyAuthoringPolicy(bool preserve)
        {
            // Kept for bootstrap compatibility; layout always rebuilds from graph at Play.
        }

        public void BuildMap(MapGraph graph)
        {
            ClearDynamicContent();
            FitLayoutToViewport();
            ComputeContentSize(out _contentWidth, out _contentHeight);
            ApplyContentRect();
            ConfigureLayers(_contentWidth, _contentHeight);

            CreateFloorLabels(graph);
            CreateConnections(graph);
            CreateNodes(graph);
            if (nodesLayer != null)
            {
                nodesLayer.SetAsLastSibling();
            }

            StartCoroutine(ScrollToBottomDeferred());
        }

        public void WireScrollRect(ScrollRect scroll) => scrollRect = scroll;

        public void RefreshInteraction(MapGraph graph, RunState runState)
        {
            var pathSet = new HashSet<int>(runState.VisitedPath);
            foreach (var pair in _nodeViews)
            {
                var node = graph.GetNode(pair.Key);
                var reachable = runState.CanTravelTo(graph, node);
                var current = pair.Key == runState.CurrentNodeId;
                pair.Value.RefreshVisual(reachable, pathSet.Contains(pair.Key), current);
            }

            RefreshConnectionHighlights(graph, runState);
        }

        public void ScrollToNode(MapNodeData node, bool immediate = false)
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
            }

            if (scrollRect == null || node == null)
            {
                return;
            }

            if (immediate)
            {
                ApplyScrollToNode(node);
            }
            else
            {
                StartCoroutine(ScrollToNodeDeferred(node));
            }
        }

        public Vector2 NodeAnchoredPosition(MapNodeData node)
        {
            var gridOriginX = GridOriginX;
            var baseY = MapLayoutConstants.ContentPaddingBottom;

            if (node.IsBoss)
            {
                var bossColumn = (MapLayoutConstants.ColumnCount - 1) * 0.5f;
                var bossY = MapLayoutConstants.FloorCount * nodeSpacingY + MapLayoutConstants.BossYOffset;
                return new Vector2(gridOriginX + bossColumn * nodeSpacingX, baseY + bossY);
            }

            var x = gridOriginX + node.Column * nodeSpacingX;
            var y = baseY + (node.Floor - 1) * nodeSpacingY;
            return new Vector2(x, y);
        }

        private float GridOriginX =>
            -((MapLayoutConstants.ColumnCount - 1) * 0.5f * nodeSpacingX);

        private float NodeVisualDiameter(MapNodeData node) =>
            node.IsBoss ? MapLayoutConstants.BossNodeDiameter : nodeDiameter;

        private void FitLayoutToViewport()
        {
            if (!fitToViewport)
            {
                return;
            }

            if (scrollRect == null)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
            }

            if (scrollRect?.viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var viewport = scrollRect.viewport.rect;
            if (viewport.width <= 10f || viewport.height <= 10f)
            {
                return;
            }

            var gridSpan = MapLayoutConstants.ColumnCount - 1;
            var labelGutter = 52f;
            var usableWidth = viewport.width * 0.94f - labelGutter * 2f;
            nodeSpacingX = usableWidth / gridSpan;
            nodeSpacingX = Mathf.Clamp(nodeSpacingX, 78f, 148f);

            nodeSpacingY = viewport.height / 5.25f;
            nodeSpacingY = Mathf.Clamp(nodeSpacingY, 68f, 108f);

            nodeDiameter = nodeSpacingX * 0.36f;
            nodeDiameter = Mathf.Clamp(nodeDiameter, 34f, 50f);
        }

        private void ComputeContentSize(out float width, out float height)
        {
            var gridWidth = (MapLayoutConstants.ColumnCount - 1) * nodeSpacingX;
            var labelGutter = nodeSpacingX * 0.6f;
            width = gridWidth + labelGutter * 2f;

            var bossCenterY = MapLayoutConstants.ContentPaddingBottom +
                              MapLayoutConstants.FloorCount * nodeSpacingY +
                              MapLayoutConstants.BossYOffset;
            height = bossCenterY +
                     MapLayoutConstants.BossNodeDiameter * 0.6f +
                     MapLayoutConstants.ContentPaddingTop;
        }

        private void ApplyContentRect()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_contentWidth, _contentHeight);
        }

        private static void ConfigureLayers(float width, float height, params RectTransform[] layers)
        {
            foreach (var layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.anchorMin = new Vector2(0.5f, 0f);
                layer.anchorMax = new Vector2(0.5f, 0f);
                layer.pivot = new Vector2(0.5f, 0f);
                layer.anchoredPosition = Vector2.zero;
                layer.sizeDelta = new Vector2(width, height);
            }
        }

        private void ConfigureLayers(float width, float height)
        {
            ConfigureLayers(width, height, connectionsLayer, nodesLayer, floorLabelsLayer);
        }

        private void ClearDynamicContent()
        {
            StopAllCoroutines();

            foreach (var view in _nodeViews.Values)
            {
                if (view != null && view != nodeTemplate)
                {
                    Destroy(view.gameObject);
                }
            }

            _nodeViews.Clear();

            foreach (var line in _connectionViews)
            {
                if (line != null && line != connectionTemplate)
                {
                    Destroy(line.gameObject);
                }
            }

            _connectionViews.Clear();

            foreach (var label in _floorLabels)
            {
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }

            _floorLabels.Clear();
        }

        private void CreateFloorLabels(MapGraph graph)
        {
            var labelX = GridOriginX - nodeSpacingX * 0.58f;
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(nodeSpacingX * 0.16f, 12f, 16f));

            for (var floor = 1; floor <= MapLayoutConstants.FloorCount; floor++)
            {
                CreateFloorLabel($"F{floor}", new Vector2(labelX, NodeAnchoredPositionForFloor(floor).y), fontSize);
            }

            if (graph.BossNode != null)
            {
                var bossPos = NodeAnchoredPosition(graph.BossNode);
                CreateFloorLabel("F16", new Vector2(labelX, bossPos.y), fontSize);
            }
        }

        private Vector2 NodeAnchoredPositionForFloor(int floor) =>
            NodeAnchoredPosition(new MapNodeData { Floor = floor, Column = 0 });

        private void CreateFloorLabel(string text, Vector2 anchoredPos, int fontSize)
        {
            var parent = floorLabelsLayer != null ? floorLabelsLayer : transform;
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 20f);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = new Color(0.55f, 0.58f, 0.62f);
            label.alignment = TextAnchor.MiddleRight;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _floorLabels.Add(label);
        }

        private void CreateNodes(MapGraph graph)
        {
            var parent = nodesLayer != null ? nodesLayer : transform;

            foreach (var node in graph.Nodes)
            {
                var clone = Instantiate(
                    nodeTemplate != null ? nodeTemplate.gameObject : CreateFallbackNode(parent),
                    parent);
                clone.SetActive(true);
                var view = clone.GetComponent<MapNodeView>();

                var rect = view.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = NodeAnchoredPosition(node);
                var diameter = NodeVisualDiameter(node);
                rect.sizeDelta = new Vector2(diameter, diameter);

                view.Bind(node);
                view.Clicked -= OnNodeClicked;
                view.Clicked += OnNodeClicked;
                _nodeViews[node.Id] = view;
            }

            if (nodeTemplate != null)
            {
                nodeTemplate.gameObject.SetActive(false);
            }
        }

        private GameObject CreateFallbackNode(Transform parent)
        {
            var go = new GameObject("Node", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void CreateConnections(MapGraph graph)
        {
            var edgeColor = new Color(0.28f, 0.3f, 0.34f, 0.9f);
            // Vẽ line cùng layer với node để tránh lệch transform khi scroll.
            var parent = nodesLayer != null ? nodesLayer : connectionsLayer != null ? connectionsLayer : transform;

            foreach (var node in graph.Nodes)
            {
                foreach (var toId in node.Outgoing)
                {
                    var to = graph.GetNode(toId);
                    if (to == null)
                    {
                        continue;
                    }

                    var clone = Instantiate(
                        connectionTemplate != null
                            ? connectionTemplate.gameObject
                            : CreateFallbackLine(parent),
                        parent);
                    clone.SetActive(true);
                    clone.transform.SetSiblingIndex(0);
                    var line = clone.GetComponent<MapConnectionLineView>();

                    line.SetEndpoints(
                        NodeAnchoredPosition(node),
                        NodeAnchoredPosition(to),
                        edgeColor,
                        baseLineThickness);
                    line.name = $"Edge_{node.Id}_to_{toId}";
                    _connectionViews.Add(line);
                }
            }

            if (connectionTemplate != null)
            {
                connectionTemplate.gameObject.SetActive(false);
            }
        }

        private GameObject CreateFallbackLine(Transform parent)
        {
            var go = new GameObject("Connection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapConnectionLineView));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            go.GetComponent<MapConnectionLineView>().WireImage(image);
            return go;
        }

        private void RefreshConnectionHighlights(MapGraph graph, RunState runState)
        {
            var pathSet = new HashSet<int>(runState.VisitedPath);
            var currentId = runState.CurrentNodeId;

            for (var i = 0; i < _connectionViews.Count; i++)
            {
                var line = _connectionViews[i];
                if (line == null)
                {
                    continue;
                }

                var parts = line.name.Split('_');
                if (parts.Length < 4 || !int.TryParse(parts[1], out var fromId) || !int.TryParse(parts[3], out var toId))
                {
                    continue;
                }

                var onVisitedPath = pathSet.Contains(fromId) && pathSet.Contains(toId);
                var isPreview = currentId >= 0 && fromId == currentId;

                var color = new Color(0.28f, 0.3f, 0.34f, 0.9f);
                var thickness = baseLineThickness;

                if (onVisitedPath)
                {
                    color = new Color(0.95f, 0.55f, 0.12f, 1f);
                    thickness = visitedLineThickness;
                }
                else if (isPreview)
                {
                    color = new Color(0.95f, 0.72f, 0.28f, 0.95f);
                    thickness = previewLineThickness;
                }

                var fromNode = graph.GetNode(fromId);
                var toNode = graph.GetNode(toId);
                if (fromNode != null && toNode != null)
                {
                    line.SetEndpoints(
                        NodeAnchoredPosition(fromNode),
                        NodeAnchoredPosition(toNode),
                        color,
                        thickness);
                }
            }
        }

        private IEnumerator ScrollToBottomDeferred()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }

            ApplyScrollNormalized(0f);
        }

        private IEnumerator ScrollToNodeDeferred(MapNodeData node)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyScrollToNode(node);
        }

        private void ApplyScrollToNode(MapNodeData node)
        {
            if (scrollRect?.content == null || scrollRect.viewport == null)
            {
                return;
            }

            var contentHeight = scrollRect.content.rect.height;
            var viewportHeight = scrollRect.viewport.rect.height;
            if (contentHeight <= viewportHeight + 1f)
            {
                ApplyScrollNormalized(0f);
                return;
            }

            var nodeY = NodeAnchoredPosition(node).y;
            var normalized = nodeY / (contentHeight - viewportHeight);
            ApplyScrollNormalized(Mathf.Clamp01(normalized * 0.85f));
        }

        private void ApplyScrollNormalized(float bottomNormalized)
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
            }

            if (scrollRect == null)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = bottomNormalized;
        }

        private void OnNodeClicked(MapNodeView view)
        {
            NodeClicked?.Invoke(view);
        }
    }
}
