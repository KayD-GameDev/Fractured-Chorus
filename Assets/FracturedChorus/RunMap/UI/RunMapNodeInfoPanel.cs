using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    public class RunMapNodeInfoPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private bool showEditPreviewInScene = true;
        [SerializeField] private MapNodeType editPreviewType = MapNodeType.Battle;

        private MapNodeData _boundNode;
        private System.Action<MapNodeData> _onConfirm;
        private System.Action _onCancel;

        public void Wire(
            RectTransform panel,
            Text title,
            Text body,
            Text hint,
            Button confirm,
            Button close,
            Text confirmText)
        {
            panelRoot = panel;
            titleText = title;
            bodyText = body;
            hintText = hint;
            confirmButton = confirm;
            closeButton = close;
            confirmLabel = confirmText;
            BindButtons();
        }

        private void BindButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCancelClicked);
            }
        }

        public void Show(
            MapNodeData node,
            bool canTravel,
            System.Action<MapNodeData> onConfirm,
            System.Action onCancel = null)
        {
            if (node == null || panelRoot == null)
            {
                return;
            }

            _boundNode = node;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            gameObject.SetActive(true);
            panelRoot.gameObject.SetActive(true);
            BindButtons();

            if (titleText != null)
            {
                titleText.text = MapNodeCatalog.Title(node.Type);
            }

            if (bodyText != null)
            {
                bodyText.text = MapNodeCatalog.Description(node.Type);
            }

            if (hintText != null)
            {
                hintText.text = node.IsSavePoint
                    ? "★ Save point"
                    : "Không lưu khi vào node này.";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(canTravel);
                confirmButton.interactable = canTravel;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = node.Type == MapNodeType.Start ? "Continue" : "Travel";
            }
        }

        public void ShowTypeInfo(MapNodeType type)
        {
            Show(
                new MapNodeData
                {
                    Type = type,
                    IsBoss = type == MapNodeType.Boss
                },
                canTravel: false,
                onConfirm: null,
                onCancel: Hide);
        }

        public void Hide()
        {
            _boundNode = null;
            _onConfirm = null;
            _onCancel = null;
            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        public void ShowEditPreview(MapNodeType type = MapNodeType.Battle)
        {
            if (panelRoot == null)
            {
                return;
            }

            editPreviewType = type;
            _boundNode = null;
            _onConfirm = null;
            gameObject.SetActive(true);
            panelRoot.gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = MapNodeCatalog.Title(type);
            }

            if (bodyText != null)
            {
                bodyText.text = MapNodeCatalog.Description(type);
            }

            if (hintText != null)
            {
                hintText.text = MapNodeCatalog.IsSavePoint(type, type == MapNodeType.Boss, false)
                    ? "★ Save point"
                    : "Không lưu khi vào node này.";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
                confirmButton.interactable = false;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = type == MapNodeType.Start ? "Continue" : "Travel";
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private void Awake()
        {
            BindButtons();
            if (Application.isPlaying)
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            if (panelRoot == null)
            {
                Hide();
                return;
            }

            if (Application.isPlaying)
            {
                if (_boundNode == null)
                {
                    Hide();
                }

                return;
            }

#if UNITY_EDITOR
            if (showEditPreviewInScene)
            {
                ShowEditPreview(editPreviewType);
            }
#endif
        }

        private void OnConfirmClicked()
        {
            if (_boundNode == null)
            {
                return;
            }

            var node = _boundNode;
            var callback = _onConfirm;
            Hide();
            callback?.Invoke(node);
        }

        private void OnCancelClicked()
        {
            var callback = _onCancel;
            Hide();
            callback?.Invoke();
        }
    }
}
