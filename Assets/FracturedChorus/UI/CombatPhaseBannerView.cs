using System.Collections;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class CombatPhaseBannerView : MonoBehaviour
    {
        public const float BattleStartDurationSec = 2f;
        public const float PlanningDurationSec = 1f;
        private const float SlideSec = 0.28f;

        private const string BattleStartResourcePath = "UI/Combat/Banners/combat_banner_battle_start_v1";
        private const string PlanningResourcePath = "UI/Combat/Banners/combat_banner_planning_phase_v1";
        private const float BannerMaxHeight = 320f;

        [SerializeField] private Image bannerImage;
        [SerializeField] private Sprite battleStartSprite;
        [SerializeField] private Sprite planningSprite;

        private Coroutine _playRoutine;
        private RectTransform _bannerRect;

        public static CombatPhaseBannerView EnsureOn(RectTransform parent)
        {
            var existing = FindAnyObjectByType<CombatPhaseBannerView>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureOverlayCanvas();
                existing.EnsureBuilt();
                return existing;
            }

            var go = new GameObject("CombatPhaseBanner", typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<CombatPhaseBannerView>();
            view.EnsureOverlayCanvas();
            view.EnsureBuilt();
            return view;
        }

        public void PlayBattleStart()
        {
            Play(ResolveSprite(ref battleStartSprite, BattleStartResourcePath), DefaultBeatHoldSec);
        }

        public void PlayPlanning()
        {
            Play(ResolveSprite(ref planningSprite, PlanningResourcePath), DefaultBeatHoldSec);
        }

        public IEnumerator PlayBattleStartRoutine(float beatDurationSec)
        {
            yield return PlayAndWait(ResolveSprite(ref battleStartSprite, BattleStartResourcePath), beatDurationSec);
        }

        public IEnumerator PlayPlanningRoutine(float beatDurationSec)
        {
            yield return PlayAndWait(ResolveSprite(ref planningSprite, PlanningResourcePath), beatDurationSec);
        }

        public void EnsureOverlayCanvas()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

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

        private void EnsureBuilt()
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
                var child = transform.Find("Banner");
                bannerImage = child != null ? child.GetComponent<Image>() : null;
            }

            if (bannerImage == null)
            {
                var imageGo = new GameObject("Banner", typeof(RectTransform), typeof(Image));
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

        private void Play(Sprite sprite, float beatHoldSec)
        {
            if (sprite == null)
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
            if (sprite == null)
            {
                yield break;
            }

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

        private IEnumerator PlayRoutine(Sprite sprite, float beatHoldSec)
        {
            gameObject.SetActive(true);
            bannerImage.enabled = true;
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
            gameObject.SetActive(false);
            _playRoutine = null;
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

        private static Sprite ResolveSprite(ref Sprite cached, string resourcePath)
        {
            if (cached != null)
            {
                return cached;
            }

            cached = Resources.Load<Sprite>(resourcePath);
            if (cached != null)
            {
                return cached;
            }

            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                cached = sprites[0];
            }

            return cached;
        }
    }
}
