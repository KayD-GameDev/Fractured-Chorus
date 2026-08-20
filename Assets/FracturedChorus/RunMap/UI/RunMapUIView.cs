using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
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
        [SerializeField] private MapNodeTemplateSetSO templateSet;

        [Header("Icons")]
        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private Sprite playerMarkerSprite;

        [Header("Background")]
        [SerializeField] private VideoClip mapBackgroundVideo;
        [SerializeField] private Sprite mapBackgroundSprite;

        [Header("Player marker")]
        [SerializeField] private RunMapPlayerMarkerConfigSO playerMarkerConfig;
        [SerializeField] private RunMapPlayerMarkerView playerMarker;
        [SerializeField] private RectTransform playerMarkerLayer;

        [Header("Layout")]
        [SerializeField] private RunMapLayoutConfigSO layoutConfig;
        [SerializeField] private bool fitToViewport = true;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RunMapScrollDriver scrollDriver;

        [Header("Scroll follow")]
        [SerializeField] [Range(0.2f, 0.6f)] private float nodeViewportAnchor = 0.38f;
        [SerializeField] [Range(0.04f, 0.2f)] private float startViewportAnchor = 0.06f;

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
        private RunState _lastRunState;
        private int _selectedNodeId = -1;
        private int _markerPreviewNodeId = -1;
        private bool _scrollMarkerHooked;

        public int SelectedNodeId => _selectedNodeId;

        public IReadOnlyDictionary<int, MapNodeView> NodeViews => _nodeViews;

        public void SetSelectedNode(int nodeId)
        {
            _selectedNodeId = nodeId;
            if (_boundGraph != null && _lastRunState != null)
            {
                RefreshInteraction(_boundGraph, _lastRunState);
            }
        }

        public void ClearSelection()
        {
            SetSelectedNode(-1);
        }

        public bool IsMarkerTraveling => playerMarker != null && playerMarker.IsTraveling;

        public void SetMarkerPreviewNodeId(int nodeId)
        {
            _markerPreviewNodeId = nodeId;
        }

        public bool TryGetNodeView(int nodeId, out MapNodeView view) => _nodeViews.TryGetValue(nodeId, out view);
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

        public void EnsureEditModePlayerMarker()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || playerMarkerConfig == null)
            {
                return;
            }

            _layout.SetConfig(layoutConfig);
            _layout.SetProfile(MapGenerationProfile.Default);
            _layout.ComputeContentSize(out var width, out var height);
            EnsurePlayerMarkerLayer(width, height);
            if (playerMarker == null)
            {
                return;
            }

            playerMarker.SetVisible(true);
            var start = _boundGraph?.StartNode ?? new MapNodeData
            {
                Floor = 0,
                Column = (MapLayoutConstants.ColumnCount - 1) / 2,
                Type = MapNodeType.Start
            };
            playerMarker.SnapTo(MapContentToMarkerLayerPosition(_layout.NodePosition(start)));
            EnsurePlayerMarkerSorting();
            EnsureMapLayerOrder();
#endif
        }

        private void OnEnable()
        {
            EnsureScrollMarkerSync();
            StripLegacySceneTemplates();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += EnsureEditModePlayerMarkerDeferred;
            }
#endif
        }

        private void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollRectChanged);
            }

            _scrollMarkerHooked = false;
        }

        private void EnsureScrollMarkerSync()
        {
            EnsureScrollRect();
            if (scrollRect == null || _scrollMarkerHooked)
            {
                return;
            }

            scrollRect.onValueChanged.AddListener(OnScrollRectChanged);
            _scrollMarkerHooked = true;
        }

        private void OnScrollRectChanged(Vector2 _)
        {
            if (playerMarker == null || playerMarker.IsTraveling || _boundGraph == null || _lastRunState == null)
            {
                return;
            }

            if (IsMarkerLayerOnMapContent())
            {
                return;
            }

            RefreshPlayerMarker(_boundGraph, _lastRunState);
        }

        private bool IsMarkerLayerOnMapContent()
        {
            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : transform as RectTransform;
            return content != null && playerMarkerLayer != null && playerMarkerLayer.parent == content;
        }

        private Vector2 MapContentToMarkerLayerPosition(Vector2 mapContentLocal)
        {
            if (IsMarkerLayerOnMapContent())
            {
                return mapContentLocal;
            }

            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : transform as RectTransform;
            if (content == null || playerMarkerLayer == null)
            {
                return mapContentLocal;
            }

            var world = content.TransformPoint(mapContentLocal);
            return playerMarkerLayer.InverseTransformPoint(world);
        }

#if UNITY_EDITOR
        private void EnsureEditModePlayerMarkerDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            EnsureEditModePlayerMarker();
        }
#endif

        public void BuildMap(MapGraph graph)
        {
            _boundGraph = graph;
            StripLegacySceneTemplates();
            ClearDynamicContent();

            EnsureScrollRect();
            _layout.SetConfig(layoutConfig);
            _layout.SetProfile(graph.Profile);
            _layout.FitToViewport(scrollRect, fitToViewport);
            _layout.ComputeContentSize(out _contentWidth, out _contentHeight);
            ApplyContentRect();
            EnsureTemplateSet();
            ResolveLayers();
            EnsureTemplateSet();
            EnsurePlayerMarkerLayer(_contentWidth, _contentHeight);
            ConfigureLayers(_contentWidth, _contentHeight, connectionsLayer, nodesLayer, floorLabelsLayer, playerMarkerLayer);

            CreateFloorLabels();
            CreateConnections(graph);
            CreateNodes(graph);
            EnsurePlayerMarkerLayer(_contentWidth, _contentHeight);
            EnsureMapBackground();
            EnsureMapLayerOrder();
            HideLayoutPreviewForRuntime();

            if (graph.StartNode == null)
            {
                Debug.LogWarning("[Fractured Chorus] RunMapUIView: map thiếu Start node — kiểm tra MapGenerator.AttachStartNode.");
            }
            else if (!_nodeViews.ContainsKey(graph.StartNode.Id))
            {
                Debug.LogWarning($"[Fractured Chorus] RunMapUIView: Start node id {graph.StartNode.Id} không có view.");
            }

            Canvas.ForceUpdateCanvases();

            Debug.Log($"[Fractured Chorus] RunMapUIView built — nodes {_nodeViews.Count}, edges {_connectionViews.Count}.");

            EnsureScrollShowsStartOnOpen(true);
        }

        public void ScrollToStartNode(bool immediate = false)
        {
            StartLayoutCoroutine(ScrollToStartDeferred(immediate));
        }

        public void EnsureScrollShowsStartOnOpen(bool immediate = true)
        {
            StartLayoutCoroutine(ScrollToStartDeferred(immediate));
        }

        public void RefreshInteraction(MapGraph graph, RunState runState)
        {
            _lastRunState = runState;
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
                    pair.Key == currentId,
                    pair.Key == _selectedNodeId);
            }

            RefreshConnectionHighlights(runState);

            if (playerMarker == null || !playerMarker.IsTraveling)
            {
                RefreshPlayerMarker(graph, runState);
            }

            EnsureMapLayerOrder();
        }

        public void AnimateTravelToNode(MapNodeData from, MapNodeData to, System.Action onComplete)
        {
            EnsurePlayerMarkerConfig();
            EnsurePlayerMarkerLayer(_contentWidth, _contentHeight);
            if (playerMarker == null || playerMarkerConfig == null || from == null || to == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!_nodeViews.TryGetValue(from.Id, out _) || !_nodeViews.TryGetValue(to.Id, out _))
            {
                onComplete?.Invoke();
                return;
            }

            var fromPos = MapContentToMarkerLayerPosition(_layout.NodePosition(from));
            var toPos = MapContentToMarkerLayerPosition(_layout.NodePosition(to));
            playerMarker.SetVisible(true);
            playerMarker.PlayTravel(fromPos, toPos, onComplete);
        }

        public void SnapMarkerToNode(MapNodeData node)
        {
            EnsurePlayerMarkerConfig();
            EnsurePlayerMarkerLayer(_contentWidth, _contentHeight);
            if (playerMarker == null || node == null)
            {
                return;
            }

            playerMarker.StopTravel();
            playerMarker.SetVisible(true);
            playerMarker.SnapTo(MapContentToMarkerLayerPosition(_layout.NodePosition(node)));
        }

        public void RevealTraveledEdge(int fromId, int toId, System.Action onComplete)
        {
            MapConnectionLineView target = null;
            for (var i = 0; i < _connectionViews.Count; i++)
            {
                var line = _connectionViews[i];
                if (line != null && line.FromNodeId == fromId && line.ToNodeId == toId)
                {
                    target = line;
                    break;
                }
            }

            if (target == null || _boundGraph == null)
            {
                onComplete?.Invoke();
                return;
            }

            var fromNode = _boundGraph.GetNode(fromId);
            var toNode = _boundGraph.GetNode(toId);
            if (fromNode == null || toNode == null)
            {
                onComplete?.Invoke();
                return;
            }

            target.PlayReveal(
                _layout.NodePosition(fromNode),
                _layout.NodePosition(toNode),
                VisitedEdgeColor,
                visitedLineThickness,
                0.28f,
                onComplete);
        }

        private void RefreshPlayerMarker(MapGraph graph, RunState runState)
        {
            if (playerMarker == null || playerMarkerConfig == null || graph == null || runState == null)
            {
                return;
            }

            if (playerMarker.IsTraveling)
            {
                return;
            }

            var currentId = _markerPreviewNodeId >= 0 ? _markerPreviewNodeId : runState.CurrentNodeId;
            if (currentId < 0 || !_nodeViews.ContainsKey(currentId))
            {
                playerMarker.SetVisible(false);
                return;
            }

            var node = graph.GetNode(currentId);
            if (node == null)
            {
                playerMarker.SetVisible(false);
                return;
            }

            playerMarker.SetVisible(true);
            playerMarker.SnapTo(MapContentToMarkerLayerPosition(_layout.NodePosition(node)));
        }

        private void EnsurePlayerMarkerLayer(float width, float height)
        {
            EnsurePlayerMarkerConfig();
            if (playerMarkerConfig == null)
            {
                return;
            }

            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : transform as RectTransform;
            if (content == null)
            {
                return;
            }

            ConsolidatePlayerMarkerLayers(content);

            if (playerMarkerLayer == null)
            {
                playerMarkerLayer = content.Find("PlayerMarkerLayer") as RectTransform;
            }

            if (playerMarkerLayer == null)
            {
                var go = new GameObject("PlayerMarkerLayer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                playerMarkerLayer = go.GetComponent<RectTransform>();
            }
            else if (playerMarkerLayer.parent != content)
            {
                playerMarkerLayer.SetParent(content, false);
            }

            ApplyContentLayerRect(playerMarkerLayer, width, height);
            RemoveNestedCanvas(playerMarkerLayer);
            MigrateLegacyMarkerOnLayer();
            playerMarker = EnsureRenMarkerOnLayer(playerMarkerLayer, playerMarker);
            if (playerMarker == null)
            {
                playerMarker = playerMarkerLayer.GetComponentInChildren<RunMapPlayerMarkerView>(true);
            }

            playerMarker?.Configure(playerMarkerConfig);
            if (playerMarker != null && playerMarkerConfig != null)
            {
                playerMarker.SetVisible(true);
            }

            EnsureMapLayerOrder();
            EnsureScrollMarkerSync();
        }

        private void EnsurePlayerMarkerConfig()
        {
            if (playerMarkerConfig != null)
            {
                return;
            }

            if (playerMarker != null)
            {
                playerMarkerConfig = playerMarker.GetConfig();
            }

            if (playerMarkerConfig != null)
            {
                return;
            }

            playerMarkerConfig = Resources.Load<RunMapPlayerMarkerConfigSO>("RunMapPlayerMarker_Default");
#if UNITY_EDITOR
            if (playerMarkerConfig == null)
            {
                playerMarkerConfig = AssetDatabase.LoadAssetAtPath<RunMapPlayerMarkerConfigSO>(
                    RunMapPlayerMarkerConfigSO.DefaultAssetPath);
            }
#endif
        }

        private void ConsolidatePlayerMarkerLayers(RectTransform content)
        {
            EnsureScrollRect();
            var layers = new List<RectTransform>();
            CollectPlayerMarkerLayers(content, layers);
            var viewport = scrollRect != null ? scrollRect.viewport : null;
            if (viewport != null && viewport != content)
            {
                CollectPlayerMarkerLayers(viewport, layers);
            }

            if (layers.Count == 0)
            {
                return;
            }

            RectTransform keep = null;
            if (playerMarkerLayer != null && layers.Contains(playerMarkerLayer))
            {
                keep = playerMarkerLayer;
            }

            foreach (var layer in layers)
            {
                if (layer.parent == content)
                {
                    keep ??= layer;
                    break;
                }
            }

            keep ??= layers[0];
            playerMarkerLayer = keep;

            foreach (var layer in layers)
            {
                if (layer == keep)
                {
                    continue;
                }

                var ren = layer.Find("RenMarker");
                if (ren != null && keep.Find("RenMarker") == null)
                {
                    ren.SetParent(keep, false);
                }

                DestroyObject(layer.gameObject);
            }
        }

        private static void CollectPlayerMarkerLayers(Transform root, List<RectTransform> results)
        {
            if (root == null)
            {
                return;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "PlayerMarkerLayer")
                {
                    results.Add(child as RectTransform);
                }
            }
        }

        private void EnsurePlayerMarkerSorting()
        {
            if (playerMarkerLayer == null)
            {
                return;
            }

            RemoveStaleRenMarkerPreview();
            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : transform as RectTransform;
            if (content != null && playerMarkerLayer.parent != content)
            {
                playerMarkerLayer.SetParent(content, false);
            }

            playerMarkerLayer.SetAsLastSibling();
            RemoveNestedCanvas(playerMarkerLayer);
        }

        private void RemoveNestedCanvas(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            var nestedCanvas = layer.GetComponent<Canvas>();
            if (nestedCanvas == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(nestedCanvas);
            }
            else
#endif
            {
                Destroy(nestedCanvas);
            }
        }

        private void RemoveStaleRenMarkerPreview()
        {
            var previewRoot = transform.Find("LayoutPreviewRoot");
            if (previewRoot == null)
            {
                return;
            }

            var stale = previewRoot.Find("RenMarkerPreview");
            if (stale == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(stale.gameObject);
                return;
            }
#endif
            Destroy(stale.gameObject);
        }

        private void HideLayoutPreviewForRuntime()
        {
            var preview = GetComponent<RunMapLayoutScenePreview>();
            if (preview != null)
            {
                preview.SuppressForRuntimeMap();
            }

            var root = transform.Find("LayoutPreviewRoot");
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }


        private static RunMapPlayerMarkerView EnsureRenMarkerOnLayer(
            RectTransform layer,
            RunMapPlayerMarkerView existing)
        {
            if (layer == null)
            {
                return null;
            }

            var renTransform = layer.Find("RenMarker");
            if (renTransform == null)
            {
                var go = new GameObject(
                    "RenMarker",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(RunMapPlayerMarkerView));
                go.transform.SetParent(layer, false);
                renTransform = go.transform;
            }
            else
            {
                if (renTransform.GetComponent<Image>() == null)
                {
                    renTransform.gameObject.AddComponent<Image>();
                }

                if (renTransform.GetComponent<RunMapPlayerMarkerView>() == null)
                {
                    renTransform.gameObject.AddComponent<RunMapPlayerMarkerView>();
                }
            }

            var marker = renTransform.GetComponent<RunMapPlayerMarkerView>();
            var duplicateViews = renTransform.GetComponents<RunMapPlayerMarkerView>();
            for (var i = 1; i < duplicateViews.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(duplicateViews[i]);
                }
                else
#endif
                {
                    Destroy(duplicateViews[i]);
                }
            }

            return marker != null ? marker : existing;
        }

        private void MigrateLegacyMarkerOnLayer()
        {
            if (playerMarkerLayer == null || playerMarkerLayer.Find("RenMarker") != null)
            {
                return;
            }

            var legacyView = playerMarkerLayer.GetComponent<RunMapPlayerMarkerView>();
            var legacyImage = playerMarkerLayer.GetComponent<Image>();
            if (legacyView == null && legacyImage == null)
            {
                return;
            }

            var renGo = new GameObject(
                "RenMarker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RunMapPlayerMarkerView));
            renGo.transform.SetParent(playerMarkerLayer, false);
            var renRect = renGo.GetComponent<RectTransform>();
            renRect.anchorMin = new Vector2(0.5f, 0f);
            renRect.anchorMax = new Vector2(0.5f, 0f);
            renRect.pivot = new Vector2(0.5f, 0f);
            renRect.anchoredPosition = legacyView != null
                ? playerMarkerLayer.anchoredPosition
                : Vector2.zero;
            renRect.sizeDelta = legacyImage != null
                ? playerMarkerLayer.sizeDelta
                : playerMarkerConfig != null
                    ? playerMarkerConfig.MarkerSize
                    : new Vector2(72f, 96f);

            var renImage = renGo.GetComponent<Image>();
            if (legacyImage != null)
            {
                renImage.sprite = legacyImage.sprite;
                renImage.color = legacyImage.color;
                renImage.preserveAspect = legacyImage.preserveAspect;
                renImage.raycastTarget = false;
            }

            var renView = renGo.GetComponent<RunMapPlayerMarkerView>();
            if (legacyView != null)
            {
                renView.Configure(legacyView.GetConfig());
            }

            DestroyLegacyMarkerComponent(legacyView);
            DestroyLegacyMarkerComponent(legacyImage);
        }

        private static void DestroyLegacyMarkerComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(component);
                return;
            }

#if UNITY_EDITOR
            Object.DestroyImmediate(component);
#endif
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
                scrollImage.raycastTarget = true;
                if (scrollImage.color.a <= 0.001f)
                {
                    scrollImage.color = new Color(1f, 1f, 1f, 0.001f);
                }
            }

            if (scrollRect.viewport != null)
            {
                var viewportImage = scrollRect.viewport.GetComponent<Image>();
                if (viewportImage == null)
                {
                    viewportImage = scrollRect.viewport.gameObject.AddComponent<Image>();
                    viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
                }

                viewportImage.raycastTarget = true;

                if (scrollRect.viewport.GetComponent<MapNodeScrollForwarder>() == null)
                {
                    scrollRect.viewport.gameObject.AddComponent<MapNodeScrollForwarder>();
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

        private float ComputeBottomScrollNormalized()
        {
            if (scrollRect?.content == null || scrollRect.viewport == null)
            {
                return 0f;
            }

            var scrollable = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
            if (scrollable <= 1f)
            {
                return 0f;
            }

            return Mathf.Clamp01(_layout.ViewportBottomGutter / scrollable);
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
            var anchor = node.Type == MapNodeType.Start ? startViewportAnchor : nodeViewportAnchor;
            var targetOffset = nodeY - viewportHeight * anchor + _layout.ViewportBottomGutter;
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
                ApplyContentLayerRect(layer, width, height);
            }
        }

        private static void ApplyContentLayerRect(RectTransform layer, float width, float height)
        {
            if (layer == null)
            {
                return;
            }

            layer.anchorMin = BottomAnchor;
            layer.anchorMax = BottomAnchor;
            layer.pivot = BottomAnchor;
            layer.anchoredPosition = Vector2.zero;
            layer.sizeDelta = new Vector2(width, height);
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
            _selectedNodeId = -1;
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
                if (view == null)
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
            if (graph == null || graph.Nodes.Count == 0)
            {
                Debug.LogError("[Fractured Chorus] RunMapUIView: MapGraph rỗng — kiểm tra MapTemplate / MapGenerator.");
                return;
            }

            var parent = nodesLayer != null ? nodesLayer : transform;
            var icons = ResolveIconSet();

            foreach (var node in graph.Nodes)
            {
                var view = InstantiateNode(node.Type, parent);
                if (view == null)
                {
                    continue;
                }

                var rect = view.GetComponent<RectTransform>();
                rect.anchorMin = BottomAnchor;
                rect.anchorMax = BottomAnchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = _layout.NodePosition(node);
                var diameter = _layout.NodeVisualDiameter(node);
                rect.sizeDelta = new Vector2(diameter, diameter);

                view.Configure(icons, graph.Profile.Sector);
                view.Bind(node);
                view.Clicked -= OnNodeClicked;
                view.Clicked += OnNodeClicked;
                _nodeViews[node.Id] = view;
            }
        }

        private MapNodeView InstantiateNode(MapNodeType type, Transform parent)
        {
            var prefab = ResolveNodePrefab(type);
            if (prefab == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapUIView: MapNode prefab chưa gán — gán MapNodeTemplateSet.");
                return null;
            }

            var clone = Instantiate(prefab, parent);
            clone.gameObject.SetActive(true);
            return clone;
        }

        private MapNodeView ResolveNodePrefab(MapNodeType type)
        {
            EnsureTemplateSet();
            return templateSet != null ? templateSet.ResolveNodePrefab(type) : null;
        }

        private MapNodeIconSetSO ResolveIconSet()
        {
            if (iconSet != null)
            {
                return iconSet;
            }

            EnsureTemplateSet();
            return templateSet != null ? templateSet.IconSet : null;
        }

        private void EnsureTemplateSet()
        {
            if (templateSet != null)
            {
                return;
            }

            templateSet = Resources.Load<MapNodeTemplateSetSO>("MapNodeTemplateSet_Default");
#if UNITY_EDITOR
            templateSet = AssetDatabase.LoadAssetAtPath<MapNodeTemplateSetSO>(MapNodeTemplateSetSO.DefaultAssetPath);
            if (templateSet != null && !Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private void StripLegacySceneTemplates()
        {
            ResolveLayers();
            DestroyNamedChild(nodesLayer, "NodeTemplate");
            DestroyNamedChild(connectionsLayer, "ConnectionTemplate");
        }

        private static void DestroyNamedChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return;
            }

            var child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            DestroyObject(child.gameObject);
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
                    if (line == null)
                    {
                        continue;
                    }

                    line.BindEdge(node.Id, toId);
                    line.SetEndpoints(fromPos, _layout.NodePosition(to), DefaultEdgeColor, baseLineThickness);
                    _connectionViews.Add(line);
                }
            }
        }

        private void ResolveLayers()
        {
            var content = transform as RectTransform;
            connectionsLayer ??= content?.Find("ConnectionsLayer") as RectTransform;
            nodesLayer ??= content?.Find("NodesLayer") as RectTransform;
            floorLabelsLayer ??= content?.Find("FloorLabelsLayer") as RectTransform;
            if (playerMarkerLayer == null)
            {
                EnsureScrollRect();
                var mapContent = scrollRect != null ? scrollRect.content : content;
                playerMarkerLayer = mapContent?.Find("PlayerMarkerLayer") as RectTransform
                    ?? content?.Find("PlayerMarkerLayer") as RectTransform;
            }
        }

        private Transform ResolveConnectionsParent()
        {
            ResolveLayers();
            return connectionsLayer != null ? connectionsLayer : nodesLayer != null ? nodesLayer : transform;
        }

        private void EnsureMapLayerOrder()
        {
            ResolveLayers();
            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : null;
            if (content == null)
            {
                return;
            }

            var backgroundLayer = content.Find("BackgroundLayer");
            var layoutPreviewRoot = content.Find("LayoutPreviewRoot");

            var index = 0;
            if (backgroundLayer != null)
            {
                backgroundLayer.SetSiblingIndex(index++);
            }

            if (layoutPreviewRoot != null)
            {
                layoutPreviewRoot.SetSiblingIndex(index++);
            }

            if (connectionsLayer != null)
            {
                connectionsLayer.SetSiblingIndex(index++);
            }

            if (floorLabelsLayer != null)
            {
                floorLabelsLayer.SetSiblingIndex(index++);
            }

            if (nodesLayer != null)
            {
                nodesLayer.SetSiblingIndex(index++);
            }

            if (playerMarkerLayer != null)
            {
                playerMarkerLayer.SetSiblingIndex(index++);
            }

            EnsurePlayerMarkerSorting();
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
                if (line != null)
                {
                    DestroyObject(child.gameObject);
                }
            }
        }

        private MapConnectionLineView SpawnConnectionLine(Transform parent)
        {
            EnsureTemplateSet();
            var prefab = templateSet != null ? templateSet.ConnectionPrefab : null;
            if (prefab != null)
            {
                var clone = Instantiate(prefab, parent);
                clone.gameObject.SetActive(true);
                return clone;
            }

            var go = new GameObject(
                "MapConnection",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MapConnectionLineView));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
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

        private IEnumerator ScrollToStartDeferred(bool immediate = false)
        {
            const int maxFrames = 24;
            for (var i = 0; i < maxFrames; i++)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (IsViewportReady && i >= 2)
                {
                    break;
                }
            }

            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }

            if (scrollRect?.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);
            }

            Canvas.ForceUpdateCanvases();
            yield return null;

            EnsureScrollDriver();
            var target = _boundGraph?.StartNode != null
                ? ComputeNormalizedForNode(_boundGraph.StartNode)
                : ComputeBottomScrollNormalized();
            scrollDriver?.ScrollToNormalized(target, immediate, useInitialTiming: !immediate);
            _layoutCoroutine = null;
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
            scrollDriver?.ScrollToNormalized(ComputeBottomScrollNormalized(), immediate, useInitialTiming: !immediate);
            _layoutCoroutine = null;
        }

        private void EnsureMapBackground()
        {
            EnsureScrollRect();
            var content = scrollRect != null ? scrollRect.content : null;
            if (content == null)
            {
                return;
            }

            var legacyOnScroll = scrollRect.transform.Find("BackgroundLayer");
            if (legacyOnScroll != null && legacyOnScroll.parent != content)
            {
                legacyOnScroll.SetParent(content, false);
            }

            var bgTransform = content.Find("BackgroundLayer");
            if (bgTransform == null)
            {
                var go = new GameObject("BackgroundLayer", typeof(RectTransform), typeof(RunMapBackgroundView));
                go.transform.SetParent(content, false);
                bgTransform = go.transform;
            }
            else if (bgTransform.parent != content)
            {
                bgTransform.SetParent(content, false);
            }

            var width = _contentWidth > 1f ? _contentWidth : content.rect.width;
            var height = _contentHeight > 1f ? _contentHeight : content.rect.height;
            if (bgTransform is RectTransform bgRect)
            {
                ApplyContentLayerRect(bgRect, width, height);
            }

            bgTransform.SetAsFirstSibling();

            if (bgTransform.GetComponent<RunMapBackgroundView>() == null)
            {
                bgTransform.gameObject.AddComponent<RunMapBackgroundView>();
            }

            var backgroundView = bgTransform.GetComponent<RunMapBackgroundView>();
            backgroundView?.SyncContentRect(width, height);
            backgroundView?.Configure(mapBackgroundSprite, mapBackgroundVideo);

            var scrollImage = scrollRect.GetComponent<Image>();
            if (scrollImage != null)
            {
                var useVideo = mapBackgroundVideo != null;
                scrollImage.color = useVideo
                    ? new Color(0.04f, 0.05f, 0.07f, 0.1f)
                    : new Color(0.04f, 0.05f, 0.07f, 0.22f);
            }
        }

        private void OnNodeClicked(MapNodeView view) => NodeClicked?.Invoke(view);
    }

    [ExecuteAlways]
    public sealed class RunMapPlayerMarkerView : MonoBehaviour
    {
        private static readonly Vector2 MarkerBottomAnchor = new Vector2(0.5f, 0f);

        [SerializeField] private Image markerImage;
        [SerializeField] private RunMapPlayerMarkerConfigSO config;

        private RectTransform _rect;
        private Coroutine _travelRoutine;
        private bool _visible = true;

        public bool IsTraveling => _travelRoutine != null;

        public void StopTravel()
        {
            if (_travelRoutine == null)
            {
                return;
            }

            StopCoroutine(_travelRoutine);
            _travelRoutine = null;
        }

        public RunMapPlayerMarkerConfigSO GetConfig() => config;

        private void Awake()
        {
            EnsureImage();
            ApplyConfig();
        }

        private void OnEnable()
        {
            EnsureImage();
            ApplyConfig();
        }

        public void Configure(RunMapPlayerMarkerConfigSO markerConfig)
        {
            config = markerConfig;
            ApplyConfig();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (markerImage != null)
            {
                markerImage.enabled = visible && config != null;
            }
        }

        public void SnapTo(Vector2 nodePosition)
        {
            EnsureImage();
            if (_rect == null)
            {
                return;
            }

            _rect.anchoredPosition = nodePosition + (config != null ? config.FootOffset : Vector2.zero);
            BringToFront();
            if (markerImage != null && config != null)
            {
                markerImage.sprite = config.IdleSprite;
                markerImage.color = Color.white;
                markerImage.rectTransform.localRotation = Quaternion.identity;
                markerImage.rectTransform.localScale = Vector3.one;
            }
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
            if (transform.parent != null)
            {
                transform.parent.SetAsLastSibling();
            }
        }

        public void PlayTravel(Vector2 fromNode, Vector2 toNode, System.Action onComplete)
        {
            EnsureImage();
            if (_travelRoutine != null)
            {
                StopCoroutine(_travelRoutine);
            }

            if (config == null || markerImage == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                SnapTo(toNode);
                onComplete?.Invoke();
                return;
            }

            _travelRoutine = StartCoroutine(TravelRoutine(fromNode, toNode, onComplete));
            if (_travelRoutine == null)
            {
                SnapTo(toNode);
                onComplete?.Invoke();
            }
        }

        private IEnumerator TravelRoutine(Vector2 fromNode, Vector2 toNode, System.Action onComplete)
        {
            SetVisible(true);
            BringToFront();
            markerImage.sprite = config.TravelSprite;

            var offset = config.FootOffset;
            var start = fromNode + offset;
            var end = toNode + offset;
            var duration = config.TravelDuration;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutQuad(t);
                var pos = Vector2.Lerp(start, end, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * config.JumpHeight;
                _rect.anchoredPosition = pos;

                markerImage.rectTransform.localRotation = Quaternion.identity;
                markerImage.rectTransform.localScale = Vector3.one;

                yield return null;
            }

            SnapTo(toNode);
            _travelRoutine = null;
            onComplete?.Invoke();
        }

        private void ApplyConfig()
        {
            EnsureImage();
            if (markerImage == null || config == null || _rect == null)
            {
                return;
            }

            markerImage.sprite = config.IdleSprite;
            _rect.sizeDelta = config.MarkerSize;
            markerImage.enabled = _visible;
            markerImage.pixelsPerUnitMultiplier = 1f;
            markerImage.color = Color.white;
            ApplyRendererSettings();
        }

        private void ApplyRendererSettings()
        {
            if (markerImage == null)
            {
                return;
            }

            markerImage.material = null;
            markerImage.maskable = false;
            markerImage.color = new Color(1f, 1f, 1f, 1f);

            var renderer = markerImage.GetComponent<CanvasRenderer>();
            if (renderer != null)
            {
                renderer.cullTransparentMesh = false;
                renderer.SetAlpha(1f);
            }
        }

        private void EnsureImage()
        {
            _rect ??= GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _rect.anchorMin = MarkerBottomAnchor;
            _rect.anchorMax = MarkerBottomAnchor;
            _rect.pivot = new Vector2(0.5f, 0f);
            _rect.localScale = Vector3.one;

            markerImage ??= GetComponent<Image>();
            if (markerImage == null)
            {
                markerImage = gameObject.AddComponent<Image>();
            }

            markerImage.raycastTarget = false;
            markerImage.preserveAspect = true;
            markerImage.type = Image.Type.Simple;
            ApplyRendererSettings();
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }
}
