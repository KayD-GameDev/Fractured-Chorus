using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        public const float BarWidth = 720f;
        public const float BarHeight = 36f;
        private const float FillInset = 3f;
        private const float CapsuleBorderTexels = 31f;
        private static readonly Color NeonPink = new Color(1f, 0.306f, 0.784f, 1f);
        private static readonly Color FillWhite = new Color(1f, 0.95f, 0.98f, 1f);
        private static readonly Color TrackInterior = new Color(0.14f, 0.05f, 0.22f, 0.92f);
        private static readonly string[] BackgroundResourcePaths =
        {
            "UI/LoadingBg/loading_bg_01",
            "UI/LoadingBg/loading_bg_02",
            "UI/LoadingBg/loading_bg_03"
        };
        private static readonly Color DimColor = new Color(0.02f, 0.01f, 0.06f, 0.82f);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fill;
        [SerializeField] private Image track;
        [SerializeField] private Text percentLabel;
        [SerializeField] private Text loadingLabel;
        [SerializeField] private RectTransform percentRect;
        [SerializeField] private RectTransform clef;
        [SerializeField] private RectTransform notesStars;
        [SerializeField] private Image skyFill;
        [SerializeField] private Image clouds;
        [SerializeField] private Image skyline;
        [SerializeField] private Image buildingsSigns;
        [SerializeField] private Image floor;

        private float _progress;
        private float _clefPhase;
        private float _notesPhase;
        private static int _lastBackgroundIndex = -1;
        private Sprite[] _backgrounds;

        public float FillAmount => _progress;
        public string PercentText => percentLabel != null ? percentLabel.text : string.Empty;
        public bool PercentVisible => percentLabel != null && percentLabel.gameObject.activeSelf;
        public CanvasGroup Group => canvasGroup;

        private void Awake()
        {
            ApplyChrome();
        }

        public void Bind(
            CanvasGroup group,
            Image fillImage,
            Text percent,
            Text loading,
            RectTransform percentTransform,
            RectTransform clefTransform,
            RectTransform notesTransform)
        {
            canvasGroup = group;
            fill = fillImage;
            percentLabel = percent;
            loadingLabel = loading;
            percentRect = percentTransform;
            clef = clefTransform;
            notesStars = notesTransform;
            if (percentLabel != null)
            {
                percentLabel.font = UiFontCatalog.Body;
            }

            if (loadingLabel != null)
            {
                loadingLabel.font = UiFontCatalog.Body;
                loadingLabel.fontStyle = FontStyle.Bold;
            }
        }

        public void BindLayers(Image sky, Image cloudImage, Image skylineImage, Image buildings, Image floorImage)
        {
            skyFill = sky;
            clouds = cloudImage;
            skyline = skylineImage;
            buildingsSigns = buildings;
            floor = floorImage;
        }

        public void ApplyChrome()
        {
            HideLayer(clouds);
            HideLayer(skyline);
            HideLayer(buildingsSigns);
            HideLayer(floor);
            HideRect(clef);
            HideRect(notesStars);

            ApplyBackground();

            var capsule = UiCircleSpriteUtil.Capsule;
            ResolveTrack();
            if (track != null)
            {
                track.sprite = capsule;
                track.type = Image.Type.Sliced;
                track.color = TrackInterior;
                track.pixelsPerUnitMultiplier = CapsuleBorderTexels / (BarHeight * 0.5f);
                var outline = track.GetComponent<Outline>() ?? track.gameObject.AddComponent<Outline>();
                outline.effectColor = NeonPink;
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = true;
            }

            if (fill != null)
            {
                fill.sprite = capsule;
                fill.type = Image.Type.Sliced;
                fill.color = FillWhite;
                fill.raycastTarget = false;
                var innerH = BarHeight - FillInset * 2f;
                fill.pixelsPerUnitMultiplier = CapsuleBorderTexels / (innerH * 0.5f);
                var rt = fill.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(FillInset, 0f);
                var glow = fill.GetComponent<Outline>() ?? fill.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(NeonPink.r, NeonPink.g, NeonPink.b, 0.9f);
                glow.effectDistance = new Vector2(2f, -2f);
                glow.useGraphicAlpha = true;
            }

            if (loadingLabel != null)
            {
                loadingLabel.color = Color.white;
                loadingLabel.fontStyle = FontStyle.Bold;
                var glow = loadingLabel.GetComponent<Outline>() ?? loadingLabel.gameObject.AddComponent<Outline>();
                glow.effectColor = NeonPink;
                glow.effectDistance = new Vector2(1.25f, -1.25f);
                glow.useGraphicAlpha = true;
            }

            if (percentLabel != null && fill != null && percentRect != null)
            {
                percentRect.SetParent(fill.rectTransform, false);
                percentRect.anchorMin = new Vector2(1f, 0.5f);
                percentRect.anchorMax = new Vector2(1f, 0.5f);
                percentRect.pivot = new Vector2(1f, 0.5f);
                percentRect.sizeDelta = new Vector2(72f, 24f);
                percentRect.anchoredPosition = new Vector2(-10f, 0f);
                percentLabel.alignment = TextAnchor.MiddleRight;
                percentLabel.fontStyle = FontStyle.Normal;
                percentLabel.fontSize = 16;
                percentLabel.color = Color.white;
            }

            SetProgress(_progress);
        }

        public void ApplyBackground()
        {
            PickRandomBackground();
        }

        public void PickRandomBackground()
        {
            if (skyFill == null)
            {
                return;
            }

            EnsureBackgrounds();
            Sprite chosen = null;
            if (_backgrounds.Length > 0)
            {
                var index = Random.Range(0, _backgrounds.Length);
                if (_backgrounds.Length > 1 && index == _lastBackgroundIndex)
                {
                    index = (index + 1) % _backgrounds.Length;
                }

                _lastBackgroundIndex = index;
                chosen = _backgrounds[index];
            }

            skyFill.sprite = chosen;
            skyFill.type = Image.Type.Simple;
            skyFill.preserveAspect = false;
            skyFill.raycastTarget = false;
            skyFill.color = chosen != null ? Color.white : DimColor;
            skyFill.gameObject.SetActive(true);
        }

        private void EnsureBackgrounds()
        {
            if (_backgrounds != null)
            {
                return;
            }

            var loaded = new Sprite[BackgroundResourcePaths.Length];
            var count = 0;
            for (var i = 0; i < BackgroundResourcePaths.Length; i++)
            {
                var sprite = Resources.Load<Sprite>(BackgroundResourcePaths[i]);
                if (sprite != null)
                {
                    loaded[count++] = sprite;
                }
            }

            _backgrounds = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                _backgrounds[i] = loaded[i];
            }
        }

        public void BuildForTests()
        {
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(transform, false);
            fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            var percentGo = new GameObject("Percent", typeof(RectTransform), typeof(Text));
            percentGo.transform.SetParent(transform, false);
            percentLabel = percentGo.GetComponent<Text>();
            percentLabel.font = UiFontCatalog.Body;
            percentRect = percentGo.GetComponent<RectTransform>();
            SetProgress(0f);
        }

        public void SetProgress(float normalized01)
        {
            _progress = Mathf.Clamp01(normalized01);
            if (fill != null)
            {
                if (fill.type == Image.Type.Filled)
                {
                    fill.fillAmount = _progress;
                }
                else
                {
                    ApplyFillWidth(_progress);
                }
            }

            if (percentLabel != null)
            {
                percentLabel.text = $"{Mathf.RoundToInt(_progress * 100f)}%";
                var show = _progress >= LoadingProgress.PercentVisibleMin;
                if (percentLabel.gameObject.activeSelf != show)
                {
                    percentLabel.gameObject.SetActive(show);
                }
            }

            if (percentRect != null && (fill == null || percentRect.parent != fill.rectTransform))
            {
                var x = Mathf.Lerp(24f, BarWidth - 40f, _progress);
                percentRect.anchoredPosition = new Vector2(x, 0f);
            }
        }

        public void SetVisible(bool visible, bool instant)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = false;
            if (instant)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        public void TickMotion(float unscaledDeltaTime)
        {
            if (clef != null && clef.gameObject.activeInHierarchy)
            {
                _clefPhase += unscaledDeltaTime * (Mathf.PI * 2f / 2.4f);
                var s = Mathf.Lerp(0.97f, 1.03f, (Mathf.Sin(_clefPhase) + 1f) * 0.5f);
                clef.localScale = new Vector3(s, s, 1f);
            }

            if (notesStars != null && notesStars.gameObject.activeInHierarchy)
            {
                _notesPhase += unscaledDeltaTime * (Mathf.PI * 2f / 3.5f);
                var y = Mathf.Sin(_notesPhase) * 6f;
                notesStars.anchoredPosition = new Vector2(notesStars.anchoredPosition.x, y);
            }
        }

        private void ApplyFillWidth(float p)
        {
            var innerH = BarHeight - FillInset * 2f;
            var maxW = BarWidth - FillInset * 2f;
            var w = Mathf.Lerp(innerH, maxW, p);
            var rt = fill.rectTransform;
            rt.sizeDelta = new Vector2(w, innerH);
            rt.anchoredPosition = new Vector2(FillInset, 0f);
        }

        private void ResolveTrack()
        {
            if (track != null || fill == null)
            {
                return;
            }

            var parent = fill.transform.parent;
            if (parent == null)
            {
                return;
            }

            var found = parent.Find("Track");
            if (found != null)
            {
                track = found.GetComponent<Image>();
            }
        }

        private static void HideLayer(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.gameObject.SetActive(false);
        }

        private static void HideRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.gameObject.SetActive(false);
        }
    }
}
