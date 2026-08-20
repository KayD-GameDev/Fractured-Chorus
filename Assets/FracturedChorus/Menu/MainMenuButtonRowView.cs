using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public class MainMenuButtonRowView : MonoBehaviour
    {
        [SerializeField] private MainMenuStartGameMenuController menuController;
        [SerializeField] private int optionIndex;
        [SerializeField] private Text label;
        [SerializeField] private Image hitArea;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite highlightSprite;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Image shard;

        private static readonly Color ShardLabel = new Color(0.07f, 0.1f, 0.22f, 0.95f);
        private static readonly Color ShardLabelHot = new Color(0.22f, 0.08f, 0.42f, 1f);
        private static readonly Color ShardLabelOff = new Color(0.12f, 0.16f, 0.28f, 0.4f);

        private Button _button;
        private bool _hovered;
        private bool _selected;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                var graphic = hitArea != null ? hitArea : VisualTarget();
                if (graphic != null)
                {
                    _button.targetGraphic = graphic;
                }
            }

            ApplyVisual(_hovered || _selected);
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
            if (hitTarget != null)
            {
                hitArea = hitTarget;
            }

            _button = GetComponent<Button>();
            if (_button == null)
            {
                return;
            }

            _button.interactable = interactable;
            if (hitArea != null)
            {
                _button.targetGraphic = hitArea;
                _button.transition = Selectable.Transition.None;
            }

            ApplyVisual(_hovered || _selected);
        }

        public void ConfigureShard(Image shardImage, Image iconImage, Sprite normal, Sprite highlight)
        {
            shard = shardImage;
            icon = iconImage;
            normalSprite = normal;
            highlightSprite = highlight;
            _button = GetComponent<Button>();
            if (_button != null && hitArea != null)
            {
                _button.targetGraphic = hitArea;
                _button.transition = Selectable.Transition.None;
            }
            else if (_button != null && shard != null)
            {
                _button.targetGraphic = shard;
                _button.transition = Selectable.Transition.None;
            }

            if (shard != null)
            {
                shard.raycastTarget = false;
            }

            ApplyVisual(_hovered || _selected);
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

            ApplyVisual(_hovered || _selected);
        }

        public void NotifyPointerEnter()
        {
            if (_button == null || !_button.interactable)
            {
                return;
            }

            _hovered = true;
            menuController?.NotifyHover(optionIndex);
            ApplyVisual(true);
        }

        public void NotifyPointerExit()
        {
            _hovered = false;
            ApplyVisual(_selected);
            menuController?.NotifyHoverExit(optionIndex);
        }

        public void ApplySelectionVisual(bool selected)
        {
            _selected = selected;
            ApplyVisual(_hovered || _selected);
        }

        private Image VisualTarget()
        {
            if (shard != null)
            {
                return shard;
            }

            return GetComponent<Image>();
        }

        private void ApplyVisual(bool bright)
        {
            var interactable = _button == null || _button.interactable;
            var visual = VisualTarget();
            var useShard = visual != null && (normalSprite != null || highlightSprite != null);
            if (useShard)
            {
                if (!interactable)
                {
                    visual.sprite = normalSprite != null ? normalSprite : visual.sprite;
                    visual.color = new Color(1f, 1f, 1f, 0.42f);
                }
                else
                {
                    visual.sprite = bright && highlightSprite != null
                        ? highlightSprite
                        : (normalSprite != null ? normalSprite : visual.sprite);
                    visual.color = Color.white;
                }

                visual.raycastTarget = false;
                transform.localScale = Vector3.one;
            }

            if (icon != null)
            {
                icon.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }

            if (label == null)
            {
                return;
            }

            if (!interactable)
            {
                label.color = useShard ? ShardLabelOff : disabledColor;
                return;
            }

            if (useShard)
            {
                label.color = bright ? ShardLabelHot : ShardLabel;
                return;
            }

            label.color = bright ? hoverColor : normalColor;
        }
    }
}
