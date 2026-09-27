using System.Collections;
using System.Collections.Generic;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    /// <summary>
    /// Manga bubbles and a simplified hand for the first tutorial QTE.
    /// Bubble 1 and 2 advance on a click inside the bubble. Bubble 3 waits for a click outside it.
    /// </summary>
    public sealed class TutorialQteGuideView : MonoBehaviour
    {
        public const string OuterCopy = "Vòng ngoài đang thu nhỏ. Đây là vòng đếm nhịp.";
        public const string InnerCopy = "Vòng trong là mốc. Hai vòng chạm nhau là lúc Perfect.";
        public const string ContactCopy = "Canh đúng lúc này và bấm chuột bên ngoài bong bóng để được Perfect.";

        private const string BubbleResource = "UI/Tutorial/tutorial_qte_bubble_v1";
        private const string HandResource = "UI/Tutorial/tutorial_qte_hand_v1";

        private static TutorialQteGuideView s_active;

        private RectTransform _root;
        private RectTransform _bubble;
        private Text _label;
        private RectTransform _hand;
        private Button _button;
        private bool _bubbleClicked;
        private bool _outsideClicked;
        private bool _outsideMode;
        private Image _pointAt;

        public static IEnumerator PlayIntro(CombatQteOverlayView qte)
        {
            var view = Ensure(qte);
            if (view == null)
            {
                yield break;
            }

            view.Show(OuterCopy, qte.OuterRing, outsideConfirms: false);
            yield return view.WaitUntilBubbleClicked();
            view.Show(InnerCopy, qte.InnerRing, outsideConfirms: false);
            yield return view.WaitUntilBubbleClicked();
            view.HideChrome();
        }

        public static IEnumerator WaitForContactClick(CombatQteOverlayView qte)
        {
            var view = Ensure(qte);
            if (view == null)
            {
                yield break;
            }

            view.Show(ContactCopy, qte.OuterRing, outsideConfirms: true);
            yield return null;
            yield return view.WaitUntilOutsideClicked();
            view.HideChrome();
        }

        private void LateUpdate()
        {
            if (_pointAt == null || _hand == null || !_hand.gameObject.activeSelf)
            {
                return;
            }

            PositionBeside(_pointAt.rectTransform);
        }

        private void OnDestroy()
        {
            if (s_active == this)
            {
                s_active = null;
            }
        }

        private void Show(string copy, Image pointAt, bool outsideConfirms)
        {
            _pointAt = pointAt;
            _outsideMode = outsideConfirms;
            _bubbleClicked = false;
            _outsideClicked = false;
            if (_label != null)
            {
                _label.text = copy;
            }

            if (_bubble != null)
            {
                _bubble.gameObject.SetActive(true);
            }

            if (_hand != null)
            {
                _hand.gameObject.SetActive(pointAt != null);
            }

            if (_button != null)
            {
                _button.interactable = !outsideConfirms;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            if (pointAt != null)
            {
                PositionBeside(pointAt.rectTransform);
            }
        }

        private void HideChrome()
        {
            _pointAt = null;
            _outsideMode = false;
            if (_bubble != null)
            {
                _bubble.gameObject.SetActive(false);
            }

            if (_hand != null)
            {
                _hand.gameObject.SetActive(false);
            }
        }

        private CustomYieldInstruction WaitUntilBubbleClicked()
        {
            return new Until(() => _bubbleClicked);
        }

        private CustomYieldInstruction WaitUntilOutsideClicked()
        {
            return new Until(() =>
            {
                if (_outsideClicked)
                {
                    return true;
                }

                if (_outsideMode && WasClickThisFrame() && !PointerHitsBubble())
                {
                    _outsideClicked = true;
                }

                return _outsideClicked;
            });
        }

        private sealed class Until : CustomYieldInstruction
        {
            private readonly System.Func<bool> _isDone;

            public Until(System.Func<bool> isDone)
            {
                _isDone = isDone;
            }

            public override bool keepWaiting => !_isDone();
        }

        private void HandleBubbleClick()
        {
            if (!_outsideMode)
            {
                _bubbleClicked = true;
            }
        }

        private void PositionBeside(RectTransform target)
        {
            var canvas = _root.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas != null ? canvas.rootCanvas.worldCamera : canvas.worldCamera
                : null;
            var world = target.TransformPoint(target.rect.center);
            var screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, camera, out var local))
            {
                return;
            }

            if (_hand != null)
            {
                var innerRing = target != null && target.name == "InnerRing";
                _hand.anchoredPosition = innerRing
                    ? new Vector2(-120f, 216f)
                    : new Vector2(-162f, 216f);
            }

            if (_bubble != null)
            {
                _bubble.anchoredPosition = local + new Vector2(-280f, 120f);
            }
        }

        private bool PointerHitsBubble()
        {
            if (EventSystem.current == null || _bubble == null)
            {
                return false;
            }

            var pointer = PointerScreen();
            var data = new PointerEventData(EventSystem.current) { position = pointer };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            for (var i = 0; i < hits.Count; i++)
            {
                var hit = hits[i].gameObject;
                if (hit != null && (hit.transform == _bubble || hit.transform.IsChildOf(_bubble)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasClickThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static Vector2 PointerScreen()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private static TutorialQteGuideView Ensure(CombatQteOverlayView qte)
        {
            if (qte == null)
            {
                return null;
            }

            if (s_active != null)
            {
                return s_active;
            }

            var go = new GameObject("TutorialQteGuide", typeof(RectTransform), typeof(TutorialQteGuideView));
            go.transform.SetParent(qte.transform, false);
            var view = go.GetComponent<TutorialQteGuideView>();
            view.Build();
            s_active = view;
            return view;
        }

        private void Build()
        {
            _root = transform as RectTransform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _root.pivot = new Vector2(0.5f, 0.5f);

            var bubbleSprite = Resources.Load<Sprite>(BubbleResource);
            var handSprite = Resources.Load<Sprite>(HandResource);

            var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _bubble = bubbleGo.GetComponent<RectTransform>();
            _bubble.SetParent(_root, false);
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f);
            _bubble.pivot = new Vector2(0.85f, 0.15f);
            _bubble.sizeDelta = new Vector2(440f, 168f);
            var bubbleImage = bubbleGo.GetComponent<Image>();
            bubbleImage.sprite = bubbleSprite;
            bubbleImage.color = bubbleSprite != null ? Color.white : new Color(1f, 0.97f, 0.9f, 0.96f);
            bubbleImage.raycastTarget = true;
            _button = bubbleGo.GetComponent<Button>();
            _button.targetGraphic = bubbleImage;
            _button.onClick.AddListener(HandleBubbleClick);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(_bubble, false);
            labelRect.anchorMin = new Vector2(0.08f, 0.22f);
            labelRect.anchorMax = new Vector2(0.92f, 0.88f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label = labelGo.AddComponent<Text>();
            _label.font = UiFontCatalog.Body;
            _label.fontSize = 22;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color(0.12f, 0.08f, 0.06f, 1f);
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;

            var handGo = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _hand = handGo.GetComponent<RectTransform>();
            _hand.SetParent(_root, false);
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.5f, 0.5f);
            _hand.pivot = new Vector2(0.92f, 0.45f);
            _hand.sizeDelta = new Vector2(168f, 168f);
            var handImage = handGo.GetComponent<Image>();
            handImage.sprite = handSprite;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;
            handImage.color = Color.white;
            _hand.gameObject.SetActive(false);
            _bubble.gameObject.SetActive(false);
        }
    }
}
