using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class HubMenuButtonTailFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image fxImage;
        [SerializeField] private float pulseSpeed = 2.4f;
        [SerializeField] private float hoverMinAlpha = 0.45f;
        [SerializeField] private float hoverMaxAlpha = 1f;
        [SerializeField] private float chaseAmplitude = 5f;
        [SerializeField] private float chaseSpeed = 1.7f;
        [SerializeField] private Color tint = new Color(0.55f, 0.95f, 1f, 1f);

        private RectTransform _fxRect;
        private Vector2 _basePos;
        private bool _hovered;
        private float _phase;
        private bool _baseCaptured;

        public static HubMenuButtonTailFx Ensure(Button button, Sprite edgeSprite)
        {
            if (button == null || edgeSprite == null)
            {
                return null;
            }

            var existing = button.GetComponent<HubMenuButtonTailFx>();
            if (existing != null)
            {
                existing.BindExisting(edgeSprite);
                return existing;
            }

            var fx = button.gameObject.AddComponent<HubMenuButtonTailFx>();
            fx.BuildOverlay(edgeSprite);
            return fx;
        }

        private void BindExisting(Sprite edgeSprite)
        {
            if (fxImage == null)
            {
                var child = transform.Find("TailEdgeFx");
                if (child != null)
                {
                    fxImage = child.GetComponent<Image>();
                    _fxRect = child as RectTransform;
                }
            }

            if (fxImage == null)
            {
                BuildOverlay(edgeSprite);
                return;
            }

            if (edgeSprite != null)
            {
                fxImage.sprite = edgeSprite;
            }

            fxImage.raycastTarget = false;
            _fxRect = fxImage.rectTransform;
            CaptureBaseFromScene();
            ApplyVisual(0f, 0f);
        }

        private void BuildOverlay(Sprite edgeSprite)
        {
            var existing = transform.Find("TailEdgeFx");
            if (existing != null)
            {
                fxImage = existing.GetComponent<Image>();
                _fxRect = existing as RectTransform;
                if (fxImage != null && edgeSprite != null)
                {
                    fxImage.sprite = edgeSprite;
                }

                if (fxImage != null)
                {
                    fxImage.raycastTarget = false;
                }

                CaptureBaseFromScene();
                ApplyVisual(0f, 0f);
                return;
            }

            var go = new GameObject("TailEdgeFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            fxImage = go.GetComponent<Image>();
            fxImage.sprite = edgeSprite;
            fxImage.color = new Color(tint.r, tint.g, tint.b, 0f);
            fxImage.raycastTarget = false;
            fxImage.preserveAspect = true;
            fxImage.type = Image.Type.Simple;
            fxImage.maskable = true;

            _fxRect = go.GetComponent<RectTransform>();
            _fxRect.anchorMin = Vector2.zero;
            _fxRect.anchorMax = Vector2.one;
            _fxRect.offsetMin = Vector2.zero;
            _fxRect.offsetMax = Vector2.zero;
            _fxRect.pivot = new Vector2(0.5f, 0.5f);
            go.transform.SetAsLastSibling();
            CaptureBaseFromScene();
            ApplyVisual(0f, 0f);
        }

        private void CaptureBaseFromScene()
        {
            if (_fxRect == null && fxImage != null)
            {
                _fxRect = fxImage.rectTransform;
            }

            if (_fxRect == null)
            {
                return;
            }

            _basePos = _fxRect.anchoredPosition;
            _baseCaptured = true;
        }

        private void OnEnable()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _hovered = false;
            CaptureBaseFromScene();
            ApplyVisual(0f, 0f);
        }

        private void Update()
        {
            if (fxImage == null)
            {
                return;
            }

            if (!_baseCaptured)
            {
                CaptureBaseFromScene();
            }

            if (!_hovered)
            {
                ApplyVisual(0f, 0f);
                return;
            }

            _phase += Time.unscaledDeltaTime;
            var pulse = (Mathf.Sin(_phase * pulseSpeed) + 1f) * 0.5f;
            var alpha = Mathf.Lerp(hoverMinAlpha, hoverMaxAlpha, pulse);
            var chase = Mathf.Sin(_phase * chaseSpeed) * chaseAmplitude;
            ApplyVisual(alpha, chase);
        }

        private void ApplyVisual(float alpha, float chaseX)
        {
            if (_fxRect != null)
            {
                _fxRect.anchoredPosition = _basePos + new Vector2(chaseX, 0f);
            }

            if (fxImage != null)
            {
                var c = tint;
                c.a = alpha;
                fxImage.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }
    }
}
