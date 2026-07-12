using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class DistrictSelectPanel : MonoBehaviour
    {
        [SerializeField] private Image panelBackground;
        [SerializeField] private Text headerTitle;
        [SerializeField] private Text headerSubtitle;
        [SerializeField] private Image headerPin;
        [SerializeField] private RectTransform rowRoot;
        [SerializeField] private Button rowTemplate;
        [SerializeField] private Sprite rowNormalSprite;
        [SerializeField] private Sprite rowSelectedSprite;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TownMapSfxController sfx;

        private readonly List<Button> _rows = new List<Button>();
        private TownLocationDefinition _location;
        private TownSubLocation _selectedSub;
        private DayPhase _phase;
        private Action<TownLocationDefinition, TownSubLocation> _onConfirm;
        private Action _onClose;
        private int _selectedIndex = -1;

        private void Awake()
        {
            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleClose);
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (TownMapInput.ConfirmPressed())
            {
                HandleConfirm();
            }
            else if (TownMapInput.CancelPressed())
            {
                HandleClose();
            }
        }

        public void BindSfx(TownMapSfxController controller)
        {
            sfx = controller;
        }

        public void Show(
            TownLocationDefinition location,
            DayPhase phase,
            Action<TownLocationDefinition, TownSubLocation> onConfirm,
            Action onClose)
        {
            _location = location;
            _phase = phase;
            _onConfirm = onConfirm;
            _onClose = onClose;
            _selectedSub = null;
            _selectedIndex = -1;
            gameObject.SetActive(true);
            sfx?.PlayOpenPanel();

            if (headerTitle != null)
            {
                headerTitle.text = "SELECT MAP";
            }

            if (headerSubtitle != null)
            {
                headerSubtitle.text = location != null ? location.DisplayName : "Where should I go?";
            }

            RebuildRows();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearRows();
        }

        private void RebuildRows()
        {
            ClearRows();
            if (_location?.SubLocations == null || rowTemplate == null || rowRoot == null)
            {
                return;
            }

            var available = new List<TownSubLocation>();
            foreach (var sub in _location.SubLocations)
            {
                if (IsSubAvailable(sub))
                {
                    available.Add(sub);
                }
            }

            for (var i = 0; i < available.Count; i++)
            {
                var sub = available[i];
                var button = Instantiate(rowTemplate, rowRoot);
                button.gameObject.SetActive(true);

                var image = button.GetComponent<Image>();
                if (image != null && rowNormalSprite != null)
                {
                    image.sprite = rowNormalSprite;
                    image.type = Image.Type.Sliced;
                }

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = sub.Label;
                    label.color = Color.white;
                }

                var index = i;
                var captured = sub;
                button.onClick.AddListener(() => SelectRow(index, button, captured));
                _rows.Add(button);
            }

            if (_rows.Count > 0)
            {
                SelectRow(0, _rows[0], available[0], playSfx: false);
            }
        }

        private bool IsSubAvailable(TownSubLocation sub)
        {
            if (sub.AllowedPhases == null || sub.AllowedPhases.Length == 0)
            {
                return true;
            }

            foreach (var phase in sub.AllowedPhases)
            {
                if (phase == _phase)
                {
                    return true;
                }
            }

            return false;
        }

        private void SelectRow(int index, Button button, TownSubLocation sub, bool playSfx = true)
        {
            _selectedIndex = index;
            _selectedSub = sub;
            if (playSfx)
            {
                sfx?.PlaySelect();
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var image = row.GetComponent<Image>();
                var label = row.GetComponentInChildren<Text>();
                var selected = i == index;
                if (image != null)
                {
                    image.sprite = selected ? rowSelectedSprite : rowNormalSprite;
                }

                if (label != null)
                {
                    label.color = selected ? new Color(0.04f, 0.1f, 0.2f) : Color.white;
                }
            }
        }

        private void HandleConfirm()
        {
            if (_location == null || _selectedSub == null)
            {
                return;
            }

            sfx?.PlayConfirm();
            _onConfirm?.Invoke(_location, _selectedSub);
        }

        private void HandleClose()
        {
            sfx?.PlayClosePanel();
            Hide();
            _onClose?.Invoke();
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                if (row == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(row.gameObject);
                }
                else
                {
                    DestroyImmediate(row.gameObject);
                }
            }

            _rows.Clear();
        }
    }
}
