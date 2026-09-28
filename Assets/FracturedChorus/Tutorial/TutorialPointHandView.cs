using System.Collections.Generic;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    /// <summary>
    /// Pointing hand. The fingertip is the pivot, so each screen point is where the finger lands.
    /// </summary>
    public sealed class TutorialPointHandView : MonoBehaviour
    {
        private const string HandResource = "UI/Tutorial/tutorial_point_hand_v1";
        private static readonly Vector2 HandPivot = new Vector2(0.771f, 0.857f);
        private const float HandPointRightDegrees = -50f;
        private const float HandSize = 232f;

        private static TutorialPointHandView s_active;

        private readonly List<RectTransform> _hands = new List<RectTransform>();
        private RectTransform _rect;
        private Sprite _sprite;

        public static void HideActive()
        {
            if (s_active != null)
            {
                s_active.Hide();
            }
        }

        public static void ShowAtWorld(Vector3 worldPoint)
        {
            ShowAtWorlds(new[] { worldPoint });
        }

        public static void ShowAtWorlds(IReadOnlyList<Vector3> worldPoints)
        {
            if (worldPoints == null || worldPoints.Count == 0)
            {
                HideActive();
                return;
            }

            var camera = WorldCamera();
            if (camera == null)
            {
                HideActive();
                return;
            }

            var screens = new List<Vector2>(worldPoints.Count);
            for (var i = 0; i < worldPoints.Count; i++)
            {
                var screen = camera.WorldToScreenPoint(worldPoints[i]);
                if (screen.z <= 0f)
                {
                    continue;
                }

                screens.Add(screen);
            }

            ShowAtScreens(screens);
        }

        public static void ShowAtRect(RectTransform target)
        {
            if (target == null)
            {
                HideActive();
                return;
            }

            var canvas = target.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas != null ? canvas.rootCanvas.worldCamera : canvas.worldCamera
                : null;
            var world = target.TransformPoint(target.rect.center);
            var screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            ShowAtScreens(new[] { screen });
        }

        public static void ShowAtScreens(IReadOnlyList<Vector2> screens)
        {
            if (screens == null || screens.Count == 0)
            {
                HideActive();
                return;
            }

            var view = Ensure();
            if (view == null || !view.EnsureSprite())
            {
                HideActive();
                return;
            }

            view.Place(screens);
        }

        public void Hide()
        {
            for (var i = 0; i < _hands.Count; i++)
            {
                if (_hands[i] != null)
                {
                    _hands[i].gameObject.SetActive(false);
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

        private void Place(IReadOnlyList<Vector2> screens)
        {
            transform.SetAsLastSibling();
            for (var i = 0; i < screens.Count; i++)
            {
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screens[i], null, out var local))
                {
                    continue;
                }

                var hand = HandAt(i);
                hand.gameObject.SetActive(true);
                hand.anchoredPosition = local;
            }

            for (var i = screens.Count; i < _hands.Count; i++)
            {
                if (_hands[i] != null)
                {
                    _hands[i].gameObject.SetActive(false);
                }
            }
        }

        private RectTransform HandAt(int index)
        {
            while (_hands.Count <= index)
            {
                _hands.Add(CreateHand());
            }

            return _hands[index];
        }

        private RectTransform CreateHand()
        {
            var go = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_rect, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = HandPivot;
            rect.sizeDelta = new Vector2(HandSize, HandSize);
            rect.localRotation = Quaternion.Euler(0f, 0f, HandPointRightDegrees);
            var image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return rect;
        }

        private bool EnsureSprite()
        {
            if (_sprite != null)
            {
                return true;
            }

            _sprite = Resources.Load<Sprite>(HandResource);
            return _sprite != null;
        }

        private static TutorialPointHandView Ensure()
        {
            if (s_active != null)
            {
                return s_active;
            }

            var go = new GameObject(
                "TutorialPointHands",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup),
                typeof(TutorialPointHandView));
            var overlay = go.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = UiCanvasLayers.Tutorial + 300;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0f;
            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var view = go.GetComponent<TutorialPointHandView>();
            view._rect = go.GetComponent<RectTransform>();
            view._rect.anchorMin = Vector2.zero;
            view._rect.anchorMax = Vector2.one;
            view._rect.pivot = new Vector2(0.5f, 0.5f);
            view._rect.offsetMin = Vector2.zero;
            view._rect.offsetMax = Vector2.zero;
            view._rect.localScale = Vector3.one;
            s_active = view;
            return view;
        }

        private static Camera WorldCamera()
        {
            var named = GameObject.Find("CombatCanvas");
            var canvas = named != null ? named.GetComponent<Canvas>() : null;
            if (canvas == null)
            {
                var timeline = FindAnyObjectByType<BeatTimelineUIView>();
                canvas = timeline != null ? timeline.GetComponentInParent<Canvas>() : null;
            }

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                return canvas.worldCamera;
            }

            return Camera.main;
        }
    }
}
