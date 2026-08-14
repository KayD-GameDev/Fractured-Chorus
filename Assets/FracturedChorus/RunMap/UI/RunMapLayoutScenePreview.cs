using FracturedChorus.Data;
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

        [SerializeField] private RunMapLayoutConfigSO layoutConfig;
        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private RunMapPlayerMarkerConfigSO playerMarkerConfig;
        [SerializeField] private RunMapUIView mapView;

        private RectTransform _root;
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
            RunMapUIView view)
        {
            layoutConfig = layout;
            iconSet = icons;
            playerMarkerConfig = marker;
            mapView = view;
            Rebuild();
        }

        public void Rebuild()
        {
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

            ResolveReferences();
            if (HasRuntimeNodeClones())
            {
                ClearPreview();
                DeactivateRoot();
                mapView?.EnsureEditModePlayerMarker();
                return;
            }

            EnsureRoot();
            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }

            ClearChildren(_root);

            _metrics.SetConfig(layoutConfig);
            _metrics.SetProfile(MapGenerationProfile.Default);

            var alpha = layoutConfig.PreviewAlpha;
            var floorCount = layoutConfig.PreviewFloorCount;
            var centerCol = (MapLayoutConstants.ColumnCount - 1) / 2;

            PlacePreviewNode(MapNodeType.Start, 0, centerCol, alpha);

            for (var floor = 1; floor <= floorCount; floor++)
            {
                PlacePreviewNode(MapNodeType.Battle, floor, centerCol - 1, alpha);
                PlacePreviewNode(MapNodeType.Battle, floor, centerCol, alpha);
                PlacePreviewNode(MapNodeType.Battle, floor, centerCol + 1, alpha);
            }

            PlacePreviewNode(MapNodeType.Boss, MapGenerationProfile.Default.BossFloor, centerCol, alpha, isBoss: true);

            SyncContentSize();
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

        private void PlacePreviewNode(MapNodeType type, int floor, int column, float alpha, bool isBoss = false)
        {
            var node = new MapNodeData
            {
                Floor = floor,
                Column = column,
                Type = type,
                IsBoss = isBoss
            };

            var pos = _metrics.NodePosition(node);
            var diameter = _metrics.NodeVisualDiameter(node);
            CreateGhostNode($"{type}_F{floor}_C{column}", pos, diameter, type, isBoss, alpha);
        }

        private void CreateGhostNode(string label, Vector2 pos, float diameter, MapNodeType type, bool isBoss, float alpha)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(diameter, diameter);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            var sprite = iconSet != null ? iconSet.Resolve(type, isBoss, PinkySectorId.Pulse) : null;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                image.sprite = UiCircleSpriteUtil.Circle;
                var fill = MapNodePalette.FillColor(type);
                fill.a = alpha;
                image.color = fill;
            }
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
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            var existing = transform.Find("LayoutPreviewRoot") as RectTransform;
            if (existing != null)
            {
                _root = existing;
                return;
            }

            var go = new GameObject("LayoutPreviewRoot", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _root = go.GetComponent<RectTransform>();
            _root.anchorMin = BottomAnchor;
            _root.anchorMax = BottomAnchor;
            _root.pivot = BottomAnchor;
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = Vector2.zero;
        }

        private void ClearPreview()
        {
            if (_root == null)
            {
                return;
            }

            ClearChildren(_root);

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

#if UNITY_EDITOR
            var so = new SerializedObject(mapView);
            layoutConfig ??= so.FindProperty("layoutConfig").objectReferenceValue as RunMapLayoutConfigSO;
            iconSet ??= so.FindProperty("iconSet").objectReferenceValue as MapNodeIconSetSO;
            playerMarkerConfig ??= so.FindProperty("playerMarkerConfig").objectReferenceValue as RunMapPlayerMarkerConfigSO;
#endif
        }
#endif
    }
}
