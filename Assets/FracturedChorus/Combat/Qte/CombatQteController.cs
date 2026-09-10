using System.Collections;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Combat.Qte
{
    public class CombatQteController : MonoBehaviour
    {
        [Header("QTE")]
        [SerializeField] private CombatQteProfileSO profile;
        [SerializeField] private CombatQteOverlayView overlay;
        [Tooltip("Tỷ lệ QTE mặc định (phase 1–2). Tăng thêm theo profile mỗi 2 phase.")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultChance = 0.30f;
        [Tooltip("Miss: đòn quái không bị hủy, dmg giảm bấy nhiêu (phase 1–2). Tăng mỗi 2 phase, cap trên profile.")]
        [Range(0f, 1f)]
        [SerializeField] private float missDamageReduction = 0.25f;

        private AudioSource _sfxSource;

        public CombatQteProfileSO Profile => profile;
        public CombatQteOverlayView Overlay => overlay;
        public float DefaultChance => defaultChance;
        public float MissDamageReduction => missDamageReduction;

        public void Configure(CombatQteProfileSO qteProfile, CombatQteOverlayView qteOverlay)
        {
            Configure(qteProfile, qteOverlay, null, null);
        }

        public void Configure(
            CombatQteProfileSO qteProfile,
            CombatQteOverlayView qteOverlay,
            float? chance,
            float? missReduction = null)
        {
            if (qteProfile != null)
            {
                profile = qteProfile;
            }

            if (qteOverlay != null)
            {
                overlay = qteOverlay;
            }

            if (chance.HasValue)
            {
                defaultChance = Mathf.Clamp01(chance.Value);
            }

            if (missReduction.HasValue)
            {
                missDamageReduction = Mathf.Clamp01(missReduction.Value);
            }
        }

        public IEnumerator TryRunIfCounterBeat(CombatSession session, int beatIndex)
        {
            CombatQteModifiers.Clear();
            if (profile == null)
            {
                Debug.LogWarning("[QTE] Missing CombatQteProfileSO — skip.");
                yield break;
            }

            if (session?.Timeline == null || beatIndex < 0)
            {
                yield break;
            }

            if (!CombatCounterResolver.HasCounterOverlapAtBeat(session.Timeline, beatIndex)
                && !CombatCounterResolver.HasCounterOverlapForEncounter(session.Timeline, beatIndex))
            {
                yield break;
            }

            var phaseIndex = TimelineConstants.GetPhaseIndex(beatIndex);
            var chance = profile.GetChance(phaseIndex, defaultChance);
            if (Random.value > chance)
            {
                yield break;
            }

            EnsureOverlay();
            if (overlay == null)
            {
                overlay = CombatQteOverlayView.EnsureCreated();
            }

            if (overlay == null)
            {
                Debug.LogWarning("[QTE] Missing CombatQteOverlayView — skip.");
                yield break;
            }

            overlay.ShowPrompt(profile);
            var grade = CombatQteGrade.Miss;
            yield return RunPrompt(resolved => grade = resolved);

            var rule = profile.GetRule(grade, phaseIndex, missDamageReduction);
            CombatQteModifiers.Apply(grade, rule);
            if (overlay != null)
            {
                overlay.ShowGrade(rule.gradeSprite);
            }

            PlayGradeSfx(rule);
            var hold = Mathf.Max(0.05f, profile.gradeHoldSeconds);
            yield return new WaitForSecondsRealtime(hold);
            if (overlay != null)
            {
                overlay.Hide();
            }

            Debug.Log(
                $"[QTE] {grade} @ beat {beatIndex} phase {phaseIndex + 1} " +
                $"(chance {chance:P0} cancel={rule.cancelEnemy} out×{rule.outgoingMult:0.00} in×{rule.incomingEnemyMult:0.00})");
        }

        public void HideImmediate()
        {
            // C# ?. does not treat destroyed UnityObjects as null.
            if (overlay != null)
            {
                overlay.Hide();
            }
        }

        private IEnumerator RunPrompt(System.Action<CombatQteGrade> onResolved)
        {
            var duration = Mathf.Max(0.05f, profile.shrinkDuration);
            var startScale = Mathf.Max(1f, profile.outerStartScale);
            var lateLimit = duration + Mathf.Max(profile.goodWindowSec, profile.perfectWindowSec);
            var elapsed = 0f;

            while (elapsed < lateLimit)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (overlay != null)
                {
                    overlay.SetOuterScale(Mathf.Lerp(startScale, 1f, t));
                    overlay.SetTimingPreview(profile.Evaluate(elapsed));
                }

                if (ReadConfirmPressed())
                {
                    onResolved(profile.Evaluate(elapsed));
                    yield break;
                }

                yield return null;
            }

            onResolved(CombatQteGrade.Miss);
        }

        private void EnsureOverlay()
        {
            if (overlay == null)
            {
                overlay = FindAnyObjectByType<CombatQteOverlayView>(FindObjectsInactive.Include);
            }
        }

        private void PlayGradeSfx(CombatQteGradeRule rule)
        {
            if (rule.sfx == null)
            {
                return;
            }

            if (_sfxSource == null)
            {
                _sfxSource = GetComponent<AudioSource>();
                if (_sfxSource == null)
                {
                    _sfxSource = gameObject.AddComponent<AudioSource>();
                    _sfxSource.playOnAwake = false;
                }
            }

            _sfxSource.PlayOneShot(rule.sfx);
        }

        private bool ReadConfirmPressed()
        {
            if (profile.acceptMouseClick && ReadMousePressedThisFrame())
            {
                return true;
            }

            return ReadKeyPressedThisFrame(profile.confirmKey);
        }

        private static bool ReadMousePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool ReadKeyPressedThisFrame(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var mapped = ToInputSystemKey(key);
                if (mapped != Key.None)
                {
                    var control = keyboard[mapped];
                    if (control != null && control.wasPressedThisFrame)
                    {
                        return true;
                    }
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Key ToInputSystemKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                default: return Key.None;
            }
        }
#endif
    }
}
