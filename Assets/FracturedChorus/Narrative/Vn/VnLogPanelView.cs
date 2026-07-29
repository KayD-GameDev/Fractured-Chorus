using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnLogPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Text titleText;

        private bool _open;
        private bool _wired;
        private RectTransform _panel;
        private RectTransform _displayRoot;
        private RectTransform _content;
        private Text _displayText;

        public bool IsOpen => _open;

        private void Awake()
        {
            EnsureWired();
            if (!_open)
            {
                HideImmediate();
            }
        }

        public void Show(VnDialogueLog log)
        {
            EnsureWired();
            if (root == null)
            {
                return;
            }

            _open = true;
            if (!root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }

            root.alpha = 1f;
            root.blocksRaycasts = true;
            root.interactable = true;

            var convenienceRoot = transform.parent;
            if (convenienceRoot != null)
            {
                convenienceRoot.SetAsLastSibling();
            }

            transform.SetAsLastSibling();
            Rebuild(log ?? VnDialogueLog.Session);
        }

        public void Refresh(VnDialogueLog log)
        {
            if (!_open)
            {
                return;
            }

            Rebuild(log ?? VnDialogueLog.Session);
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            _wired = true;

            if (root == null)
            {
                root = GetComponent<CanvasGroup>();
            }

            _panel = transform.Find("Panel") as RectTransform;
            if (panelBackground == null && _panel != null)
            {
                panelBackground = _panel.GetComponent<Image>();
            }

            if (panelBackground != null)
            {
                panelBackground.raycastTarget = false;
                panelBackground.color = new Color(0.03f, 0.06f, 0.12f, 0.98f);
            }

            if (backdropButton != null)
            {
                var backdropImage = backdropButton.GetComponent<Image>();
                if (backdropImage != null)
                {
                    backdropImage.color = new Color(0f, 0f, 0f, 0.85f);
                }
            }

            if (scrollRect != null)
            {
                scrollRect.gameObject.SetActive(false);
            }
            else
            {
                var oldScroll = transform.Find("Panel/Scroll");
                if (oldScroll != null)
                {
                    oldScroll.gameObject.SetActive(false);
                }
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(false);
            }

            EnsureTitle();
            EnsureDisplay();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
                closeButton.transform.SetAsLastSibling();
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveListener(Hide);
                backdropButton.onClick.AddListener(Hide);
            }
        }

        private void EnsureTitle()
        {
            if (_panel == null)
            {
                return;
            }

            if (titleText == null)
            {
                var existing = _panel.Find("LogTitle");
                if (existing != null)
                {
                    titleText = existing.GetComponent<Text>();
                }
            }

            if (titleText != null)
            {
                return;
            }

            var go = new GameObject("LogTitle", typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -16f);
            rect.sizeDelta = new Vector2(-160f, 36f);
            titleText = go.AddComponent<Text>();
            ApplyChromeFont(titleText, 22);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.75f, 0.9f, 1f, 1f);
            titleText.raycastTarget = false;
        }

        private void EnsureDisplay()
        {
            if (_panel == null)
            {
                return;
            }

            var existing = _panel.Find("LogDisplay");
            if (existing != null)
            {
                _displayRoot = existing as RectTransform;
                scrollRect = existing.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    _content = scrollRect.content;
                    if (_content != null)
                    {
                        _displayText = _content.GetComponentInChildren<Text>(true);
                    }
                }
            }

            if (_displayRoot == null)
            {
                var rootGo = new GameObject("LogDisplay", typeof(RectTransform));
                _displayRoot = rootGo.GetComponent<RectTransform>();
                _displayRoot.SetParent(_panel, false);
                _displayRoot.anchorMin = new Vector2(0.04f, 0.06f);
                _displayRoot.anchorMax = new Vector2(0.96f, 0.84f);
                _displayRoot.offsetMin = Vector2.zero;
                _displayRoot.offsetMax = Vector2.zero;

                scrollRect = rootGo.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 40f;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform));
                var viewport = viewportGo.GetComponent<RectTransform>();
                viewport.SetParent(_displayRoot, false);
                Stretch(viewport);
                viewportGo.AddComponent<RectMask2D>();
                var viewportImage = viewportGo.AddComponent<Image>();
                viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
                viewportImage.raycastTarget = true;

                var contentGo = new GameObject("Content", typeof(RectTransform));
                _content = contentGo.GetComponent<RectTransform>();
                _content.SetParent(viewport, false);
                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot = new Vector2(0.5f, 1f);
                _content.anchoredPosition = Vector2.zero;
                _content.sizeDelta = new Vector2(0f, 100f);

                var textGo = new GameObject("Body", typeof(RectTransform));
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.SetParent(_content, false);
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(0f, 100f);
                _displayText = textGo.AddComponent<Text>();

                scrollRect.viewport = viewport;
                scrollRect.content = _content;
            }

            if (_displayText == null && _content != null)
            {
                _displayText = _content.GetComponentInChildren<Text>(true);
            }

            if (_displayText == null)
            {
                return;
            }

            ApplyDialogueFont(_displayText, 28);
            _displayText.alignment = TextAnchor.UpperLeft;
            _displayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _displayText.verticalOverflow = VerticalWrapMode.Overflow;
            _displayText.raycastTarget = false;
            _displayText.color = Color.white;
            _displayText.supportRichText = false;
            _displayText.lineSpacing = 1.2f;
            _displayText.gameObject.SetActive(true);
            _displayRoot.SetAsLastSibling();
            if (closeButton != null)
            {
                closeButton.transform.SetAsLastSibling();
            }
        }

        private void Rebuild(VnDialogueLog log)
        {
            EnsureWired();

            var count = log != null ? log.Count : 0;
            if (titleText != null)
            {
                titleText.text = count <= 0 ? "LOG" : $"LOG  ·  {count}";
                titleText.transform.SetAsLastSibling();
            }

            if (_displayText == null || _content == null)
            {
                return;
            }

            _displayText.text = BuildBody(log);
            _displayText.color = Color.white;
            if (_displayText.font == null)
            {
                ApplyDialogueFont(_displayText, 28);
            }

            Canvas.ForceUpdateCanvases();
            var width = 800f;
            if (scrollRect != null && scrollRect.viewport != null && scrollRect.viewport.rect.width > 8f)
            {
                width = scrollRect.viewport.rect.width;
            }
            else if (_displayRoot != null && _displayRoot.rect.width > 8f)
            {
                width = _displayRoot.rect.width;
            }

            var height = MeasureHeight(_displayText, width);
            var textRect = _displayText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            if (closeButton != null)
            {
                closeButton.transform.SetAsLastSibling();
            }
        }

        private static float MeasureHeight(Text text, float width)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return 64f;
            }

            var settings = text.GetGenerationSettings(new Vector2(Mathf.Max(64f, width), 0f));
            settings.generateOutOfBounds = true;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = new TextGenerator();
            generator.Populate(text.text, settings);
            var height = generator.GetPreferredHeight(text.text, settings) / Mathf.Max(0.01f, text.pixelsPerUnit);
            if (height < 8f)
            {
                var lines = 1;
                for (var i = 0; i < text.text.Length; i++)
                {
                    if (text.text[i] == '\n')
                    {
                        lines++;
                    }
                }

                height = text.fontSize * 1.4f * lines;
            }

            return Mathf.Max(80f, height + 40f);
        }

        private static string BuildBody(VnDialogueLog log)
        {
            if (log == null || log.Count == 0)
            {
                return "(No dialogue yet)";
            }

            var builder = new StringBuilder(log.Count * 96);
            for (var i = 0; i < log.Entries.Count; i++)
            {
                var entry = log.Entries[i];
                if (!string.IsNullOrWhiteSpace(entry.Speaker))
                {
                    builder.Append(entry.Speaker);
                    builder.Append('\n');
                }

                builder.Append(entry.Text);
                if (i < log.Entries.Count - 1)
                {
                    builder.Append("\n\n");
                }
            }

            return builder.ToString();
        }

        private static void ApplyChromeFont(Text text, int size)
        {
            if (text == null)
            {
                return;
            }

            UiFontCatalog.Apply(text, UiFontRole.Display, size);
        }

        private static void ApplyDialogueFont(Text text, int size)
        {
            if (text == null)
            {
                return;
            }

            VnUiFont.Apply(text, size, FontStyle.Normal);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void HideImmediate()
        {
            _open = false;
            if (root == null)
            {
                root = GetComponent<CanvasGroup>();
            }

            if (root == null)
            {
                return;
            }

            root.alpha = 0f;
            root.blocksRaycasts = false;
            root.interactable = false;
            if (root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
            }
        }
    }
}
