using System.Collections;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class CombatPhaseBannerView : MonoBehaviour
    {
        public const string ObjectName = "BattleInfo";
        public const string BannerChildName = "Banner";
        public const float BattleStartDurationSec = 2f;
        public const float PlanningDurationSec = 1f;
        private const float SlideSec = 0.28f;
        private const float BannerMaxHeight = 320f;

        [SerializeField] private Image bannerImage;
        [SerializeField] private Sprite battleStartSprite;
        [SerializeField] private Sprite planningSprite;

        public enum BannerPreviewKind
        {
            Planning = 0,
            BattleStart = 1
        }

        public Image BannerImage => bannerImage;
        public Sprite PlanningSprite => planningSprite;
        public Sprite BattleStartSprite => battleStartSprite;

        private Coroutine _playRoutine;
        private RectTransform _bannerRect;
        private bool _warnedMissingPlanning;
        private bool _warnedMissingBattleStart;

        public void PlayBattleStart()
        {
            Play(battleStartSprite, DefaultBeatHoldSec, ref _warnedMissingBattleStart, "Battle Start");
        }

        public void PlayPlanning()
        {
            Play(planningSprite, DefaultBeatHoldSec, ref _warnedMissingPlanning, "Planning Phase");
        }

        public IEnumerator PlayBattleStartRoutine(float beatDurationSec)
        {
            if (!TryResolveSprite(battleStartSprite, ref _warnedMissingBattleStart, "Battle Start"))
            {
                yield break;
            }

            yield return PlayAndWait(battleStartSprite, beatDurationSec);
        }

        public IEnumerator PlayPlanningRoutine(float beatDurationSec)
        {
            if (!TryResolveSprite(planningSprite, ref _warnedMissingPlanning, "Planning Phase"))
            {
                yield break;
            }

            yield return PlayAndWait(planningSprite, beatDurationSec);
        }

        public void EnsureOverlayCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.PhaseBanner;
            canvas.pixelPerfect = false;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.blocksRaycasts = false;
            group.interactable = false;
            group.ignoreParentGroups = true;
            transform.SetAsLastSibling();
        }

        public void EnsureBuilt()
        {
            EnsureOverlayCanvas();
            var root = transform as RectTransform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0.5f);

            if (bannerImage == null)
            {
                var child = transform.Find(BannerChildName);
                bannerImage = child != null ? child.GetComponent<Image>() : null;
            }

            if (bannerImage == null)
            {
                var imageGo = new GameObject(BannerChildName, typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(transform, false);
                bannerImage = imageGo.GetComponent<Image>();
            }

            bannerImage.raycastTarget = false;
            bannerImage.preserveAspect = true;
            bannerImage.type = Image.Type.Simple;
            bannerImage.color = Color.white;
            bannerImage.material = null;
            bannerImage.enabled = false;
            _bannerRect = bannerImage.rectTransform;
            _bannerRect.anchorMin = new Vector2(0.5f, 0.55f);
            _bannerRect.anchorMax = new Vector2(0.5f, 0.55f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerImage.transform.SetAsLastSibling();
        }

        private static float DefaultBeatHoldSec => 60f / TimelineConstants.BossRemixBpm;

        private void Play(Sprite sprite, float beatHoldSec, ref bool warned, string label)
        {
            if (!TryResolveSprite(sprite, ref warned, label))
            {
                return;
            }

            EnsureBuilt();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayRoutine(sprite, beatHoldSec));
        }

        private IEnumerator PlayAndWait(Sprite sprite, float beatHoldSec)
        {
            EnsureBuilt();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            yield return PlayRoutine(sprite, beatHoldSec);
        }

        private static bool TryResolveSprite(Sprite sprite, ref bool warned, string label)
        {
            if (sprite != null)
            {
                return true;
            }

            if (!warned)
            {
                warned = true;
                Debug.LogWarning(
                    $"[BattleInfo] Missing {label} sprite. Assign it on the BattleInfo Inspector.");
            }

            return false;
        }

        private IEnumerator PlayRoutine(Sprite sprite, float beatHoldSec)
        {
            gameObject.SetActive(true);
            ShowBannerVisual();
            bannerImage.sprite = sprite;
            bannerImage.SetNativeSize();
            FitBannerSize();
            bannerImage.transform.SetAsLastSibling();

            var travel = ResolveTravelX();
            var from = new Vector2(-travel, 0f);
            var mid = Vector2.zero;
            var to = new Vector2(travel, 0f);
            var hold = Mathf.Max(0.05f, beatHoldSec);

            _bannerRect.anchoredPosition = from;
            yield return Slide(_bannerRect, from, mid, SlideSec);
            yield return new WaitForSeconds(hold);
            yield return Slide(_bannerRect, mid, to, SlideSec);
            HideBannerVisual();
            _playRoutine = null;
        }

        public void PreviewBanner(BannerPreviewKind kind)
        {
            EnsureBuilt();
            var sprite = kind == BannerPreviewKind.Planning ? planningSprite : battleStartSprite;
            if (bannerImage == null || sprite == null)
            {
                return;
            }

            ShowBannerVisual();
            bannerImage.sprite = sprite;
        }

        public void ApplyBannerNativeSize()
        {
            if (bannerImage == null || bannerImage.sprite == null)
            {
                return;
            }

            bannerImage.SetNativeSize();
            _bannerRect = bannerImage.rectTransform;
        }

        public void ShowBannerVisual()
        {
            if (bannerImage != null)
            {
                bannerImage.enabled = true;
            }

            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
            }
        }

        public void HideBannerVisual()
        {
            if (bannerImage != null)
            {
                bannerImage.enabled = false;
            }

            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
            }

            if (_bannerRect != null)
            {
                _bannerRect.anchoredPosition = Vector2.zero;
            }
        }

        private void FitBannerSize()
        {
            var size = _bannerRect.sizeDelta;
            if (size.y <= BannerMaxHeight || size.y < 1f)
            {
                return;
            }

            var scale = BannerMaxHeight / size.y;
            _bannerRect.sizeDelta = size * scale;
        }

        private float ResolveTravelX()
        {
            var parent = transform as RectTransform;
            var width = parent != null ? parent.rect.width : 0f;
            if (width < 100f)
            {
                width = 1920f;
            }

            return width * 0.65f + _bannerRect.sizeDelta.x * 0.5f;
        }

        private static IEnumerator Slide(RectTransform rect, Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                rect.anchoredPosition = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / duration);
                var eased = u * u * (3f - 2f * u);
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            rect.anchoredPosition = to;
        }
    }
}
