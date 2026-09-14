using System;

namespace FracturedChorus.Combat.Presentation
{
    public enum AstraStageTvPhase
    {
        Idle = 0,
        Dropping = 1,
        Rolling = 2,
        Locked = 3
    }

    /// <summary>
    /// Drop then vertical reel (top → bottom) then lock one random face.
    /// Pure timing; no Unity types so Editor tests can inject dt + seed.
    /// </summary>
    public sealed class AstraStageTvSequence
    {
        public const int FaceCount = 5;

        public AstraStageTvPhase Phase { get; private set; }
        public float DropT { get; private set; }
        public float ReelOffset { get; private set; }
        public int LockedFaceIndex { get; private set; } = -1;

        private float _dropDuration;
        private float _spinDuration;
        private float _spinSpeed;
        private float _decelDuration;
        private float _dropElapsed;
        private float _rollElapsed;
        private float _decelStartOffset;
        private float _decelTargetOffset;
        private bool _decelStarted;
        private Random _rng;

        public void Begin(
            float dropDurationSec,
            float spinDurationSec,
            float spinSpeedFacesPerSec,
            float decelDurationSec,
            int? seed = null)
        {
            _dropDuration = Math.Max(0.05f, dropDurationSec);
            _spinDuration = Math.Max(0.05f, spinDurationSec);
            _spinSpeed = Math.Max(0.5f, spinSpeedFacesPerSec);
            _decelDuration = Clamp(decelDurationSec, 0.05f, _spinDuration);
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
            Phase = AstraStageTvPhase.Dropping;
            DropT = 0f;
            ReelOffset = 0f;
            LockedFaceIndex = -1;
            _dropElapsed = 0f;
            _rollElapsed = 0f;
            _decelStarted = false;
            _decelStartOffset = 0f;
            _decelTargetOffset = 0f;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || Phase == AstraStageTvPhase.Idle || Phase == AstraStageTvPhase.Locked)
            {
                return;
            }

            if (Phase == AstraStageTvPhase.Dropping)
            {
                _dropElapsed += dt;
                DropT = Clamp(_dropElapsed / _dropDuration, 0f, 1f);
                if (DropT < 1f)
                {
                    return;
                }

                DropT = 1f;
                Phase = AstraStageTvPhase.Rolling;
                _rollElapsed = 0f;
            }

            if (Phase != AstraStageTvPhase.Rolling)
            {
                return;
            }

            _rollElapsed += dt;
            var remaining = _spinDuration - _rollElapsed;
            if (remaining > _decelDuration)
            {
                ReelOffset = Wrap(ReelOffset + _spinSpeed * dt);
                return;
            }

            if (!_decelStarted)
            {
                BeginDecel();
            }

            var decelT = Clamp(1f - remaining / _decelDuration, 0f, 1f);
            ReelOffset = Lerp(_decelStartOffset, _decelTargetOffset, Smooth01(decelT));
            if (decelT < 1f)
            {
                return;
            }

            ReelOffset = Wrap(_decelTargetOffset);
            Phase = AstraStageTvPhase.Locked;
        }

        public static float Smooth01(float t)
        {
            t = Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private void BeginDecel()
        {
            _decelStarted = true;
            LockedFaceIndex = _rng.Next(0, FaceCount);
            _decelStartOffset = ReelOffset;
            var target = LockedFaceIndex;
            while (target < _decelStartOffset + 0.35f)
            {
                target += FaceCount;
            }

            _decelTargetOffset = target;
        }

        private static float Wrap(float offset)
        {
            var cycle = FaceCount;
            offset %= cycle;
            if (offset < 0f)
            {
                offset += cycle;
            }

            return offset;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
