using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(Button))]
    public class MainMenuButtonRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private MainMenuStartGameMenuController menuController;
        [SerializeField] private int optionIndex;
        [SerializeField] private Text label;
        [SerializeField] private Image hitArea;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.45f);

        private Button _button;
        private bool _hovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (hitArea != null)
            {
                _button.targetGraphic = hitArea;
            }

            ApplyVisual(false);
        }

        public void Configure(
            MainMenuStartGameMenuController controller,
            int index,
            Text labelText,
            Image hitTarget,
            bool interactable)
        {
            menuController = controller;
            optionIndex = index;
            label = labelText;
            hitArea = hitTarget;
            _button = GetComponent<Button>();
            _button.interactable = interactable;
            if (hitArea != null)
            {
                _button.targetGraphic = hitArea;
            }

            ApplyVisual(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (!interactable)
            {
                _hovered = false;
            }

            ApplyVisual(_hovered);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable)
            {
                return;
            }

            _hovered = true;
            menuController?.NotifyHover(optionIndex);
            ApplyVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyVisual(false);
        }

        public void ApplySelectionVisual(bool selected)
        {
            ApplyVisual(_hovered || selected);
        }

        private void ApplyVisual(bool bright)
        {
            if (label == null)
            {
                return;
            }

            if (_button != null && !_button.interactable)
            {
                label.color = disabledColor;
                return;
            }

            label.color = bright ? hoverColor : normalColor;
        }
    }
}
