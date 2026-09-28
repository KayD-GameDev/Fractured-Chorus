using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    /// <summary>
    /// Dims the combat view and lifts the objects that belong to the current guide.
    /// World sprites move to the TutorialFocus layer and an overlay camera draws them
    /// after the UI dimmer. UI widgets reparent onto a higher canvas, then return.
    /// </summary>
    public sealed class TutorialFocusOverlay : MonoBehaviour
    {
        public const int FocusLayer = 8;
        private const int DimmerSort = 1000;
        private const int LiftSort = 1150;
        private const int FrameSort = 1140;
        private const int LaneSort = 1170;
        private const int CounterSort = 1280;

        private static readonly GridPosition CellFrom = new GridPosition(GridSide.Player, 1, 1);
        private static readonly GridPosition CellTo = new GridPosition(GridSide.Player, 1, 0);

        private static TutorialFocusOverlay s_active;
        private static readonly HashSet<EntityId> LiftedIds = new HashSet<EntityId>();

        private readonly List<UiLift> _uiLifts = new List<UiLift>();
        private readonly List<WorldLift> _worldLifts = new List<WorldLift>();
        private readonly List<LayerSnap> _layerSnaps = new List<LayerSnap>();

        private Canvas _host;
        private RectTransform _dimmerRect;
        private Image _dimmer;
        private RectTransform _liftRoot;
        private RectTransform _frameOverlay;
        private RectTransform _laneOverlay;
        private RectTransform _counterOverlay;
        private readonly List<RectTransform> _skillVisuals = new List<RectTransform>();
        private readonly List<BeatTimelineUIView.SkillLaneAnchor> _skillAnchors =
            new List<BeatTimelineUIView.SkillLaneAnchor>();
        private readonly List<RectTransform> _dropPreview = new List<RectTransform>();
        private Transform _worldRoot;
        private Camera _focusCamera;
        private Camera _baseCamera;
        private int _savedCullingMask;
        private bool _maskSaved;

        public static bool IsLifted(Transform target) =>
            target != null && LiftedIds.Contains(target.GetEntityId());

        public static void SyncFormation()
        {
            var view = Ensure();
            if (view == null)
            {
                return;
            }

            view.ShowDimmer();
            view.LiftFormationSubjects();
        }

        public static void SyncSkillDrag()
        {
            var view = Ensure();
            if (view == null)
            {
                return;
            }

            view.ShowDimmer();
            view.LiftSkillSubjects();
        }

        public static void SyncParty()
        {
            var view = Ensure();
            if (view == null)
            {
                return;
            }

            view.ShowDimmer();
            view.LiftParty();
        }

        public static void Release()
        {
            if (s_active == null)
            {
                return;
            }

            s_active.RestoreAll();
            s_active.HideDimmer();
        }

        private void OnDestroy()
        {
            if (ShouldAbandonLifts())
            {
                DetachOverlayChildren(_frameOverlay);
                DetachOverlayChildren(_laneOverlay);
                DetachOverlayChildren(_counterOverlay);
                AbandonLifts();
            }
            else
            {
                RestoreAll();
                if (_focusCamera != null)
                {
                    DetachFocusCamera();
                }
            }

            _focusCamera = null;
            if (_frameOverlay != null)
            {
                Destroy(_frameOverlay.gameObject);
                _frameOverlay = null;
            }

            if (_laneOverlay != null)
            {
                Destroy(_laneOverlay.gameObject);
                _laneOverlay = null;
            }

            if (_counterOverlay != null)
            {
                Destroy(_counterOverlay.gameObject);
                _counterOverlay = null;
            }

            if (s_active == this)
            {
                s_active = null;
            }
        }

        private void LateUpdate()
        {
            if (_focusCamera == null || _baseCamera == null)
            {
                return;
            }

            _focusCamera.transform.SetPositionAndRotation(
                _baseCamera.transform.position,
                _baseCamera.transform.rotation);
            _focusCamera.orthographic = _baseCamera.orthographic;
            _focusCamera.orthographicSize = _baseCamera.orthographicSize;
            _focusCamera.fieldOfView = _baseCamera.fieldOfView;
            _focusCamera.nearClipPlane = _baseCamera.nearClipPlane;
            _focusCamera.farClipPlane = _baseCamera.farClipPlane;
        }

        private void LiftFormationSubjects()
        {
            LiftWorld(FindCell(CellFrom));
            LiftWorld(FindCell(CellTo));
            LiftWorld(FindRen());
        }

        private void LiftParty()
        {
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                var unitView = views[i];
                if (unitView == null || unitView.Side != GridSide.Player || unitView.Unit == null)
                {
                    continue;
                }

                LiftWorld(unitView.transform);
            }
        }

        private void LiftSkillSubjects()
        {
            var panel = FindAnyObjectByType<SkillPanelUIView>();
            var timeline = FindAnyObjectByType<BeatTimelineUIView>();
            if (panel == null || !panel.IsVisible || panel.CurrentUnit == null || timeline == null)
            {
                return;
            }

            EnsureGuideOverlays();
            panel.TryGetBasicAttackSlot(out var slot);
            timeline.TryGetLaneLine(panel.CurrentUnit, out var lane);
            timeline.TryGetFirstPhaseImpactNote(out var note);
            timeline.TryGetFirstPhaseBeatFrame(out var frame);
            timeline.CollectUnitSkillVisuals(panel.CurrentUnit, _skillVisuals);
            var ghost = panel.DragGhostRect;
            if (!HasSkillLiftSet(slot, lane, note, frame, ghost))
            {
                RestoreUiLiftsOnly();
            }

            LiftUi(frame, _frameOverlay, keepScreen: true);
            LiftUi(lane, _laneOverlay, keepScreen: true);
            LiftUi(note, _counterOverlay, keepScreen: true);
            LiftUi(slot, _counterOverlay, keepScreen: true);
            LiftUi(ghost, _counterOverlay, keepScreen: true);
            for (var i = 0; i < _skillVisuals.Count; i++)
            {
                LiftUi(_skillVisuals[i], _counterOverlay, keepScreen: true);
            }

            SnapSkillsOntoLane(timeline, panel.CurrentUnit);
            BringToFront(note);
            BringToFront(slot);
            BringToFront(ghost);
            for (var i = 0; i < _skillVisuals.Count; i++)
            {
                BringToFront(_skillVisuals[i]);
            }

            LiftDropPreview(timeline);
        }

        private void SnapSkillsOntoLane(BeatTimelineUIView timeline, CombatUnit unit)
        {
            if (timeline == null || _counterOverlay == null || !OverlayReady(_counterOverlay))
            {
                return;
            }

            var canvas = _counterOverlay.GetComponentInParent<Canvas>();
            var canvasScale = Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);
            timeline.CollectUnitSkillLaneAnchors(unit, _skillAnchors);
            for (var i = 0; i < _skillAnchors.Count; i++)
            {
                var anchor = _skillAnchors[i];
                var rect = anchor.Rect;
                if (rect == null || !IsLifted(rect) || rect.parent != _counterOverlay)
                {
                    continue;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = ScreenToCanvasLocal(_counterOverlay, anchor.Screen, canvasScale);
            }
        }

        private void LiftDropPreview(BeatTimelineUIView timeline)
        {
            if (timeline == null || _counterOverlay == null || !OverlayReady(_counterOverlay))
            {
                return;
            }

            timeline.CollectDropPreviewRects(_dropPreview);
            for (var i = 0; i < _dropPreview.Count; i++)
            {
                var rect = _dropPreview[i];
                if (rect == null)
                {
                    continue;
                }

                if (rect.parent != _counterOverlay)
                {
                    PlaceKeepingScreen(rect, _counterOverlay);
                }

                rect.SetAsLastSibling();
            }
        }

        private static void BringToFront(RectTransform rect)
        {
            if (rect != null)
            {
                rect.SetAsLastSibling();
            }
        }

        private bool HasSkillLiftSet(
            RectTransform slot,
            RectTransform lane,
            RectTransform note,
            RectTransform frame,
            RectTransform ghost)
        {
            var expected = CountDistinct(slot, lane, note, frame, ghost);
            for (var i = 0; i < _skillVisuals.Count; i++)
            {
                var skill = _skillVisuals[i];
                if (skill != null && skill != slot && skill != lane && skill != note && skill != frame && skill != ghost)
                {
                    expected++;
                }
            }

            if (_uiLifts.Count != expected)
            {
                return false;
            }

            return IsReady(slot, _counterOverlay)
                   && IsReady(lane, _laneOverlay)
                   && IsReady(note, _counterOverlay)
                   && IsReady(frame, _frameOverlay)
                   && IsReady(ghost, _counterOverlay)
                   && SkillsReady();
        }

        private bool SkillsReady()
        {
            for (var i = 0; i < _skillVisuals.Count; i++)
            {
                if (!IsReady(_skillVisuals[i], _counterOverlay))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsReady(RectTransform rect, RectTransform parent)
        {
            return rect == null || (IsLifted(rect) && rect.parent == parent);
        }

        private static int CountDistinct(
            RectTransform a,
            RectTransform b,
            RectTransform c,
            RectTransform d,
            RectTransform e)
        {
            var count = 0;
            Count(a, ref count);
            if (b != null && b != a)
            {
                count++;
            }

            if (c != null && c != a && c != b)
            {
                count++;
            }

            if (d != null && d != a && d != b && d != c)
            {
                count++;
            }

            if (e != null && e != a && e != b && e != c && e != d)
            {
                count++;
            }

            return count;
        }

        private static void Count(RectTransform rect, ref int count)
        {
            if (rect != null)
            {
                count++;
            }
        }

        private void LiftUi(RectTransform rect, RectTransform parent, bool keepScreen)
        {
            if (rect == null || parent == null || IsLifted(rect))
            {
                return;
            }

            if (keepScreen && !OverlayReady(parent))
            {
                return;
            }

            _uiLifts.Add(new UiLift
            {
                Rect = rect,
                Parent = rect.parent,
                Sibling = rect.GetSiblingIndex(),
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                Pivot = rect.pivot,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                LocalScale = rect.localScale,
                LocalRotation = rect.localRotation
            });

            LiftedIds.Add(rect.GetEntityId());
            if (!keepScreen)
            {
                rect.SetParent(parent, true);
                rect.SetAsLastSibling();
                return;
            }

            PlaceKeepingScreen(rect, parent);
            rect.SetAsLastSibling();
        }

        private static bool OverlayReady(RectTransform parent)
        {
            if (parent == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            var canvas = parent.GetComponentInParent<Canvas>();
            return canvas != null
                   && canvas.scaleFactor > 0.01f
                   && parent.rect.width > 200f
                   && parent.rect.height > 200f;
        }

        private static void PlaceKeepingScreen(RectTransform rect, RectTransform parent)
        {
            var sourceCamera = CameraFor(rect);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[0]);
            var topRight = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[2]);
            var screenPivot = RectTransformUtility.WorldToScreenPoint(sourceCamera, rect.position);
            var stretch = rect.anchorMin != rect.anchorMax;
            var pivot = rect.pivot;
            var sizeDelta = rect.sizeDelta;
            var scale = rect.localScale;
            var rotation = rect.localRotation;
            var canvas = parent.GetComponentInParent<Canvas>();
            var canvasScale = Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);

            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localRotation = rotation;

            if (stretch)
            {
                var center = (bottomLeft + topRight) * 0.5f;
                var screenSize = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.sizeDelta = screenSize / canvasScale;
                rect.anchoredPosition = ScreenToCanvasLocal(parent, center, canvasScale);
                return;
            }

            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.localScale = scale;
            rect.anchoredPosition = ScreenToCanvasLocal(parent, screenPivot, canvasScale);
        }

        private static Vector2 ScreenToCanvasLocal(RectTransform parent, Vector2 screen, float canvasScale)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            {
                return local;
            }

            return new Vector2(
                (screen.x - Screen.width * 0.5f) / canvasScale,
                (screen.y - Screen.height * 0.5f) / canvasScale);
        }

        private static Camera CameraFor(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return root.worldCamera;
        }

        private void EnsureGuideOverlays()
        {
            _frameOverlay = EnsureScreenOverlay(_frameOverlay, "TutorialFocusFrame", FrameSort, raycaster: false);
            _laneOverlay = EnsureScreenOverlay(_laneOverlay, "TutorialFocusLane", LaneSort, raycaster: false);
            _counterOverlay = EnsureScreenOverlay(_counterOverlay, "TutorialFocusCounter", CounterSort, raycaster: true);
        }

        private static RectTransform EnsureScreenOverlay(RectTransform current, string name, int sort, bool raycaster)
        {
            if (current != null)
            {
                return current;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0f;
            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private void LiftWorld(Transform target)
        {
            if (target == null || IsLifted(target))
            {
                return;
            }

            EnsureWorldCamera();
            if (_worldRoot == null)
            {
                return;
            }

            _worldLifts.Add(new WorldLift
            {
                Target = target,
                Parent = target.parent,
                Sibling = target.GetSiblingIndex()
            });
            CaptureLayers(target);
            LiftedIds.Add(target.GetEntityId());
            target.SetParent(_worldRoot, true);
            SetLayerRecursively(target, FocusLayer);
        }

        private void RestoreAll()
        {
            RestoreUiLiftsOnly();

            for (var i = _worldLifts.Count - 1; i >= 0; i--)
            {
                var lift = _worldLifts[i];
                if (lift.Target == null)
                {
                    continue;
                }

                LiftedIds.Remove(lift.Target.GetEntityId());
                if (!CanReparent(lift.Target, lift.Parent))
                {
                    continue;
                }

                lift.Target.SetParent(lift.Parent, true);
                lift.Target.SetSiblingIndex(Mathf.Clamp(lift.Sibling, 0, lift.Parent.childCount - 1));
            }

            _worldLifts.Clear();
            RestoreLayers();

            if (_maskSaved && _baseCamera != null)
            {
                _baseCamera.cullingMask = _savedCullingMask;
                _maskSaved = false;
            }
        }

        private void RestoreUiLiftsOnly()
        {
            for (var i = _uiLifts.Count - 1; i >= 0; i--)
            {
                var lift = _uiLifts[i];
                if (lift.Rect == null)
                {
                    continue;
                }

                LiftedIds.Remove(lift.Rect.GetEntityId());
                if (!CanReparent(lift.Rect, lift.Parent))
                {
                    continue;
                }

                lift.Rect.SetParent(lift.Parent, false);
                lift.Rect.SetSiblingIndex(Mathf.Clamp(lift.Sibling, 0, lift.Parent.childCount - 1));
                lift.Rect.anchorMin = lift.AnchorMin;
                lift.Rect.anchorMax = lift.AnchorMax;
                lift.Rect.pivot = lift.Pivot;
                lift.Rect.anchoredPosition = lift.AnchoredPosition;
                lift.Rect.sizeDelta = lift.SizeDelta;
                lift.Rect.localScale = lift.LocalScale;
                lift.Rect.localRotation = lift.LocalRotation;
            }

            _uiLifts.Clear();
        }

        private static void DetachOverlayChildren(RectTransform overlay)
        {
            if (overlay == null)
            {
                return;
            }

            for (var i = overlay.childCount - 1; i >= 0; i--)
            {
                var child = overlay.GetChild(i);
                if (child != null)
                {
                    child.SetParent(null, false);
                }
            }
        }

        private void AbandonLifts()
        {
            LiftedIds.Clear();
            _uiLifts.Clear();
            _worldLifts.Clear();
            _layerSnaps.Clear();
            _maskSaved = false;
        }

        private bool ShouldAbandonLifts()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return true;
            }
#endif
            return !IsLoadedSceneObject(transform);
        }

        private static bool CanReparent(Transform child, Transform parent)
        {
            return IsLoadedSceneObject(child) && IsLoadedSceneObject(parent);
        }

        private static bool IsLoadedSceneObject(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            var scene = target.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private void CaptureLayers(Transform root)
        {
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                _layerSnaps.Add(new LayerSnap { Target = current, Layer = current.gameObject.layer });
                for (var i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }
        }

        private void RestoreLayers()
        {
            for (var i = 0; i < _layerSnaps.Count; i++)
            {
                var snap = _layerSnaps[i];
                if (snap.Target != null)
                {
                    snap.Target.gameObject.layer = snap.Layer;
                }
            }

            _layerSnaps.Clear();
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                current.gameObject.layer = layer;
                for (var i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }
        }

        private void ShowDimmer()
        {
            if (_dimmerRect != null)
            {
                _dimmerRect.gameObject.SetActive(true);
                _dimmerRect.SetAsLastSibling();
            }
        }

        private void HideDimmer()
        {
            if (_dimmerRect != null)
            {
                _dimmerRect.gameObject.SetActive(false);
            }
        }

        private void EnsureWorldCamera()
        {
            if (_host == null)
            {
                return;
            }

            var baseCamera = _host.renderMode == RenderMode.ScreenSpaceOverlay
                ? Camera.main
                : (_host.worldCamera != null ? _host.worldCamera : Camera.main);
            if (baseCamera == null)
            {
                return;
            }

            _baseCamera = baseCamera;
            if (_worldRoot == null)
            {
                var rootGo = new GameObject("TutorialFocusWorld");
                _worldRoot = rootGo.transform;
            }

            if (_focusCamera == null)
            {
                var cameraGo = new GameObject("TutorialFocusCamera");
                _focusCamera = cameraGo.AddComponent<Camera>();
                _focusCamera.clearFlags = CameraClearFlags.Depth;
                _focusCamera.cullingMask = 1 << FocusLayer;
                _focusCamera.depth = baseCamera.depth + 2f;
                var overlayData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
                overlayData.renderType = CameraRenderType.Overlay;
            }

            var baseData = baseCamera.GetUniversalAdditionalCameraData();
            var stacked = baseData.renderType == CameraRenderType.Base
                          && baseData.cameraStack.Contains(_focusCamera);
            if (baseData.renderType == CameraRenderType.Base && !stacked)
            {
                baseData.cameraStack.Add(_focusCamera);
                stacked = true;
            }

            if (stacked && !_maskSaved)
            {
                _savedCullingMask = baseCamera.cullingMask;
                baseCamera.cullingMask = _savedCullingMask & ~(1 << FocusLayer);
                _maskSaved = true;
            }
        }

        private void DetachFocusCamera()
        {
            if (_baseCamera != null && _focusCamera != null)
            {
                var baseData = _baseCamera.GetUniversalAdditionalCameraData();
                baseData.cameraStack.Remove(_focusCamera);
            }

            if (_focusCamera != null)
            {
                Destroy(_focusCamera.gameObject);
                _focusCamera = null;
            }
        }

        private static Transform FindCell(GridPosition position)
        {
            var markers = FindObjectsByType<GridCellMarker>(FindObjectsInactive.Exclude);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker != null
                    && marker.Side == position.Side
                    && marker.Row == position.Row
                    && marker.Column == position.Column)
                {
                    return marker.transform;
                }
            }

            return null;
        }

        private static Transform FindRen()
        {
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                if (TutorialDirector.IsRenUnit(views[i]))
                {
                    return views[i].transform;
                }
            }

            return null;
        }

        private static TutorialFocusOverlay Ensure()
        {
            if (s_active != null)
            {
                return s_active;
            }

            var named = GameObject.Find("CombatCanvas");
            var host = named != null ? named.GetComponent<Canvas>() : null;
            if (host == null)
            {
                var timeline = FindAnyObjectByType<BeatTimelineUIView>();
                host = timeline != null ? timeline.GetComponentInParent<Canvas>() : null;
            }

            if (host == null)
            {
                return null;
            }

            host = host.rootCanvas != null ? host.rootCanvas : host;
            var existing = host.GetComponentInChildren<TutorialFocusOverlay>(true);
            if (existing != null)
            {
                s_active = existing;
                return existing;
            }

            var go = new GameObject("TutorialFocusOverlay", typeof(TutorialFocusOverlay));
            go.transform.SetParent(host.transform, false);
            var view = go.GetComponent<TutorialFocusOverlay>();
            view._host = host;
            view.Build(host);
            s_active = view;
            return view;
        }

        private void Build(Canvas host)
        {
            _dimmerRect = CreateCanvasChild("TutorialFocusDimmer", host, DimmerSort, raycaster: true);
            _dimmer = _dimmerRect.gameObject.AddComponent<Image>();
            _dimmer.color = new Color(0f, 0f, 0f, 0.8f);
            _dimmer.raycastTarget = true;
            Stretch(_dimmerRect);
            _dimmerRect.gameObject.SetActive(false);

            _liftRoot = CreateCanvasChild("TutorialFocusLift", host, LiftSort, raycaster: true);
            Stretch(_liftRoot);
        }

        private static RectTransform CreateCanvasChild(string name, Canvas host, int sort, bool raycaster)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(host.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sort;
            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private struct UiLift
        {
            public RectTransform Rect;
            public Transform Parent;
            public int Sibling;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector3 LocalScale;
            public Quaternion LocalRotation;
        }

        private struct WorldLift
        {
            public Transform Target;
            public Transform Parent;
            public int Sibling;
        }

        private struct LayerSnap
        {
            public Transform Target;
            public int Layer;
        }
    }
}
