using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        public const float BarWidth = 720f;
        public const float BarHeight = 36f;
        private static readonly Color NeonPink = new Color(1f, 0.306f, 0.784f, 1f);
        private static readonly Color FillWhite = new Color(1f, 0.92f, 0.96f, 1f);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fill;
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

        private float _clefPhase;
        private float _notesPhase;

        public float FillAmount => fill != null ? fill.fillAmount : 0f;
        public string PercentText => percentLabel != null ? percentLabel.text : string.Empty;
        public bool PercentVisible => percentLabel != null && percentLabel.gameObject.activeSelf;
        public CanvasGroup Group => canvasGroup;

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
        }

        public void BindLayers(Image sky, Image cloudImage, Image skylineImage, Image buildings, Image floorImage)
        {
            skyFill = sky;
            clouds = cloudImage;
            skyline = skylineImage;
            buildingsSigns = buildings;
            floor = floorImage;
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
            percentLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentRect = percentGo.GetComponent<RectTransform>();
            SetProgress(0f);
        }

        public void SetProgress(float normalized01)
        {
            var p = Mathf.Clamp01(normalized01);
            if (fill != null)
            {
                fill.fillAmount = p;
            }

            if (percentLabel != null)
            {
                percentLabel.text = $"{Mathf.RoundToInt(p * 100f)}%";
                var show = p >= LoadingProgress.PercentVisibleMin;
                if (percentLabel.gameObject.activeSelf != show)
                {
                    percentLabel.gameObject.SetActive(show);
                }
            }

            if (percentRect != null)
            {
                var x = Mathf.Lerp(24f, BarWidth - 40f, p);
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
            _clefPhase += unscaledDeltaTime * (Mathf.PI * 2f / 2.4f);
            _notesPhase += unscaledDeltaTime * (Mathf.PI * 2f / 3.5f);
            if (clef != null)
            {
                var s = Mathf.Lerp(0.97f, 1.03f, (Mathf.Sin(_clefPhase) + 1f) * 0.5f);
                clef.localScale = new Vector3(s, s, 1f);
            }

            if (notesStars != null)
            {
                var y = Mathf.Sin(_notesPhase) * 6f;
                notesStars.anchoredPosition = new Vector2(notesStars.anchoredPosition.x, y);
            }
        }
    }
}
