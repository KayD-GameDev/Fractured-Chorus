using System;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class TownMapPinView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image pinImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite selectedSprite;

        private TownLocationDefinition _definition;
        private Action<TownLocationDefinition> _onSelected;
        private bool _selected;

        public string LocationId => _definition != null ? _definition.Id : string.Empty;

        public void Bind(
            TownLocationDefinition definition,
            Sprite icon,
            Sprite idle,
            Sprite selected,
            Action<TownLocationDefinition> onSelected)
        {
            _definition = definition;
            _onSelected = onSelected;
            idleSprite = idle;
            selectedSprite = selected;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (labelText != null)
            {
                labelText.text = definition.DisplayName;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onSelected?.Invoke(_definition));
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (pinImage != null)
            {
                pinImage.sprite = selected && selectedSprite != null ? selectedSprite : idleSprite;
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(!selected);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public bool MatchesPhase(DayPhase phase, GameMetaState state)
        {
            if (_definition == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_definition.RequiredFlag) && !state.Flags.Has(_definition.RequiredFlag))
            {
                return false;
            }

            if (_definition.AvailablePhases == null || _definition.AvailablePhases.Length == 0)
            {
                return true;
            }

            foreach (var available in _definition.AvailablePhases)
            {
                if (available == phase)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
