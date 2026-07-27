using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Clips long single-line text and scrolls right-to-left when it overflows the viewport.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MarqueeTextUI : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float scrollSpeed = 42f;
        [SerializeField] private float edgePauseSeconds = 1.25f;

        private RectTransform _viewport;
        private RectTransform _labelRect;
        private string _currentText = string.Empty;
        private bool _marqueeActive;
        private float _scrollOffset;
        private float _pauseTimer;
        private MarqueePhase _phase = MarqueePhase.StartPause;

        private enum MarqueePhase
        {
            StartPause = 0,
            Scrolling = 1,
            EndPause = 2
        }

        public Text Label => label;

        public void BindLabel(Text text)
        {
            label = text;
            CacheRefs();
        }

        public void SetText(string text)
        {
            if (label == null)
            {
                return;
            }

            var next = text ?? string.Empty;
            if (_currentText == next && label.text == next)
            {
                return;
            }

            _currentText = next;
            label.text = next;
            RefreshLayout();
        }

        private void Awake()
        {
            CacheRefs();
            if (label != null && string.IsNullOrEmpty(_currentText))
            {
                _currentText = label.text ?? string.Empty;
            }

            RefreshLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (label != null)
            {
                RefreshLayout();
            }
        }

        private void Update()
        {
            if (!_marqueeActive || label == null || _labelRect == null || _viewport == null)
            {
                return;
            }

            var viewWidth = _viewport.rect.width;
            var textWidth = label.preferredWidth;
            var scrollRange = textWidth - viewWidth;
            if (scrollRange <= 1f)
            {
                _marqueeActive = false;
                ApplyCenteredLayout();
                return;
            }

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.unscaledDeltaTime;
                return;
            }

            switch (_phase)
            {
                case MarqueePhase.StartPause:
                    _phase = MarqueePhase.Scrolling;
                    break;

                case MarqueePhase.Scrolling:
                    _scrollOffset += scrollSpeed * Time.unscaledDeltaTime;
                    if (_scrollOffset >= scrollRange)
                    {
                        _scrollOffset = scrollRange;
                        _labelRect.anchoredPosition = new Vector2(-_scrollOffset, 0f);
                        _phase = MarqueePhase.EndPause;
                        _pauseTimer = edgePauseSeconds;
                        return;
                    }

                    break;

                case MarqueePhase.EndPause:
                    _scrollOffset = 0f;
                    _labelRect.anchoredPosition = Vector2.zero;
                    _phase = MarqueePhase.StartPause;
                    _pauseTimer = edgePauseSeconds;
                    return;
            }

            _labelRect.anchoredPosition = new Vector2(-_scrollOffset, 0f);
        }

        private void CacheRefs()
        {
            _viewport = transform as RectTransform;
            if (label != null)
            {
                _labelRect = label.rectTransform;
            }
        }

        private void RefreshLayout()
        {
            if (label == null || _viewport == null)
            {
                CacheRefs();
            }

            if (label == null || _viewport == null)
            {
                return;
            }

            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            Canvas.ForceUpdateCanvases();
            var viewWidth = _viewport.rect.width;
            var textWidth = label.preferredWidth;
            _marqueeActive = textWidth > viewWidth + 1f;

            if (_marqueeActive)
            {
                ApplyMarqueeLayout(textWidth);
                _scrollOffset = 0f;
                _phase = MarqueePhase.StartPause;
                _pauseTimer = edgePauseSeconds;
                _labelRect.anchoredPosition = Vector2.zero;
                return;
            }

            ApplyCenteredLayout();
        }

        private void ApplyCenteredLayout()
        {
            label.alignment = TextAnchor.MiddleCenter;
            _labelRect.anchorMin = Vector2.zero;
            _labelRect.anchorMax = Vector2.one;
            _labelRect.pivot = new Vector2(0.5f, 0.5f);
            _labelRect.offsetMin = Vector2.zero;
            _labelRect.offsetMax = Vector2.zero;
            _labelRect.anchoredPosition = Vector2.zero;
        }

        private void ApplyMarqueeLayout(float textWidth)
        {
            label.alignment = TextAnchor.MiddleLeft;
            _labelRect.anchorMin = new Vector2(0f, 0.5f);
            _labelRect.anchorMax = new Vector2(0f, 0.5f);
            _labelRect.pivot = new Vector2(0f, 0.5f);
            _labelRect.sizeDelta = new Vector2(textWidth, _viewport.rect.height);
            _labelRect.anchoredPosition = Vector2.zero;
        }
    }
}
