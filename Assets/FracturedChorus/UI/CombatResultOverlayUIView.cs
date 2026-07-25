using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CombatResultOverlayUIView : MonoBehaviour
    {
        private const string VictorySpritePath = "UI/Combat/Result/combat_result_victory_v1";
        private const string DefeatSpritePath = "UI/Combat/Result/combat_result_defeat_v1";
        private const string ContinueSpritePath = "UI/Combat/Result/combat_btn_continue_v1";
        private const string RetrySpritePath = "UI/Combat/Result/combat_btn_retry_v1";
        private const int OverlaySortOrder = 500;

        private static readonly Vector2 TitleAnchor = new Vector2(0.5f, 0.62f);
        private static readonly Vector2 TitlePos = new Vector2(-3f, 48.67f);
        private static readonly Vector2 TitleSize = new Vector2(1156.18f, 444.66f);
        private static readonly Vector2 ContinueAnchor = new Vector2(0.5f, 0.28f);
        private static readonly Vector2 ContinuePos = new Vector2(-7f, 234f);
        private static readonly Vector2 ContinueSize = new Vector2(594.64f, 213.18f);
        private static readonly Vector2 RetryAnchor = new Vector2(0.5f, 0.12f);
        private static readonly Vector2 RetryPos = new Vector2(-12f, 307f);
        private static readonly Vector2 RetrySize = new Vector2(594.64f, 213.18f);

        [Header("Scene refs — chỉnh tay trong Hierarchy")]
        [SerializeField] private Image dimmer;
        [SerializeField] private Image titleImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Image continueImage;
        [SerializeField] private Image retryImage;
        [SerializeField] private Text rewardLabel;

        [Header("Sprites (Inspector hoặc Resources)")]
        [SerializeField] private Sprite victorySprite;
        [SerializeField] private Sprite defeatSprite;
        [SerializeField] private Sprite continueSprite;
        [SerializeField] private Sprite retrySprite;

        [Tooltip("Bật = không đụng RectTransform lúc Play. Chỉ đổi sprite title + ẩn/hiện Retry.")]
        [SerializeField] private bool preserveSceneLayout = true;

        private Action _onContinue;
        private Action _onRetry;

        public static CombatResultOverlayUIView EnsureOnCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<CombatResultOverlayUIView>(true);
            if (existing != null)
            {
                existing.WireSceneReferences();
                return existing;
            }

            var go = new GameObject("CombatResultOverlay", typeof(RectTransform), typeof(CombatResultOverlayUIView));
            go.transform.SetParent(canvasRoot, false);
            var view = go.GetComponent<CombatResultOverlayUIView>();
            view.preserveSceneLayout = false;
            view.BuildDefaultHierarchy();
            view.ApplyDefaultSprites();
            go.SetActive(false);
            return view;
        }

        public void WireSceneReferences()
        {
            if (dimmer == null)
            {
                dimmer = FindChildImage("Dimmer");
            }

            if (titleImage == null)
            {
                titleImage = FindChildImage("Title");
            }

            if (continueButton == null)
            {
                var t = transform.Find("ContinueButton");
                if (t != null)
                {
                    continueButton = t.GetComponent<Button>();
                    continueImage = t.GetComponent<Image>();
                }
            }

            if (retryButton == null)
            {
                var t = transform.Find("RetryButton");
                if (t != null)
                {
                    retryButton = t.GetComponent<Button>();
                    retryImage = t.GetComponent<Image>();
                }
            }

            if (continueImage == null && continueButton != null)
            {
                continueImage = continueButton.GetComponent<Image>();
            }

            if (retryImage == null && retryButton != null)
            {
                retryImage = retryButton.GetComponent<Image>();
            }

            if (rewardLabel == null)
            {
                var rewardTransform = transform.Find("RewardLabel");
                if (rewardTransform != null)
                {
                    rewardLabel = rewardTransform.GetComponent<Text>();
                }
            }
        }

        public void Bind(Action onContinue, Action onRetry)
        {
            _onContinue = onContinue;
            _onRetry = onRetry;
            WireSceneReferences();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinue);
                continueButton.onClick.AddListener(HandleContinue);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(HandleRetry);
                retryButton.onClick.AddListener(HandleRetry);
            }
        }

        public void Show(bool victory, string rewardSummary = null)
        {
            WireSceneReferences();
            EnsureRewardLabel();
            gameObject.SetActive(true);
            BringToFront();

            EnsureSpritesLoaded();

            if (titleImage != null)
            {
                titleImage.sprite = victory ? victorySprite : defeatSprite;
                titleImage.enabled = titleImage.sprite != null;
                titleImage.preserveAspect = true;
            }

            if (!preserveSceneLayout)
            {
                if (continueImage != null && continueSprite != null)
                {
                    continueImage.sprite = continueSprite;
                    continueImage.preserveAspect = true;
                }

                if (retryImage != null && retrySprite != null)
                {
                    retryImage.sprite = retrySprite;
                    retryImage.preserveAspect = true;
                }
            }

            if (rewardLabel != null)
            {
                var showReward = victory && !string.IsNullOrWhiteSpace(rewardSummary);
                rewardLabel.gameObject.SetActive(showReward);
                if (showReward)
                {
                    rewardLabel.text = rewardSummary;
                }
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!victory);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void BringToFront()
        {
            transform.SetAsLastSibling();
            EnsureOverlayCanvas();
        }

        public void BuildDefaultHierarchy()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            dimmer = CreateImage("Dimmer", root, new Color(0f, 0f, 0f, 0.72f), fullStretch: true);
            dimmer.raycastTarget = true;

            titleImage = CreateImage("Title", root, Color.white, fullStretch: false);
            ApplyRect(titleImage.rectTransform, TitleAnchor, TitlePos, TitleSize);

            continueButton = CreateSpriteButton("ContinueButton", root, out continueImage);
            ApplyRect(continueButton.transform as RectTransform, ContinueAnchor, ContinuePos, ContinueSize);

            retryButton = CreateSpriteButton("RetryButton", root, out retryImage);
            ApplyRect(retryButton.transform as RectTransform, RetryAnchor, RetryPos, RetrySize);

            EnsureRewardLabel();
            EnsureOverlayCanvas();
        }

        private void EnsureRewardLabel()
        {
            if (rewardLabel != null)
            {
                return;
            }

            var existing = transform.Find("RewardLabel");
            if (existing != null)
            {
                rewardLabel = existing.GetComponent<Text>();
                if (rewardLabel != null)
                {
                    return;
                }
            }

            var go = new GameObject("RewardLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);
            rewardLabel = go.GetComponent<Text>();
            rewardLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (rewardLabel.font == null)
            {
                rewardLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            rewardLabel.fontSize = 28;
            rewardLabel.alignment = TextAnchor.MiddleCenter;
            rewardLabel.color = new Color(0.85f, 0.95f, 1f, 1f);
            rewardLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            rewardLabel.verticalOverflow = VerticalWrapMode.Overflow;
            rewardLabel.raycastTarget = false;

            var rt = rewardLabel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.42f);
            rt.anchorMax = new Vector2(0.5f, 0.42f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900f, 80f);
            go.SetActive(false);
        }

        private void EnsureOverlayCanvas()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            if (parentCanvas != null && parentCanvas != canvas)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.planeDistance = parentCanvas.planeDistance;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void ApplyRect(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        public void ApplyDefaultSprites()
        {
            EnsureSpritesLoaded();
            if (titleImage != null)
            {
                titleImage.sprite = victorySprite;
                titleImage.preserveAspect = true;
            }

            if (continueImage != null)
            {
                continueImage.sprite = continueSprite;
                continueImage.preserveAspect = true;
            }

            if (retryImage != null)
            {
                retryImage.sprite = retrySprite;
                retryImage.preserveAspect = true;
            }
        }

        private void EnsureSpritesLoaded()
        {
            if (victorySprite == null)
            {
                victorySprite = LoadSprite(VictorySpritePath);
            }

            if (defeatSprite == null)
            {
                defeatSprite = LoadSprite(DefeatSpritePath);
            }

            if (continueSprite == null)
            {
                continueSprite = LoadSprite(ContinueSpritePath);
            }

            if (retrySprite == null)
            {
                retrySprite = LoadSprite(RetrySpritePath);
            }
        }

        private Image FindChildImage(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool fullStretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            if (fullStretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            return image;
        }

        private static Button CreateSpriteButton(string name, Transform parent, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
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

        private void HandleContinue() => _onContinue?.Invoke();

        private void HandleRetry() => _onRetry?.Invoke();
    }
}
