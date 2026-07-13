using System.Collections;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CounterNoteResolveChipView : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _rim;
        private Text _label;
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
            _rect.sizeDelta = new Vector2(36f, 28f);

            _rim = goImage(_rect, "Rim", new Vector2(36f, 28f));
            _rim.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(_rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label = labelGo.GetComponent<Text>();
            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontSize = 16;
            _label.fontStyle = FontStyle.Bold;
            _label.color = Color.white;
            _label.raycastTarget = false;
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_label.font == null)
            {
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        public void Play(Vector2 anchoredPos, BossNoteTier tier, int hitsDelta, float duration = 0.3f)
        {
            gameObject.SetActive(true);
            _rect.anchoredPosition = anchoredPos;
            var color = BossNoteTierColors.ForTier(tier);
            _rim.color = color;
            _label.text = hitsDelta <= 1 ? "−1" : $"×{hitsDelta}";
            _label.color = Color.white;

            if (_pulse != null)
            {
                StopCoroutine(_pulse);
            }

            _pulse = StartCoroutine(PulseRoutine(duration));
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

        private IEnumerator PulseRoutine(float duration)
        {
            var elapsed = 0f;
            var start = _rect.anchoredPosition;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.35f
                    ? Mathf.Lerp(0.7f, 1.1f, t / 0.35f)
                    : Mathf.Lerp(1.1f, 1f, (t - 0.35f) / 0.65f);
                _rect.localScale = Vector3.one * scale;
                _rect.anchoredPosition = start + new Vector2(0f, 16f * t);
                var cg = GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                }

                yield return null;
            }

            gameObject.SetActive(false);
            _pulse = null;
        }

        private static Image goImage(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            return image;
        }
    }
}
