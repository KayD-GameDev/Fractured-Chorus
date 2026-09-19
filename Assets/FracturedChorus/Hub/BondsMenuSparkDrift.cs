using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondsMenuSparkDrift : MonoBehaviour
    {
        [SerializeField] private Image[] sparkImages;

        private struct SparkMotion
        {
            public Image Image;
            public Vector2 BasePosition;
            public Color BaseColor;
            public float RiseSpeed;
            public float SwayAmplitude;
            public float SwayHz;
            public float TwinkleHz;
            public float Phase;
        }

        private SparkMotion[] _motions;
        private float _loopHeight;

        private void Awake()
        {
            WireReferences();
            CacheMotions();
        }

        private void OnEnable()
        {
            CacheMotions();
        }

        private void WireReferences()
        {
            if (sparkImages != null && sparkImages.Length > 0)
            {
                return;
            }

            var images = GetComponentsInChildren<Image>(true);
            if (images.Length == 0)
            {
                return;
            }

            sparkImages = images;
        }

        private void CacheMotions()
        {
            WireReferences();
            if (sparkImages == null || sparkImages.Length == 0)
            {
                _motions = null;
                return;
            }

            var parent = transform as RectTransform;
            _loopHeight = parent != null ? Mathf.Max(parent.rect.height, 720f) : 900f;

            _motions = new SparkMotion[sparkImages.Length];
            for (var i = 0; i < sparkImages.Length; i++)
            {
                var image = sparkImages[i];
                if (image == null)
                {
                    continue;
                }

                var rect = image.rectTransform;
                var seed = i * 1.618f + 0.37f;
                _motions[i] = new SparkMotion
                {
                    Image = image,
                    BasePosition = rect.anchoredPosition,
                    BaseColor = image.color,
                    RiseSpeed = 18f + seed * 9f,
                    SwayAmplitude = 6f + seed * 4f,
                    SwayHz = 0.08f + seed * 0.03f,
                    TwinkleHz = 0.35f + seed * 0.12f,
                    Phase = seed * 2.4f
                };
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || _motions == null)
            {
                return;
            }

            var t = Time.unscaledTime;
            for (var i = 0; i < _motions.Length; i++)
            {
                ref var motion = ref _motions[i];
                var image = motion.Image;
                if (image == null)
                {
                    continue;
                }

                var rect = image.rectTransform;
                var rise = Mathf.Repeat(t * motion.RiseSpeed + motion.Phase * 48f, _loopHeight);
                var sway = Mathf.Sin(t * motion.SwayHz * Mathf.PI * 2f + motion.Phase) * motion.SwayAmplitude;
                rect.anchoredPosition = new Vector2(motion.BasePosition.x + sway, motion.BasePosition.y + rise);

                var twinkle = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * motion.TwinkleHz * Mathf.PI * 2f + motion.Phase));
                var color = motion.BaseColor;
                color.a = motion.BaseColor.a * twinkle;
                image.color = color;
            }
        }
    }
}
