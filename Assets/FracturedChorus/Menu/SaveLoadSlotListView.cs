using System;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class SaveLoadSlotListView : MonoBehaviour
    {
        public enum Mode
        {
            Load,
            Save
        }

        private CanvasGroup _canvasGroup;
        private Text _titleLabel;
        private Text _detailLabel;
        private Button _primaryButton;
        private Button _deleteButton;
        private Text _primaryLabel;
        private SlotRow[] _rows = Array.Empty<SlotRow>();
        private SaveSlotHeader[] _headers = Array.Empty<SaveSlotHeader>();
        private Mode _mode;
        private int _selectedSlot = -1;
        private Action<int> _onLoad;
        private Action<int> _onSave;
        private Action _onClosed;

        private sealed class SlotRow
        {
            public Button Button;
            public Image Background;
            public Text Label;
        }

        public bool IsOpen => _canvasGroup != null && _canvasGroup.gameObject.activeSelf;

        public static SaveLoadSlotListView Show(
            Transform parent,
            Mode mode,
            Action<int> onLoad = null,
            Action<int> onSave = null,
            Action onClosed = null)
        {
            if (parent == null)
            {
                Debug.LogError("[Fractured Chorus] SaveLoadSlotListView.Show: parent null.");
                return null;
            }

            var existing = parent.GetComponentInChildren<SaveLoadSlotListView>(true);
            if (existing != null)
            {
                existing.Open(mode, onLoad, onSave, onClosed);
                return existing;
            }

            var view = Build(parent);
            view.Open(mode, onLoad, onSave, onClosed);
            return view;
        }

        public void Open(Mode mode, Action<int> onLoad, Action<int> onSave, Action onClosed)
        {
            _mode = mode;
            _onLoad = onLoad;
            _onSave = onSave;
            _onClosed = onClosed;
            _selectedSlot = -1;
            RefreshHeaders();
            UpdateDetail();
            if (_canvasGroup != null)
            {
                _canvasGroup.gameObject.SetActive(true);
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.gameObject.SetActive(false);
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            _onClosed?.Invoke();
        }

        private void RefreshHeaders()
        {
            _headers = GameMetaSaveLoad.ListHeaders();
            for (var i = 0; i < _rows.Length; i++)
            {
                var header = i < _headers.Length ? _headers[i] : SaveSlotHeader.Empty(i);
                _rows[i].Label.text = FormatRowLabel(header);
            }

            ApplySelectionVisuals();
        }

        private void SelectSlot(int slot)
        {
            _selectedSlot = slot;
            ApplySelectionVisuals();
            UpdateDetail();
        }

        private void ApplySelectionVisuals()
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                _rows[i].Background.color = i == _selectedSlot ? FcColorTokens.Selection.RowBackground : FcColorTokens.Surface.Row;
            }
        }

        private void UpdateDetail()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = _mode == Mode.Load ? "LOAD GAME" : "SAVE GAME";
            }

            if (_detailLabel == null)
            {
                return;
            }

            if (_selectedSlot < 0 || _selectedSlot >= _headers.Length)
            {
                _detailLabel.text = "Select a slot.";
                SetPrimaryEnabled(false, _mode == Mode.Load ? "Load" : "Save");
                SetDeleteEnabled(false);
                return;
            }

            var header = _headers[_selectedSlot];
            if (header.isEmpty)
            {
                _detailLabel.text = $"Slot {_selectedSlot + 1:00}\nEmpty";
                if (_mode == Mode.Load)
                {
                    SetPrimaryEnabled(false, "Load");
                    SetDeleteEnabled(false);
                }
                else
                {
                    SetPrimaryEnabled(true, "Save");
                    SetDeleteEnabled(false);
                }

                return;
            }

            _detailLabel.text =
                $"Slot {_selectedSlot + 1:00}\n" +
                $"{header.dateMonth:00}/{header.dateDay:00} · {PhaseLabel(header.phase)}\n" +
                $"{header.locationLabel}\n" +
                $"Notes {header.notes} · {DifficultyLabel(header.difficulty)}";

            if (_mode == Mode.Load)
            {
                SetPrimaryEnabled(true, "Load");
                SetDeleteEnabled(true);
            }
            else
            {
                SetPrimaryEnabled(true, "Overwrite");
                SetDeleteEnabled(true);
            }
        }

        private void OnPrimaryClicked()
        {
            if (_selectedSlot < 0)
            {
                return;
            }

            if (_mode == Mode.Load)
            {
                if (_selectedSlot >= _headers.Length || _headers[_selectedSlot].isEmpty)
                {
                    return;
                }

                _onLoad?.Invoke(_selectedSlot);
                Hide();
                return;
            }

            _onSave?.Invoke(_selectedSlot);
            RefreshHeaders();
            UpdateDetail();
        }

        private void OnDeleteClicked()
        {
            if (_selectedSlot < 0)
            {
                return;
            }

            if (_selectedSlot >= _headers.Length || _headers[_selectedSlot].isEmpty)
            {
                return;
            }

            GameMetaSaveLoad.Delete(_selectedSlot);
            RefreshHeaders();
            UpdateDetail();
        }

        private void SetPrimaryEnabled(bool enabled, string label)
        {
            if (_primaryButton != null)
            {
                _primaryButton.interactable = enabled;
            }

            if (_primaryLabel != null)
            {
                _primaryLabel.text = label;
            }
        }

        private void SetDeleteEnabled(bool enabled)
        {
            if (_deleteButton != null)
            {
                _deleteButton.interactable = enabled;
            }
        }

        private static string FormatRowLabel(SaveSlotHeader header)
        {
            if (header.isEmpty)
            {
                return $"SLOT {header.slotIndex + 1:00}  —  EMPTY";
            }

            return
                $"SLOT {header.slotIndex + 1:00}  ·  {header.dateMonth:00}/{header.dateDay:00}  ·  {header.notes} NOTES";
        }

        private static string PhaseLabel(int phase) => phase switch
        {
            0 => "Morning",
            1 => "Day",
            2 => "Evening",
            _ => "Day"
        };

        private static string DifficultyLabel(int difficulty) => difficulty switch
        {
            (int)GameDifficulty.OnBeat => "On Beat",
            (int)GameDifficulty.OffBeat => "Off Beat",
            _ => "Cadence"
        };

        private static SaveLoadSlotListView Build(Transform parent)
        {
            var rootGo = new GameObject("SaveLoadSlotListView", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var canvasGroup = rootGo.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(rootGo.transform, false);
            Stretch(dimGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dimImage = dimGo.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.55f);
            dimImage.raycastTarget = true;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(rootGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 620f);
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = FcColorTokens.Surface.Modal;

            var title = CreateText(panelGo.transform, "Title", "LOAD GAME", 28, TextAnchor.UpperCenter, FontStyle.Bold);
            Stretch(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            title.color = FcColorTokens.Brand.Cyan;

            var listGo = new GameObject("SlotList", typeof(RectTransform));
            listGo.transform.SetParent(panelGo.transform, false);
            Stretch(listGo.GetComponent<RectTransform>(), new Vector2(0.05f, 0.22f), new Vector2(0.58f, 0.86f), Vector2.zero, Vector2.zero);

            var detail = CreateText(panelGo.transform, "Detail", "Select a slot.", 20, TextAnchor.UpperLeft);
            Stretch(detail.rectTransform, new Vector2(0.6f, 0.42f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);
            detail.color = FcColorTokens.Brand.TextMuted;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;

            var primary = CreateButton(panelGo.transform, "PrimaryButton", "Load", new Vector2(0.62f, 0.28f), new Vector2(0.95f, 0.36f));
            var delete = CreateButton(panelGo.transform, "DeleteButton", "Delete", new Vector2(0.62f, 0.18f), new Vector2(0.95f, 0.26f));
            var close = CreateButton(panelGo.transform, "CloseButton", "Close", new Vector2(0.62f, 0.08f), new Vector2(0.95f, 0.16f));

            var view = rootGo.AddComponent<SaveLoadSlotListView>();
            view._canvasGroup = canvasGroup;
            view._titleLabel = title;
            view._detailLabel = detail;
            view._primaryButton = primary.Button;
            view._primaryLabel = primary.Label;
            view._deleteButton = delete.Button;
            view._rows = new SlotRow[GameMetaSaveLoad.SlotCount];

            for (var i = 0; i < GameMetaSaveLoad.SlotCount; i++)
            {
                var slotIndex = i;
                var row = CreateSlotRow(listGo.transform, i);
                view._rows[i] = row;
                row.Button.onClick.AddListener(() => view.SelectSlot(slotIndex));
            }

            primary.Button.onClick.AddListener(view.OnPrimaryClicked);
            delete.Button.onClick.AddListener(view.OnDeleteClicked);
            close.Button.onClick.AddListener(view.Hide);

            rootGo.SetActive(false);
            return view;
        }

        private static SlotRow CreateSlotRow(Transform parent, int index)
        {
            var go = new GameObject($"Slot_{index:00}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            var yMax = 1f - index * 0.095f;
            var yMin = yMax - 0.085f;
            Stretch(rect, new Vector2(0f, yMin), new Vector2(1f, yMax), Vector2.zero, Vector2.zero);

            var image = go.GetComponent<Image>();
            image.color = FcColorTokens.Surface.Row;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(go.transform, "Label", $"SLOT {index + 1:00}", 18, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);
            label.color = Color.white;

            return new SlotRow
            {
                Button = button,
                Background = image,
                Label = label
            };
        }

        private static (Button Button, Text Label) CreateButton(
            Transform parent,
            string name,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            image.color = FcColorTokens.Surface.Row;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var label = CreateText(go.transform, "Label", labelText, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.color = FcColorTokens.Brand.Cyan;
            return (button, label);
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor anchor,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = ResolveFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Font ResolveFont() => UiFontCatalog.Body;

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
