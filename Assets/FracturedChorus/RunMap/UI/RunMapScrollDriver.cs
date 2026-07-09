using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    /// <summary>Tuning ScrollRect + smooth programmatic scroll (node follow, initial F1).</summary>
    [RequireComponent(typeof(ScrollRect))]
    public class RunMapScrollDriver : MonoBehaviour, IBeginDragHandler, IScrollHandler
    {
        [SerializeField] private ScrollRect scrollRect;

        [Header("Drag / wheel feel")]
        [SerializeField] [Range(0.1f, 1f)] private float scrollSpeedScale = 1f;
        [SerializeField] private float scrollSensitivity = 58f;
        [SerializeField] private float wheelScrollMultiplier = 0.17f;
        [SerializeField] private float decelerationRate = 0.035f;
        [SerializeField] private float elasticity = 0.04f;

        [Header("Animated scroll")]
        [SerializeField] private float smoothTime = 0.36f;
        [SerializeField] private float initialScrollSmoothTime = 0.28f;

        private Coroutine _scrollCoroutine;
        private float _smoothVelocity;

        public ScrollRect ScrollRect => scrollRect;

        private void Awake()
        {
            scrollRect ??= GetComponent<ScrollRect>();
            ApplyScrollFeel();
        }

        public void ApplyScrollFeel()
        {
            scrollRect ??= GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = scrollSensitivity * scrollSpeedScale * wheelScrollMultiplier;
            scrollRect.decelerationRate = decelerationRate;
            scrollRect.elasticity = elasticity;
        }

        public void ApplyWheelScroll(PointerEventData eventData)
        {
            if (scrollRect == null || eventData == null)
            {
                return;
            }

            StopScrollAnimation();
            scrollRect.OnScroll(eventData);
        }

        public void ScrollToNormalized(float target, bool immediate = false, bool useInitialTiming = false)
        {
            if (scrollRect == null)
            {
                return;
            }

            target = Mathf.Clamp01(target);

            if (immediate || !isActiveAndEnabled)
            {
                StopScrollAnimation();
                scrollRect.velocity = Vector2.zero;
                scrollRect.verticalNormalizedPosition = target;
                return;
            }

            StopScrollAnimation();
            var duration = (useInitialTiming ? initialScrollSmoothTime : smoothTime) / Mathf.Max(scrollSpeedScale, 0.05f);
            _scrollCoroutine = StartCoroutine(AnimateScrollTo(target, duration));
        }

        public void StopScrollAnimation()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
            }

            _smoothVelocity = 0f;
        }

        private IEnumerator AnimateScrollTo(float target, float animSmoothTime)
        {
            scrollRect.velocity = Vector2.zero;

            while (true)
            {
                var current = scrollRect.verticalNormalizedPosition;
                if (Mathf.Abs(current - target) <= 0.0005f)
                {
                    break;
                }

                current = Mathf.SmoothDamp(
                    current,
                    target,
                    ref _smoothVelocity,
                    animSmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

                scrollRect.verticalNormalizedPosition = current;
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = target;
            scrollRect.velocity = Vector2.zero;
            _scrollCoroutine = null;
        }

        public void OnBeginDrag(PointerEventData eventData) => StopScrollAnimation();

        public void OnScroll(PointerEventData eventData) => ApplyWheelScroll(eventData);
    }
}
