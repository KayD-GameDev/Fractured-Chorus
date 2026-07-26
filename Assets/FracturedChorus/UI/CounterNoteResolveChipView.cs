using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CounterNoteResolveChipView : MonoBehaviour
    {
        private const string PerfectSpriteResourcePath = "UI/Combat/combat_perfect_popup_v1";
        private const string OverlayCanvasName = "PerfectChipCanvas";
        private const int OverlaySortOrder = 530;
        private const int PoolCap = 8;

        public static Vector2 DisplaySize { get; set; } = new Vector2(168f, 112f);
        public static float DefaultDuration { get; set; } = 0.55f;

        private static Sprite _perfectSprite;
        private static readonly Queue<CounterNoteResolveChipView> Pool = new();
        private static RectTransform _overlayRoot;
        private static Camera _worldCamera;

        private RectTransform _rect;
        private Image _image;
        private CanvasGroup _canvasGroup;
        private Coroutine _pulse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pool.Clear();
            _overlayRoot = null;
            _worldCamera = null;
            _perfectSprite = null;
        }

        public static void SpawnAboveWorld(Vector3 worldPosition, BossNoteTier tier = BossNoteTier.Red)
        {
            EnsureOverlay();
            var chip = Rent();
            var anchored = WorldToCanvas(worldPosition);
            chip.Play(anchored, tier);
        }

        public static CounterNoteResolveChipView Create(RectTransform parent)
        {
            var go = new GameObject(
                "ResolveChip",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(CounterNoteResolveChipView));
            var view = go.GetComponent<CounterNoteResolveChipView>();
            view.Build(parent);
            return view;
        }

        public void Build(RectTransform parent)
        {
            _rect = GetComponent<RectTransform>();
            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = DisplaySize;

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var imageGo = new GameObject("PerfectSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var imageRect = imageGo.GetComponent<RectTransform>();
            imageRect.SetParent(_rect, false);
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            _image = imageGo.GetComponent<Image>();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            _image.color = Color.white;
            _image.sprite = ResolvePerfectSprite();
        }

        public void Play(Vector2 anchoredPos, BossNoteTier tier, float duration = -1f)
        {
            gameObject.SetActive(true);
            _rect.SetAsLastSibling();
            _rect.sizeDelta = DisplaySize;
            _rect.anchoredPosition = anchoredPos;
            _rect.localScale = Vector3.one * 0.7f;

            if (_image.sprite == null)
            {
                _image.sprite = ResolvePerfectSprite();
            }

            _image.color = Color.white;
            _ = tier;

            if (_pulse != null)
            {
                StopCoroutine(_pulse);
            }

            var playDuration = duration > 0f ? duration : DefaultDuration;
            _pulse = StartCoroutine(PulseRoutine(playDuration));
        }

        public void ForceHide()
        {
            if (_pulse != null)
            {
                StopCoroutine(_pulse);
                _pulse = null;
            }

            gameObject.SetActive(false);
            Recycle();
        }

        private static void EnsureOverlay()
        {
            if (_overlayRoot != null)
            {
                return;
            }

            var existing = GameObject.Find(OverlayCanvasName);
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            var go = new GameObject(
                OverlayCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;
            canvas.overrideSorting = true;
            canvas.pixelPerfect = true;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.GetComponent<GraphicRaycaster>().enabled = false;
            _overlayRoot = go.GetComponent<RectTransform>();
            _worldCamera = Camera.main;
        }

        private static CounterNoteResolveChipView Rent()
        {
            while (Pool.Count > 0)
            {
                var pooled = Pool.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            return Create(_overlayRoot);
        }

        private void Recycle()
        {
            if (Pool.Count < PoolCap)
            {
                Pool.Enqueue(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private static Vector2 WorldToCanvas(Vector3 worldPosition)
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_worldCamera == null || _overlayRoot == null)
            {
                return Vector2.zero;
            }

            var screen = _worldCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                screen,
                null,
                out var local);
            return local;
        }

        private static Sprite ResolvePerfectSprite()
        {
#if UNITY_EDITOR
            _perfectSprite = null;
#endif
            if (_perfectSprite != null)
            {
                return _perfectSprite;
            }

            _perfectSprite = Resources.Load<Sprite>(PerfectSpriteResourcePath);
            if (_perfectSprite != null)
            {
                return _perfectSprite;
            }

            var tex = Resources.Load<Texture2D>(PerfectSpriteResourcePath);
            if (tex == null)
            {
                Debug.LogWarning("[CounterPerfect] Missing Resources sprite: " + PerfectSpriteResourcePath);
                return null;
            }

            _perfectSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _perfectSprite;
        }

        private IEnumerator PulseRoutine(float duration)
        {
            var elapsed = 0f;
            var start = _rect.anchoredPosition;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.28f
                    ? Mathf.Lerp(0.55f, 1.15f, t / 0.28f)
                    : Mathf.Lerp(1.15f, 1f, (t - 0.28f) / 0.72f);
                _rect.localScale = Vector3.one * scale;
                _rect.anchoredPosition = start + new Vector2(0f, 48f * t);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
                }

                yield return null;
            }

            gameObject.SetActive(false);
            _pulse = null;
            Recycle();
        }
    }
}
