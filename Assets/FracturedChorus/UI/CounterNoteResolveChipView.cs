using System.Collections;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CounterNoteResolveChipView : MonoBehaviour
    {
        private const string PerfectSpriteResourcePath = "UI/Combat/combat_perfect_popup_v1";

        public static Vector2 DisplaySize { get; set; } = new Vector2(168f, 112f);
        public static float DefaultDuration { get; set; } = 0.55f;

        private static Sprite _perfectSprite;

        private RectTransform _rect;
        private Image _image;
        private Coroutine _pulse;

        public static CounterNoteResolveChipView Create(RectTransform parent)
        {
            var go = new GameObject("ResolveChip", typeof(RectTransform), typeof(CanvasGroup), typeof(CounterNoteResolveChipView));
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
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.28f
                    ? Mathf.Lerp(0.55f, 1.15f, t / 0.28f)
                    : Mathf.Lerp(1.15f, 1f, (t - 0.28f) / 0.72f);
                _rect.localScale = Vector3.one * scale;
                _rect.anchoredPosition = start + new Vector2(0f, 42f * t);
                if (cg != null)
                {
                    cg.alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
                }

                yield return null;
            }

            gameObject.SetActive(false);
            _pulse = null;
        }
    }
}
