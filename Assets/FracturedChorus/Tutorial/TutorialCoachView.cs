using System;
using FracturedChorus.Combat.Core;
using FracturedChorus.Localization;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
        [SerializeField] private Sprite defaultCoachPortrait;

        private const int CoachBodyFontSize = 28;
        private const int CoachButtonFontSize = 40;

        private Action _onNext;
        private Action _onBack;
        private bool _slideshowMode;
        private bool _blocksCombatUi;
        private Image _clipSprite;
        private RawImage _clipVideo;
        private VideoPlayer _video;
        private RenderTexture _videoTexture;
        private AspectRatioFitter _clipFitter;
        private Vector2 _bodyAnchorMin;
        private Vector2 _bodyAnchorMax;
        private bool _bodyAnchorsCaptured;
        private Vector2 _panelHomePosition;
        private bool _panelHomeCaptured;

        private void Awake()
        {
            SanitizePanelFonts();
        }

        public bool IsVisible => root != null && root.activeInHierarchy;
        public bool BlocksCombatUi => _blocksCombatUi && IsVisible;
        public RectTransform PanelRect => panelRect;

        public void PlacePanelY(float y)
        {
            if (this == null || panelRect == null)
            {
                return;
            }

            if (!_panelHomeCaptured)
            {
                _panelHomePosition = panelRect.anchoredPosition;
                _panelHomeCaptured = true;
            }

            var pos = _panelHomePosition;
            pos.y = y;
            panelRect.anchoredPosition = pos;
        }

        public void RestorePanelPosition()
        {
            if (this == null || !_panelHomeCaptured || panelRect == null)
            {
                return;
            }

            panelRect.anchoredPosition = _panelHomePosition;
            _panelHomeCaptured = false;
        }

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

        public void Show(string bodyCopy, Action onNext, Sprite portrait, Sprite panel, VideoClip panelClip = null)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = false;
            _blocksCombatUi = true;
            _onBack = null;
            _onNext = onNext;

            SetPanelVisible(true);
            SetPanelRaycast(true);
            ApplyContent(bodyCopy, portrait, panel, null, panelClip);
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
            Action onPrimary,
            VideoClip panelClip = null)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = true;
            _blocksCombatUi = true;
            _onBack = onBack;
            _onNext = onPrimary;

            SetPanelVisible(true);
            SetPanelRaycast(true);
            ApplyContent(bodyCopy, portrait, panel, progressText, panelClip);
            ApplySlideshowLayout(panel != null);
            if (bodyLabel != null)
            {
                bodyLabel.alignment = TextAnchor.UpperLeft;
            }

            ApplyDimmer(slideshowDimmerAlpha);
            SetBackVisible(showBack);
            if (backLabel != null)
            {
                backLabel.text = GameLoc.Tr("BACK");
            }

            SetPrimaryLabel(string.IsNullOrEmpty(primaryLabel) ? "NEXT" : primaryLabel);
            SetPrimaryVisible(onPrimary != null);
            SetVisible(true);
        }

        public void ShowFloatingHint(string bodyCopy, Sprite portrait = null)
        {
            EnsureBuilt();
            EnsureSlideshowControls();
            _slideshowMode = false;
            _blocksCombatUi = false;
            _onBack = null;
            _onNext = null;

            SetPanelVisible(true);
            var bust = portrait ?? defaultCoachPortrait ?? TutorialCadenceTrackLibrary.LoadCodaPortrait();
            ApplyContent(bodyCopy, bust, null, null, null);
            ApplyDimmer(0f);
            SetBackVisible(false);
            SetPrimaryVisible(false);

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
            if (this == null)
            {
                return;
            }

            StopPanelClip();
            _onNext = null;
            _onBack = null;
            _slideshowMode = false;
            _blocksCombatUi = false;
            ApplyDimmer(0f);
            SetPanelRaycast(false);
            if (root != null)
            {
                root.SetActive(false);
            }

            RefreshCombatOverlays();
            TutorialDirector.NotifyCoachHidden();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(visible);
            }
        }

        private void ApplyContent(string bodyCopy, Sprite portrait, Sprite panel, string progressText, VideoClip panelClip)
        {
            SanitizePanelFonts();
            if (bodyLabel != null)
            {
                bodyLabel.text = GameLoc.Tr(bodyCopy ?? string.Empty);
                if (bodyLabel.fontSize < 1)
                {
                    bodyLabel.fontSize = CoachBodyFontSize;
                }
            }

            var bust = portrait ?? defaultCoachPortrait ?? TutorialCadenceTrackLibrary.LoadCodaPortrait();
            ApplySprite(coachPortrait, bust, preserveAspect: true);
            ApplyPanelMedia(panel, panelClip);

            if (progressLabel != null)
            {
                var hasProgress = !string.IsNullOrEmpty(progressText);
                progressLabel.gameObject.SetActive(hasProgress);
                progressLabel.text = progressText ?? string.Empty;
            }
        }

        private void ApplyPanelMedia(Sprite panel, VideoClip panelClip)
        {
            StopPanelClip();
            var hasVideo = panelClip != null;
            var hasSprite = !hasVideo && panel != null;
            if (panelImage != null)
            {
                panelImage.sprite = null;
                panelImage.enabled = false;
            }

            if (!hasVideo && !hasSprite)
            {
                SetClipVisible(sprite: false, video: false);
                FitBodyToEmptySlot();
                return;
            }

            EnsureClipSurfaces();
            RestoreBodyAnchors();
            if (hasVideo)
            {
                PlayPanelClip(panelClip);
                return;
            }

            if (_clipSprite != null)
            {
                _clipSprite.sprite = panel;
                _clipSprite.preserveAspect = false;
                _clipSprite.color = Color.white;
                _clipSprite.enabled = true;
                FitClipAspect(_clipSprite.rectTransform, panel.rect.width, panel.rect.height);
            }

            if (_clipVideo != null)
            {
                _clipVideo.enabled = false;
            }
        }

        private void PlayPanelClip(VideoClip clip)
        {
            if (_clipVideo == null || clip == null)
            {
                return;
            }

            var width = Mathf.Max(16, (int)clip.width);
            var height = Mathf.Max(16, (int)clip.height);
            _videoTexture = new RenderTexture(width, height, 0);
            _clipVideo.texture = _videoTexture;
            _clipVideo.enabled = true;
            if (_clipSprite != null)
            {
                _clipSprite.enabled = false;
            }

            FitClipAspect(_clipVideo.rectTransform, width, height);
            if (_video == null)
            {
                _video = gameObject.GetComponent<VideoPlayer>();
                if (_video == null)
                {
                    _video = gameObject.AddComponent<VideoPlayer>();
                }
            }

            _video.playOnAwake = false;
            _video.isLooping = true;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoTexture;
            _video.clip = clip;
            _video.Stop();
            _video.Play();
        }

        private void StopPanelClip()
        {
            if (_video != null)
            {
                _video.Stop();
                _video.clip = null;
                _video.targetTexture = null;
            }

            if (_clipVideo != null)
            {
                _clipVideo.texture = null;
                _clipVideo.enabled = false;
            }

            if (_clipSprite != null)
            {
                _clipSprite.enabled = false;
            }

            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Destroy(_videoTexture);
                _videoTexture = null;
            }
        }

        private void SetClipVisible(bool sprite, bool video)
        {
            if (_clipSprite != null)
            {
                _clipSprite.enabled = sprite;
            }

            if (_clipVideo != null)
            {
                _clipVideo.enabled = video;
            }
        }

        private void EnsureClipSurfaces()
        {
            if (panelImage == null)
            {
                return;
            }

            var slot = panelImage.rectTransform;
            if (_clipSprite == null)
            {
                var spriteGo = new GameObject("ClipSprite", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                spriteGo.transform.SetParent(slot, false);
                _clipSprite = spriteGo.GetComponent<Image>();
                _clipSprite.raycastTarget = false;
                Stretch(spriteGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            if (_clipVideo == null)
            {
                var videoGo = new GameObject("ClipVideo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                videoGo.transform.SetParent(slot, false);
                _clipVideo = videoGo.GetComponent<RawImage>();
                _clipVideo.raycastTarget = false;
                _clipVideo.enabled = false;
                Stretch(videoGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private void FitClipAspect(RectTransform rect, float width, float height)
        {
            if (rect == null || width <= 1f || height <= 1f)
            {
                return;
            }

            _clipFitter = rect.GetComponent<AspectRatioFitter>();
            if (_clipFitter == null)
            {
                _clipFitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            }

            _clipFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _clipFitter.aspectRatio = width / height;
        }

        private void CaptureBodyAnchors()
        {
            if (_bodyAnchorsCaptured || bodyLabel == null)
            {
                return;
            }

            _bodyAnchorMin = bodyLabel.rectTransform.anchorMin;
            _bodyAnchorMax = bodyLabel.rectTransform.anchorMax;
            _bodyAnchorsCaptured = true;
        }

        private void RestoreBodyAnchors()
        {
            CaptureBodyAnchors();
            if (bodyLabel == null)
            {
                return;
            }

            bodyLabel.rectTransform.anchorMin = _bodyAnchorMin;
            bodyLabel.rectTransform.anchorMax = _bodyAnchorMax;
        }

        private void FitBodyToEmptySlot()
        {
            CaptureBodyAnchors();
            if (bodyLabel == null)
            {
                return;
            }

            var top = _bodyAnchorMax.y;
            if (panelImage != null)
            {
                top = Mathf.Max(top, panelImage.rectTransform.anchorMax.y);
            }

            bodyLabel.rectTransform.anchorMin = _bodyAnchorMin;
            bodyLabel.rectTransform.anchorMax = new Vector2(_bodyAnchorMax.x, top);
        }

        private void OnDestroy()
        {
            StopPanelClip();
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
                nextLabel.text = GameLoc.Tr(label);
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
            if (this == null)
            {
                return;
            }

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
                    Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.06f),
                        new Vector2(0.64f, 0.2f), Vector2.zero, Vector2.zero);
                }
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
                backButton.onClick.AddListener(HandleBack);
                backButton.gameObject.SetActive(false);
                UiButtonHoverFeedback.Ensure(backButton.gameObject);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
                nextButton.onClick.AddListener(HandleNext);
                if (!preserveSceneLayout)
                {
                    Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.06f), new Vector2(0.96f, 0.2f),
                        Vector2.zero, Vector2.zero);
                }

                UiButtonHoverFeedback.Ensure(nextButton.gameObject);
            }

            MatchBackButtonToNext();

            if (progressLabel == null)
            {
                var existing = panel.Find("Progress");
                if (existing != null)
                {
                    progressLabel = existing.GetComponent<Text>();
                }
                else
                {
                    progressLabel = CreateText(panel, "Progress", string.Empty, 16, TextAnchor.MiddleCenter);
                    progressLabel.color = new Color(0.7f, 0.85f, 1f, 0.85f);
                }
            }

            if (progressLabel != null)
            {
                PlaceProgressUnderPortrait(progressLabel);
                progressLabel.gameObject.SetActive(false);
            }
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
            Stretch(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-672f, -294f),
                new Vector2(672f, 294f));
            panelRect.localScale = Vector3.one;
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

            bodyLabel = CreateText(panel.transform, "Body", string.Empty, CoachBodyFontSize, TextAnchor.UpperLeft);
            Stretch(bodyLabel.rectTransform, new Vector2(0.24f, 0.22f), new Vector2(0.96f, 0.4f), Vector2.zero,
                Vector2.zero);
            bodyLabel.color = Color.white;
            bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;

            progressLabel = CreateText(panel.transform, "Progress", string.Empty, 16, TextAnchor.MiddleCenter);
            PlaceProgressUnderPortrait(progressLabel);
            progressLabel.color = new Color(0.7f, 0.85f, 1f, 0.85f);
            progressLabel.gameObject.SetActive(false);

            backButton = CreateButton(panel.transform, "BackButton", "Back", out backLabel);
            Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.06f), new Vector2(0.64f, 0.2f),
                Vector2.zero, Vector2.zero);
            backButton.onClick.AddListener(HandleBack);
            backButton.gameObject.SetActive(false);
            UiButtonHoverFeedback.Ensure(backButton.gameObject);

            nextButton = CreateButton(panel.transform, "NextButton", "Next", out nextLabel);
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.06f), new Vector2(0.96f, 0.2f),
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
                canvas.planeDistance = parentCanvas.planeDistance;
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
            labelText = CreateText(go.transform, "Label", label, CoachButtonFontSize, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.color = FcColorTokens.Brand.Cyan;
            labelText.fontStyle = FontStyle.Bold;
            return button;
        }

        private void SanitizePanelFonts()
        {
            var scope = panelRect != null ? panelRect : transform as RectTransform;
            if (scope == null)
            {
                return;
            }

            var labels = scope.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                if (label.fontSize < 1)
                {
                    label.fontSize = CoachBodyFontSize;
                }

                label.resizeTextForBestFit = false;
                if (label.resizeTextMinSize < 1)
                {
                    label.resizeTextMinSize = 1;
                }

                if (label.resizeTextMaxSize < label.fontSize)
                {
                    label.resizeTextMaxSize = label.fontSize;
                }
            }
        }

        private void MatchBackButtonToNext()
        {
            if (backButton == null || nextButton == null)
            {
                return;
            }

            var next = nextButton.GetComponent<RectTransform>();
            var back = backButton.GetComponent<RectTransform>();
            if (next == null || back == null)
            {
                return;
            }

            var width = next.anchorMax.x - next.anchorMin.x;
            var gap = 0.04f;
            back.anchorMin = new Vector2(next.anchorMin.x - gap - width, next.anchorMin.y);
            back.anchorMax = new Vector2(next.anchorMin.x - gap, next.anchorMax.y);
            back.offsetMin = next.offsetMin;
            back.offsetMax = next.offsetMax;
            back.pivot = next.pivot;
            back.localScale = Vector3.one;

            var nextImage = nextButton.targetGraphic as Image;
            var backImage = backButton.targetGraphic as Image;
            if (nextImage != null && backImage != null)
            {
                backImage.sprite = nextImage.sprite;
                backImage.type = nextImage.type;
                backImage.preserveAspect = nextImage.preserveAspect;
                backImage.color = nextImage.color;
            }

            ApplyButtonLabel(nextLabel);
            ApplyButtonLabel(backLabel);
        }

        private static void ApplyButtonLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = CoachButtonFontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void PlaceProgressUnderPortrait(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.16f), Vector2.zero,
                Vector2.zero);
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
