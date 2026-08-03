using System;
using FracturedChorus.Combat.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    public sealed class TutorialCoachView : MonoBehaviour
    {
        [Header("Scene refs — kéo Panel để chỉnh vị trí khung")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Text backLabel;
        [SerializeField] private Image coachPortrait;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image dimmer;
        [SerializeField] private Text progressLabel;
        [Tooltip("Bật: không ghi đè RectTransform Panel/Body khi Show (chỉnh tay Hierarchy rồi Ctrl+S).")]
        [SerializeField] private bool preserveSceneLayout = true;
        [SerializeField] [Range(0f, 1f)] private float slideshowDimmerAlpha = 0.12f;

        private Action _onNext;
        private Action _onBack;
        private bool _slideshowMode;
        private bool _blocksCombatUi;

        public bool IsVisible => root != null && root.activeInHierarchy;
        public bool BlocksCombatUi => _blocksCombatUi && IsVisible;
        public RectTransform PanelRect => panelRect;

        public static bool FindAnyVisible()
        {
            foreach (var coach in FindObjectsByType<TutorialCoachView>(FindObjectsInactive.Exclude))
            {
                if (coach != null && coach.BlocksCombatUi)
                {
                    return true;
                }
            }

            return false;
        }

        public static TutorialCoachView Ensure(Transform host)
        {
            if (host == null)
            {
                return null;
            }

            var existing = host.GetComponentInChildren<TutorialCoachView>(true);
            if (existing != null)
            {
                existing.EnsureBuilt();
                existing.EnsureSlideshowControls();
                return existing;
            }

            var go = new GameObject("TutorialCoach", typeof(RectTransform), typeof(TutorialCoachView));
            go.transform.SetParent(host, false);
            var view = go.GetComponent<TutorialCoachView>();
            view.preserveSceneLayout = false;
            view.BuildHierarchy();
            go.SetActive(false);
            return view;
        }

        public void Show(string bodyCopy, Action onNext)
        {
            Show(bodyCopy, onNext, null, null);
        }

        public void Show(string bodyCopy, Action onNext, Sprite portrait, Sprite panel)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = false;
            _blocksCombatUi = true;
            _onBack = null;
            _onNext = onNext;

            SetPanelVisible(true);
            SetPanelRaycast(true);
            ApplyContent(bodyCopy, portrait, panel, null);
            if (bodyLabel != null)
            {
                bodyLabel.alignment = TextAnchor.UpperLeft;
            }

            ApplyDimmer(FcColorTokens.Surface.DimmerBlack.a);
            SetBackVisible(false);
            SetPrimaryLabel("Next");
            SetPrimaryVisible(onNext != null);
            SetVisible(true);
        }

        public void ShowSlide(
            string bodyCopy,
            Sprite portrait,
            Sprite panel,
            string progressText,
            bool showBack,
            string primaryLabel,
            Action onBack,
            Action onPrimary)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = true;
            _blocksCombatUi = true;
            _onBack = onBack;
            _onNext = onPrimary;

            SetPanelVisible(true);
            SetPanelRaycast(true);
            ApplyContent(bodyCopy, portrait, panel, progressText);
            ApplySlideshowLayout(panel != null);
            if (bodyLabel != null)
            {
                bodyLabel.alignment = TextAnchor.UpperLeft;
            }

            ApplyDimmer(slideshowDimmerAlpha);
            SetBackVisible(showBack);
            SetPrimaryLabel(string.IsNullOrEmpty(primaryLabel) ? "Next" : primaryLabel);
            SetPrimaryVisible(onPrimary != null);
            SetVisible(true);
        }

        public void ShowFloatingHint(string bodyCopy)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = false;
            _blocksCombatUi = false;
            _onBack = null;
            _onNext = null;

            SetPanelVisible(true);
            ApplyContent(bodyCopy, null, null, null);
            ApplyDimmer(0f);
            SetBackVisible(false);
            SetPrimaryVisible(false);
            if (coachPortrait != null)
            {
                coachPortrait.enabled = false;
            }

            if (panelImage != null)
            {
                panelImage.enabled = false;
            }

            if (progressLabel != null)
            {
                progressLabel.gameObject.SetActive(false);
            }

            ApplyFloatingHintLayout();
            SetVisible(true);
            RefreshCombatOverlays();
        }

        private void ApplyFloatingHintLayout()
        {
            if (panelRect == null)
            {
                return;
            }

            SetPanelRaycast(false);
            if (!preserveSceneLayout)
            {
                Stretch(panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -140f),
                    new Vector2(420f, -24f));
            }

            if (bodyLabel != null)
            {
                bodyLabel.alignment = TextAnchor.MiddleCenter;
                bodyLabel.raycastTarget = false;
                if (!preserveSceneLayout)
                {
                    Stretch(bodyLabel.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), Vector2.zero,
                        Vector2.zero);
                }
            }
        }

        private void SetPanelRaycast(bool enabled)
        {
            if (panelRect == null)
            {
                return;
            }

            var panelImageComp = panelRect.GetComponent<Image>();
            if (panelImageComp != null)
            {
                panelImageComp.raycastTarget = enabled;
            }
        }

        public void Hide()
        {
            _onNext = null;
            _onBack = null;
            _slideshowMode = false;
            _blocksCombatUi = false;
            if (root != null)
            {
                root.SetActive(false);
            }

            RefreshCombatOverlays();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(visible);
            }
        }

        private void ApplyContent(string bodyCopy, Sprite portrait, Sprite panel, string progressText)
        {
            if (bodyLabel != null)
            {
                bodyLabel.text = bodyCopy ?? string.Empty;
            }

            ApplySprite(coachPortrait, portrait, preserveAspect: true);
            ApplySprite(panelImage, panel, preserveAspect: true);

            if (progressLabel != null)
            {
                var hasProgress = !string.IsNullOrEmpty(progressText);
                progressLabel.gameObject.SetActive(hasProgress);
                progressLabel.text = progressText ?? string.Empty;
            }
        }

        private void ApplySlideshowLayout(bool hasPanelImage)
        {
            if (preserveSceneLayout || bodyLabel == null)
            {
                return;
            }

            if (hasPanelImage)
            {
                Stretch(panelImage.rectTransform, new Vector2(0.24f, 0.42f), new Vector2(0.96f, 0.92f), Vector2.zero,
                    Vector2.zero);
                Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.22f), new Vector2(0.96f, 0.4f), Vector2.zero,
                    Vector2.zero);
            }
            else
            {
                Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.22f), new Vector2(0.96f, 0.92f), Vector2.zero,
                    Vector2.zero);
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }

            EnsureCanvasOnTop();
            RefreshCombatOverlays();
        }

        private void SetBackVisible(bool visible)
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(visible);
            }
        }

        private void SetPrimaryVisible(bool visible)
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(visible);
            }
        }

        private void SetPrimaryLabel(string label)
        {
            if (nextLabel != null)
            {
                nextLabel.text = label;
            }
        }

        private static void RefreshCombatOverlays()
        {
            var combat = FindAnyObjectByType<CombatController>();
            combat?.RefreshExecuteOverlayVisibility();
        }

        private void EnsureBuilt()
        {
            if (root != null)
            {
                WireExistingRefs();
                return;
            }

            BuildHierarchy();
        }

        private void ApplyDimmer(float alpha)
        {
            if (dimmer == null)
            {
                var t = transform.Find("Dimmer");
                if (t != null)
                {
                    dimmer = t.GetComponent<Image>();
                }
            }

            if (dimmer == null)
            {
                return;
            }

            var c = dimmer.color;
            c.a = Mathf.Clamp01(alpha);
            dimmer.color = c;
            dimmer.raycastTarget = alpha > 0.01f;
            dimmer.gameObject.SetActive(true);
        }

        private void WireExistingRefs()
        {
            if (panelRect == null)
            {
                var panelTf = transform.Find("Panel");
                if (panelTf != null)
                {
                    panelRect = panelTf as RectTransform;
                }
            }

            if (dimmer == null)
            {
                var t = transform.Find("Dimmer");
                if (t != null)
                {
                    dimmer = t.GetComponent<Image>();
                }
            }

            if (coachPortrait == null)
            {
                var t = transform.Find("Panel/CoachPortrait");
                if (t != null)
                {
                    coachPortrait = t.GetComponent<Image>();
                }
            }

            if (panelImage == null)
            {
                var t = transform.Find("Panel/PanelImage");
                if (t != null)
                {
                    panelImage = t.GetComponent<Image>();
                }
            }

            if (bodyLabel == null)
            {
                var t = transform.Find("Panel/Body");
                if (t != null)
                {
                    bodyLabel = t.GetComponent<Text>();
                }
            }

            if (nextButton == null)
            {
                var t = transform.Find("Panel/NextButton");
                if (t != null)
                {
                    nextButton = t.GetComponent<Button>();
                    nextLabel = t.Find("Label")?.GetComponent<Text>();
                }
            }
        }

        private void EnsureSlideshowControls()
        {
            WireExistingRefs();
            var panel = panelRect != null ? panelRect.transform : transform.Find("Panel");
            if (panel == null)
            {
                return;
            }

            if (backButton == null)
            {
                var existing = panel.Find("BackButton");
                if (existing != null)
                {
                    backButton = existing.GetComponent<Button>();
                    backLabel = existing.Find("Label")?.GetComponent<Text>();
                }
                else
                {
                    backButton = CreateButton(panel, "BackButton", "Back", out backLabel);
                    Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.06f),
                        new Vector2(0.48f, 0.18f), Vector2.zero, Vector2.zero);
                }
            }

            backButton.onClick.RemoveListener(HandleBack);
            backButton.onClick.AddListener(HandleBack);
            backButton.gameObject.SetActive(false);
            UiButtonHoverFeedback.Ensure(backButton.gameObject);

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
                nextButton.onClick.AddListener(HandleNext);
                Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.06f), new Vector2(0.96f, 0.18f),
                    Vector2.zero, Vector2.zero);
                UiButtonHoverFeedback.Ensure(nextButton.gameObject);
            }

            if (progressLabel == null)
            {
                var existing = panel.Find("Progress");
                if (existing != null)
                {
                    progressLabel = existing.GetComponent<Text>();
                }
                else
                {
                    progressLabel = CreateText(panel, "Progress", string.Empty, 16, TextAnchor.MiddleRight);
                    Stretch(progressLabel.rectTransform, new Vector2(0.7f, 0.9f), new Vector2(0.96f, 0.98f),
                        Vector2.zero, Vector2.zero);
                    progressLabel.color = new Color(0.7f, 0.85f, 1f, 0.85f);
                }
            }

            progressLabel.gameObject.SetActive(false);
        }

        private void BuildHierarchy()
        {
            var overlayRect = GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            dimmer = CreateImage(transform, "Dimmer", FcColorTokens.Surface.DimmerBlack);
            Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dimmer.raycastTarget = true;

            var panel = CreateImage(transform, "Panel", FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.94f));
            panelRect = panel.rectTransform;
            Stretch(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -280f),
                new Vector2(520f, 280f));
            panel.raycastTarget = true;

            coachPortrait = CreateImage(panel.transform, "CoachPortrait", Color.white);
            Stretch(coachPortrait.rectTransform, new Vector2(0.02f, 0.2f), new Vector2(0.22f, 0.96f), Vector2.zero,
                Vector2.zero);
            coachPortrait.preserveAspect = true;
            coachPortrait.raycastTarget = false;
            coachPortrait.enabled = false;

            panelImage = CreateImage(panel.transform, "PanelImage", Color.white);
            Stretch(panelImage.rectTransform, new Vector2(0.24f, 0.42f), new Vector2(0.96f, 0.92f), Vector2.zero,
                Vector2.zero);
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = false;
            panelImage.enabled = false;

            bodyLabel = CreateText(panel.transform, "Body", string.Empty, 22, TextAnchor.UpperLeft);
            Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.22f), new Vector2(0.96f, 0.4f), Vector2.zero,
                Vector2.zero);
            bodyLabel.color = Color.white;
            bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;

            progressLabel = CreateText(panel.transform, "Progress", string.Empty, 16, TextAnchor.MiddleRight);
            Stretch(progressLabel.rectTransform, new Vector2(0.7f, 0.9f), new Vector2(0.96f, 0.98f), Vector2.zero,
                Vector2.zero);
            progressLabel.color = new Color(0.7f, 0.85f, 1f, 0.85f);
            progressLabel.gameObject.SetActive(false);

            backButton = CreateButton(panel.transform, "BackButton", "Back", out backLabel);
            Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.06f), new Vector2(0.48f, 0.18f),
                Vector2.zero, Vector2.zero);
            backButton.onClick.AddListener(HandleBack);
            backButton.gameObject.SetActive(false);
            UiButtonHoverFeedback.Ensure(backButton.gameObject);

            nextButton = CreateButton(panel.transform, "NextButton", "Next", out nextLabel);
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.06f), new Vector2(0.96f, 0.18f),
                Vector2.zero, Vector2.zero);
            nextButton.onClick.AddListener(HandleNext);
            UiButtonHoverFeedback.Ensure(nextButton.gameObject);

            EnsureCanvasOnTop();
            root = gameObject;
        }

        private void EnsureCanvasOnTop()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            var parentCanvas = transform.parent != null
                ? transform.parent.GetComponentInParent<Canvas>()
                : null;
            if (parentCanvas != null && parentCanvas != canvas)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.planeDistance = Mathf.Max(1f, parentCanvas.planeDistance - 1f);
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.Tutorial;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            transform.SetAsLastSibling();
        }

        private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            if (sprite == null)
            {
                image.sprite = null;
                image.enabled = false;
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.enabled = true;
        }

        private void HandleNext()
        {
            var callback = _onNext;
            if (!_slideshowMode)
            {
                Hide();
            }

            callback?.Invoke();
        }

        private void HandleBack()
        {
            _onBack?.Invoke();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, fontSize);
            text.text = content;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.18f, 0.32f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            labelText = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.color = FcColorTokens.Brand.Cyan;
            labelText.fontStyle = FontStyle.Bold;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
