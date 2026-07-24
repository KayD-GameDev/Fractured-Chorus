using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class OffBeatTrackRowView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        private static readonly Color Idle = new Color(0.75f, 0.9f, 1f, 0.85f);
        private static readonly Color Selected = new Color(0f, 0.831f, 1f, 1f);
        private static readonly Color HoverBg = new Color(0f, 0.55f, 0.75f, 0.18f);
        private static readonly Color SelectedBg = new Color(0f, 0.55f, 0.75f, 0.32f);
        private static readonly Color ClearBg = new Color(0f, 0f, 0f, 0f);

        [SerializeField] private Text titleLabel;
        [SerializeField] private Text artistLabel;
        [SerializeField] private Image background;
        [SerializeField] private Image favoriteMark;

        private int _index;
        private Action<int> _onSelect;
        private Action<int> _onHover;
        private bool _selected;

        public int Index => _index;

        public void Configure(Text title, Text artist, Image bg, Image favorite)
        {
            titleLabel = title;
            artistLabel = artist;
            background = bg;
            favoriteMark = favorite;
        }

        public void Bind(int index, OffBeatTrackSO track, bool favorite, Action<int> onSelect, Action<int> onHover)
        {
            _index = index;
            _onSelect = onSelect;
            _onHover = onHover;

            if (titleLabel != null)
            {
                titleLabel.text = track != null ? track.title : "—";
            }

            if (artistLabel != null)
            {
                artistLabel.text = track != null ? track.artist : string.Empty;
            }

            if (favoriteMark != null)
            {
                favoriteMark.enabled = favorite;
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshColors();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onSelect?.Invoke(_index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHover?.Invoke(_index);
            if (!_selected && background != null)
            {
                background.color = HoverBg;
            }
        }

        private void RefreshColors()
        {
            if (titleLabel != null)
            {
                titleLabel.color = _selected ? Selected : Idle;
            }

            if (artistLabel != null)
            {
                artistLabel.color = _selected ? Selected : Idle * 0.75f;
            }

            if (background != null)
            {
                background.color = _selected ? SelectedBg : ClearBg;
            }
        }
    }
}
