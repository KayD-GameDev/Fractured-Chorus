using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Combat.Core
{
    /// <summary>
    /// Guard mới: người chơi GIỮ Spacebar xuyên suốt beat đòn (đỏ) của quái.
    /// Block được tính khi đòn quái resolve (ở cuối beat): người chơi đã giữ Spacebar
    /// liên tục từ trước/đầu beat cho tới lúc đó.
    /// </summary>
    public class GuardInputController : MonoBehaviour
    {
        [Tooltip("Tolerance (seconds): late Spacebar press after beat start still counts as a block.")]
        [SerializeField] private float pressGraceSeconds = 0.1f;

        [Tooltip("Remaining damage after a successful block (0 = fully blocked).")]
        [Range(0f, 1f)]
        [SerializeField] private float blockedDamageRemaining = 0f;

        private bool _isHeld;
        private float _holdStartTime = float.PositiveInfinity;

        public float BlockedDamageRemaining => blockedDamageRemaining;

        public bool IsHoldingGuard => _isHeld;

        private void Update()
        {
            var held = ReadGuardHeld();

            if (held && !_isHeld)
            {
                _holdStartTime = Time.unscaledTime;
            }
            else if (!held)
            {
                _holdStartTime = float.PositiveInfinity;
            }

            _isHeld = held;
        }

        /// <summary>
        /// Có block đòn quái không? True nếu đang giữ Spacebar VÀ đã giữ liên tục từ
        /// (trước/đầu beat = beatStartRealtime + dung sai) cho tới giờ.
        /// </summary>
        public bool HeldThroughBeatSince(float beatStartRealtime)
        {
            return _isHeld && _holdStartTime <= beatStartRealtime + pressGraceSeconds;
        }

        private static bool ReadGuardHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }
    }
}
