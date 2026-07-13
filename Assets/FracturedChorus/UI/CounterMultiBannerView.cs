using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CounterMultiBannerView : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Text _label;
        private Coroutine _hideRoutine;

        public static CounterMultiBannerView Create(RectTransform parent)
        {
            var go = new GameObject("MultiBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(CounterMultiBannerView));
            var view = go.GetComponent<CounterMultiBannerView>();
            view.Build(parent);
            return view;
        }

        public void Build(RectTransform parent)
        {
            _rect = GetComponent<RectTransform>();
            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = new Vector2(120f, 32f);
            _rect.anchoredPosition = new Vector2(0f, -8f);

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.SetParent(_rect, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bg = bgGo.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
            bg.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(_rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label = labelGo.GetComponent<Text>();
            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontSize = 18;
            _label.fontStyle = FontStyle.Bold;
            _label.color = new Color(1f, 0.92f, 0.55f, 1f);
            _label.raycastTarget = false;
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_label.font == null)
            {
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            gameObject.SetActive(false);
        }

        public void ShowOrRefresh(int count, float lifetime = 0.6f)
        {
            gameObject.SetActive(true);
            _label.text = $"MULTI ×{count}";
            _canvasGroup.alpha = 1f;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            _hideRoutine = StartCoroutine(HideAfter(lifetime));
        }

        public void HideImmediate()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator HideAfter(float lifetime)
        {
            yield return new WaitForSecondsRealtime(lifetime);
            var elapsed = 0f;
            const float fade = 0.15f;
            var start = _canvasGroup.alpha;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fade);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _hideRoutine = null;
        }
    }
}
