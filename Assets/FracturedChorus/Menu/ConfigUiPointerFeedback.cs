using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class ConfigUiPointerFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField] private Image graphic;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.94f;
        [SerializeField] private bool playPressSfx = true;

        private Sprite _spriteBeforeHover;
        private Vector3 _baseScale = Vector3.one;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;
        private MainMenuStartGameController _sfx;

        private void Awake()
        {
            if (graphic == null)
            {
                graphic = GetComponent<Image>();
            }

            CaptureBase();
        }

        public void Bind(MainMenuStartGameController sfx, Sprite hover = null, Sprite pressed = null)
        {
            _sfx = sfx;
            if (hover != null)
            {
                hoverSprite = hover;
            }

            if (pressed != null)
            {
                pressedSprite = pressed;
            }

            CaptureBase();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            _hovered = true;
            ApplyVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            _pressed = true;
            ApplyVisual();
            if (playPressSfx)
            {
                ResolveSfx()?.PlayButtonPressSfx();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyVisual();
        }

        private void CaptureBase()
        {
            _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        }

        private void ApplyVisual()
        {
            var interactable = IsInteractable();
            var showPress = _pressed && interactable;
            var showHover = _hovered && !showPress && !_selected && interactable;
            var scale = _baseScale;
            if (showPress)
            {
                scale *= pressScale;
            }
            else if (showHover)
            {
                scale *= hoverScale;
            }

            transform.localScale = scale;
            if (graphic == null)
            {
                return;
            }

            Sprite next = null;
            if (showPress && pressedSprite != null && !_selected)
            {
                next = pressedSprite;
            }
            else if (showHover && hoverSprite != null)
            {
                next = hoverSprite;
            }

            if (next != null)
            {
                if (graphic.sprite != hoverSprite && graphic.sprite != pressedSprite)
                {
                    _spriteBeforeHover = graphic.sprite;
                }

                graphic.sprite = next;
                return;
            }

            if ((graphic.sprite == hoverSprite || graphic.sprite == pressedSprite) && _spriteBeforeHover != null)
            {
                graphic.sprite = _spriteBeforeHover;
            }
        }

        private bool IsInteractable()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                return button.interactable;
            }

            var slider = GetComponentInParent<Slider>();
            return slider == null || slider.interactable;
        }

        private MainMenuStartGameController ResolveSfx()
        {
            if (_sfx == null)
            {
                _sfx = FindAnyObjectByType<MainMenuStartGameController>();
            }

            return _sfx;
        }
    }
}
