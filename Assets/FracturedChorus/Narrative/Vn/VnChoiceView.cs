using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text promptText;
        [SerializeField] private RectTransform optionsRoot;
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.72f, 0.95f, 0.95f);
        [SerializeField] private Color idleColor = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        private readonly Text[] _labels = new Text[3];
        private readonly Image[] _backgrounds = new Image[3];
        private readonly Button[] _buttons = new Button[3];
        private string[] _options = Array.Empty<string>();
        private int _selectedIndex;
        private bool _active;
        private int _ignoreInputUntilFrame = -1;
        private Action<int> _onChosen;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Hide()
        {
            _active = false;
            _onChosen = null;
            _ignoreInputUntilFrame = -1;
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
            EnsureUi();
            _options = options ?? Array.Empty<string>();
            _onChosen = onChosen;
            _selectedIndex = 0;
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

            RefreshVisuals();

            if (root != null)
            {
                root.gameObject.SetActive(true);
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }
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

            _selectedIndex = (_selectedIndex + delta + count) % count;
            RefreshVisuals();
        }

        private void Confirm()
        {
            if (!_active)
            {
                return;
            }

            var count = VisibleCount();
            if (count <= 0 || _selectedIndex < 0 || _selectedIndex >= count)
            {
                return;
            }

            var chosen = _selectedIndex;
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
            for (var i = 0; i < _backgrounds.Length; i++)
            {
                if (_backgrounds[i] == null)
                {
                    continue;
                }

                _backgrounds[i].color = i == _selectedIndex ? selectedColor : idleColor;
            }
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
                Stretch(optionsRoot, new Vector2(0.55f, 0.28f), new Vector2(0.96f, 0.72f));
            }

            if (promptText == null)
            {
                var promptGo = new GameObject("Prompt", typeof(RectTransform));
                promptGo.transform.SetParent(transform, false);
                var promptRect = promptGo.GetComponent<RectTransform>();
                Stretch(promptRect, new Vector2(0.55f, 0.74f), new Vector2(0.96f, 0.9f));
                promptText = promptGo.AddComponent<Text>();
                VnUiFont.Apply(promptText, 26, FontStyle.Bold);
                promptText.alignment = TextAnchor.MiddleLeft;
                promptText.color = Color.white;
                promptText.raycastTarget = false;
            }

            for (var i = 0; i < 3; i++)
            {
                if (_buttons[i] != null)
                {
                    continue;
                }

                var row = optionsRoot.Find($"Option_{i}");
                if (row == null)
                {
                    var rowGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    rowGo.transform.SetParent(optionsRoot, false);
                    var rowRect = rowGo.GetComponent<RectTransform>();
                    var yMax = 1f - (i * 0.34f);
                    var yMin = yMax - 0.3f;
                    Stretch(rowRect, new Vector2(0f, yMin), new Vector2(1f, yMax));

                    var bg = rowGo.GetComponent<Image>();
                    bg.color = idleColor;

                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(rowGo.transform, false);
                    Stretch(labelGo.GetComponent<RectTransform>(), new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f));
                    var label = labelGo.AddComponent<Text>();
                    VnUiFont.Apply(label, 28, FontStyle.Normal);
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
                        Confirm();
                    });
                }
            }
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
