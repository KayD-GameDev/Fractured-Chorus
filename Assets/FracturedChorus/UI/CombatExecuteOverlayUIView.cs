using System;
using FracturedChorus.Combat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CombatExecuteOverlayUIView : MonoBehaviour
    {
        private const string DeploySpriteResourcePath = "UI/Combat/combat_btn_deploy_v1";
        private const string ExecuteSpriteResourcePath = "UI/Combat/combat_btn_execute_v1";

        [Header("References")]
        [SerializeField] private Button executeButton;
        [SerializeField] private Text labelText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private RectTransform buttonRect;
        [SerializeField] private CombatController combatController;

        [Header("Sprites")]
        [SerializeField] private Sprite deploySprite;
        [SerializeField] private Sprite executeSprite;
        [SerializeField] private bool hideLabelWhenUsingSprites = true;

        [Header("Layout")]
        [Tooltip("Bật chỉ khi muốn code ép size/pos từ Button Size / Anchored Position. Mặc định tắt = tôn trọng RectTransform trên scene.")]
        [SerializeField] private bool applyScriptedLayout;
        [SerializeField] private Vector2 buttonSize = new Vector2(360f, 140f);
        [SerializeField] private Vector2 buttonAnchoredPosition = Vector2.zero;

        private Action _onExecuteClicked;
        private bool _warnedMissingButton;
        private string _currentLabel;

        public void WireReferences()
        {
            executeButton = ResolveExecuteButton();

            if (executeButton != null)
            {
                if (buttonRect == null)
                {
                    buttonRect = executeButton.transform as RectTransform;
                }

                if (buttonImage == null)
                {
                    buttonImage = executeButton.targetGraphic as Image;
                    if (buttonImage == null)
                    {
                        buttonImage = executeButton.GetComponent<Image>();
                    }
                }

                if (labelText == null)
                {
                    labelText = executeButton.GetComponentInChildren<Text>(true);
                }
            }

            if (combatController == null)
            {
                combatController = FindAnyObjectByType<CombatController>();
            }

            EnsureSpritesLoaded();
            if (applyScriptedLayout)
            {
                ApplyLayout();
            }
        }

        public void SetLabel(string text)
        {
            if (executeButton == null || buttonImage == null)
            {
                WireReferences();
            }
            else
            {
                EnsureSpritesLoaded();
            }

            _currentLabel = text;
            ApplyVisualForLabel(text);
        }

        public void ApplyLayout()
        {
            if (buttonRect == null && executeButton != null)
            {
                buttonRect = executeButton.transform as RectTransform;
            }

            if (buttonRect == null)
            {
                return;
            }

            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = buttonAnchoredPosition;
            buttonRect.sizeDelta = buttonSize;

            var overlayRect = transform as RectTransform;
            if (overlayRect != null)
            {
                overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.anchoredPosition = Vector2.zero;
                overlayRect.sizeDelta = buttonSize;
            }
        }

        private void ApplyVisualForLabel(string text)
        {
            EnsureSpritesLoaded();
            var useDeploy = string.Equals(text, "Deploy", StringComparison.OrdinalIgnoreCase);
            var sprite = useDeploy ? deploySprite : executeSprite;
            if (sprite == null)
            {
                sprite = useDeploy ? executeSprite : deploySprite;
            }

            if (buttonImage != null && sprite != null)
            {
                buttonImage.sprite = sprite;
                buttonImage.color = Color.white;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = true;
                ApplyAlphaHitTest(buttonImage, sprite);
                UiButtonHoverFeedback.Ensure(executeButton != null ? executeButton.gameObject : buttonImage.gameObject)
                    ?.RecaptureBaseFromGraphic();
                if (hideLabelWhenUsingSprites && labelText != null)
                {
                    labelText.enabled = false;
                    return;
                }
            }

            if (labelText != null && !string.IsNullOrEmpty(text))
            {
                labelText.enabled = true;
                labelText.text = text;
            }
        }

        private void EnsureSpritesLoaded()
        {
            if (deploySprite == null)
            {
                deploySprite = LoadSprite(DeploySpriteResourcePath);
            }

            if (executeSprite == null)
            {
                executeSprite = LoadSprite(ExecuteSpriteResourcePath);
            }
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Restrict clicks to opaque sprite pixels. Only set when texture is readable —
        /// otherwise Unity logs an error and ignores the threshold.
        /// </summary>
        private static void ApplyAlphaHitTest(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            var tex = sprite != null ? sprite.texture : null;
            if (tex != null && tex.isReadable)
            {
                image.alphaHitTestMinimumThreshold = 0.1f;
                return;
            }

            // Keep full-rect raycast until import settings reimport with Read/Write.
            image.alphaHitTestMinimumThreshold = 0f;
        }

        private Button ResolveExecuteButton()
        {
            var buttonTransform = transform.Find("ExecuteButton");
            if (buttonTransform != null)
            {
                var button = buttonTransform.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }

                var image = buttonTransform.GetComponent<Image>();
                if (image == null)
                {
                    image = buttonTransform.gameObject.AddComponent<Image>();
                    image.color = Color.white;
                    image.raycastTarget = true;
                }

                button = buttonTransform.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.interactable = true;
                return button;
            }

            if (executeButton != null)
            {
                return executeButton;
            }

            return GetComponentInChildren<Button>(true);
        }

        private void Awake()
        {
            WireReferences();
            if (!string.IsNullOrEmpty(_currentLabel))
            {
                ApplyVisualForLabel(_currentLabel);
            }
            else
            {
                ApplyVisualForLabel("Deploy");
            }
        }

        private void OnValidate()
        {
            if (buttonSize.x < 1f)
            {
                buttonSize.x = 1f;
            }

            if (buttonSize.y < 1f)
            {
                buttonSize.y = 1f;
            }
        }

        public void Bind(Action onExecuteClicked)
        {
            WireReferences();
            _onExecuteClicked = onExecuteClicked;
            executeButton = ResolveExecuteButton();

            if (executeButton == null)
            {
                if (!_warnedMissingButton)
                {
                    _warnedMissingButton = true;
                    Debug.LogWarning(
                        "[ExecuteOverlay] ExecuteButton not found. Add a child named ExecuteButton with Image + Button.");
                }

                return;
            }

            executeButton.onClick.RemoveListener(HandleClick);
            executeButton.onClick.AddListener(HandleClick);
            UiButtonHoverFeedback.Ensure(executeButton.gameObject);
        }

        public void OnExecutePressed()
        {
            HandleClick();
        }

        public void SetVisible(bool visible)
        {
            if (executeButton == null)
            {
                WireReferences();
            }

            if (executeButton != null)
            {
                if (!visible)
                {
                    UiButtonHoverFeedback.Ensure(executeButton.gameObject)?.ResetHoverState();
                }

                executeButton.gameObject.SetActive(visible);

                if (visible)
                {
                    if (buttonImage != null)
                    {
                        buttonImage.color = Color.white;
                    }

                    executeButton.interactable = true;
                    var feedback = UiButtonHoverFeedback.Ensure(executeButton.gameObject);
                    feedback?.RecaptureBaseFromGraphic();
                    feedback?.ResetHoverState();
                }

                return;
            }

            gameObject.SetActive(visible);
        }

        private void HandleClick()
        {
            if (_onExecuteClicked != null)
            {
                _onExecuteClicked.Invoke();
                return;
            }

            if (combatController == null)
            {
                combatController = FindAnyObjectByType<CombatController>();
            }

            combatController?.StartRound();
        }
    }
}
