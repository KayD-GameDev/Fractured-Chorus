using System;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    /// <summary>
    /// Panel 10 slot dùng chung cho Load và Save, có tab bar LOAD | SAVE.
    /// Chạy được hai kiểu: bind vào hierarchy dựng sẵn trong scene (chỉnh layout bằng tay được),
    /// hoặc tự dựng bằng code khi scene chưa có gì.
    /// </summary>
    public sealed class SaveLoadSlotListView : MonoBehaviour
    {
        public enum Mode
        {
            Load,
            Save
        }

        [Serializable]
        private sealed class SlotRow
        {
            public Button Button;
            public Image Background;
            public Text Label;
        }

        [Header("Scene bindings (bỏ trống nếu muốn dựng runtime)")]
        [SerializeField] private CanvasGroup sceneCanvasGroup;
        [SerializeField] private Text sceneTitleLabel;
        [SerializeField] private Text sceneDetailLabel;
        [SerializeField] private Button scenePrimaryButton;
        [SerializeField] private Text scenePrimaryLabel;
        [SerializeField] private Button sceneDeleteButton;
        [SerializeField] private Button sceneCloseButton;
        [SerializeField] private Button sceneLoadTabButton;
        [SerializeField] private Text sceneLoadTabLabel;
        [SerializeField] private Button sceneSaveTabButton;
        [SerializeField] private Text sceneSaveTabLabel;
        [SerializeField] private SlotRow[] sceneSlots = Array.Empty<SlotRow>();
        [SerializeField] private ConfirmDialogView sceneConfirmDialog;

        private CanvasGroup _canvasGroup;
        private Text _titleLabel;
        private Text _detailLabel;
        private Button _primaryButton;
        private Button _deleteButton;
        private Text _primaryLabel;
        private Button _loadTabButton;
        private Text _loadTabLabel;
        private Button _saveTabButton;
        private Text _saveTabLabel;
        private ConfirmDialogView _confirmDialog;
        private SlotRow[] _rows = Array.Empty<SlotRow>();
        private SaveSlotHeader[] _headers = Array.Empty<SaveSlotHeader>();
        private Mode _mode;
        private int _selectedSlot = -1;
        private Action<int> _onLoad;
        private Action<int> _onSave;
        private Action _onClosed;
        private bool _sessionActive;
        private bool _bound;

        public bool IsOpen => _canvasGroup != null && _canvasGroup.gameObject.activeSelf;

        public static SaveLoadSlotListView Show(
            Transform parent,
            Mode mode,
            Action<int> onLoad = null,
            Action<int> onSave = null,
            Action onClosed = null,
            bool sessionActive = false)
        {
            if (parent == null)
            {
                Debug.LogError("[Fractured Chorus] SaveLoadSlotListView.Show: parent null.");
                return null;
            }

            var existing = parent.GetComponentInChildren<SaveLoadSlotListView>(true);
            if (existing != null)
            {
                existing.Open(mode, onLoad, onSave, onClosed, sessionActive);
                return existing;
            }

            var view = Build(parent);
            view.Open(mode, onLoad, onSave, onClosed, sessionActive);
            return view;
        }

        public void Open(
            Mode mode,
            Action<int> onLoad,
            Action<int> onSave,
            Action onClosed,
            bool sessionActive = false)
        {
            EnsureBound();

            _onLoad = onLoad;
            _onSave = onSave;
            _onClosed = onClosed;

            // Mở tab SAVE nghĩa là đang trong ván chơi, kể cả khi caller quên báo.
            _sessionActive = sessionActive || mode == Mode.Save;

            if (_canvasGroup != null)
            {
                _canvasGroup.gameObject.SetActive(true);
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            SetMode(mode);
            transform.SetAsLastSibling();
            UiEscapeGate.Push(this);
        }

        public void Hide()
        {
            _confirmDialog?.Close();
            UiEscapeGate.Pop(this);

            if (_canvasGroup != null)
            {
                _canvasGroup.gameObject.SetActive(false);
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        private void Awake()
        {
            EnsureBound();
        }

        private void OnDisable()
        {
            UiEscapeGate.Pop(this);
        }

        private void Update()
        {
            if (IsOpen && UiCancelInput.WasPressed() && UiEscapeGate.TryConsume(this))
            {
                Hide();
            }
        }

        /// <summary>
        /// Instance dựng bằng Build() đã có sẵn reference; instance đặt trong scene thì lấy từ
        /// các field [SerializeField]. Gọi được nhiều lần, chỉ nối listener một lần.
        /// </summary>
        private void EnsureBound()
        {
            if (_bound)
            {
                return;
            }

            if (sceneCanvasGroup == null && _canvasGroup == null)
            {
                // Chưa Build() và scene cũng chưa gán gì — chờ Build() gọi lại.
                return;
            }

            _bound = true;

            if (sceneCanvasGroup != null)
            {
                BindSceneHierarchy();
            }

            _loadTabButton?.onClick.AddListener(() => SetMode(Mode.Load));
            _saveTabButton?.onClick.AddListener(() => SetMode(Mode.Save));

            for (var i = 0; i < _rows.Length; i++)
            {
                var slotIndex = i;
                _rows[i]?.Button?.onClick.AddListener(() => SelectSlot(slotIndex));
            }
        }

        private void BindSceneHierarchy()
        {
            _canvasGroup = sceneCanvasGroup;
            _titleLabel = sceneTitleLabel;
            _detailLabel = sceneDetailLabel;
            _primaryButton = scenePrimaryButton;
            _primaryLabel = scenePrimaryLabel;
            _deleteButton = sceneDeleteButton;
            _loadTabButton = sceneLoadTabButton;
            _loadTabLabel = sceneLoadTabLabel;
            _saveTabButton = sceneSaveTabButton;
            _saveTabLabel = sceneSaveTabLabel;
            _confirmDialog = sceneConfirmDialog;
            _rows = sceneSlots ?? Array.Empty<SlotRow>();

            _primaryButton?.onClick.AddListener(OnPrimaryClicked);
            _deleteButton?.onClick.AddListener(OnDeleteClicked);
            sceneCloseButton?.onClick.AddListener(Hide);
        }

        private void SetMode(Mode mode)
        {
            // Tab chỉ bật khi có callback tương ứng: Load mở từ main menu, Save dùng trong ván chơi.
            if (mode == Mode.Save && !CanSave)
            {
                mode = Mode.Load;
            }
            else if (mode == Mode.Load && !CanLoad && CanSave)
            {
                mode = Mode.Save;
            }

            _mode = mode;
            _selectedSlot = -1;
            RefreshTabs();
            RefreshHeaders();
            UpdateDetail();
        }

        private bool CanLoad => _onLoad != null;

        private bool CanSave => _onSave != null && _sessionActive;

        private void RefreshTabs()
        {
            ApplyTabVisual(_loadTabButton, _loadTabLabel, CanLoad, _mode == Mode.Load);
            ApplyTabVisual(_saveTabButton, _saveTabLabel, CanSave, _mode == Mode.Save);
        }

        private static void ApplyTabVisual(Button button, Text label, bool available, bool selected)
        {
            if (button != null)
            {
                button.interactable = available && !selected;
            }

            if (label == null)
            {
                return;
            }

            if (!available)
            {
                label.color = FcColorTokens.Brand.TextMuted * new Color(1f, 1f, 1f, 0.45f);
                return;
            }

            label.color = selected ? FcColorTokens.Brand.Cyan : FcColorTokens.Brand.TextIdle;
        }

        private void RefreshHeaders()
        {
            _headers = GameMetaSaveLoad.ListHeaders();
            for (var i = 0; i < _rows.Length; i++)
            {
                if (_rows[i]?.Label == null)
                {
                    continue;
                }

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
                if (_rows[i]?.Background == null)
                {
                    continue;
                }

                _rows[i].Background.color = i == _selectedSlot
                    ? FcColorTokens.Selection.RowBackground
                    : FcColorTokens.Surface.Row;
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
            if (header.isCorrupted)
            {
                _detailLabel.text =
                    $"Slot {_selectedSlot + 1:00}\nCORRUPTED\n" +
                    "File save sai chữ ký hoặc hỏng.\nChỉ có thể xóa hoặc ghi đè.";
                SetPrimaryEnabled(_mode == Mode.Save, "Overwrite");
                SetDeleteEnabled(true);
                return;
            }

            if (header.isEmpty)
            {
                _detailLabel.text = $"Slot {_selectedSlot + 1:00}\nEmpty";
                SetPrimaryEnabled(_mode == Mode.Save, _mode == Mode.Load ? "Load" : "Save");
                SetDeleteEnabled(false);
                return;
            }

            _detailLabel.text =
                $"Slot {_selectedSlot + 1:00}\n" +
                $"{header.dateMonth:00}/{header.dateDay:00} · {PhaseLabel(header.phase)}\n" +
                $"{header.locationLabel}\n" +
                $"Notes {header.notes} · {DifficultyLabel(header.difficulty)}\n" +
                $"Playtime {header.FormatPlayTime()}";

            SetPrimaryEnabled(true, _mode == Mode.Load ? "Load" : "Overwrite");
            SetDeleteEnabled(true);
        }

        private void OnPrimaryClicked()
        {
            if (_selectedSlot < 0 || _selectedSlot >= _headers.Length)
            {
                return;
            }

            var header = _headers[_selectedSlot];
            var slotLabel = $"SLOT {_selectedSlot + 1:00}";

            if (_mode == Mode.Load)
            {
                if (header.isEmpty || header.isCorrupted)
                {
                    return;
                }

                if (_sessionActive)
                {
                    AskThen(
                        "THOÁT VÁN ĐANG CHƠI?",
                        $"Tải {slotLabel} sẽ bỏ tiến trình chưa lưu của ván hiện tại.",
                        () => PerformLoad(_selectedSlot),
                        "TẢI");
                    return;
                }

                PerformLoad(_selectedSlot);
                return;
            }

            if (header.isEmpty)
            {
                PerformSave(_selectedSlot);
                return;
            }

            AskThen(
                "GHI ĐÈ SLOT?",
                header.isCorrupted
                    ? $"{slotLabel} đang hỏng. Ghi đè sẽ thay bằng dữ liệu mới."
                    : $"Ghi đè {slotLabel}? Dữ liệu cũ sẽ mất.",
                () => PerformSave(_selectedSlot),
                "GHI ĐÈ");
        }

        private void OnDeleteClicked()
        {
            if (_selectedSlot < 0 || _selectedSlot >= _headers.Length || _headers[_selectedSlot].isEmpty)
            {
                return;
            }

            var slot = _selectedSlot;
            AskThen(
                "XÓA SLOT?",
                $"Xóa hẳn SLOT {slot + 1:00}? Không khôi phục lại được.",
                () =>
                {
                    GameMetaSaveLoad.Delete(slot);
                    RefreshHeaders();
                    UpdateDetail();
                },
                "XÓA");
        }

        private void PerformLoad(int slot)
        {
            _onLoad?.Invoke(slot);
            Hide();
        }

        private void PerformSave(int slot)
        {
            _onSave?.Invoke(slot);
            RefreshHeaders();
            UpdateDetail();
        }

        /// <summary>
        /// Hỏi xác nhận rồi mới chạy. Nếu không dựng được dialog thì chạy luôn — thà mất confirm
        /// còn hơn nút bấm không phản hồi gì.
        /// </summary>
        private void AskThen(string title, string message, Action onConfirm, string confirmText)
        {
            _confirmDialog ??= ConfirmDialogView.Ensure(_canvasGroup != null ? _canvasGroup.transform : transform);

            if (_confirmDialog == null)
            {
                onConfirm?.Invoke();
                return;
            }

            _confirmDialog.Ask(title, message, onConfirm, null, confirmText, "HỦY");
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
            if (header.isCorrupted)
            {
                return $"SLOT {header.slotIndex + 1:00}  —  CORRUPTED";
            }

            if (header.isEmpty)
            {
                return $"SLOT {header.slotIndex + 1:00}  —  EMPTY";
            }

            return
                $"SLOT {header.slotIndex + 1:00}  ·  {header.dateMonth:00}/{header.dateDay:00}" +
                $"  ·  {header.notes} NOTES  ·  {header.FormatPlayTime()}";
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
            Stretch(title.rectTransform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            title.color = FcColorTokens.Brand.Cyan;

            var tabBarGo = new GameObject("TabBar", typeof(RectTransform));
            tabBarGo.transform.SetParent(panelGo.transform, false);
            Stretch(tabBarGo.GetComponent<RectTransform>(), new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);

            var loadTab = CreateButton(tabBarGo.transform, "Tab_Load", "LOAD", new Vector2(0f, 0f), new Vector2(0.48f, 1f));
            var saveTab = CreateButton(tabBarGo.transform, "Tab_Save", "SAVE", new Vector2(0.52f, 0f), new Vector2(1f, 1f));

            var listGo = new GameObject("SlotList", typeof(RectTransform));
            listGo.transform.SetParent(panelGo.transform, false);
            Stretch(listGo.GetComponent<RectTransform>(), new Vector2(0.05f, 0.2f), new Vector2(0.58f, 0.8f), Vector2.zero, Vector2.zero);

            var detail = CreateText(panelGo.transform, "Detail", "Select a slot.", 20, TextAnchor.UpperLeft);
            Stretch(detail.rectTransform, new Vector2(0.6f, 0.42f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
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
            view._loadTabButton = loadTab.Button;
            view._loadTabLabel = loadTab.Label;
            view._saveTabButton = saveTab.Button;
            view._saveTabLabel = saveTab.Label;
            view._rows = new SlotRow[GameMetaSaveLoad.SlotCount];

            for (var i = 0; i < GameMetaSaveLoad.SlotCount; i++)
            {
                view._rows[i] = CreateSlotRow(listGo.transform, i);
            }

            primary.Button.onClick.AddListener(view.OnPrimaryClicked);
            delete.Button.onClick.AddListener(view.OnDeleteClicked);
            close.Button.onClick.AddListener(view.Hide);

            view._confirmDialog = ConfirmDialogView.Ensure(rootGo.transform);
            view.EnsureBound();

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
