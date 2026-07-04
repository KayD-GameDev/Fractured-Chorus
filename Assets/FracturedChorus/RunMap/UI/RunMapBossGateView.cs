using System;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    /// <summary>Boss gate overlay — hiện sau khi chọn node F16; không load combat ngay.</summary>
    public class RunMapBossGateView : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(0.04f, 0.05f, 0.08f, 0.82f);
        private static readonly Color PanelColor = new Color(0.11f, 0.12f, 0.15f, 0.98f);
        private static readonly Color AccentColor = new Color(0.75f, 0.22f, 0.18f, 1f);

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button fightButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text fightLabel;
        [SerializeField] private Text cancelLabel;

        private Action _onFight;
        private Action _onCancel;
        private bool _built;

        public bool IsVisible => gameObject.activeSelf;

        public void Show(Action onFight, Action onCancel)
        {
            EnsureBuilt();
            _onFight = onFight;
            _onCancel = onCancel;
            SetLoading(false);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            SetLoading(false);
            gameObject.SetActive(false);
            _onFight = null;
            _onCancel = null;
        }

        public void SetLoading(bool loading)
        {
            if (fightButton != null)
            {
                fightButton.interactable = !loading;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = !loading;
            }

            if (fightLabel != null)
            {
                fightLabel.text = loading ? "Entering battle…" : "Enter battle";
            }
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                WireButtons();
                return;
            }

            _built = true;
            BuildUi();
            WireButtons();
            gameObject.SetActive(false);
        }

        private void WireButtons()
        {
            if (fightButton != null)
            {
                fightButton.onClick.RemoveListener(HandleFight);
                fightButton.onClick.AddListener(HandleFight);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancel);
                cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        private void HandleFight() => _onFight?.Invoke();

        private void HandleCancel() => _onCancel?.Invoke();

        private void BuildUi()
        {
            var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            StretchFull(root);

            var dim = CreateImage("Dim", transform, DimColor);
            StretchFull(dim.rectTransform);

            var panel = CreateImage("Panel", transform, PanelColor);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520f, 320f);

            titleText = CreateText("Title", panel.transform, "Oni — Cadence Vault", 28, FontStyle.Bold,
                new Color(0.95f, 0.94f, 0.92f), TextAnchor.UpperCenter);
            PlaceText(titleText.rectTransform, new Vector2(0f, -28f), new Vector2(480f, 40f));

            bodyText = CreateText(
                "Body",
                panel.transform,
                "F16 · Boss Beat Timeline\nRen covers the stolen hit song.",
                20,
                FontStyle.Normal,
                new Color(0.78f, 0.8f, 0.84f),
                TextAnchor.UpperCenter);
            PlaceText(bodyText.rectTransform, new Vector2(0f, -88f), new Vector2(460f, 72f));

            hintText = CreateText(
                "Hint",
                panel.transform,
                "Prepare formation · EXECUTE on beat",
                16,
                FontStyle.Italic,
                new Color(0.58f, 0.6f, 0.66f),
                TextAnchor.UpperCenter);
            PlaceText(hintText.rectTransform, new Vector2(0f, -168f), new Vector2(440f, 28f));

            fightButton = CreateButton("FightButton", panel.transform, AccentColor, out fightLabel, "Enter battle");
            var fightRect = fightButton.GetComponent<RectTransform>();
            fightRect.anchorMin = new Vector2(0.5f, 0f);
            fightRect.anchorMax = new Vector2(0.5f, 0f);
            fightRect.pivot = new Vector2(0.5f, 0f);
            fightRect.anchoredPosition = new Vector2(64f, 28f);
            fightRect.sizeDelta = new Vector2(200f, 48f);

            cancelButton = CreateButton(
                "CancelButton",
                panel.transform,
                new Color(0.22f, 0.24f, 0.28f, 1f),
                out cancelLabel,
                "Back to map");
            var cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(-64f, 28f);
            cancelRect.sizeDelta = new Vector2(200f, 48f);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void PlaceText(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiCircleSpriteUtil.White;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Color bgColor,
            out Text label,
            string labelTextValue)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiCircleSpriteUtil.White;
            image.type = Image.Type.Simple;
            image.color = bgColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            label = CreateText("Label", go.transform, labelTextValue, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(label.rectTransform);
            return button;
        }

        public static RunMapBossGateView EnsureOnCanvas(Transform canvas)
        {
            var existing = canvas.GetComponentInChildren<RunMapBossGateView>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("BossGateOverlay", typeof(RectTransform), typeof(RunMapBossGateView));
            go.transform.SetParent(canvas, false);
            return go.GetComponent<RunMapBossGateView>();
        }
    }
}
