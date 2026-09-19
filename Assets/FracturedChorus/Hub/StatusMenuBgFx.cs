using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class StatusMenuBgFx : MonoBehaviour
    {
        [SerializeField] private Image sunRaysImage;
        [SerializeField] private RectTransform cloudNear;
        [SerializeField] private RectTransform cloudFar;
        [SerializeField] private float cloudNearDriftSpeed = 8f;
        [SerializeField] private float cloudFarDriftSpeed = 4f;
        [SerializeField] private float sunPulseAmplitude = 0.07f;
        [SerializeField] private float sunPulseHz = 0.22f;

        private Vector2 _nearBase;
        private Vector2 _farBase;
        private Color _sunBaseColor;
        private float _nearLoopWidth;
        private float _farLoopWidth;

        private void Awake()
        {
            WireReferences();
            CacheBases();
        }

        private void OnEnable()
        {
            CacheBases();
        }

        public void WireReferences()
        {
            if (sunRaysImage == null)
            {
                sunRaysImage = transform.Find("MenuBgSunRays")?.GetComponent<Image>();
            }

            if (cloudNear == null)
            {
                cloudNear = transform.Find("MenuBgCloudsNear") as RectTransform;
            }

            if (cloudFar == null)
            {
                cloudFar = transform.Find("MenuBgCloudsFar") as RectTransform;
            }
        }

        private void CacheBases()
        {
            if (cloudNear != null)
            {
                _nearBase = cloudNear.anchoredPosition;
                _nearLoopWidth = LoopWidth(cloudNear);
            }

            if (cloudFar != null)
            {
                _farBase = cloudFar.anchoredPosition;
                _farLoopWidth = LoopWidth(cloudFar);
            }

            if (sunRaysImage != null)
            {
                _sunBaseColor = sunRaysImage.color;
            }
        }

        private static float LoopWidth(RectTransform cloud)
        {
            var parent = cloud.parent as RectTransform;
            var span = parent != null ? parent.rect.width : cloud.rect.width;
            return Mathf.Max(span, 256f);
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var t = Time.unscaledTime;

            if (sunRaysImage != null)
            {
                var c = _sunBaseColor;
                c.a = Mathf.Clamp01(
                    _sunBaseColor.a + Mathf.Sin(t * Mathf.PI * 2f * sunPulseHz) * sunPulseAmplitude);
                sunRaysImage.color = c;
            }

            if (cloudNear != null && cloudNear.gameObject.activeInHierarchy)
            {
                var x = _nearBase.x - Mathf.Repeat(t * cloudNearDriftSpeed, _nearLoopWidth);
                cloudNear.anchoredPosition = new Vector2(x, _nearBase.y);
            }

            if (cloudFar != null && cloudFar.gameObject.activeInHierarchy)
            {
                var x = _farBase.x + Mathf.Repeat(t * cloudFarDriftSpeed, _farLoopWidth);
                cloudFar.anchoredPosition = new Vector2(x, _farBase.y);
            }
        }
    }
}
