using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CombatImpactFeel : MonoBehaviour
    {
        public static CombatImpactFeel ActiveInstance { get; private set; }

        [Header("Camera shake")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float traumaDecayPerSecond = 1.35f;
        [SerializeField] private Vector2 maxShakeOffset = new(0.18f, 0.12f);
        [SerializeField] private float maxShakeRollDegrees = 1.4f;

        [Header("Hit-stop — Ultimate (punchy)")]
        [SerializeField] private float ultimateTrauma = 0.78f;
        [SerializeField] private float ultimateHitStopSeconds = 0.16f;
        [SerializeField] [Range(0.01f, 1f)] private float ultimateTimeScale = 0.08f;

        [Header("Hit-stop — Medium")]
        [SerializeField] private float mediumTrauma = 0.42f;
        [SerializeField] private float mediumHitStopSeconds = 0.055f;
        [SerializeField] [Range(0.01f, 1f)] private float mediumTimeScale = 0.12f;

        private float _trauma;
        private float _noiseTime;
        private Vector3 _lastShakeOffset;
        private float _lastShakeRoll;
        private Vector3 _shakeAppliedPos;
        private bool _shakeOnCamera;
        private float _savedTimeScale = 1f;
        private bool _hitStopActive;
        private Coroutine _hitStopRoutine;

        public static CombatImpactFeel Ensure(Transform host = null, Camera camera = null)
        {
            if (ActiveInstance != null)
            {
                if (camera != null)
                {
                    ActiveInstance.BindCamera(camera);
                }

                return ActiveInstance;
            }

            var existing = FindAnyObjectByType<CombatImpactFeel>();
            if (existing != null)
            {
                ActiveInstance = existing;
                if (camera != null)
                {
                    existing.BindCamera(camera);
                }

                return existing;
            }

            var parentGo = host != null
                ? host.gameObject
                : GameObject.Find("CombatRoot")
                  ?? FindAnyObjectByType<EncounterDirector>()?.gameObject;
            if (parentGo == null)
            {
                parentGo = new GameObject("CombatImpactFeel");
            }

            var feel = parentGo.GetComponent<CombatImpactFeel>() ?? parentGo.AddComponent<CombatImpactFeel>();
            ActiveInstance = feel;
            if (camera != null)
            {
                feel.BindCamera(camera);
            }

            return feel;
        }

        public static void PunchUltimateNow() => Ensure()?.PunchUltimate();

        public static void PunchMediumNow() => Ensure()?.PunchMedium();

        public void BindCamera(Camera camera)
        {
            if (camera != null)
            {
                targetCamera = camera;
            }
        }

        public void PunchUltimate()
        {
            AddTrauma(ultimateTrauma);
            StartHitStop(ultimateHitStopSeconds, ultimateTimeScale);
        }

        public void PunchMedium()
        {
            AddTrauma(mediumTrauma);
            StartHitStop(mediumHitStopSeconds, mediumTimeScale);
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        public void CancelAll()
        {
            if (_hitStopRoutine != null)
            {
                StopCoroutine(_hitStopRoutine);
                _hitStopRoutine = null;
            }

            RestoreTimeScale();
            ClearShakeFromCamera();
            _trauma = 0f;
        }

        private void OnEnable()
        {
            ActiveInstance = this;
        }

        private void OnDisable()
        {
            CancelAll();
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }

        private void LateUpdate()
        {
            var cam = ResolveCamera();
            if (cam == null)
            {
                return;
            }

            ClearShakeFromCamera();

            if (_trauma <= 0.001f)
            {
                _trauma = 0f;
                return;
            }

            var dt = Time.unscaledDeltaTime;
            _trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * dt);
            _noiseTime += dt * 30f;
            var shake = _trauma * _trauma;
            _lastShakeOffset = new Vector3(
                maxShakeOffset.x * shake * Mathf.Sin(_noiseTime * 1.7f),
                maxShakeOffset.y * shake * Mathf.Sin(_noiseTime * 2.3f),
                0f);
            _lastShakeRoll = maxShakeRollDegrees * shake * Mathf.Sin(_noiseTime * 1.1f);

            cam.transform.position += _lastShakeOffset;
            cam.transform.Rotate(0f, 0f, _lastShakeRoll);
            _shakeAppliedPos = cam.transform.position;
            _shakeOnCamera = true;
        }

        private void StartHitStop(float durationSeconds, float scale)
        {
            if (durationSeconds <= 0f)
            {
                return;
            }

            if (_hitStopRoutine != null)
            {
                StopCoroutine(_hitStopRoutine);
                RestoreTimeScale();
            }

            _hitStopRoutine = StartCoroutine(HitStopRoutine(durationSeconds, scale));
        }

        private IEnumerator HitStopRoutine(float durationSeconds, float scale)
        {
            if (!_hitStopActive)
            {
                _savedTimeScale = Time.timeScale > 0.001f ? Time.timeScale : 1f;
            }

            _hitStopActive = true;
            Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, durationSeconds));
            RestoreTimeScale();
            _hitStopRoutine = null;
        }

        private void RestoreTimeScale()
        {
            if (!_hitStopActive)
            {
                return;
            }

            Time.timeScale = _savedTimeScale > 0.001f ? _savedTimeScale : 1f;
            _hitStopActive = false;
            _savedTimeScale = 1f;
        }

        private void ClearShakeFromCamera()
        {
            var cam = ResolveCamera();
            if (cam == null)
            {
                _lastShakeOffset = Vector3.zero;
                _lastShakeRoll = 0f;
                _shakeOnCamera = false;
                return;
            }

            if (_shakeOnCamera
                && (cam.transform.position - _shakeAppliedPos).sqrMagnitude < 0.0001f
                && (_lastShakeOffset.sqrMagnitude > 0f || Mathf.Abs(_lastShakeRoll) > 0.0001f))
            {
                cam.transform.position -= _lastShakeOffset;
                cam.transform.Rotate(0f, 0f, -_lastShakeRoll);
            }

            _lastShakeOffset = Vector3.zero;
            _lastShakeRoll = 0f;
            _shakeOnCamera = false;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            targetCamera = Camera.main;
            return targetCamera;
        }

        [ContextMenu("Test Punch Ultimate")]
        public void EditorTestPunchUltimate()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CombatImpactFeel] Punch test requires Play Mode.");
                return;
            }

            PunchUltimate();
        }
    }
}
