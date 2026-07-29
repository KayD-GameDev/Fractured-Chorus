using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class EnemyStrikeChoreographer : MonoBehaviour
    {
        private const int MaxQueuedStrikes = 8;

        [Header("Timing")]
        [SerializeField] private float lungeSeconds = 0.1f;
        [SerializeField] private float lungeSpeed = 32f;
        [SerializeField] private float impactHoldSeconds = 0.22f;
        [SerializeField] private float retreatSeconds = 0.22f;
        [SerializeField] private float knockbackSeconds = 0.12f;
        [SerializeField] private float knockbackSpeed = 40f;
        [SerializeField] private float counterHoldSeconds = 0.28f;
        [SerializeField] [Range(0.05f, 0.95f)] private float skillImpactNormalizedTime = 0.35f;

        [Header("Focus")]
        [SerializeField] [Range(0f, 1f)] private float dimFactor = 0.35f;
        [SerializeField] private float dimFadeSeconds = 0.12f;

        [Header("Placement")]
        [Tooltip("Minimum horizontal gap between enemy feet and the receiver when lunging.")]
        [SerializeField] private float strikeStandoffX = 2.4f;
        [Tooltip("Battlefield mid X the enemy is knocked toward after a counter.")]
        [SerializeField] private float midStagingX = 0f;

        [Header("Refs")]
        [SerializeField] private CombatFocusDimmer focusDimmer;

        private static readonly HashSet<CombatUnit> ActiveAttackers = new();
        private static readonly Dictionary<CombatUnit, HpChangeInfo> PendingHpFeedback = new();
        private static bool _ownsEnemyBodies;
        private static bool _deferringHpFeedback;
        private static EnemyStrikeChoreographer _activeInstance;

        private readonly Queue<EnemyStrikeReport> _pending = new();
        private readonly List<UnitView> _focusScratch = new();
        private readonly List<CombatUnit> _counterPlayersScratch = new();
        private readonly List<(UnitView View, AgendaEntry Entry)> _counterEntriesScratch = new();
        private readonly Dictionary<CombatUnit, Vector3> _homePositions = new();
        private CombatSession _session;
        private Coroutine _routine;
        private bool _enabled;

        public bool IsBusy =>
            _enabled && (_routine != null || _pending.Count > 0 || ActiveAttackers.Count > 0);

        public static EnemyStrikeChoreographer ActiveInstance => _activeInstance;

        public static bool IsChoreographing(CombatUnit unit) =>
            _ownsEnemyBodies && unit != null && unit.Side == GridSide.Enemy;

        public static bool OwnsCounterPresentation => _ownsEnemyBodies;

        public static bool TryDeferHpFeedback(CombatUnit unit, HpChangeInfo change)
        {
            if (!_ownsEnemyBodies || !_deferringHpFeedback || unit == null || !change.ShouldShowFeedback)
            {
                return false;
            }

            PendingHpFeedback[unit] = change;
            return true;
        }

        public static void ClearOwnership()
        {
            _ownsEnemyBodies = false;
            _deferringHpFeedback = false;
            PendingHpFeedback.Clear();
            ActiveAttackers.Clear();
            _activeInstance = null;
        }

        public void Configure(CombatSession session, bool choreographyEnabled)
        {
            Unsubscribe();

            _session = session;
            _enabled = choreographyEnabled;
            _ownsEnemyBodies = choreographyEnabled && session != null;
            _activeInstance = choreographyEnabled ? this : null;
            EnsureFocusDimmer();

            if (_session == null || !_enabled)
            {
                return;
            }

            _session.OnEnemyStrikeResolved += HandleEnemyStrikeResolved;
            _session.OnBeforeResolveBeat += HandleBeforeResolveBeat;
            _session.OnPhaseChanged += HandlePhaseChanged;
        }

        private void HandleBeforeResolveBeat(int beatIndex)
        {
            if (!_enabled || _session?.Timeline == null)
            {
                return;
            }

            if (_routine != null || ActiveAttackers.Count > 0)
            {
                return;
            }

            if (!CombatCounterResolver.WillPresentStrikeAtBeat(_session.Timeline, beatIndex))
            {
                return;
            }

            _deferringHpFeedback = true;
            PendingHpFeedback.Clear();
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase == CombatPhase.Planning && !IsBusy)
            {
                focusDimmer?.ReleaseImmediate();
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

            if (report.WasCountered &&
                !CombatCounterResolver.ShouldPresentCounterBodyAtBeat(_session?.Timeline, report.BeatIndex))
            {
                FlushRemainingHpFeedback();
                return;
            }

            if (_pending.Count >= MaxQueuedStrikes)
            {
                FlushRemainingHpFeedback();
                return;
            }

            if (report.WasCountered)
            {
                CollectCounteringEntries(report.BeatIndex);
                CombatCounterResolver.MarkCounterPresentations(
                    _counterEntriesScratch.Select(e => e.Entry));
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
                FlushRemainingHpFeedback();
                yield break;
            }

            EnsureHomeCaptured(report.Attacker, attackerView);
            ActiveAttackers.Add(report.Attacker);

            CollectFocusCast(report, attackerView, receiverView);
            focusDimmer?.Focus(_focusScratch);

            var strikeFeet = ResolveStrikeAnchor(receiverView, report.Target);
            attackerView.PlayMovingLoop();
            yield return attackerView.MoveFeetToRoutine(
                strikeFeet,
                ResolveMoveSeconds(attackerView.FeetWorldPosition, strikeFeet, lungeSpeed, lungeSeconds));

            if (report.WasCountered)
            {
                yield return PlayCounterImpact(report, attackerView);
            }
            else
            {
                attackerView.PlayCounterHold();
                receiverView.PlayBeCounteredHold();

                var attackClipLength = Mathf.Max(
                    attackerView.EstimateCounterClipLength(),
                    counterHoldSeconds);
                var impactDelay = attackClipLength * skillImpactNormalizedTime;
                if (impactDelay > 0f)
                {
                    yield return new WaitForSeconds(impactDelay);
                }

                FlushHpFeedback(report.Target);

                var tail = attackClipLength * (1f - skillImpactNormalizedTime) + impactHoldSeconds;
                if (tail > 0f)
                {
                    yield return new WaitForSeconds(tail);
                }

                FlushRemainingHpFeedback();
            }

            RestoreIdleExcept(attackerView);
            yield return FinishStrikeMovement(report, attackerView);

            ActiveAttackers.Remove(report.Attacker);
            focusDimmer?.Release();
        }

        private void EnsureHomeCaptured(CombatUnit attacker, UnitView attackerView)
        {
            if (_homePositions.ContainsKey(attacker))
            {
                return;
            }

            attackerView.CaptureAnchor();
            _homePositions[attacker] = attackerView.AnchorPosition;
        }

        private IEnumerator FinishStrikeMovement(EnemyStrikeReport report, UnitView attackerView)
        {
            var hasMoreStrikes = _pending.Count > 0;
            var home = _homePositions.TryGetValue(report.Attacker, out var stored)
                ? stored
                : attackerView.AnchorPosition;

            if (hasMoreStrikes)
            {
                if (!report.WasCountered)
                {
                    attackerView.PlayMovingLoop();
                    yield return attackerView.MoveFeetToRoutine(
                        ResolveMidStaging(attackerView),
                        retreatSeconds);
                }

                attackerView.PlayIdleState();
                yield break;
            }

            attackerView.PlayMovingLoop();
            yield return attackerView.MoveToRoutine(home, retreatSeconds);
            attackerView.PlayIdleState();
            _homePositions.Remove(report.Attacker);
        }

        private void RestoreIdleExcept(UnitView keepMoving)
        {
            foreach (var view in _focusScratch)
            {
                if (view == null || view == keepMoving)
                {
                    continue;
                }

                view.PlayIdleState();
            }
        }

        private IEnumerator PlayCounterImpact(EnemyStrikeReport report, UnitView attackerView)
        {
            CollectCounteringEntries(report.BeatIndex);
            if (_counterEntriesScratch.Count == 0)
            {
                FlushRemainingHpFeedback();
                yield break;
            }

            var counterBody = CombatCounterResolver.SelectCounterBody(
                _counterEntriesScratch.Select(e => e.Entry.Unit).ToList());

            foreach (var (view, entry) in _counterEntriesScratch)
            {
                if (entry.Unit == counterBody)
                {
                    view.PlayCounterHold();
                }
            }

            attackerView.PlayBeCounteredHold();
            var mid = ResolveMidStaging(attackerView);
            var knockback = StartCoroutine(
                attackerView.MoveFeetToRoutine(
                    mid,
                    ResolveMoveSeconds(attackerView.FeetWorldPosition, mid, knockbackSpeed, knockbackSeconds)));

            foreach (var (view, entry) in _counterEntriesScratch)
            {
                view.PlayAttackAnimationHold(entry.Skill);
            }

            var skillImpactDelay = 0f;
            foreach (var (view, entry) in _counterEntriesScratch)
            {
                skillImpactDelay = Mathf.Max(
                    skillImpactDelay,
                    view.EstimateSkillClipLength(entry.Skill) * skillImpactNormalizedTime);
            }

            if (skillImpactDelay > 0f)
            {
                yield return new WaitForSeconds(skillImpactDelay);
            }

            FlushHpFeedback(report.Attacker);

            var skillTail = impactHoldSeconds;
            foreach (var (view, entry) in _counterEntriesScratch)
            {
                var clipLength = view.EstimateSkillClipLength(entry.Skill);
                skillTail = Mathf.Max(skillTail, clipLength * (1f - skillImpactNormalizedTime));
            }

            if (skillTail > 0f)
            {
                yield return new WaitForSeconds(skillTail);
            }

            if (knockback != null)
            {
                yield return knockback;
            }

            FlushRemainingHpFeedback();
        }

        private static float ResolveMoveSeconds(Vector3 from, Vector3 to, float speed, float fallbackSeconds)
        {
            var distance = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(to.x, to.y));
            if (speed <= 0.01f)
            {
                return Mathf.Max(0.04f, fallbackSeconds);
            }

            return Mathf.Clamp(distance / speed, 0.04f, Mathf.Max(0.04f, fallbackSeconds));
        }

        private static void FlushHpFeedback(CombatUnit unit)
        {
            if (unit == null || !PendingHpFeedback.TryGetValue(unit, out var change))
            {
                return;
            }

            PendingHpFeedback.Remove(unit);
            var view = UnitView.FindForUnit(unit);
            view?.PlayHpFeedback(change);
        }

        private static void FlushRemainingHpFeedback()
        {
            if (PendingHpFeedback.Count == 0)
            {
                _deferringHpFeedback = false;
                return;
            }

            var remaining = PendingHpFeedback.Keys.ToList();
            foreach (var unit in remaining)
            {
                FlushHpFeedback(unit);
            }

            _deferringHpFeedback = false;
        }

        private void CollectFocusCast(EnemyStrikeReport report, UnitView attackerView, UnitView receiverView)
        {
            _focusScratch.Clear();
            _focusScratch.Add(attackerView);
            _focusScratch.Add(receiverView);

            if (!report.WasCountered || _session?.Timeline == null)
            {
                return;
            }

            CombatCounterResolver.CollectCounteringPlayerUnits(
                _session.Timeline,
                report.BeatIndex,
                _counterPlayersScratch);
            foreach (var unit in _counterPlayersScratch)
            {
                var view = UnitView.FindForUnit(unit);
                if (view != null && !_focusScratch.Contains(view))
                {
                    _focusScratch.Add(view);
                }
            }
        }

        private void CollectCounteringEntries(int beatIndex)
        {
            _counterEntriesScratch.Clear();
            if (_session?.Timeline == null || beatIndex < 0)
            {
                return;
            }

            CombatCounterResolver.CollectCounteringPlayerUnits(
                _session.Timeline,
                beatIndex,
                _counterPlayersScratch);

            foreach (var unit in _counterPlayersScratch)
            {
                AgendaEntry match = null;
                foreach (var entry in _session.Timeline.Agenda)
                {
                    if (entry?.Unit != unit || entry.Skill == null || entry.Skill.IsGuard)
                    {
                        continue;
                    }

                    var activeOnBeat = false;
                    foreach (var activeBeat in CombatCounterResolver.GetActiveBeatIndices(entry))
                    {
                        if (activeBeat == beatIndex)
                        {
                            activeOnBeat = true;
                            break;
                        }
                    }

                    if (!activeOnBeat)
                    {
                        continue;
                    }

                    match = entry;
                    break;
                }

                if (match == null)
                {
                    continue;
                }

                var view = UnitView.FindForUnit(unit);
                if (view != null)
                {
                    _counterEntriesScratch.Add((view, match));
                }
            }
        }

        private Vector3 ResolveStrikeAnchor(UnitView receiverView, CombatUnit receiver)
        {
            var receiverFeet = receiverView.FeetWorldPosition;
            var towardCenter = Mathf.Sign(midStagingX - receiverFeet.x);
            if (Mathf.Approximately(towardCenter, 0f))
            {
                towardCenter = receiver.Side == GridSide.Player ? 1f : -1f;
            }

            var standoff = Mathf.Max(0.5f, strikeStandoffX);
            var strikeX = receiverFeet.x + towardCenter * standoff;

            if (receiver.GridPosition.IsValid())
            {
                var rowY = HexBoardLayout.GetWorldPosition(receiver.GridPosition).y;
                return new Vector3(strikeX, rowY, receiverFeet.z);
            }

            return new Vector3(strikeX, receiverFeet.y, receiverFeet.z);
        }

        private Vector3 ResolveMidStaging(UnitView attackerView)
        {
            var feet = attackerView.FeetWorldPosition;
            return new Vector3(midStagingX, feet.y, feet.z);
        }

        private void AbortAll()
        {
            _pending.Clear();

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            FlushRemainingHpFeedback();

            foreach (var attacker in ActiveAttackers)
            {
                var view = UnitView.FindForUnit(attacker);
                if (view == null)
                {
                    continue;
                }

                if (_homePositions.TryGetValue(attacker, out var home))
                {
                    view.transform.position = new Vector3(home.x, home.y, view.transform.position.z);
                }
                else
                {
                    view.SnapToAnchor();
                }

                view.PlayIdleState();
            }

            foreach (var view in _focusScratch)
            {
                view?.PlayIdleState();
            }

            ActiveAttackers.Clear();
            _homePositions.Clear();
            focusDimmer?.ReleaseImmediate();
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.OnEnemyStrikeResolved -= HandleEnemyStrikeResolved;
            _session.OnBeforeResolveBeat -= HandleBeforeResolveBeat;
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
            PendingHpFeedback.Clear();
            _deferringHpFeedback = false;
            _ownsEnemyBodies = false;
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
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
