using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Modal Yes/No dùng chung. Hoạt động theo hai kiểu: nếu scene đã dựng sẵn hierarchy thì gán
    /// các reference vào Inspector, còn không thì <see cref="Ensure"/> tự dựng bằng code lúc runtime.
    /// ESC luôn tương đương Cancel.
    /// </summary>
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        [Header("Scene bindings (bỏ trống nếu dựng runtime)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text cancelLabel;

        private Action _onConfirm;
        private Action _onCancel;
        private bool _wired;

        public bool IsOpen => canvasGroup != null && canvasGroup.gameObject.activeSelf;

        /// <summary>Lấy dialog có sẵn dưới parent, hoặc dựng mới nếu chưa có.</summary>
        public static ConfirmDialogView Ensure(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogError("[Fractured Chorus] ConfirmDialogView.Ensure: parent null.");
                return null;
            }

            var existing = parent.GetComponentInChildren<ConfirmDialogView>(true);
            if (existing != null)
            {
                existing.EnsureWired();
                return existing;
            }

            return Build(parent);
        }

        public void Ask(
            string title,
            string message,
            Action onConfirm,
            Action onCancel = null,
            string confirmText = "YES",
            string cancelText = "NO")
        {
            EnsureWired();

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (titleLabel != null)
            {
                titleLabel.text = title ?? string.Empty;
            }

            if (messageLabel != null)
            {
                messageLabel.text = message ?? string.Empty;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = confirmText;
            }

            if (cancelLabel != null)
            {
                cancelLabel.text = cancelText;
            }

            if (canvasGroup != null)
            {
                canvasGroup.gameObject.SetActive(true);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            transform.SetAsLastSibling();
            UiEscapeGate.Push(this);
        }

        public void Close()
        {
            _onConfirm = null;
            _onCancel = null;
            UiEscapeGate.Pop(this);
            HideVisuals();
        }

        private void Awake()
        {
            EnsureWired();
            HideVisuals();
        }

        private void OnDisable()
        {
            UiEscapeGate.Pop(this);
        }

        private void Update()
        {
            if (IsOpen && UiCancelInput.WasPressed() && UiEscapeGate.TryConsume(this))
            {
                OnCancelClicked();
            }
        }

        /// <summary>
        /// Nối sự kiện đúng một lần. Cần thiết vì dialog dựng từ scene không đi qua Build().
        /// </summary>
        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            _wired = true;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);
        }

        private void OnConfirmClicked()
        {
            var callback = _onConfirm;
            Close();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = _onCancel;
            Close();
            callback?.Invoke();
        }

        private void HideVisuals()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }

        private static ConfirmDialogView Build(Transform parent)
        {
            var rootGo = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(rootGo.transform, false);
            Stretch(dimGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            dimGo.GetComponent<Image>().color = FcColorTokens.Surface.DimmerBlack;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(rootGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 260f);
            panelGo.GetComponent<Image>().color = FcColorTokens.Surface.Modal;

            var title = CreateText(panelGo.transform, "Title", "CONFIRM", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(title.rectTransform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.92f));
            title.color = FcColorTokens.Brand.Cyan;

            var message = CreateText(panelGo.transform, "Message", string.Empty, 20, TextAnchor.UpperCenter);
            Stretch(message.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.72f));
            message.color = FcColorTokens.Brand.TextPrimary;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;

            var confirm = CreateButton(panelGo.transform, "ConfirmButton", "YES", new Vector2(0.1f, 0.1f), new Vector2(0.46f, 0.28f));
            var cancel = CreateButton(panelGo.transform, "CancelButton", "NO", new Vector2(0.54f, 0.1f), new Vector2(0.9f, 0.28f));
            confirm.Label.color = FcColorTokens.Brand.RedSelection;

            var view = rootGo.AddComponent<ConfirmDialogView>();
            view.canvasGroup = rootGo.GetComponent<CanvasGroup>();
            view.titleLabel = title;
            view.messageLabel = message;
            view.confirmButton = confirm.Button;
            view.confirmLabel = confirm.Label;
            view.cancelButton = cancel.Button;
            view.cancelLabel = cancel.Label;
            view.EnsureWired();

            rootGo.SetActive(false);
            return view;
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
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var image = go.GetComponent<Image>();
            image.color = FcColorTokens.Surface.Row;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(go.transform, "Label", labelText, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
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
            text.font = UiFontCatalog.Body;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
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
