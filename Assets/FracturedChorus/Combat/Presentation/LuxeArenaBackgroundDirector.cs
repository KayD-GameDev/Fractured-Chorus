using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class LuxeArenaBackgroundDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class AudiencePanel
        {
            public RawImage Primary;
            public RawImage Secondary;
            [Range(0f, 1f)] public float WaveAnchorX = 0.5f;
        }

        [Serializable]
        public sealed class SpotlightBinding
        {
            public RectTransform Transform;
            public Image Image;
            public float BaseAngle;
        }

        [SerializeField] private Image baseImage;
        [SerializeField] private RectTransform layersRoot;
        [SerializeField] private AudiencePanel[] audiencePanels;
        [SerializeField] private SpotlightBinding[] spotlights;
        [SerializeField] private Texture2D[] audienceWaveFrames;
        [SerializeField] [Range(0.1f, 4f)] private float waveCyclesPerSecond = 0.35f;
        [SerializeField] [Range(0.5f, 4f)] private float waveCrestWidth = 1.35f;
        [SerializeField] [Range(0f, 1f)] private float waveTroughAlpha = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float wavePeakAlpha = 0.85f;
        [SerializeField] [Range(0.1f, 1f)] private float audienceAlpha = 0.55f;
        [SerializeField] [Range(0.2f, 8f)] private float frameMorphSpeed = 1.1f;
        [SerializeField] [Range(0f, 0.03f)] private float shimmerAmount = 0.008f;
        [SerializeField] private bool enableSpotlightRig = true;
        [SerializeField] [Range(0.05f, 0.6f)] private float spotlightMaxAlpha = 0.28f;
        [SerializeField] [Range(0.05f, 2f)] private float spotlightPulseSpeed = 0.35f;
        [SerializeField] [Range(0f, 12f)] private float spotlightSwayDegrees = 4.5f;
        [SerializeField] [Range(0.05f, 1f)] private float spotlightSwaySpeed = 0.22f;

        private float _wavePhase;
        private float _framePhase;
        private float _pulsePhase;
        private float _swayPhase;
        private Rect[] _primaryBaseUv;
        private Rect[] _secondaryBaseUv;
        private bool _uvCached;

        public void ApplySceneWiring(
            Image baseImg,
            RectTransform layers,
            AudiencePanel[] panels,
            SpotlightBinding[] spots,
            Texture2D[] frames)
        {
            baseImage = baseImg;
            layersRoot = layers;
            audiencePanels = panels;
            spotlights = spots;
            audienceWaveFrames = frames;
            _uvCached = false;
            CacheUvRects();
            EnsurePanelAnchors();
            SnapAudienceTextures();
        }

        private void OnEnable()
        {
            _uvCached = false;
            CacheUvRects();
            EnsurePanelAnchors();
            SnapAudienceTextures();
        }

        private void Update()
        {
            TickAudienceWave();
            TickSpotlights();
        }

        private void TickAudienceWave()
        {
            if (audienceWaveFrames == null || audienceWaveFrames.Length == 0 || audiencePanels == null ||
                audiencePanels.Length == 0)
            {
                return;
            }

            CacheUvRects();
            EnsurePanelAnchors();

            _wavePhase = Mathf.Repeat(_wavePhase + Time.unscaledDeltaTime * waveCyclesPerSecond, 1f);
            _framePhase += Time.unscaledDeltaTime * frameMorphSpeed;

            var frameCount = audienceWaveFrames.Length;
            var frameFloat = Mathf.Repeat(_framePhase, frameCount);
            var indexA = Mathf.FloorToInt(frameFloat) % frameCount;
            var indexB = (indexA + 1) % frameCount;
            var frameBlend = Smooth01(frameFloat - indexA);

            var texA = audienceWaveFrames[indexA];
            var texB = audienceWaveFrames[indexB];

            for (var i = 0; i < audiencePanels.Length; i++)
            {
                var panel = audiencePanels[i];
                if (panel == null)
                {
                    continue;
                }

                if (panel.Primary != null)
                {
                    panel.Primary.texture = texA;
                }

                if (panel.Secondary != null)
                {
                    panel.Secondary.texture = texB;
                }

                var x = panel.WaveAnchorX;
                var crest = TravelingCrest(x, _wavePhase, waveCrestWidth);
                var localAlpha = Mathf.Lerp(waveTroughAlpha, wavePeakAlpha, crest) * audienceAlpha;

                SetRawAlpha(panel.Primary, localAlpha * (1f - frameBlend));
                SetRawAlpha(panel.Secondary, localAlpha * frameBlend);

                ApplyShimmer(panel.Primary, i, _primaryBaseUv);
                ApplyShimmer(panel.Secondary, i, _secondaryBaseUv);
            }
        }

        private static float TravelingCrest(float x, float phase, float width)
        {
            var delta = Mathf.Abs(Mathf.Repeat(x - phase + 0.5f, 1f) - 0.5f) * 2f;
            var soft = Mathf.Clamp01(1f - delta * width);
            return Smooth01(soft);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void ApplyShimmer(RawImage image, int panelIndex, Rect[] baseUv)
        {
            if (image == null || baseUv == null || panelIndex < 0 || panelIndex >= baseUv.Length ||
                shimmerAmount <= 0f)
            {
                return;
            }

            var uv = baseUv[panelIndex];
            var bob = Mathf.Sin((_wavePhase + panelIndex * 0.17f) * Mathf.PI * 2f) * shimmerAmount;
            uv.y = Mathf.Clamp01(uv.y + bob);
            image.uvRect = uv;
        }

        private void CacheUvRects()
        {
            if (_uvCached || audiencePanels == null)
            {
                return;
            }

            _primaryBaseUv = new Rect[audiencePanels.Length];
            _secondaryBaseUv = new Rect[audiencePanels.Length];
            for (var i = 0; i < audiencePanels.Length; i++)
            {
                var panel = audiencePanels[i];
                _primaryBaseUv[i] = panel?.Primary != null ? panel.Primary.uvRect : new Rect(0f, 0f, 1f, 1f);
                _secondaryBaseUv[i] = panel?.Secondary != null ? panel.Secondary.uvRect : _primaryBaseUv[i];
            }

            _uvCached = true;
        }

        private void EnsurePanelAnchors()
        {
            if (audiencePanels == null)
            {
                return;
            }

            for (var i = 0; i < audiencePanels.Length; i++)
            {
                var panel = audiencePanels[i];
                if (panel?.Primary == null)
                {
                    continue;
                }

                var uv = panel.Primary.uvRect;
                panel.WaveAnchorX = Mathf.Clamp01(uv.x + uv.width * 0.5f);
            }
        }

        private void SnapAudienceTextures()
        {
            if (audienceWaveFrames == null || audienceWaveFrames.Length == 0 || audiencePanels == null)
            {
                return;
            }

            var a = audienceWaveFrames[0];
            var b = audienceWaveFrames.Length > 1 ? audienceWaveFrames[1] : a;
            for (var i = 0; i < audiencePanels.Length; i++)
            {
                var panel = audiencePanels[i];
                if (panel == null)
                {
                    continue;
                }

                if (panel.Primary != null)
                {
                    panel.Primary.texture = a;
                }

                if (panel.Secondary != null)
                {
                    panel.Secondary.texture = b;
                }
            }
        }

        private void TickSpotlights()
        {
            if (!enableSpotlightRig || spotlights == null || spotlights.Length == 0)
            {
                return;
            }

            _pulsePhase += Time.unscaledDeltaTime * spotlightPulseSpeed * Mathf.PI * 2f;
            _swayPhase += Time.unscaledDeltaTime * spotlightSwaySpeed * Mathf.PI * 2f;

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
                    c.a = spotlightMaxAlpha * pulse;
                    spot.Image.color = c;
                }

                if (spot.Transform != null)
                {
                    var sway = Mathf.Sin(_swayPhase + i * 0.9f) * spotlightSwayDegrees;
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
