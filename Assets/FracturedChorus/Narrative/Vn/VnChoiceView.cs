using System;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text promptText;
        [SerializeField] private RectTransform optionsRoot;

        private readonly Text[] _labels = new Text[3];
        private readonly Image[] _backgrounds = new Image[3];
        private readonly Button[] _buttons = new Button[3];
        private string[] _options = Array.Empty<string>();
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _active;
        private int _ignoreInputUntilFrame = -1;
        private Action<int> _onChosen;

        private Color _selectedColor;
        private Color _hoverColor;
        private Color _idleColor;

        private void Awake()
        {
            NormalizeColors();
            EnsureUi();
            Hide();
        }

        public void Hide()
        {
            _active = false;
            _onChosen = null;
            _ignoreInputUntilFrame = -1;
            _hoverIndex = -1;
            if (root != null)
            {
                root.alpha = 0f;
                root.interactable = false;
                root.blocksRaycasts = false;
                root.gameObject.SetActive(false);
            }
        }

        public void Show(string prompt, string[] options, Action<int> onChosen)
        {
            NormalizeColors();
            EnsureUi();
            _options = options ?? Array.Empty<string>();
            _onChosen = onChosen;
            _selectedIndex = VisibleCount() > 0 ? 0 : -1;
            _hoverIndex = -1;
            _ignoreInputUntilFrame = Time.frameCount + 1;
            _active = true;

            if (promptText != null)
            {
                var hasPrompt = !string.IsNullOrWhiteSpace(prompt);
                promptText.gameObject.SetActive(hasPrompt);
                promptText.text = hasPrompt ? prompt : string.Empty;
            }

            for (var i = 0; i < _labels.Length; i++)
            {
                var visible = i < _options.Length && !string.IsNullOrWhiteSpace(_options[i]);
                if (_buttons[i] != null)
                {
                    _buttons[i].gameObject.SetActive(visible);
                }

                if (visible && _labels[i] != null)
                {
                    _labels[i].text = _options[i];
                }
            }

            LayoutVisibleOptions();
            RefreshVisuals();

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }
        }

        public void SetPointerHover(int optionIndex)
        {
            if (!_active || Time.frameCount <= _ignoreInputUntilFrame)
            {
                return;
            }

            if (optionIndex < 0 || optionIndex >= VisibleCount() || optionIndex == _hoverIndex)
            {
                return;
            }

            _hoverIndex = optionIndex;
            RefreshVisuals();
        }

        public void ClearPointerHover(int optionIndex)
        {
            if (!_active || _hoverIndex != optionIndex)
            {
                return;
            }

            _hoverIndex = -1;
            RefreshVisuals();
        }

        private void Update()
        {
            if (!_active || Time.frameCount <= _ignoreInputUntilFrame)
            {
                return;
            }

            if (PrologueInput.WasUpPressedThisFrame())
            {
                MoveSelection(-1);
            }
            else if (PrologueInput.WasDownPressedThisFrame())
            {
                MoveSelection(1);
            }
            else if (VnInput.WasAdvancePressedThisFrame())
            {
                Confirm();
            }
        }

        private void MoveSelection(int delta)
        {
            var count = VisibleCount();
            if (count <= 0)
            {
                return;
            }

            if (_selectedIndex < 0)
            {
                _selectedIndex = delta > 0 ? 0 : count - 1;
            }
            else
            {
                _selectedIndex = (_selectedIndex + delta + count) % count;
            }

            _hoverIndex = -1;
            RefreshVisuals();
        }

        private void Confirm()
        {
            if (!_active)
            {
                return;
            }

            var count = VisibleCount();
            var chosen = _hoverIndex >= 0 && _hoverIndex < count ? _hoverIndex : _selectedIndex;
            if (count <= 0 || chosen < 0 || chosen >= count)
            {
                return;
            }

            var callback = _onChosen;
            Hide();
            callback?.Invoke(chosen);
        }

        private int VisibleCount()
        {
            var count = 0;
            for (var i = 0; i < _options.Length && i < _labels.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(_options[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private void RefreshVisuals()
        {
            var count = VisibleCount();
            for (var i = 0; i < _backgrounds.Length; i++)
            {
                if (_backgrounds[i] == null)
                {
                    continue;
                }

                if (i >= count)
                {
                    continue;
                }

                if (_selectedIndex == i)
                {
                    _backgrounds[i].color = _selectedColor;
                }
                else if (_hoverIndex == i)
                {
                    _backgrounds[i].color = _hoverColor;
                }
                else
                {
                    _backgrounds[i].color = _idleColor;
                }
            }
        }

        private void LayoutVisibleOptions()
        {
            if (optionsRoot == null)
            {
                return;
            }

            var count = VisibleCount();
            if (count <= 0)
            {
                return;
            }

            const float bottom = 0.04f;
            const float top = 0.96f;
            const float gap = 0.04f;
            var rowHeight = (top - bottom - gap * (count - 1)) / count;

            var slot = 0;
            for (var i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null)
                {
                    continue;
                }

                var visible = i < _options.Length && !string.IsNullOrWhiteSpace(_options[i]);
                if (!visible)
                {
                    continue;
                }

                var yMax = top - slot * (rowHeight + gap);
                var yMin = yMax - rowHeight;
                Stretch(_buttons[i].GetComponent<RectTransform>(), new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
                slot++;
            }
        }

        private void NormalizeColors()
        {
            _selectedColor = FcColorTokens.Selection.VnChoiceHighlight;
            _hoverColor = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanHover, 0.88f);
            _idleColor = FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0f);
        }

        private void EnsureUi()
        {
            if (root == null)
            {
                root = GetComponent<CanvasGroup>();
                if (root == null)
                {
                    root = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (optionsRoot == null)
            {
                var existing = transform.Find("Options");
                if (existing != null)
                {
                    optionsRoot = existing.GetComponent<RectTransform>();
                }
            }

            if (promptText == null)
            {
                var prompt = transform.Find("Prompt");
                if (prompt != null)
                {
                    promptText = prompt.GetComponent<Text>();
                }
            }

            if (optionsRoot == null)
            {
                var optionsGo = new GameObject("Options", typeof(RectTransform));
                optionsGo.transform.SetParent(transform, false);
                optionsRoot = optionsGo.GetComponent<RectTransform>();
                Stretch(optionsRoot, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            }

            if (promptText == null)
            {
                var promptGo = new GameObject("Prompt", typeof(RectTransform));
                promptGo.transform.SetParent(transform, false);
                var promptRect = promptGo.GetComponent<RectTransform>();
                Stretch(promptRect, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.96f));
                promptText = promptGo.AddComponent<Text>();
                UiFontCatalog.Apply(promptText, UiFontRole.Display, 26);
                promptText.alignment = TextAnchor.MiddleLeft;
                promptText.color = Color.white;
                promptText.raycastTarget = false;
            }

            for (var i = 0; i < 3; i++)
            {
                if (_buttons[i] != null)
                {
                    EnsureRowPointer(_buttons[i].gameObject, i);
                    continue;
                }

                var row = optionsRoot.Find($"Option_{i}");
                if (row == null)
                {
                    var rowGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    rowGo.transform.SetParent(optionsRoot, false);

                    var bg = rowGo.GetComponent<Image>();
                    bg.color = _idleColor;

                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(rowGo.transform, false);
                    Stretch(labelGo.GetComponent<RectTransform>(), new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
                    var label = labelGo.AddComponent<Text>();
                    UiFontCatalog.Apply(label, UiFontRole.DisplaySecondary, 28);
                    label.alignment = TextAnchor.MiddleLeft;
                    label.color = Color.white;
                    label.raycastTarget = false;

                    _buttons[i] = rowGo.GetComponent<Button>();
                    _backgrounds[i] = bg;
                    _labels[i] = label;
                }
                else
                {
                    _buttons[i] = row.GetComponent<Button>();
                    _backgrounds[i] = row.GetComponent<Image>();
                    _labels[i] = row.Find("Label")?.GetComponent<Text>();
                }

                EnsureRowPointer(_buttons[i].gameObject, i);

                var index = i;
                if (_buttons[i] != null)
                {
                    _buttons[i].onClick.RemoveAllListeners();
                    _buttons[i].onClick.AddListener(() =>
                    {
                        if (!_active || Time.frameCount <= _ignoreInputUntilFrame)
                        {
                            return;
                        }

                        _selectedIndex = index;
                        _hoverIndex = -1;
                        Confirm();
                    });
                }
            }
        }

        private static void EnsureRowPointer(GameObject rowGo, int optionIndex)
        {
            if (rowGo == null)
            {
                return;
            }

            var view = rowGo.GetComponentInParent<VnChoiceView>();
            var pointer = rowGo.GetComponent<VnChoiceRowPointer>();
            if (pointer == null)
            {
                pointer = rowGo.AddComponent<VnChoiceRowPointer>();
            }

            pointer.Initialize(view, optionIndex);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
