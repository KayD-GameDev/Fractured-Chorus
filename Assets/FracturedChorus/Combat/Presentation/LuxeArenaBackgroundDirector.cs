using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class LuxeArenaBackgroundDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class SpotlightBinding
        {
            public RectTransform Transform;
            public Image Image;
            public float BaseAngle;
        }

        [SerializeField] private LuxeArenaBackgroundConfig config;
        [SerializeField] private Image baseImage;
        [SerializeField] private Image floorImage;
        [SerializeField] private Image grandstandImage;
        [SerializeField] private RectTransform layersRoot;
        [SerializeField] private RawImage audiencePrimary;
        [SerializeField] private RawImage audienceSecondary;
        [SerializeField] private SpotlightBinding[] spotlights;
        [SerializeField] private Texture2D[] audienceWaveFrames;

        private float _framePhase;
        private float _pulsePhase;
        private float _swayPhase;
        private float _audiencePulsePhase;
        private Rect _primaryBaseUv;
        private Rect _secondaryBaseUv;
        private bool _uvCached;

        public LuxeArenaBackgroundConfig Config => config;

        public void ApplySceneWiring(
            LuxeArenaBackgroundConfig cfg,
            Image baseImg,
            Image floor,
            Image grandstand,
            RectTransform layers,
            RawImage primary,
            RawImage secondary,
            SpotlightBinding[] spots,
            Texture2D[] frames)
        {
            config = cfg;
            baseImage = baseImg;
            floorImage = floor;
            grandstandImage = grandstand;
            layersRoot = layers;
            audiencePrimary = primary;
            audienceSecondary = secondary;
            spotlights = spots;
            audienceWaveFrames = frames;
            _uvCached = false;
            CacheUvRects();
            SnapAudienceTextures();
        }

        private void OnEnable()
        {
            _uvCached = false;
            CacheUvRects();
            SnapAudienceTextures();
        }

        private void Update()
        {
            TickAudience();
            TickSpotlights();
        }

        private Texture2D[] ResolveFrames()
        {
            if (config != null && config.AudienceFrames != null && config.AudienceFrames.Length > 0)
            {
                return config.AudienceFrames;
            }

            return audienceWaveFrames;
        }

        private void TickAudience()
        {
            var frames = ResolveFrames();
            if (frames == null || frames.Length == 0 || audiencePrimary == null)
            {
                return;
            }

            CacheUvRects();

            var morphSpeed = config != null ? config.FrameMorphSpeed : 1.1f;
            var audAlpha = config != null ? config.AudienceAlpha : 0.7f;
            var shimmer = config != null ? config.ShimmerAmount : 0.006f;
            var pulseAmount = config != null ? config.PulseAmount : 0.12f;
            var pulseSpeed = config != null ? config.PulseSpeed : 0.4f;

            _framePhase += Time.unscaledDeltaTime * morphSpeed;
            _audiencePulsePhase += Time.unscaledDeltaTime * pulseSpeed * Mathf.PI * 2f;

            var frameCount = frames.Length;
            var frameFloat = Mathf.Repeat(_framePhase, frameCount);
            var indexA = Mathf.FloorToInt(frameFloat) % frameCount;
            var indexB = (indexA + 1) % frameCount;
            var frameBlend = Smooth01(frameFloat - indexA);

            audiencePrimary.texture = frames[indexA];
            if (audienceSecondary != null)
            {
                audienceSecondary.texture = frames[indexB];
            }

            var pulse = 1f - pulseAmount + pulseAmount * (0.5f + 0.5f * Mathf.Sin(_audiencePulsePhase));
            var alpha = audAlpha * pulse;

            SetRawAlpha(audiencePrimary, alpha * (1f - frameBlend));
            SetRawAlpha(audienceSecondary, alpha * frameBlend);

            ApplyShimmer(audiencePrimary, _primaryBaseUv, shimmer, 0f);
            ApplyShimmer(audienceSecondary, _secondaryBaseUv, shimmer, 0.37f);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void ApplyShimmer(RawImage image, Rect baseUv, float shimmerAmount, float phaseOffset)
        {
            if (image == null || shimmerAmount <= 0f)
            {
                return;
            }

            var uv = baseUv;
            var bob = Mathf.Sin((_framePhase + phaseOffset) * Mathf.PI * 2f) * shimmerAmount;
            uv.y = Mathf.Clamp01(uv.y + bob);
            image.uvRect = uv;
        }

        private void CacheUvRects()
        {
            if (_uvCached)
            {
                return;
            }

            _primaryBaseUv = audiencePrimary != null ? audiencePrimary.uvRect : new Rect(0f, 0f, 1f, 1f);
            _secondaryBaseUv = audienceSecondary != null ? audienceSecondary.uvRect : _primaryBaseUv;
            _uvCached = true;
        }

        private void SnapAudienceTextures()
        {
            var frames = ResolveFrames();
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (audiencePrimary != null)
            {
                audiencePrimary.texture = frames[0];
            }

            if (audienceSecondary != null)
            {
                audienceSecondary.texture = frames.Length > 1 ? frames[1] : frames[0];
            }
        }

        private void TickSpotlights()
        {
            var enabled = config == null || config.EnableSpotlightRig;
            if (!enabled || spotlights == null || spotlights.Length == 0)
            {
                return;
            }

            var maxAlpha = config != null ? config.SpotlightMaxAlpha : 0.28f;
            var pulseSpeed = config != null ? config.SpotlightPulseSpeed : 0.35f;
            var swayDegrees = config != null ? config.SpotlightSwayDegrees : 4.5f;
            var swaySpeed = config != null ? config.SpotlightSwaySpeed : 0.22f;

            _pulsePhase += Time.unscaledDeltaTime * pulseSpeed * Mathf.PI * 2f;
            _swayPhase += Time.unscaledDeltaTime * swaySpeed * Mathf.PI * 2f;

            for (var i = 0; i < spotlights.Length; i++)
            {
                var spot = spotlights[i];
                if (spot == null)
                {
                    continue;
                }

                if (spot.Image != null)
                {
                    var pulse = 0.62f + 0.38f * (0.5f + 0.5f * Mathf.Sin(_pulsePhase + i * 0.7f));
                    var c = spot.Image.color;
                    c.a = maxAlpha * pulse;
                    spot.Image.color = c;
                }

                if (spot.Transform != null)
                {
                    var sway = Mathf.Sin(_swayPhase + i * 0.9f) * swayDegrees;
                    spot.Transform.localRotation = Quaternion.Euler(0f, 0f, spot.BaseAngle + sway);
                }
            }
        }

        private static void SetRawAlpha(RawImage image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
