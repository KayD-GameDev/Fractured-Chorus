using System;
using FracturedChorus.Menu;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class HubConfigOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueLabel;
        [SerializeField] private Button closeButton;

        private Action _onClosed;
        private bool _bound;

        public bool IsOpen => root != null && root.activeSelf;

        public static HubConfigOverlayUI Ensure(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.GetComponentInChildren<HubConfigOverlayUI>(true);
            if (existing != null)
            {
                return existing;
            }

            return Build(parent);
        }

        public static HubConfigOverlayUI Show(Transform parent, Action onClosed = null)
        {
            if (parent == null)
            {
                Debug.LogError("[HubConfig] parent null.");
                return null;
            }

            var existing = Ensure(parent);
            existing.Open(onClosed);
            return existing;
        }

        public void Open(Action onClosed)
        {
            EnsureBound();
            _onClosed = onClosed;

            if (root != null)
            {
                root.SetActive(true);
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(MainMenuGameSettings.MasterVolume);
            }

            RefreshVolumeLabel(MainMenuGameSettings.MasterVolume);
            transform.SetAsLastSibling();
            UiEscapeGate.Push(this);
        }

        public void Hide()
        {
            UiEscapeGate.Pop(this);
            if (root != null)
            {
                root.SetActive(false);
            }

            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        private void Update()
        {
            if (IsOpen && UiCancelInput.WasPressed() && UiEscapeGate.TryConsume(this))
            {
                Hide();
            }
        }

        private void OnDisable()
        {
            UiEscapeGate.Pop(this);
        }

        private void EnsureBound()
        {
            if (_bound)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(value =>
                {
                    MainMenuGameSettings.SetMasterVolume(value);
                    RefreshVolumeLabel(value);
                });
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            _bound = true;
        }

        private void RefreshVolumeLabel(float value)
        {
            if (volumeValueLabel == null)
            {
                return;
            }

            volumeValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private static HubConfigOverlayUI Build(Transform parent)
        {
            var rootGo = new GameObject("HubConfigOverlay", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(parent, false);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var dimGo = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(rootGo.transform, false);
            var dimRect = dimGo.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImage = dimGo.GetComponent<Image>();
            dimImage.color = FcColorTokens.Surface.DimmerBlack;
            dimImage.raycastTarget = true;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(rootGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.38f);
            panelRect.anchorMax = new Vector2(0.7f, 0.62f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = FcColorTokens.Surface.Modal;
            panelImage.raycastTarget = true;

            var title = CreateText(panelGo.transform, "Title", "CONFIG", 28, TextAnchor.MiddleCenter);
            Stretch(title.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.color = FcColorTokens.Brand.Cyan;

            var volumeLabel = CreateText(panelGo.transform, "VolumeLabel", "Volume", 20, TextAnchor.MiddleLeft);
            Stretch(volumeLabel.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.35f, 0.62f));
            volumeLabel.color = FcColorTokens.Brand.TextPrimary;

            var valueLabel = CreateText(panelGo.transform, "VolumeValue", "85%", 20, TextAnchor.MiddleRight);
            Stretch(valueLabel.rectTransform, new Vector2(0.72f, 0.38f), new Vector2(0.92f, 0.62f));
            valueLabel.color = FcColorTokens.Brand.Cyan;

            var sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(panelGo.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            Stretch(sliderRect, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.36f));

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            Stretch(bgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = FcColorTokens.Surface.Track;
            bgImage.raycastTarget = true;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.75f));

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            Stretch(fillGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.color = FcColorTokens.Brand.CyanSoft;
            fillImage.raycastTarget = false;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleArea.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 28f);
            var handleImage = handleGo.GetComponent<Image>();
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;

            var slider = sliderGo.GetComponent<Slider>();
            slider.targetGraphic = handleImage;
            slider.fillRect = fillGo.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panelGo.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.86f, 0.78f);
            closeRect.anchorMax = new Vector2(0.96f, 0.94f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            var closeImage = closeGo.GetComponent<Image>();
            closeImage.color = FcColorTokens.Brand.CyanDim;
            var closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = CreateText(closeGo.transform, "Label", "X", 18, TextAnchor.MiddleCenter);
            Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.one);
            closeLabel.color = Color.white;

            var overlay = rootGo.AddComponent<HubConfigOverlayUI>();
            overlay.root = rootGo;
            overlay.volumeSlider = slider;
            overlay.volumeValueLabel = valueLabel;
            overlay.closeButton = closeButton;
            rootGo.SetActive(false);
            return overlay;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFontCatalog.ApplyAutomatic(text);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
