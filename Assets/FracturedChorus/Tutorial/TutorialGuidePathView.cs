using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    /// <summary>
    /// Screen-space dashed gold arrow. Positions come from live scene transforms each frame.
    /// Sprites: Art/UI/Tutorial (source) and Resources/UI/Tutorial (runtime load).
    /// </summary>
    public sealed class TutorialGuidePathView : MonoBehaviour
    {
        private const string DashResource = "UI/Tutorial/tutorial_guide_dash_v1";
        private const string ArrowResource = "UI/Tutorial/tutorial_guide_arrowhead_v1";
        private const float DashWidth = 36f;
        private const float DashHeight = 14f;
        private const float DashGap = 16f;
        private const float ArrowWidth = 34f;
        private const float ArrowHeight = 30f;

        private static readonly GridPosition FormationFrom = new GridPosition(GridSide.Player, 1, 1);
        private static readonly GridPosition FormationTo = new GridPosition(GridSide.Player, 1, 0);

        private static TutorialGuidePathView s_active;
        private static bool s_missingSpriteLogged;

        private readonly List<Vector2> _screenPoints = new List<Vector2>(4);
        private readonly List<Image> _dashes = new List<Image>();
        private RectTransform _rect;
        private Image _arrow;
        private Sprite _dashSprite;
        private Sprite _arrowSprite;

        public static void HideActive()
        {
            if (s_active != null)
            {
                s_active.Hide();
            }
        }

        public static void ShowFormationArrow()
        {
            var view = Ensure();
            if (view == null || !view.TryShowFormation())
            {
                HideActive();
            }
        }

        public static void ShowSkillDragArrow()
        {
            var view = Ensure();
            if (view == null || !view.TryShowSkillDrag())
            {
                HideActive();
            }
        }

        public void Hide()
        {
            if (_arrow != null)
            {
                _arrow.gameObject.SetActive(false);
            }

            for (var i = 0; i < _dashes.Count; i++)
            {
                if (_dashes[i] != null)
                {
                    _dashes[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (s_active == this)
            {
                s_active = null;
            }
        }

        private bool TryShowFormation()
        {
            var worldCamera = WorldCamera();
            if (worldCamera == null)
            {
                return false;
            }

            var from = worldCamera.WorldToScreenPoint(GridCellMarker.ResolveWorld(FormationFrom));
            var to = worldCamera.WorldToScreenPoint(GridCellMarker.ResolveWorld(FormationTo));
            if (from.z <= 0f || to.z <= 0f)
            {
                return false;
            }

            _screenPoints.Clear();
            _screenPoints.Add(from);
            _screenPoints.Add(to);
            return ShowPolyline(_screenPoints);
        }

        private bool TryShowSkillDrag()
        {
            var panel = FindAnyObjectByType<SkillPanelUIView>();
            var timeline = FindAnyObjectByType<BeatTimelineUIView>();
            if (panel == null || timeline == null || !panel.IsVisible || panel.CurrentUnit == null)
            {
                return false;
            }

            if (!panel.TryGetBasicAttackSlot(out var slot) || slot == null)
            {
                return false;
            }

            if (!timeline.TryGetLaneLine(panel.CurrentUnit, out var lane) || lane == null)
            {
                return false;
            }

            if (!timeline.TryGetFirstPhaseImpactNote(out var note) || note == null)
            {
                return false;
            }

            var skill = RectCenterScreen(slot);
            if (!TryLaneScreenSpan(lane, CameraFor(lane), out var laneMinX, out var laneMaxX, out var laneY))
            {
                return false;
            }

            var noteScreen = RectCenterScreen(note);
            var end = new Vector2(Mathf.Clamp(noteScreen.x, laneMinX, laneMaxX), laneY);
            if ((end - skill).sqrMagnitude <= 16f)
            {
                return false;
            }

            _screenPoints.Clear();
            _screenPoints.Add(skill);
            _screenPoints.Add(end);
            return ShowPolyline(_screenPoints);
        }

        private bool ShowPolyline(List<Vector2> screenPoints)
        {
            if (!EnsureSprites() || _rect == null || screenPoints == null || screenPoints.Count < 2)
            {
                return false;
            }

            var local = new List<Vector2>(screenPoints.Count);
            for (var i = 0; i < screenPoints.Count; i++)
            {
                if (!ScreenToLocal(screenPoints[i], out var point))
                {
                    return false;
                }

                if (local.Count == 0 || (point - local[local.Count - 1]).sqrMagnitude > 4f)
                {
                    local.Add(point);
                }
            }

            if (local.Count < 2)
            {
                return false;
            }

            transform.SetAsLastSibling();
            Rebuild(local);
            return true;
        }

        private void Rebuild(List<Vector2> localPoints)
        {
            var length = PolylineLength(localPoints);
            var arrowReserve = ArrowWidth * 0.55f;
            var arrowDistance = Mathf.Max(0f, length - arrowReserve);
            if (!Sample(localPoints, arrowDistance, out var arrowPos, out var arrowDir))
            {
                arrowPos = localPoints[localPoints.Count - 1];
                arrowDir = localPoints[localPoints.Count - 1] - localPoints[0];
            }

            Place(_arrow.rectTransform, arrowPos, arrowDir, new Vector2(ArrowWidth, ArrowHeight));
            _arrow.gameObject.SetActive(true);

            var pitch = DashWidth + DashGap;
            var stop = Mathf.Max(0f, arrowDistance - ArrowWidth * 0.35f);
            var placed = 0;
            for (var distance = pitch * 0.5f; distance + DashWidth * 0.5f <= stop; distance += pitch)
            {
                if (!Sample(localPoints, distance, out var pos, out var dir))
                {
                    break;
                }

                var dash = RentDash(placed);
                Place(dash.rectTransform, pos, dir, new Vector2(DashWidth, DashHeight));
                dash.gameObject.SetActive(true);
                placed++;
            }

            for (var i = placed; i < _dashes.Count; i++)
            {
                _dashes[i].gameObject.SetActive(false);
            }
        }

        private Image RentDash(int index)
        {
            while (_dashes.Count <= index)
            {
                _dashes.Add(CreateImage("GuideDash", _dashSprite));
            }

            return _dashes[index];
        }

        private bool EnsureSprites()
        {
            if (_dashSprite == null)
            {
                _dashSprite = Resources.Load<Sprite>(DashResource);
            }

            if (_arrowSprite == null)
            {
                _arrowSprite = Resources.Load<Sprite>(ArrowResource);
            }

            if (_dashSprite != null && _arrowSprite != null)
            {
                if (_arrow == null)
                {
                    _arrow = CreateImage("GuideArrow", _arrowSprite);
                }

                return true;
            }

            if (!s_missingSpriteLogged)
            {
                s_missingSpriteLogged = true;
                Debug.LogWarning("[Tutorial] Missing guide sprites at Resources/" + DashResource + ".");
            }

            return false;
        }

        private Image CreateImage(string imageName, Sprite sprite)
        {
            var go = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_rect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            go.SetActive(false);
            return image;
        }

        private static void Place(RectTransform rect, Vector2 localPoint, Vector2 direction, Vector2 size)
        {
            rect.anchoredPosition = localPoint;
            rect.sizeDelta = size;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private bool ScreenToLocal(Vector2 screen, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out local);
        }

        private Camera WorldCamera()
        {
            return CombatCamera() ?? Camera.main;
        }

        private static Camera CombatCamera()
        {
            var canvas = FindCombatCanvas();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static TutorialGuidePathView Ensure()
        {
            if (s_active != null)
            {
                return s_active;
            }

            var go = new GameObject(
                "TutorialGuidePath",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup),
                typeof(TutorialGuidePathView));
            var overlay = go.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = UiCanvasLayers.Tutorial + 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0f;
            var view = go.GetComponent<TutorialGuidePathView>();
            view.EnsureRect();
            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            s_active = view;
            return view;
        }

        private void EnsureRect()
        {
            _rect = transform as RectTransform;
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _rect.localScale = Vector3.one;
        }

        private static Canvas FindCombatCanvas()
        {
            var named = GameObject.Find("CombatCanvas");
            if (named != null)
            {
                var canvas = named.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return canvas;
                }
            }

            var timeline = FindAnyObjectByType<BeatTimelineUIView>();
            return timeline != null ? timeline.GetComponentInParent<Canvas>() : null;
        }

        private static Vector2 RectCenterScreen(RectTransform rect)
        {
            var world = rect.TransformPoint(rect.rect.center);
            return RectTransformUtility.WorldToScreenPoint(CameraFor(rect), world);
        }

        private static Camera CameraFor(RectTransform rect)
        {
            if (rect == null)
            {
                return null;
            }

            var canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return root.worldCamera;
        }

        private static bool TryLaneScreenSpan(
            RectTransform lane,
            Camera camera,
            out float minX,
            out float maxX,
            out float y)
        {
            var corners = new Vector3[4];
            lane.GetWorldCorners(corners);
            minX = float.MaxValue;
            maxX = float.MinValue;
            var sumY = 0f;
            for (var i = 0; i < corners.Length; i++)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                sumY += screen.y;
            }

            y = sumY / corners.Length;
            return maxX > minX + 1f;
        }

        private static float PolylineLength(List<Vector2> points)
        {
            var length = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }

            return length;
        }

        private static bool Sample(List<Vector2> points, float distance, out Vector2 position, out Vector2 direction)
        {
            var remaining = Mathf.Max(0f, distance);
            for (var i = 1; i < points.Count; i++)
            {
                var delta = points[i] - points[i - 1];
                var segment = delta.magnitude;
                if (segment < 0.001f)
                {
                    continue;
                }

                if (remaining <= segment)
                {
                    position = Vector2.Lerp(points[i - 1], points[i], remaining / segment);
                    direction = delta / segment;
                    return true;
                }

                remaining -= segment;
            }

            position = points[points.Count - 1];
            direction = points[points.Count - 1] - points[points.Count - 2];
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            return false;
        }
    }
}
