using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RunMapLayoutScenePreview : MonoBehaviour
    {
        private static readonly Vector2 BottomAnchor = new Vector2(0.5f, 0f);
        private static readonly Color PreviewEdgeColor = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color FloorLabelColor = new Color(0.55f, 0.58f, 0.62f);
        private const float PreviewEdgeThickness = 4f;
        private const HideFlags PreviewHideFlags = HideFlags.DontSave;
        private const string MapTemplateAssetPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapTemplate_Default.asset";

        [SerializeField] private RunMapLayoutConfigSO layoutConfig;
        [SerializeField] private MapTemplateSO mapTemplate;
        [SerializeField] private MapNodeTemplateSetSO templateSet;
        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private RunMapPlayerMarkerConfigSO playerMarkerConfig;
        [SerializeField] private RunMapUIView mapView;

        private RectTransform _root;
        private RectTransform _connectionsRoot;
        private RectTransform _labelsRoot;
        private RectTransform _nodesRoot;
        private readonly RunMapLayoutMetrics _metrics = new RunMapLayoutMetrics();
        private bool _runtimeSuppressed;

        public void SuppressForRuntimeMap()
        {
            _runtimeSuppressed = true;
            ClearPreview();
            DeactivateRoot();
        }

        public void AllowScenePreview()
        {
            _runtimeSuppressed = false;
            Rebuild();
        }

        private void DeactivateRoot()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public void Configure(
            RunMapLayoutConfigSO layout,
            MapNodeIconSetSO icons,
            RunMapPlayerMarkerConfigSO marker,
            RunMapUIView view,
            MapNodeTemplateSetSO templates = null,
            MapTemplateSO template = null)
        {
            layoutConfig = layout;
            iconSet = icons;
            playerMarkerConfig = marker;
            mapView = view;
            if (templates != null)
            {
                templateSet = templates;
            }

            if (template != null)
            {
                mapTemplate = template;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            if (!Application.isPlaying)
            {
                _runtimeSuppressed = false;
            }

            if (layoutConfig == null || !layoutConfig.ShowLayoutPreviewInScene || _runtimeSuppressed)
            {
                ClearPreview();
                DeactivateRoot();
                mapView?.EnsureEditModePlayerMarker();
                return;
            }

            if (Application.isPlaying)
            {
                ClearPreview();
                DeactivateRoot();
                return;
            }

#if UNITY_EDITOR
            ResolveReferences();
#endif
            if (HasRuntimeNodeClones())
            {
                ClearPreview();
                DeactivateRoot();
                mapView?.EnsureEditModePlayerMarker();
                return;
            }

            EnsureTemplateSet();
            var template = ResolveMapTemplate();
            var seed = template != null ? template.defaultSeed : 42;
            var graph = GeneratePreviewGraph(template, seed);
            if (graph == null || graph.Nodes.Count == 0)
            {
                ClearPreview();
                DeactivateRoot();
                return;
            }

            _metrics.SetConfig(layoutConfig);
            _metrics.SetProfile(graph.Profile);
            _metrics.ResetToDefaults();
            SyncContentSize();

            EnsureRoot();
            if (_root != null)
            {
                _root.gameObject.hideFlags = PreviewHideFlags;
                _root.gameObject.SetActive(true);
            }

            ClearChildren(_connectionsRoot);
            ClearChildren(_labelsRoot);
            ClearChildren(_nodesRoot);

            foreach (var node in graph.Nodes)
            {
                foreach (var toId in node.Outgoing)
                {
                    var to = graph.GetNode(toId);
                    if (to == null)
                    {
                        continue;
                    }

                    SpawnPreviewConnection(
                        _metrics.NodePosition(node),
                        _metrics.NodePosition(to));
                }
            }

            PlaceFloorLabels(graph);

            var icons = iconSet != null ? iconSet : templateSet != null ? templateSet.IconSet : null;
            foreach (var node in graph.Nodes)
            {
                PlaceGraphNode(node, icons, graph.Profile.Sector);
            }

            mapView?.EnsureEditModePlayerMarker();
        }

        private bool HasRuntimeNodeClones()
        {
            mapView ??= GetComponent<RunMapUIView>();
            if (mapView == null)
            {
                return false;
            }

            var nodesLayer = mapView.transform.Find("NodesLayer");
            if (nodesLayer == null)
            {
                return false;
            }

            for (var i = 0; i < nodesLayer.childCount; i++)
            {
                var child = nodesLayer.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                if (child.GetComponent<MapNodeView>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceGraphNode(MapNodeData node, MapNodeIconSetSO icons, PinkySectorId sector)
        {
            var pos = _metrics.NodePosition(node);
            var diameter = _metrics.NodeVisualDiameter(node);
            var label = $"{node.Type}_F{node.Floor}_C{node.Column}";
            var prefab = templateSet != null ? templateSet.ResolveNodePrefab(node.Type) : null;
            if (prefab != null)
            {
                var view = InstantiatePreview(prefab, _nodesRoot);
                if (view != null)
                {
                    view.gameObject.name = label;
                    view.gameObject.hideFlags = PreviewHideFlags;
                    view.Configure(icons, sector);
                    view.Bind(node);
                    var button = view.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = false;
                    }

                    ApplyPreviewRect(view.GetComponent<RectTransform>(), pos, diameter);
                    return;
                }
            }

            CreateFallbackGhost(label, pos, diameter, node.Type, node.IsBoss);
        }

        private void SpawnPreviewConnection(Vector2 from, Vector2 to)
        {
            var prefab = templateSet != null ? templateSet.ConnectionPrefab : null;
            if (prefab != null)
            {
                var line = InstantiatePreview(prefab, _connectionsRoot);
                if (line != null)
                {
                    line.gameObject.hideFlags = PreviewHideFlags;
                    line.gameObject.SetActive(true);
                    line.SetEndpoints(from, to, PreviewEdgeColor, PreviewEdgeThickness);
                    return;
                }
            }

            var go = new GameObject("Connection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapConnectionLineView));
            go.hideFlags = PreviewHideFlags;
            go.transform.SetParent(_connectionsRoot, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            var lineView = go.GetComponent<MapConnectionLineView>();
            lineView.WireImage(image);
            lineView.SetEndpoints(from, to, PreviewEdgeColor, PreviewEdgeThickness);
        }

        private static T InstantiatePreview<T>(T prefab, Transform parent) where T : Component
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
                if (instance == null)
                {
                    return Instantiate(prefab, parent);
                }

                return instance.GetComponent<T>();
            }
#endif
            return Instantiate(prefab, parent);
        }

        private static MapGraph GeneratePreviewGraph(MapTemplateSO template, int seed)
        {
            if (template == null)
            {
                return MapGenerator.Generate(seed);
            }

            var profile = new MapGenerationProfile
            {
                ColumnCount = Mathf.Max(1, template.columnCount),
                FloorCount = Mathf.Max(1, template.floorCount),
                BossFloor = Mathf.Max(template.floorCount + 1, template.bossFloor),
                PathCount = Mathf.Max(1, template.pathCount)
            };
            var weights = NodeTypeAssigner.WeightsFromTemplate(template);
            return MapGenerator.Generate(seed, profile, weights, template.pathCount);
        }

        private void PlaceFloorLabels(MapGraph graph)
        {
            var labelX = _metrics.FloorLabelX;
            var fontSize = _metrics.FloorLabelFontSize;
            var floorCount = graph.Profile.FloorCount;
            for (var floor = 1; floor <= floorCount; floor++)
            {
                CreateFloorLabel($"F{floor}", new Vector2(labelX, _metrics.FloorPosition(floor).y), fontSize);
            }

            if (graph.BossNode != null)
            {
                CreateFloorLabel(
                    $"F{graph.Profile.BossFloor}",
                    new Vector2(labelX, _metrics.NodePosition(graph.BossNode).y),
                    fontSize);
            }
        }

        private void CreateFloorLabel(string text, Vector2 anchoredPos, int fontSize)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.hideFlags = PreviewHideFlags;
            go.transform.SetParent(_labelsRoot, false);

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
            label.font = UiFontCatalog.Body;
            label.raycastTarget = false;
        }

        private void CreateFallbackGhost(string label, Vector2 pos, float diameter, MapNodeType type, bool isBoss)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.hideFlags = PreviewHideFlags;
            go.transform.SetParent(_nodesRoot, false);
            ApplyPreviewRect(go.GetComponent<RectTransform>(), pos, diameter);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var sprite = iconSet != null ? iconSet.Resolve(type, isBoss, PinkySectorId.Pulse) : null;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = UiCircleSpriteUtil.Circle;
                image.color = MapNodePalette.FillColor(type);
            }
        }

        private static void ApplyPreviewRect(RectTransform rect, Vector2 pos, float diameter)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(diameter, diameter);
        }

        private void EnsureTemplateSet()
        {
            if (templateSet == null)
            {
#if UNITY_EDITOR
                templateSet = AssetDatabase.LoadAssetAtPath<MapNodeTemplateSetSO>(MapNodeTemplateSetSO.DefaultAssetPath);
#endif
            }

            if (iconSet == null && templateSet != null)
            {
                iconSet = templateSet.IconSet;
            }
        }

        private MapTemplateSO ResolveMapTemplate()
        {
            if (mapTemplate != null)
            {
                return mapTemplate;
            }

            var bootstrap = GetComponentInParent<RunMapBootstrap>(true);
            if (bootstrap == null)
            {
                bootstrap = FindAnyObjectByType<RunMapBootstrap>(FindObjectsInactive.Include);
            }

            if (bootstrap != null && bootstrap.Template != null)
            {
                mapTemplate = bootstrap.Template;
                return mapTemplate;
            }

#if UNITY_EDITOR
            mapTemplate = AssetDatabase.LoadAssetAtPath<MapTemplateSO>(MapTemplateAssetPath);
#endif
            return mapTemplate;
        }

        private void SyncContentSize()
        {
            var content = transform as RectTransform;
            if (content == null)
            {
                return;
            }

            _metrics.ComputeContentSize(out var width, out var height);
            content.sizeDelta = new Vector2(width, height);
            if (_root != null)
            {
                _root.sizeDelta = new Vector2(width, height);
            }
        }

        private void EnsureRoot()
        {
            if (_root == null)
            {
                var existing = transform.Find("LayoutPreviewRoot") as RectTransform;
                if (existing != null)
                {
                    _root = existing;
                }
                else
                {
                    var go = new GameObject("LayoutPreviewRoot", typeof(RectTransform));
                    go.hideFlags = PreviewHideFlags;
                    go.transform.SetParent(transform, false);
                    _root = go.GetComponent<RectTransform>();
                }
            }

            _root.anchorMin = BottomAnchor;
            _root.anchorMax = BottomAnchor;
            _root.pivot = BottomAnchor;
            _root.anchoredPosition = Vector2.zero;
            _root.gameObject.hideFlags = PreviewHideFlags;

            _connectionsRoot = EnsureChildLayer(_root, "PreviewConnections");
            _labelsRoot = EnsureChildLayer(_root, "PreviewLabels");
            _nodesRoot = EnsureChildLayer(_root, "PreviewNodes");
            _connectionsRoot.SetSiblingIndex(0);
            _labelsRoot.SetSiblingIndex(1);
            _nodesRoot.SetSiblingIndex(2);
        }

        private static RectTransform EnsureChildLayer(RectTransform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                existing.anchorMin = BottomAnchor;
                existing.anchorMax = BottomAnchor;
                existing.pivot = BottomAnchor;
                existing.anchoredPosition = Vector2.zero;
                existing.sizeDelta = parent.sizeDelta;
                existing.gameObject.hideFlags = PreviewHideFlags;
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.hideFlags = PreviewHideFlags;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
            rect.pivot = BottomAnchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = parent.sizeDelta;
            return rect;
        }

        private void ClearPreview()
        {
            if (_root == null)
            {
                return;
            }

            ClearChildren(_connectionsRoot);
            ClearChildren(_labelsRoot);
            ClearChildren(_nodesRoot);
            ClearChildren(_root);
            _connectionsRoot = null;
            _labelsRoot = null;
            _nodesRoot = null;

            var staleRen = _root.Find("RenMarkerPreview");
            if (staleRen != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(staleRen.gameObject);
                }
                else
#endif
                {
                    Destroy(staleRen.gameObject);
                }
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                    continue;
                }
#endif
                Destroy(child);
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += RebuildDeferred;
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += RebuildDeferred;
            }
        }

        private void RebuildDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            _runtimeSuppressed = false;
            ResolveReferences();
            Rebuild();
        }

        private void ResolveReferences()
        {
            mapView ??= GetComponent<RunMapUIView>();
            if (mapView == null)
            {
                mapView = GetComponentInParent<RunMapUIView>();
            }

            if (mapView == null)
            {
                return;
            }

            var so = new SerializedObject(mapView);
            layoutConfig ??= so.FindProperty("layoutConfig").objectReferenceValue as RunMapLayoutConfigSO;
            iconSet ??= so.FindProperty("iconSet").objectReferenceValue as MapNodeIconSetSO;
            playerMarkerConfig ??= so.FindProperty("playerMarkerConfig").objectReferenceValue as RunMapPlayerMarkerConfigSO;
            var templateProp = so.FindProperty("templateSet");
            if (templateSet == null && templateProp != null)
            {
                templateSet = templateProp.objectReferenceValue as MapNodeTemplateSetSO;
            }
        }
#endif
    }
}
