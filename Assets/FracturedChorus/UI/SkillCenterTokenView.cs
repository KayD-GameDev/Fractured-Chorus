using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Object tròn ở trung tâm bảng skill. Người chơi kéo nó vào một ô kỹ năng để chọn skill đó.
    /// Thả ra ngoài → tự bật về tâm.
    /// </summary>
    public class SkillCenterTokenView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color TokenColor = new Color(0.92f, 0.86f, 0.55f, 1f);

        private RectTransform _rect;
        private RectTransform _panelRect;
        private Canvas _canvas;
        private Camera _uiCamera;
        private CanvasGroup _canvasGroup;
        private SkillPanelUIView _panel;
        private Vector2 _homePosition;

        public void Build(RectTransform parent, float size, SkillPanelUIView panel,
            Canvas canvas, Camera uiCamera, Vector2 homePosition)
        {
            _panel = panel;
            _panelRect = parent;
            _canvas = canvas;
            _uiCamera = uiCamera;
            _homePosition = homePosition;

            _rect = gameObject.GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(size, size);
            _rect.anchoredPosition = homePosition;

            var image = gameObject.AddComponent<Image>();
            image.sprite = UiCircleSpriteUtil.Circle;
            image.type = Image.Type.Simple;
            image.color = TokenColor;
            image.raycastTarget = true;

            var star = new GameObject("Core", typeof(RectTransform));
            var starRect = star.GetComponent<RectTransform>();
            starRect.SetParent(_rect, false);
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
            var starImage = star.AddComponent<Image>();
            starImage.sprite = UiCircleSpriteUtil.Circle;
            starImage.color = new Color(0.2f, 0.22f, 0.3f, 1f);
            starImage.raycastTarget = false;

            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void ResetToHome()
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = _homePosition;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRect == null || _rect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _panelRect, eventData.position, ResolveEventCamera(eventData), out var local))
            {
                _rect.anchoredPosition = local;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
            }

            var consumed = _panel != null && _panel.TrySelectSlotAtScreenPoint(eventData.position);
            if (!consumed)
            {
                ResetToHome();
            }
        }

        private Camera ResolveEventCamera(PointerEventData eventData)
        {
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _uiCamera != null ? _uiCamera : eventData.pressEventCamera;
        }
    }
}
