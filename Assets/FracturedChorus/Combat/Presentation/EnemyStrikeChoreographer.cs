using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    /// <summary>
    /// Plays the enemy melee beat as a lunge → impact → retreat sequence while the timeline
    /// keeps running. Damage is already resolved by CombatSession; this is presentation only.
    /// </summary>
    public class EnemyStrikeChoreographer : MonoBehaviour
    {
        private const int MaxQueuedStrikes = 3;

        [Header("Timing")]
        [SerializeField] private float lungeSeconds = 0.2f;
        [SerializeField] private float impactHoldSeconds = 0.22f;
        [SerializeField] private float retreatSeconds = 0.28f;

        [Header("Focus")]
        [SerializeField] [Range(0f, 1f)] private float dimFactor = 0.35f;
        [SerializeField] private float dimFadeSeconds = 0.12f;

        [Header("Placement")]
        [Tooltip("Fallback step in front of a receiver that already sits in the front column.")]
        [SerializeField] private float frontStepX = 1.6f;

        [Header("Refs")]
        [SerializeField] private CombatFocusDimmer focusDimmer;

        private static readonly HashSet<CombatUnit> ActiveAttackers = new();
        private static bool _ownsEnemyBodies;

        private readonly Queue<EnemyStrikeReport> _pending = new();
        private readonly List<UnitView> _focusScratch = new();
        private CombatSession _session;
        private Coroutine _routine;
        private bool _enabled;

        /// <summary>
        /// True while this choreographer owns enemy body animation. The counter driver runs one
        /// frame earlier than beat resolution, so ownership is claimed on Configure rather than
        /// when a sequence starts.
        /// </summary>
        public static bool IsChoreographing(CombatUnit unit) =>
            _ownsEnemyBodies && unit != null && unit.Side == GridSide.Enemy;

        /// <summary>Call on combat bootstrap so a previous scene cannot leave ownership claimed.</summary>
        public static void ClearOwnership()
        {
            _ownsEnemyBodies = false;
            ActiveAttackers.Clear();
        }

        public void Configure(CombatSession session, bool choreographyEnabled)
        {
            Unsubscribe();

            _session = session;
            _enabled = choreographyEnabled;
            _ownsEnemyBodies = choreographyEnabled && session != null;
            EnsureFocusDimmer();

            if (_session == null || !_enabled)
            {
                return;
            }

            _session.OnEnemyStrikeResolved += HandleEnemyStrikeResolved;
            _session.OnEncounterEnded += AbortAll;
            _session.OnPhaseChanged += HandlePhaseChanged;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase == CombatPhase.Planning)
            {
                AbortAll();
            }
        }

        public void ResetPresentation()
        {
            AbortAll();
        }

        private void EnsureFocusDimmer()
        {
            if (focusDimmer == null)
            {
                focusDimmer = GetComponent<CombatFocusDimmer>();
            }

            if (focusDimmer == null)
            {
                focusDimmer = FindAnyObjectByType<CombatFocusDimmer>();
            }

            if (focusDimmer == null)
            {
                focusDimmer = gameObject.AddComponent<CombatFocusDimmer>();
            }

            focusDimmer.Configure(dimFactor, dimFadeSeconds);
        }

        private void HandleEnemyStrikeResolved(EnemyStrikeReport report)
        {
            if (!_enabled || !isActiveAndEnabled || !report.IsValid)
            {
                return;
            }

            if (_pending.Count >= MaxQueuedStrikes)
            {
                return;
            }

            _pending.Enqueue(report);
            if (_routine == null)
            {
                _routine = StartCoroutine(DrainQueue());
            }
        }

        private IEnumerator DrainQueue()
        {
            while (_pending.Count > 0)
            {
                yield return PlayStrike(_pending.Dequeue());
            }

            _routine = null;
        }

        private IEnumerator PlayStrike(EnemyStrikeReport report)
        {
            var attackerView = UnitView.FindForUnit(report.Attacker);
            var receiverView = UnitView.FindForUnit(report.Target);
            if (attackerView == null || receiverView == null)
            {
                yield break;
            }

            ActiveAttackers.Add(report.Attacker);
            attackerView.CaptureAnchor();

            _focusScratch.Clear();
            _focusScratch.Add(attackerView);
            _focusScratch.Add(receiverView);
            focusDimmer?.Focus(_focusScratch);

            attackerView.PlayMovingLoop();
            yield return attackerView.MoveFeetToRoutine(ResolveStrikeAnchor(receiverView, report.Target), lungeSeconds);

            if (report.WasCountered)
            {
                attackerView.PlayBeCounteredRestart();
            }
            else
            {
                attackerView.PlayCounterRestart();
                receiverView.PlayBeCounteredRestart();
            }

            if (impactHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(impactHoldSeconds);
            }

            attackerView.PlayMovingLoop();
            yield return attackerView.MoveToRoutine(attackerView.AnchorPosition, retreatSeconds);
            attackerView.PlayIdleState();

            ActiveAttackers.Remove(report.Attacker);
            focusDimmer?.Release();
        }

        /// <summary>Cell directly in front of the receiver, or a step into no-man's land when already frontmost.</summary>
        private Vector3 ResolveStrikeAnchor(UnitView receiverView, CombatUnit receiver)
        {
            var position = receiver.GridPosition;
            if (!position.IsValid())
            {
                return receiverView.FeetWorldPosition + FrontStepOffset(position.Side);
            }

            var frontColumn = position.Column - 1;
            if (frontColumn >= PositionalModifiers.FrontColumnIndex)
            {
                return HexBoardLayout.GetWorldPosition(position.Side, position.Row, frontColumn);
            }

            return HexBoardLayout.GetWorldPosition(position) + FrontStepOffset(position.Side);
        }

        private Vector3 FrontStepOffset(GridSide side)
        {
            var sign = side == GridSide.Player ? 1f : -1f;
            return new Vector3(frontStepX * sign, 0f, 0f);
        }

        private void AbortAll()
        {
            _pending.Clear();

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            foreach (var attacker in ActiveAttackers)
            {
                UnitView.FindForUnit(attacker)?.SnapToAnchor();
            }

            ActiveAttackers.Clear();
            focusDimmer?.ReleaseImmediate();
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.OnEnemyStrikeResolved -= HandleEnemyStrikeResolved;
            _session.OnEncounterEnded -= AbortAll;
            _session.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void OnDisable()
        {
            AbortAll();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ActiveAttackers.Clear();
            _ownsEnemyBodies = false;
        }

        private void OnValidate()
        {
            if (focusDimmer != null)
            {
                focusDimmer.Configure(dimFactor, dimFadeSeconds);
            }
        }
    }
}
