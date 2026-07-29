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
        private static readonly Vector2 BottomAnchor = new Vector2(0.5f, 0f);
        private static readonly Color DefaultEdgeColor = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color VisitedEdgeColor = new Color(0.95f, 0.55f, 0.12f, 1f);
        private static readonly Color PreviewEdgeColor = new Color(0.95f, 0.72f, 0.28f, 0.95f);
        private static readonly Color FloorLabelColor = new Color(0.55f, 0.58f, 0.62f);
        private static Font s_floorLabelFont;

        [Header("Layers")]
        [SerializeField] private RectTransform connectionsLayer;
        [SerializeField] private RectTransform nodesLayer;
        [SerializeField] private RectTransform floorLabelsLayer;
        [SerializeField] private MapNodeView nodeTemplate;
        [SerializeField] private MapConnectionLineView connectionTemplate;

        [Header("Layout")]
        [SerializeField] private bool fitToViewport = true;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RunMapScrollDriver scrollDriver;

        [Header("Scroll follow")]
        [SerializeField] [Range(0.2f, 0.6f)] private float nodeViewportAnchor = 0.38f;

        [Header("Path visuals")]
        [SerializeField] private float baseLineThickness = 4f;
        [SerializeField] private float visitedLineThickness = 7f;
        [SerializeField] private float previewLineThickness = 5.5f;

        private readonly RunMapLayoutMetrics _layout = new RunMapLayoutMetrics();
        private readonly Dictionary<int, MapNodeView> _nodeViews = new Dictionary<int, MapNodeView>();
        private readonly List<MapConnectionLineView> _connectionViews = new List<MapConnectionLineView>();
        private readonly List<Text> _floorLabels = new List<Text>();

        private float _contentWidth;
        private float _contentHeight;
        private Coroutine _layoutCoroutine;
        private MapGraph _boundGraph;

        public IReadOnlyDictionary<int, MapNodeView> NodeViews => _nodeViews;

        /// <summary>Scroll viewport đã có kích thước — tránh build map khi layout = 0.</summary>
        public bool IsViewportReady
        {
            get
            {
                EnsureScrollRect();
                if (scrollRect?.viewport == null)
                {
                    return false;
                }

                var rect = scrollRect.viewport.rect;
                return rect.width > 10f && rect.height > 10f;
            }
        }

        public event System.Action<MapNodeView> NodeClicked;

        public void BuildMap(MapGraph graph)
        {
            _boundGraph = graph;
            if (nodeTemplate != null)
            {
                nodeTemplate.gameObject.SetActive(false);
            }

            ClearDynamicContent();

            EnsureScrollRect();
            _layout.SetProfile(graph.Profile);
            _layout.FitToViewport(scrollRect, fitToViewport);
            _layout.ComputeContentSize(out _contentWidth, out _contentHeight);
            ApplyContentRect();
            ResolveLayers();
            ConfigureLayers(_contentWidth, _contentHeight, connectionsLayer, nodesLayer, floorLabelsLayer);

            CreateFloorLabels();
            CreateConnections(graph);
            CreateNodes(graph);
            EnsureMapLayerOrder();

            Canvas.ForceUpdateCanvases();

            Debug.Log($"[Fractured Chorus] RunMapUIView built — nodes {_nodeViews.Count}, edges {_connectionViews.Count}.");

            StartLayoutCoroutine(ScrollToBottomDeferred());
        }

        public void RefreshInteraction(MapGraph graph, RunState runState)
        {
            var currentId = runState.CurrentNodeId;

            foreach (var pair in _nodeViews)
            {
                var node = graph.GetNode(pair.Key);
                if (node == null)
                {
                    continue;
                }

                pair.Value.RefreshVisual(
                    runState.CanSelectNode(graph, node),
                    runState.IsVisited(pair.Key),
                    pair.Key == currentId);
            }

            RefreshConnectionHighlights(runState);
        }

        public void ScrollToNode(MapNodeData node, bool immediate = false)
        {
            if (node == null)
            {
                return;
            }

            EnsureScrollDriver();
            scrollDriver?.ScrollToNormalized(ComputeNormalizedForNode(node), immediate);
        }

        public void ScrollToStartFloor(bool immediate = false)
        {
            StartLayoutCoroutine(ScrollToBottomDeferred(immediate));
        }

        private void EnsureScrollRect()
        {
            scrollRect ??= GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            var scrollImage = scrollRect.GetComponent<Image>();
            if (scrollImage != null)
            {
                scrollImage.raycastTarget = false;
            }

            if (scrollRect.viewport != null)
            {
                var viewportImage = scrollRect.viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.raycastTarget = true;
                }

                EnsureLayerScrollForwarder(floorLabelsLayer);
            }
        }

        private static void EnsureLayerScrollForwarder(RectTransform layer)
        {
            if (layer == null || layer.GetComponent<MapNodeScrollForwarder>() != null)
            {
                return;
            }

            layer.gameObject.AddComponent<MapNodeScrollForwarder>();
        }

        private void EnsureScrollDriver()
        {
            EnsureScrollRect();
            if (scrollRect == null)
            {
                return;
            }

            scrollDriver ??= scrollRect.GetComponent<RunMapScrollDriver>();
            if (scrollDriver == null)
            {
                scrollDriver = scrollRect.gameObject.AddComponent<RunMapScrollDriver>();
            }

            scrollDriver.ApplyScrollFeel();
        }

        private void StartLayoutCoroutine(IEnumerator routine)
        {
            if (_layoutCoroutine != null)
            {
                StopCoroutine(_layoutCoroutine);
            }

            _layoutCoroutine = StartCoroutine(routine);
        }

        private float ComputeNormalizedForNode(MapNodeData node)
        {
            if (scrollRect?.content == null || scrollRect.viewport == null)
            {
                return 0f;
            }

            var contentHeight = scrollRect.content.rect.height;
            var viewportHeight = scrollRect.viewport.rect.height;
            var scrollable = contentHeight - viewportHeight;
            if (scrollable <= 1f)
            {
                return 0f;
            }

            var nodeY = _layout.NodePosition(node).y;
            var targetOffset = nodeY - viewportHeight * nodeViewportAnchor;
            return Mathf.Clamp01(targetOffset / scrollable);
        }

        private void ApplyContentRect()
        {
            if (transform is not RectTransform rect)
            {
                return;
            }

            rect.pivot = BottomAnchor;
            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
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

                layer.anchorMin = BottomAnchor;
                layer.anchorMax = BottomAnchor;
                layer.pivot = BottomAnchor;
                layer.anchoredPosition = Vector2.zero;
                layer.sizeDelta = new Vector2(width, height);
            }
        }

        private void ClearDynamicContent()
        {
            if (_layoutCoroutine != null)
            {
                StopCoroutine(_layoutCoroutine);
                _layoutCoroutine = null;
            }

            scrollDriver?.StopScrollAnimation();

            _nodeViews.Clear();
            ClearNodeClones(nodesLayer);
            ClearConnectionClones(connectionsLayer);
            ClearConnectionClones(nodesLayer);
            _connectionViews.Clear();
            ClearFloorLabelClones(floorLabelsLayer);
            _floorLabels.Clear();
        }

        private void ClearNodeClones(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            for (var i = layer.childCount - 1; i >= 0; i--)
            {
                var child = layer.GetChild(i);
                var view = child.GetComponent<MapNodeView>();
                if (view == null || view == nodeTemplate)
                {
                    continue;
                }

                DestroyObject(child.gameObject);
            }
        }

        private void ClearFloorLabelClones(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            for (var i = layer.childCount - 1; i >= 0; i--)
            {
                DestroyObject(layer.GetChild(i).gameObject);
            }
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }

        private void CreateFloorLabels()
        {
            var labelX = _layout.FloorLabelX;
            var fontSize = _layout.FloorLabelFontSize;
            var floorCount = _boundGraph?.Profile.FloorCount ?? MapLayoutConstants.FloorCount;

            for (var floor = 1; floor <= floorCount; floor++)
            {
                CreateFloorLabel($"F{floor}", new Vector2(labelX, _layout.FloorPosition(floor).y), fontSize);
            }

            if (_boundGraph?.BossNode != null)
            {
                CreateFloorLabel(
                    $"F{_boundGraph.Profile.BossFloor}",
                    new Vector2(labelX, _layout.NodePosition(_boundGraph.BossNode).y),
                    fontSize);
            }
        }

        private void CreateFloorLabel(string text, Vector2 anchoredPos, int fontSize)
        {
            var parent = floorLabelsLayer != null ? floorLabelsLayer : transform;
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 20f);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = FloorLabelColor;
            label.alignment = TextAnchor.MiddleRight;
            label.font = s_floorLabelFont ??= UiFontCatalog.Body;
            label.raycastTarget = false;
            _floorLabels.Add(label);
        }

        private void CreateNodes(MapGraph graph)
        {
            if (nodeTemplate == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapUIView: NodeTemplate chưa gán — chạy Run Map → Setup Scene Hierarchy.");
                return;
            }

            if (graph == null || graph.Nodes.Count == 0)
            {
                Debug.LogError("[Fractured Chorus] RunMapUIView: MapGraph rỗng — kiểm tra MapTemplate / MapGenerator.");
                nodeTemplate.gameObject.SetActive(false);
                return;
            }

            var parent = nodesLayer != null ? nodesLayer : transform;

            foreach (var node in graph.Nodes)
            {
                var clone = InstantiateNodeFromTemplate(parent);
                if (clone == null)
                {
                    continue;
                }

                var view = clone.GetComponent<MapNodeView>();
                var rect = view.GetComponent<RectTransform>();

                rect.anchorMin = BottomAnchor;
                rect.anchorMax = BottomAnchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = _layout.NodePosition(node);
                var diameter = _layout.NodeVisualDiameter(node);
                rect.sizeDelta = new Vector2(diameter, diameter);

                view.Bind(node);
                view.Clicked -= OnNodeClicked;
                view.Clicked += OnNodeClicked;
                _nodeViews[node.Id] = view;
            }

            nodeTemplate.gameObject.SetActive(false);
        }

        private GameObject InstantiateNodeFromTemplate(Transform parent)
        {
            var clone = Instantiate(nodeTemplate.gameObject, parent);
            if (!clone.activeSelf)
            {
                clone.SetActive(true);
            }

            return clone;
        }

        private void CreateConnections(MapGraph graph)
        {
            var parent = ResolveConnectionsParent();

            foreach (var node in graph.Nodes)
            {
                var fromPos = _layout.NodePosition(node);

                foreach (var toId in node.Outgoing)
                {
                    var to = graph.GetNode(toId);
                    if (to == null)
                    {
                        continue;
                    }

                    var line = SpawnConnectionLine(parent);
                    line.BindEdge(node.Id, toId);
                    line.SetEndpoints(fromPos, _layout.NodePosition(to), DefaultEdgeColor, baseLineThickness);
                    _connectionViews.Add(line);
                }
            }

            connectionTemplate?.gameObject.SetActive(false);
        }

        private void ResolveLayers()
        {
            var content = transform as RectTransform;
            connectionsLayer ??= content?.Find("ConnectionsLayer") as RectTransform;
            nodesLayer ??= content?.Find("NodesLayer") as RectTransform;
            floorLabelsLayer ??= content?.Find("FloorLabelsLayer") as RectTransform;
        }

        private Transform ResolveConnectionsParent()
        {
            ResolveLayers();
            return connectionsLayer != null ? connectionsLayer : nodesLayer != null ? nodesLayer : transform;
        }

        private void EnsureMapLayerOrder()
        {
            ResolveLayers();
            var index = 0;
            if (connectionsLayer != null)
            {
                connectionsLayer.SetSiblingIndex(index++);
            }

            if (floorLabelsLayer != null)
            {
                floorLabelsLayer.SetSiblingIndex(index++);
            }

            nodesLayer?.SetAsLastSibling();
        }

        private void ClearConnectionClones(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            for (var i = layer.childCount - 1; i >= 0; i--)
            {
                var child = layer.GetChild(i);
                var line = child.GetComponent<MapConnectionLineView>();
                if (line != null && line != connectionTemplate)
                {
                    DestroyObject(child.gameObject);
                }
            }
        }

        private MapConnectionLineView SpawnConnectionLine(Transform parent)
        {
            if (connectionTemplate != null)
            {
                var clone = Instantiate(connectionTemplate.gameObject, parent);
                if (!clone.activeSelf)
                {
                    clone.SetActive(true);
                }

                return clone.GetComponent<MapConnectionLineView>();
            }

            var go = new GameObject("Connection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapConnectionLineView));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiCircleSpriteUtil.White;
            image.raycastTarget = false;
            var line = go.GetComponent<MapConnectionLineView>();
            line.WireImage(image);
            return line;
        }

        private void RefreshConnectionHighlights(RunState runState)
        {
            if (_boundGraph == null)
            {
                return;
            }

            var currentId = runState.CurrentNodeId;

            foreach (var line in _connectionViews)
            {
                if (line == null || line.FromNodeId < 0)
                {
                    continue;
                }

                var fromId = line.FromNodeId;
                var toId = line.ToNodeId;
                var onVisitedPath = runState.IsVisited(fromId) && runState.IsVisited(toId);
                var isPreview = currentId >= 0 && fromId == currentId;

                var color = DefaultEdgeColor;
                var thickness = baseLineThickness;

                if (onVisitedPath)
                {
                    color = VisitedEdgeColor;
                    thickness = visitedLineThickness;
                }
                else if (isPreview)
                {
                    color = PreviewEdgeColor;
                    thickness = previewLineThickness;
                }

                var fromNode = _boundGraph.GetNode(fromId);
                var toNode = _boundGraph.GetNode(toId);
                if (fromNode != null && toNode != null)
                {
                    line.SetEndpoints(
                        _layout.NodePosition(fromNode),
                        _layout.NodePosition(toNode),
                        color,
                        thickness);
                }
            }
        }

        private IEnumerator ScrollToBottomDeferred(bool immediate = false)
        {
            const int maxFrames = 12;
            for (var i = 0; i < maxFrames; i++)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (IsViewportReady)
                {
                    break;
                }
            }

            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }

            EnsureScrollDriver();
            scrollDriver?.ScrollToNormalized(0f, immediate, useInitialTiming: !immediate);
            _layoutCoroutine = null;
        }

        private void OnNodeClicked(MapNodeView view) => NodeClicked?.Invoke(view);
    }
}
