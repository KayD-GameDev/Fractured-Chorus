using UnityEngine;

namespace FracturedChorus.Menu
{
    public sealed class TitleHudRingMotion : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = -12f;
        [SerializeField] private float pulseAmplitude = 0.02f;
        [SerializeField] private float pulseHz = 0.28f;
        [SerializeField] private float glitchEverySeconds = 3.6f;
        [SerializeField] private float glitchDegrees = 5.5f;
        [SerializeField] private float glitchSeconds = 0.07f;

        private float _pulsePhase;
        private float _glitchHold;
        private float _nextGlitch;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _nextGlitch = glitchEverySeconds;
        }

        private void OnEnable()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            var extraZ = 0f;
            _nextGlitch -= dt;
            if (_nextGlitch <= 0f)
            {
                _glitchHold = glitchSeconds;
                _nextGlitch = glitchEverySeconds + Random.Range(-0.7f, 0.7f);
            }

            if (_glitchHold > 0f)
            {
                _glitchHold -= dt;
                extraZ = glitchDegrees * Mathf.Sign(degreesPerSecond);
            }

            transform.Rotate(0f, 0f, (degreesPerSecond * dt) + extraZ * dt * 18f, Space.Self);

            _pulsePhase += dt * pulseHz * (Mathf.PI * 2f);
            var pulse = 1f + Mathf.Sin(_pulsePhase) * pulseAmplitude;
            transform.localScale = _baseScale * pulse;
        }
    }
}
