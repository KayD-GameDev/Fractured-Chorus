using System.Collections;
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

        [SerializeField] private Image bannerImage;
        [SerializeField] private Sprite battleStartSprite;
        [SerializeField] private Sprite planningSprite;
        [SerializeField] private float battleStartHoldSec = BattleStartDurationSec;
        [SerializeField] private float planningHoldSec = PlanningDurationSec;

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
        private bool _capturedSceneRect;
        private Vector2 _restSize;
        private Vector2 _restPosition;
        private Vector2 _restAnchorMin;
        private Vector2 _restAnchorMax;
        private Vector2 _restPivot;
        private Vector3 _restScale;
        private Quaternion _restRotation;

        private void Awake()
        {
            BindBanner();
            if (bannerImage == null)
            {
                EnsureBuilt();
            }

            CaptureBannerSceneRect();
        }

        public void PlayBattleStart()
        {
            Play(battleStartSprite, ResolveHoldSec(battleStartHoldSec), ref _warnedMissingBattleStart, "Battle Start");
        }

        public void PlayPlanning()
        {
            Play(planningSprite, ResolveHoldSec(planningHoldSec), ref _warnedMissingPlanning, "Planning Phase");
        }

        public IEnumerator PlayBattleStartRoutine()
        {
            if (!TryResolveSprite(battleStartSprite, ref _warnedMissingBattleStart, "Battle Start"))
            {
                yield break;
            }

            yield return PlayAndWait(battleStartSprite, ResolveHoldSec(battleStartHoldSec));
        }

        public IEnumerator PlayPlanningRoutine()
        {
            if (!TryResolveSprite(planningSprite, ref _warnedMissingPlanning, "Planning Phase"))
            {
                yield break;
            }

            yield return PlayAndWait(planningSprite, ResolveHoldSec(planningHoldSec));
        }

        public void ConfigureNewOverlayCanvas()
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
        }

        public void EnsureBuilt()
        {
            if (GetComponent<Canvas>() == null)
            {
                ConfigureNewOverlayCanvas();
            }

            BindBanner();
            if (bannerImage == null)
            {
                var imageGo = new GameObject(BannerChildName, typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(transform, false);
                bannerImage = imageGo.GetComponent<Image>();
                bannerImage.raycastTarget = false;
                bannerImage.preserveAspect = true;
                bannerImage.type = Image.Type.Simple;
                bannerImage.color = Color.white;
                _bannerRect = bannerImage.rectTransform;
                _bannerRect.anchorMin = new Vector2(0.5f, 0.55f);
                _bannerRect.anchorMax = new Vector2(0.5f, 0.55f);
                _bannerRect.pivot = new Vector2(0.5f, 0.5f);
                _capturedSceneRect = false;
            }

            CaptureBannerSceneRect();
        }

        private void BindBanner()
        {
            if (bannerImage == null)
            {
                var child = transform.Find(BannerChildName);
                bannerImage = child != null ? child.GetComponent<Image>() : null;
            }

            if (bannerImage != null)
            {
                _bannerRect = bannerImage.rectTransform;
            }
        }

        private void CaptureBannerSceneRect()
        {
            if (_capturedSceneRect || _bannerRect == null)
            {
                return;
            }

            _restAnchorMin = _bannerRect.anchorMin;
            _restAnchorMax = _bannerRect.anchorMax;
            _restPivot = _bannerRect.pivot;
            _restSize = _bannerRect.sizeDelta;
            _restPosition = _bannerRect.anchoredPosition;
            _restScale = _bannerRect.localScale;
            _restRotation = _bannerRect.localRotation;
            _capturedSceneRect = true;
        }

        private void ApplyBannerSceneRect(float anchoredX)
        {
            if (_bannerRect == null)
            {
                return;
            }

            CaptureBannerSceneRect();
            _bannerRect.anchorMin = _restAnchorMin;
            _bannerRect.anchorMax = _restAnchorMax;
            _bannerRect.pivot = _restPivot;
            _bannerRect.sizeDelta = _restSize;
            _bannerRect.localScale = _restScale;
            _bannerRect.localRotation = _restRotation;
            _bannerRect.anchoredPosition = new Vector2(anchoredX, _restPosition.y);
        }

        private static float ResolveHoldSec(float authored)
        {
            return Mathf.Max(0.05f, authored);
        }

        private void Play(Sprite sprite, float beatHoldSec, ref bool warned, string label)
        {
            if (!TryResolveSprite(sprite, ref warned, label))
            {
                return;
            }

            BindBanner();
            if (bannerImage == null)
            {
                EnsureBuilt();
            }

            CaptureBannerSceneRect();
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
            BindBanner();
            if (bannerImage == null)
            {
                EnsureBuilt();
            }

            CaptureBannerSceneRect();
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

        private IEnumerator PlayRoutine(Sprite sprite, float holdSec)
        {
            if (bannerImage == null || _bannerRect == null)
            {
                yield break;
            }

            gameObject.SetActive(true);
            ShowBannerVisual();
            bannerImage.sprite = sprite;
            bannerImage.transform.SetAsLastSibling();
            ApplyBannerSceneRect(_restPosition.x);

            var travel = ResolveTravelX();
            var from = new Vector2(-travel, _restPosition.y);
            var mid = _restPosition;
            var to = new Vector2(travel, _restPosition.y);
            var hold = Mathf.Max(0.05f, holdSec);

            ApplyBannerSceneRect(from.x);
            yield return Slide(_bannerRect, from, mid, SlideSec);
            yield return new WaitForSeconds(hold);
            yield return Slide(_bannerRect, mid, to, SlideSec);
            HideBannerVisual();
            _playRoutine = null;
        }

        public void PreviewBanner(BannerPreviewKind kind)
        {
            BindBanner();
            if (bannerImage == null)
            {
                EnsureBuilt();
            }

            RecaptureBannerSceneRect();
            var sprite = kind == BannerPreviewKind.Planning ? planningSprite : battleStartSprite;
            if (bannerImage == null || sprite == null)
            {
                return;
            }

            ShowBannerVisual();
            bannerImage.sprite = sprite;
        }

        public void RecaptureBannerSceneRect()
        {
            BindBanner();
            _capturedSceneRect = false;
            CaptureBannerSceneRect();
        }

        public void ApplyBannerNativeSize()
        {
            if (bannerImage == null || bannerImage.sprite == null)
            {
                return;
            }

            bannerImage.SetNativeSize();
            _bannerRect = bannerImage.rectTransform;
            _capturedSceneRect = false;
            CaptureBannerSceneRect();
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

            if (Application.isPlaying && _bannerRect != null)
            {
                ApplyBannerSceneRect(_restPosition.x);
            }
        }

        private float ResolveTravelX()
        {
            var parent = transform as RectTransform;
            var width = parent != null ? parent.rect.width : 0f;
            if (width < 100f)
            {
                width = 1920f;
            }

            var bannerWidth = _restSize.x > 1f ? _restSize.x : _bannerRect.sizeDelta.x;
            return width * 0.65f + bannerWidth * 0.5f;
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
