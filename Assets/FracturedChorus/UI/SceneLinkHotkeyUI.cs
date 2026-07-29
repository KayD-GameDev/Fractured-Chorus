using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.UI
{
    public enum SceneLinkHotkeyPlacement
    {
        TopRight = 0,
        BottomLeft = 1,
        PromptBarInline = 2
    }

    /// <summary>
    /// Shortcut UI: key badge + action label; click or press B to activate.
    /// </summary>
    public sealed class SceneLinkHotkeyUI : MonoBehaviour
    {
        public const string DefaultObjectName = "SceneLinkHotkey";
        public const string OverlayObjectName = "SceneLinkOverlay";

        [SerializeField] private Button button;
        [SerializeField] private Text keyBadge;
        [SerializeField] private Text actionLabel;

        private Action _onActivate;
        private bool _listening;
        private SceneLinkHotkeyPlacement _placement = SceneLinkHotkeyPlacement.TopRight;

        public static Transform EnsureSceneLinkOverlay(Transform canvasRoot, Transform insertAboveLayer = null)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var overlayTransform = canvasRoot.Find(OverlayObjectName);
            GameObject overlayGo;
            if (overlayTransform == null)
            {
                overlayGo = new GameObject(OverlayObjectName, typeof(RectTransform));
                overlayGo.transform.SetParent(canvasRoot, false);

                var rect = overlayGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);

                var overlayCanvas = overlayGo.AddComponent<Canvas>();
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = 10;
                overlayGo.AddComponent<GraphicRaycaster>();

                var group = overlayGo.AddComponent<CanvasGroup>();
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            else
            {
                overlayGo = overlayTransform.gameObject;
            }

            if (insertAboveLayer != null)
            {
                overlayGo.transform.SetSiblingIndex(insertAboveLayer.GetSiblingIndex() + 1);
            }
            else
            {
                overlayGo.transform.SetAsLastSibling();
            }

            return overlayGo.transform;
        }

        public static SceneLinkHotkeyUI Ensure(
            Transform parent,
            string actionText,
            Action onActivate,
            string objectName = DefaultObjectName,
            SceneLinkHotkeyPlacement placement = SceneLinkHotkeyPlacement.TopRight,
            bool bringToFront = false,
            Transform insertAboveLayer = null,
            bool persistInScene = false)
        {
            if (parent == null)
            {
                return null;
            }

            var link = FindOrCreate(parent, objectName, actionText, placement);
            link.Bind(onActivate);
            link.gameObject.SetActive(true);
            link.ApplyPlacement(placement);
            link.BringToFront(bringToFront, insertAboveLayer);
            if (Application.isPlaying && !persistInScene)
            {
                link.gameObject.hideFlags = HideFlags.DontSave;
            }
            else if (persistInScene)
            {
                link.gameObject.hideFlags = HideFlags.None;
            }

            return link;
        }

        public void Bind(Action onActivate)
        {
            _onActivate = onActivate;
            _listening = onActivate != null;
            ResolveRefs();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(Activate);
            if (_listening)
            {
                button.onClick.AddListener(Activate);
            }
        }

        public void SetListening(bool listening)
        {
            _listening = listening && _onActivate != null;
        }

        public void SetActionLabel(string actionText)
        {
            ResolveRefs();
            if (actionLabel != null)
            {
                actionLabel.text = actionText;
            }
        }

        private void Update()
        {
            if (!_listening || _onActivate == null || !isActiveAndEnabled)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                Activate();
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Activate);
            }
        }

        private void Activate()
        {
            _onActivate?.Invoke();
        }

        private void ResolveRefs()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (keyBadge == null)
            {
                var key = transform.Find("KeyBadge/Label");
                if (key != null)
                {
                    keyBadge = key.GetComponent<Text>();
                }
            }

            if (actionLabel == null)
            {
                var label = transform.Find("ActionLabel");
                if (label != null)
                {
                    actionLabel = label.GetComponent<Text>();
                }
            }
        }

        private static SceneLinkHotkeyUI FindOrCreate(
            Transform parent,
            string objectName,
            string actionText,
            SceneLinkHotkeyPlacement placement)
        {
            var existing = FindExisting(parent, objectName);
            if (existing != null)
            {
                if (existing.transform.parent != parent)
                {
                    existing.transform.SetParent(parent, false);
                }

                existing.ResolveRefs();
                existing.SetActionLabel(actionText);
                existing.ApplyPlacement(placement);
                return existing;
            }

            return Build(parent, objectName, actionText, placement);
        }

        private static SceneLinkHotkeyUI FindExisting(Transform parent, string objectName)
        {
            var onParent = parent.Find(objectName);
            if (onParent != null)
            {
                return onParent.GetComponent<SceneLinkHotkeyUI>()
                       ?? onParent.gameObject.AddComponent<SceneLinkHotkeyUI>();
            }

            var canvasRoot = parent.GetComponentInParent<Canvas>()?.transform ?? parent.root;
            if (canvasRoot == null)
            {
                return null;
            }

            foreach (Transform child in canvasRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != objectName)
                {
                    continue;
                }

                var link = child.GetComponent<SceneLinkHotkeyUI>();
                if (link != null)
                {
                    return link;
                }
            }

            return null;
        }

        private void ApplyPlacement(SceneLinkHotkeyPlacement placement)
        {
            _placement = placement;
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            var layout = GetComponent<LayoutElement>();
            switch (placement)
            {
                case SceneLinkHotkeyPlacement.PromptBarInline:
                    if (layout == null)
                    {
                        layout = gameObject.AddComponent<LayoutElement>();
                    }

                    layout.minWidth = 140f;
                    layout.preferredWidth = 150f;
                    layout.minHeight = 48f;
                    layout.preferredHeight = 48f;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(150f, 48f);
                    transform.SetAsFirstSibling();
                    break;

                case SceneLinkHotkeyPlacement.BottomLeft:
                    if (layout != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(layout);
                        }
                        else
                        {
                            DestroyImmediate(layout);
                        }
                    }

                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    rect.anchoredPosition = new Vector2(24f, 24f);
                    rect.sizeDelta = new Vector2(220f, 52f);
                    break;

                default:
                    if (layout != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(layout);
                        }
                        else
                        {
                            DestroyImmediate(layout);
                        }
                    }

                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = Vector2.one;
                    rect.anchoredPosition = new Vector2(-24f, -24f);
                    rect.sizeDelta = new Vector2(220f, 52f);
                    break;
            }
        }

        private void BringToFront(bool bringToFront, Transform insertAboveLayer = null)
        {
            if (insertAboveLayer != null)
            {
                var layerCanvas = insertAboveLayer.GetComponentInParent<Canvas>();
                var layerRoot = layerCanvas != null ? layerCanvas.transform : insertAboveLayer.parent;
                if (layerRoot != null && transform.parent != layerRoot)
                {
                    transform.SetParent(layerRoot, false);
                    ApplyPlacement(_placement);
                }

                var index = insertAboveLayer.GetSiblingIndex();
                transform.SetSiblingIndex(index + 1);
                return;
            }

            if (!bringToFront)
            {
                transform.SetAsLastSibling();
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var frontRoot = canvas != null ? canvas.transform : transform.parent;
            if (frontRoot != null && transform.parent != frontRoot)
            {
                transform.SetParent(frontRoot, false);
                ApplyPlacement(_placement);
            }

            transform.SetAsLastSibling();
        }

        private static SceneLinkHotkeyUI Build(
            Transform parent,
            string objectName,
            string actionText,
            SceneLinkHotkeyPlacement placement)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.38f, 1f);
            colors.pressedColor = new Color(0.12f, 0.2f, 0.28f, 1f);
            button.colors = colors;

            var keyRoot = new GameObject("KeyBadge", typeof(RectTransform));
            keyRoot.transform.SetParent(go.transform, false);
            var keyRect = keyRoot.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 0.5f);
            keyRect.anchorMax = new Vector2(0f, 0.5f);
            keyRect.pivot = new Vector2(0f, 0.5f);
            keyRect.anchoredPosition = new Vector2(10f, 0f);
            keyRect.sizeDelta = new Vector2(36f, 36f);
            var keyBg = keyRoot.AddComponent<Image>();
            keyBg.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            keyBg.raycastTarget = false;

            var keyLabelGo = new GameObject("Label", typeof(RectTransform));
            keyLabelGo.transform.SetParent(keyRoot.transform, false);
            var keyLabel = keyLabelGo.AddComponent<Text>();
            keyLabel.font = UiFontCatalog.Body;
            keyLabel.text = "B";
            keyLabel.fontSize = 22;
            keyLabel.fontStyle = FontStyle.Bold;
            keyLabel.alignment = TextAnchor.MiddleCenter;
            keyLabel.color = new Color(0.08f, 0.1f, 0.14f, 1f);
            keyLabel.raycastTarget = false;
            StretchFull(keyLabel.rectTransform);

            var actionGo = new GameObject("ActionLabel", typeof(RectTransform));
            actionGo.transform.SetParent(go.transform, false);
            var action = actionGo.AddComponent<Text>();
            action.font = UiFontCatalog.Body;
            action.text = actionText;
            action.fontSize = 18;
            action.alignment = TextAnchor.MiddleLeft;
            action.color = Color.white;
            action.raycastTarget = false;
            var actionRect = action.rectTransform;
            actionRect.anchorMin = Vector2.zero;
            actionRect.anchorMax = Vector2.one;
            actionRect.offsetMin = new Vector2(56f, 4f);
            actionRect.offsetMax = new Vector2(-12f, -4f);

            var link = go.AddComponent<SceneLinkHotkeyUI>();
            link.button = button;
            link.keyBadge = keyLabel;
            link.actionLabel = action;
            link.ApplyPlacement(placement);
            return link;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
