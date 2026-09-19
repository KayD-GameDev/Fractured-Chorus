using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FracturedChorus.VFX
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(ButterflyBezierFlight))]
    [RequireComponent(typeof(ButterflyWingAnimator))]
    [RequireComponent(typeof(ButterflyTrailController))]
    public sealed class ButterflyTransitionController : MonoBehaviour
    {
        [Header("Sequence")]
        [SerializeField] private float duration = 4.2f;
        [SerializeField] private bool holdAtEnd = true;
        [SerializeField] private float hoverAuraEmission = 0.45f;
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private bool playOnEnable = true;

        [Header("Butterfly")]
        [SerializeField] private Image butterflySprite;
        [SerializeField] private Image butterflyGlow;
        [SerializeField] private RectTransform butterflyRoot;
        [SerializeField] private RectTransform trailOrigin;
        [SerializeField] private Sprite[] wingFrames;
        [SerializeField] private float wingAnimationSpeed = 8f;
        [SerializeField] private AnimationCurve scaleCurve = DefaultScaleCurve();
        [SerializeField] private float rotationLimit = 20f;

        [Header("Flight")]
        [SerializeField] private Vector2 startPoint = ButterflyBezierFlight.DefaultStartPoint;
        [SerializeField] private Vector2 controlPointA = ButterflyBezierFlight.DefaultControlA;
        [SerializeField] private Vector2 controlPointB = ButterflyBezierFlight.DefaultControlB;
        [SerializeField] private Vector2 endPoint = ButterflyBezierFlight.DefaultEndPoint;
        [SerializeField] private AnimationCurve speedCurve = ButterflyBezierFlight.DefaultSpeedCurve();

        [Header("Trail")]
        [SerializeField] private float starDustEmission = 42f;
        [SerializeField] private float fragmentEmission = 10f;
        [SerializeField] private float musicFragmentEmission = 5f;
        [SerializeField] private float trailLifetime = 2.1f;
        [SerializeField] private AnimationCurve particleEmissionCurve = DefaultEmissionCurve();

        [Header("Visual")]
        [SerializeField] private float butterflyBrightness = 1f;
        [SerializeField] private float trailBrightness = 1f;
        [SerializeField] private float bloomIntensity = 0.55f;
        [SerializeField] private AnimationCurve glowCurve = DefaultGlowCurve();
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration;
        [SerializeField] private float fadeOutDuration = 1.0f;

        [Header("Events")]
        [SerializeField] private UnityEvent onTransitionStarted = new UnityEvent();
        [SerializeField] private UnityEvent onButterflyExit = new UnityEvent();
        [SerializeField] private UnityEvent onTransitionFinished = new UnityEvent();

        private ButterflyBezierFlight _flight;
        private ButterflyWingAnimator _wings;
        private ButterflyTrailController _trail;
        private RectTransform _canvasRect;
        private bool _playing;
        private bool _exitRaised;
        private bool _finishedRaised;
        private bool _skipTick;
        private float _elapsed;
        private Vector3 _rootBaseScale = Vector3.one;

        public void SetFlightPoints(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end)
        {
            startPoint = start;
            controlPointA = controlA;
            controlPointB = controlB;
            endPoint = end;
            var flight = _flight != null ? _flight : GetComponent<ButterflyBezierFlight>();
            flight?.SetNormalizedPoints(startPoint, controlPointA, controlPointB, endPoint);
        }

        public Vector2 StartPoint => startPoint;
        public Vector2 ControlPointA => controlPointA;
        public Vector2 ControlPointB => controlPointB;
        public Vector2 EndPoint => endPoint;
        public UnityEvent OnTransitionStarted => onTransitionStarted;
        public UnityEvent OnButterflyExit => onButterflyExit;
        public UnityEvent OnTransitionFinished => onTransitionFinished;
        public bool IsPlaying => _playing;
        public CanvasGroup RootGroup => rootGroup;

        public void ApplyPrologueBackgroundMode()
        {
            autoPlay = false;
            playOnEnable = false;
            holdAtEnd = true;
            fadeInDuration = 0f;
            fadeOutDuration = 0f;
            starDustEmission = 0f;
            fragmentEmission = 28f;
            hoverAuraEmission = 0.85f;
            Cache();
            _trail?.SetFieldAmbientEnabled(false);
        }

        public void SetPresentationAlpha(float alpha)
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void Awake()
        {
            Cache();
        }

        private void OnEnable()
        {
            if (Application.isPlaying && playOnEnable)
            {
                Play();
            }
        }

        private void Start()
        {
            if (Application.isPlaying && autoPlay && !_playing)
            {
                Play();
            }
        }

        private void LateUpdate()
        {
            if (!_playing)
            {
                return;
            }

            if (_skipTick)
            {
                _skipTick = false;
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, 0.033f));
        }

        public void Play()
        {
            try
            {
                Cache();
                ResetTransition();
                _playing = true;
                _elapsed = 0f;
                _exitRaised = false;
                _finishedRaised = false;
                _skipTick = true;
                ApplyCurvesToChildren();
                _wings.Play();
                _trail.Play();
                if (butterflyRoot != null)
                {
                    butterflyRoot.gameObject.SetActive(true);
                    _rootBaseScale = Vector3.one;
                }

                SetFade(0f);
                Tick(0f);
                onTransitionStarted?.Invoke();
            }
            catch (Exception error)
            {
                Debug.LogError("Failed to play butterfly transition: " + error);
                _playing = false;
            }
        }

        public void Stop()
        {
            _playing = false;
            _wings?.Stop();
            _trail?.StopEmitting();
        }

        public void ResetTransition()
        {
            Cache();
            _playing = false;
            _elapsed = 0f;
            _exitRaised = false;
            _finishedRaised = false;
            _wings?.Stop();
            _trail?.StopImmediate();
            if (butterflyRoot != null)
            {
                butterflyRoot.gameObject.SetActive(true);
            }

            if (butterflyGlow != null)
            {
                var glow = butterflyGlow.color;
                glow.a = 0f;
                butterflyGlow.color = glow;
            }

            SetFade(1f);
            if (rootGroup != null)
            {
                rootGroup.alpha = 1f;
            }
        }

        public void BindSprites(Sprite[] wings, Image sprite, Image glow)
        {
            if (wings != null && wings.Length > 0)
            {
                wingFrames = wings;
            }

            if (sprite != null)
            {
                butterflySprite = sprite;
            }

            if (glow != null)
            {
                butterflyGlow = glow;
            }
        }

        private void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            var length = Mathf.Max(0.1f, duration);
            var u = Mathf.Clamp01(_elapsed / length);
            var hovering = holdAtEnd && _elapsed >= length;
            _flight.Apply(u, _elapsed, hovering ? 0.28f : 1f);
            var scale = hovering
                ? 1f
                : (scaleCurve != null && scaleCurve.length > 0 ? scaleCurve.Evaluate(u) : 1f);
            ApplyCameraDrift(u, scale);
            _wings.Tick(deltaTime, 1f);

            var emission = hovering
                ? Mathf.Max(0.2f, hoverAuraEmission)
                : (particleEmissionCurve != null && particleEmissionCurve.length > 0
                    ? Mathf.Max(0f, particleEmissionCurve.Evaluate(u))
                    : 1f);
            var trailFade = hovering || u < 0.92f ? 1f : 1f - Mathf.InverseLerp(0.92f, 1f, u);
            if (!holdAtEnd && u >= 0.78f)
            {
                _trail.StopEmitting();
            }

            var origin = Vector2.zero;
            if (_canvasRect != null)
            {
                if (trailOrigin != null)
                {
                    origin = (Vector2)_canvasRect.InverseTransformPoint(trailOrigin.position);
                }
                else if (butterflyRoot != null)
                {
                    origin = (Vector2)_canvasRect.InverseTransformPoint(butterflyRoot.position);
                }
            }
            else if (butterflyRoot != null)
            {
                origin = butterflyRoot.anchoredPosition;
            }

            if (hovering)
            {
                origin += new Vector2(Mathf.Cos(_elapsed * 2.1f), Mathf.Sin(_elapsed * 1.65f)) * 6f;
            }

            _trail.Tick(deltaTime, origin, emission, trailFade * trailBrightness, hovering);
            ApplyGlow(u, hovering);
            ApplyFade(u, hovering);

            if (!_exitRaised && u >= 1f)
            {
                _exitRaised = true;
                onButterflyExit?.Invoke();
            }

            if (!holdAtEnd && _elapsed >= length && !_finishedRaised)
            {
                _finishedRaised = true;
                _playing = false;
                _wings.Stop();
                _trail.StopEmitting();
                onTransitionFinished?.Invoke();
            }
        }

        private void ApplyGlow(float u, bool hovering)
        {
            if (butterflyGlow == null)
            {
                return;
            }

            var glow = hovering
                ? 0.88f + 0.12f * Mathf.Sin(_elapsed * 2.4f)
                : (glowCurve != null && glowCurve.length > 0 ? glowCurve.Evaluate(u) : 1f);
            var color = butterflyGlow.color;
            color.a = Mathf.Clamp01(glow * bloomIntensity * (hovering ? 0.9f : 0.55f));
            butterflyGlow.color = color;
            if (butterflySprite != null)
            {
                var spriteColor = Color.white;
                spriteColor.a = Mathf.Clamp01(0.75f + butterflyBrightness * 0.25f);
                butterflySprite.color = spriteColor;
            }
        }

        private void ApplyFade(float u, bool hovering)
        {
            if (hovering)
            {
                SetFade(0f);
                return;
            }

            var fadeIn = fadeInDuration / Mathf.Max(0.1f, duration);
            var fadeOutStart = 1f - fadeOutDuration / Mathf.Max(0.1f, duration);
            var alpha = 0f;
            if (u < fadeIn)
            {
                alpha = 1f - Mathf.Clamp01(u / Mathf.Max(0.0001f, fadeIn));
            }
            else if (!holdAtEnd && u > fadeOutStart)
            {
                alpha = Mathf.InverseLerp(fadeOutStart, 1f, u);
            }

            SetFade(alpha);
        }

        private void ApplyCameraDrift(float u, float scale)
        {
            if (butterflyRoot == null)
            {
                return;
            }

            var zoom = 1f + 0.015f * u;
            butterflyRoot.localScale = _rootBaseScale * scale * zoom;
        }

        private void SetFade(float alpha)
        {
            if (fadeOverlay == null)
            {
                return;
            }

            fadeOverlay.alpha = Mathf.Clamp01(alpha);
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }

        private void Cache()
        {
            if (_flight == null)
            {
                _flight = GetComponent<ButterflyBezierFlight>();
            }

            if (_wings == null)
            {
                _wings = GetComponent<ButterflyWingAnimator>();
            }

            if (_trail == null)
            {
                _trail = GetComponent<ButterflyTrailController>();
            }

            if (_canvasRect == null)
            {
                _canvasRect = transform as RectTransform;
            }

            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            ResolveSceneRefs();
            _flight.Bind(_canvasRect, butterflyRoot);
            _flight.BindTuning(speedCurve, rotationLimit);
            _flight.SetNormalizedPoints(startPoint, controlPointA, controlPointB, endPoint);
            _wings.Bind(butterflySprite, wingFrames);
            _wings.SetSpeed(wingAnimationSpeed);
            _trail.ResolveFrom(transform);
            _trail.SetRates(starDustEmission, fragmentEmission, musicFragmentEmission, trailLifetime, trailBrightness);
        }

        private void ResolveSceneRefs()
        {
            if (butterflyRoot == null)
            {
                butterflyRoot = transform.Find(ButterflyTransitionHierarchy.ButterflyRootName) as RectTransform;
            }

            if (butterflySprite == null)
            {
                butterflySprite = transform.Find(
                    ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.ButterflySpriteName)
                    ?.GetComponent<Image>();
            }

            if (butterflyGlow == null)
            {
                butterflyGlow = transform.Find(
                    ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.ButterflyGlowName)
                    ?.GetComponent<Image>();
            }

            if (trailOrigin == null)
            {
                trailOrigin = transform.Find(
                    ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.TrailOriginName)
                    as RectTransform;
            }

            if (fadeOverlay == null)
            {
                fadeOverlay = transform.Find(ButterflyTransitionHierarchy.FadeOverlayName)?.GetComponent<CanvasGroup>();
            }
        }

        private void ApplyCurvesToChildren()
        {
            Cache();
        }

        public static AnimationCurve DefaultScaleCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.7f, 0f, 0.8f),
                new Keyframe(0.38f, 1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
        }

        public static AnimationCurve DefaultGlowCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.08f, 0.25f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 1f));
        }

        public static AnimationCurve DefaultEmissionCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.7f),
                new Keyframe(0.18f, 1f),
                new Keyframe(0.72f, 0.75f),
                new Keyframe(1f, 0.45f));
        }

        private void Reset()
        {
            duration = 4.2f;
            holdAtEnd = true;
            hoverAuraEmission = 0.45f;
            scaleCurve = DefaultScaleCurve();
            speedCurve = ButterflyBezierFlight.DefaultSpeedCurve();
            glowCurve = DefaultGlowCurve();
            particleEmissionCurve = DefaultEmissionCurve();
            startPoint = ButterflyBezierFlight.DefaultStartPoint;
            controlPointA = ButterflyBezierFlight.DefaultControlA;
            controlPointB = ButterflyBezierFlight.DefaultControlB;
            endPoint = ButterflyBezierFlight.DefaultEndPoint;
        }

        private void OnValidate()
        {
            duration = Mathf.Max(1f, duration);
            rotationLimit = Mathf.Clamp(rotationLimit, 0f, 45f);
            wingAnimationSpeed = Mathf.Clamp(wingAnimationSpeed, 4f, 12f);
            var flight = GetComponent<ButterflyBezierFlight>();
            if (flight != null)
            {
                flight.SetNormalizedPoints(startPoint, controlPointA, controlPointB, endPoint);
                flight.BindTuning(speedCurve, rotationLimit);
            }
        }
    }
}
