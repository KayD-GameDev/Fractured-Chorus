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
        [SerializeField] private float impactHoldSeconds = 0.22f;
        [SerializeField] private float retreatSeconds = 0.22f;
        [SerializeField] private float knockbackSeconds = 0.12f;
        [SerializeField] private float knockbackSpeed = 40f;
        [SerializeField] [Range(0.05f, 0.95f)] private float skillImpactNormalizedTime = 0.35f;

        [Header("Focus")]
        [SerializeField] [Range(0f, 1f)] private float dimFactor = 0.35f;
        [SerializeField] private float dimFadeSeconds = 0.12f;

        [Header("Placement")]
        [Tooltip("Minimum horizontal gap between enemy feet and the receiver when lunging.")]
        [SerializeField] private float strikeStandoffX = 2.4f;
        [Tooltip("Battlefield mid X the enemy is knocked toward after a counter.")]
        [SerializeField] private float midStagingX = 0f;

        [Header("Enemy Projectile Volley")]
        [SerializeField] private Sprite bossSwordSprite;
        [SerializeField] private Sprite bossSwordImpactSprite;
        [SerializeField] private Sprite gruntMicBoltSprite;
        [SerializeField] private Sprite gruntEyeBoltSprite;
        [SerializeField] private Sprite gruntImpactSprite;
        [SerializeField] private Material bossSwordAdditiveMaterial;
        [SerializeField] private float swordTravelSeconds = 0.32f;
        [SerializeField] private float swordImpactSeconds = 0.18f;
        [SerializeField] private float swordShotGapSeconds = 0.08f;
        [SerializeField] private float swordVerticalSpread = 0.22f;
        [SerializeField] private float swordWorldLength = 1.9f;
        [SerializeField] private float gruntBoltWorldLength = 1.35f;
        [SerializeField] private float swordImpactWorldSize = 1.7f;
        [SerializeField] private float castHoldSeconds = 0.14f;
        [SerializeField] private bool loadSwordResourcesFallback = true;
        [SerializeField] private Transform swordShotParent;

        private const string BossDespairUnitId = "boss_despair";
        private const string GruntEyeUnitId = "grunt_right";

        [Header("Refs")]
        [SerializeField] private CombatFocusDimmer focusDimmer;
        [SerializeField] private PlayerSkillShotChoreographer playerSkillShotChoreographer;
        [SerializeField] private CharlotteSkillChoreographer charlotteSkillChoreographer;

        private Material _runtimeSwordAdditive;

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
            EnsurePlayerSkillShotChoreographer();

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
            if (!_enabled || _session?.Timeline == null || EncounterDirector.IsPresenting)
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
            if (phase != CombatPhase.Planning)
            {
                return;
            }

            // Phase xong → Planning: luôn abort lunge và kéo mọi attacker về ô home.
            AbortAll();
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

        private void EnsurePlayerSkillShotChoreographer()
        {
            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = GetComponent<PlayerSkillShotChoreographer>();
            }

            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = FindAnyObjectByType<PlayerSkillShotChoreographer>();
            }
        }

        private void EnsureCharlotteSkillChoreographer()
        {
            if (charlotteSkillChoreographer != null)
            {
                return;
            }

            charlotteSkillChoreographer = GetComponent<CharlotteSkillChoreographer>()
                                          ?? FindAnyObjectByType<CharlotteSkillChoreographer>();
            if (charlotteSkillChoreographer == null)
            {
                charlotteSkillChoreographer = gameObject.AddComponent<CharlotteSkillChoreographer>();
            }

            charlotteSkillChoreographer.EnsureDefaults();
        }

        private void HandleEnemyStrikeResolved(EnemyStrikeReport report)
        {
            if (!_enabled || !isActiveAndEnabled || !report.IsValid || EncounterDirector.IsPresenting)
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
            SnapAllHomedUnits();
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

            EnsureSwordSprites();
            attackerView.PlayCounterHold();
            if (report.WasCountered)
            {
                CollectCounteringEntries(report.BeatIndex);
                foreach (var (view, _) in _counterEntriesScratch)
                {
                    view.PlayCounterHold();
                }
            }

            if (castHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(castHoldSeconds);
            }

            var swordCount = Mathf.Clamp(report.SwordCount, 1, 3);
            yield return PresentEnemyVolley(
                attackerView,
                receiverView,
                swordCount,
                report.WasCountered,
                report.BeatIndex);

            if (report.WasCountered)
            {
                yield return PlayCounterImpact(report, attackerView);
            }
            else
            {
                receiverView.PlayBeCounteredHold();
                FlushHpFeedback(report.Target);
                if (impactHoldSeconds > 0f)
                {
                    yield return new WaitForSeconds(impactHoldSeconds);
                }

                FlushRemainingHpFeedback();
            }

            RestoreIdleExcept(attackerView);
            yield return FinishStrikeMovement(report, attackerView);

            ActiveAttackers.Remove(report.Attacker);
            focusDimmer?.Release();
        }

        public float GetProjectileContactDelaySeconds()
        {
            return Mathf.Max(0.01f, swordTravelSeconds) * 0.55f;
        }

        public IEnumerator PresentEnemyVolley(
            UnitView attackerView,
            UnitView receiverView,
            int projectileCount,
            bool countered,
            int beatIndex = -1)
        {
            if (countered)
            {
                var shieldHold = Mathf.Max(0.01f, swordTravelSeconds) + 1.2f;
                SpawnCharlotteCounterShields(beatIndex, receiverView, attackerView, shieldHold);
            }

            var kit = ResolveProjectileKit(attackerView != null ? attackerView.Unit : null);
            var mode = !countered
                ? BossSwordShotMode.Hit
                : kit == EnemyProjectileKit.DespairSword
                    ? BossSwordShotMode.Deflect
                    : BossSwordShotMode.Vanish;
            yield return PlayEnemyVolley(attackerView, receiverView, projectileCount, mode, kit);

            if (countered)
            {
                yield return CharlotteCounterShieldView.DismissAllAndWait();
            }
        }

        private void SpawnCharlotteCounterShields(
            int beatIndex,
            UnitView receiverView,
            UnitView attackerView,
            float holdSeconds)
        {
            var faceToward = attackerView != null
                ? attackerView.FeetWorldPosition
                : (Vector3?)null;
            var parent = transform;
            var spawned = false;

            if (beatIndex >= 0)
            {
                CollectCounteringEntries(beatIndex);
                foreach (var (view, _) in _counterEntriesScratch)
                {
                    if (CharlotteCounterShieldView.TrySpawnFor(view, faceToward, holdSeconds, parent) != null)
                    {
                        spawned = true;
                    }
                }
            }

            if (!spawned && receiverView != null)
            {
                CharlotteCounterShieldView.TrySpawnFor(receiverView, faceToward, holdSeconds, parent);
            }
        }

        public IEnumerator PresentSwordVolley(
            UnitView attackerView,
            UnitView receiverView,
            int swordCount,
            BossSwordShotMode mode)
        {
            var kit = ResolveProjectileKit(attackerView != null ? attackerView.Unit : null);
            yield return PlayEnemyVolley(attackerView, receiverView, swordCount, mode, kit);
        }

        private enum EnemyProjectileKit
        {
            DespairSword = 0,
            MicBolt = 1,
            EyeBolt = 2
        }

        private static EnemyProjectileKit ResolveProjectileKit(CombatUnit attacker)
        {
            if (attacker == null)
            {
                return EnemyProjectileKit.MicBolt;
            }

            if (attacker.UnitId == BossDespairUnitId)
            {
                return EnemyProjectileKit.DespairSword;
            }

            if (attacker.UnitId == GruntEyeUnitId)
            {
                return EnemyProjectileKit.EyeBolt;
            }

            return EnemyProjectileKit.MicBolt;
        }

        private IEnumerator PlayEnemyVolley(
            UnitView attackerView,
            UnitView receiverView,
            int projectileCount,
            BossSwordShotMode mode,
            EnemyProjectileKit kit)
        {
            var settings = BuildProjectileSettings(kit);
            if (settings.Sword == null)
            {
                yield break;
            }

            var from = ResolveAim(attackerView);
            var to = ResolveAim(receiverView);
            var parent = swordShotParent != null ? swordShotParent : transform;
            var gap = Mathf.Max(0f, swordShotGapSeconds);
            var travel = Mathf.Max(0.01f, settings.TravelSeconds);
            if (mode == BossSwordShotMode.Deflect)
            {
                travel = travel * 0.55f + Mathf.Max(0.01f, settings.ImpactSeconds)
                         + Mathf.Max(0.01f, settings.DeflectSeconds);
            }
            else if (mode == BossSwordShotMode.Vanish)
            {
                travel = travel * 0.55f + Mathf.Max(0.01f, settings.ImpactSeconds) * 2f;
            }
            else
            {
                travel += Mathf.Max(0.01f, settings.ImpactSeconds);
            }

            var count = Mathf.Clamp(projectileCount, 1, 3);
            for (var i = 0; i < count; i++)
            {
                var offsetY = (i - (count - 1) * 0.5f) * swordVerticalSpread;
                var shotFrom = from + new Vector3(0f, offsetY, 0f);
                var shotTo = to + new Vector3(0f, offsetY * 0.35f, 0f);
                BossSwordShotView.Spawn(shotFrom, shotTo, settings, mode, parent);
                if (gap > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(gap);
                }
            }

            if (travel > 0f)
            {
                yield return new WaitForSeconds(travel);
            }
        }

        private BossSwordShotSettings BuildProjectileSettings(EnemyProjectileKit kit)
        {
            EnsureProjectileSprites();
            Sprite projectile;
            Sprite impact;
            var length = swordWorldLength;
            switch (kit)
            {
                case EnemyProjectileKit.DespairSword:
                    projectile = bossSwordSprite;
                    impact = bossSwordImpactSprite;
                    break;
                case EnemyProjectileKit.EyeBolt:
                    projectile = gruntEyeBoltSprite;
                    impact = gruntImpactSprite;
                    length = gruntBoltWorldLength;
                    break;
                default:
                    projectile = gruntMicBoltSprite;
                    impact = gruntImpactSprite;
                    length = gruntBoltWorldLength;
                    break;
            }

            var isSword = kit == EnemyProjectileKit.DespairSword;
            return new BossSwordShotSettings
            {
                Sword = projectile,
                Impact = impact,
                AdditiveMaterial = ResolveSwordAdditive(),
                TravelSeconds = swordTravelSeconds,
                ImpactSeconds = swordImpactSeconds,
                SwordWorldLength = length,
                ImpactWorldSize = swordImpactWorldSize,
                SpriteFacingOffsetDegrees = isSword ? 135f : 0f,
                ProjectileAdditive = !isSword,
                SortingOrder = 42
            };
        }

        private void EnsureSwordSprites() => EnsureProjectileSprites();

        private void EnsureProjectileSprites()
        {
            if (!loadSwordResourcesFallback)
            {
                return;
            }

            if (bossSwordSprite == null)
            {
                bossSwordSprite = Resources.Load<Sprite>("VFX/Combat/Boss/boss_sword_projectile_v1");
            }

            if (bossSwordImpactSprite == null)
            {
                bossSwordImpactSprite = Resources.Load<Sprite>("VFX/Combat/Boss/boss_sword_impact_v1");
            }

            if (gruntMicBoltSprite == null)
            {
                gruntMicBoltSprite = Resources.Load<Sprite>("VFX/Combat/Grunt/astra_mic_bolt_v1");
            }

            if (gruntEyeBoltSprite == null)
            {
                gruntEyeBoltSprite = Resources.Load<Sprite>("VFX/Combat/Grunt/astra_eye_bolt_v1");
            }

            if (gruntImpactSprite == null)
            {
                gruntImpactSprite = Resources.Load<Sprite>("VFX/Combat/Grunt/astra_grunt_impact_v1");
            }
        }

        private Material ResolveSwordAdditive()
        {
            if (bossSwordAdditiveMaterial != null)
            {
                return bossSwordAdditiveMaterial;
            }

            if (_runtimeSwordAdditive != null)
            {
                return _runtimeSwordAdditive;
            }

            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _runtimeSwordAdditive = new Material(shader)
            {
                name = "BossSwordVfxAdditive_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _runtimeSwordAdditive;
        }

        private static Vector3 ResolveAim(UnitView view)
        {
            if (view == null)
            {
                return Vector3.zero;
            }

            return view.GetSkillPanelAnchorWorld();
        }

        private void EnsureHomeCaptured(CombatUnit attacker, UnitView attackerView)
        {
            if (_homePositions.ContainsKey(attacker) || attackerView == null)
            {
                return;
            }

            _homePositions[attacker] = attackerView.transform.position;
        }

        private static Vector3 ResolveAuthoritativeHome(CombatUnit attacker, UnitView attackerView)
        {
            if (attackerView == null)
            {
                return Vector3.zero;
            }

            if (EncounterDirector.ActiveInstance != null
                && EncounterDirector.ActiveInstance.TryGetPhaseHomeRoot(attackerView, out var phaseRoot))
            {
                return phaseRoot;
            }

            if (attacker != null && attacker.GridPosition.IsValid())
            {
                var cell = HexBoardLayout.GetWorldPosition(attacker.GridPosition);
                var rootToFeet = attackerView.transform.position - attackerView.FeetWorldPosition;
                return new Vector3(
                    cell.x + rootToFeet.x,
                    cell.y + rootToFeet.y,
                    attackerView.transform.position.z);
            }

            return attackerView.AnchorPosition;
        }

        private IEnumerator FinishStrikeMovement(EnemyStrikeReport report, UnitView attackerView)
        {
            var hasMoreStrikes = _pending.Count > 0;
            var home = _homePositions.TryGetValue(report.Attacker, out var stored)
                ? stored
                : ResolveAuthoritativeHome(report.Attacker, attackerView);

            if (hasMoreStrikes)
            {
                attackerView.PlayMovingLoop();
                yield return attackerView.MoveFeetToRoutine(
                    ResolveMidStaging(attackerView),
                    retreatSeconds);
                attackerView.PlayIdleState();
                yield break;
            }

            attackerView.PlayMovingLoop();
            yield return attackerView.MoveToRoutine(home, retreatSeconds);
            SnapUnitToHome(attackerView, home);
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

            UnitView bodyView = null;
            AgendaEntry bodyEntry = null;
            foreach (var (view, entry) in _counterEntriesScratch)
            {
                if (entry.Unit != counterBody)
                {
                    continue;
                }

                bodyView = view;
                bodyEntry = entry;
                break;
            }

            EnsurePlayerSkillShotChoreographer();
            EnsureCharlotteSkillChoreographer();
            yield return CharlotteCounterShieldView.DismissAllAndWait();

            if (bodyView != null
                && bodyEntry?.Skill != null
                && charlotteSkillChoreographer != null
                && charlotteSkillChoreographer.Handles(bodyEntry.Skill, bodyView))
            {
                attackerView.PlayBeCounteredHold();
                var charlotteMid = ResolveMidStaging(attackerView);
                var charlotteKnockback = StartCoroutine(
                    attackerView.MoveFeetToRoutine(
                        charlotteMid,
                        ResolveMoveSeconds(
                            attackerView.FeetWorldPosition,
                            charlotteMid,
                            knockbackSpeed,
                            knockbackSeconds)));

                yield return charlotteSkillChoreographer.PlaySkillRoutine(
                    bodyView,
                    attackerView,
                    bodyEntry.Skill,
                    returnHome: true,
                    onImpact: () => FlushHpFeedback(report.Attacker));

                if (charlotteKnockback != null)
                {
                    yield return charlotteKnockback;
                }

                FlushRemainingHpFeedback();
                yield break;
            }

            var useMeleeEngage = bodyView != null
                                 && bodyEntry?.Skill != null
                                 && playerSkillShotChoreographer != null
                                 && playerSkillShotChoreographer.IsMeleeSkill(bodyEntry.Skill);

            if (useMeleeEngage)
            {
                yield return PlayMeleeCounterImpact(report, attackerView, bodyView, bodyEntry);
                yield break;
            }

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

        private IEnumerator PlayMeleeCounterImpact(
            EnemyStrikeReport report,
            UnitView attackerView,
            UnitView bodyView,
            AgendaEntry bodyEntry)
        {
            yield return CharlotteCounterShieldView.DismissAllAndWait();
            bodyView.PlayCounterHold();
            attackerView.PlayBeCounteredHold();

            var mid = ResolveMidStaging(attackerView);
            yield return attackerView.MoveFeetToRoutine(
                mid,
                ResolveMoveSeconds(attackerView.FeetWorldPosition, mid, knockbackSpeed, knockbackSeconds));

            if (playerSkillShotChoreographer.TryBeginCounterMeleeEngage(
                    report.BeatIndex,
                    bodyView,
                    attackerView,
                    bodyEntry.Skill,
                    () => FlushHpFeedback(report.Attacker),
                    out var engageRoutine)
                && engageRoutine != null)
            {
                yield return engageRoutine;
            }
            else
            {
                bodyView.PlayAttackAnimationHold(bodyEntry.Skill);
                var clipLength = bodyView.EstimateSkillClipLength(bodyEntry.Skill);
                var impactDelay = clipLength * skillImpactNormalizedTime;
                if (impactDelay > 0f)
                {
                    yield return new WaitForSeconds(impactDelay);
                }

                FlushHpFeedback(report.Attacker);
                var tail = Mathf.Max(impactHoldSeconds, clipLength * (1f - skillImpactNormalizedTime));
                if (tail > 0f)
                {
                    yield return new WaitForSeconds(tail);
                }
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
            SnapAllHomedUnits();

            foreach (var view in _focusScratch)
            {
                view?.PlayIdleState();
            }

            ActiveAttackers.Clear();
            focusDimmer?.ReleaseImmediate();
        }

        private void SnapAllHomedUnits()
        {
            var attackers = new HashSet<CombatUnit>(ActiveAttackers);
            foreach (var unit in _homePositions.Keys)
            {
                attackers.Add(unit);
            }

            foreach (var attacker in attackers)
            {
                var view = UnitView.FindForUnit(attacker);
                if (view == null)
                {
                    continue;
                }

                var home = _homePositions.TryGetValue(attacker, out var stored)
                    ? stored
                    : ResolveAuthoritativeHome(attacker, view);
                SnapUnitToHome(view, home);
                view.PlayIdleState();
            }

            _homePositions.Clear();
        }

        private static void SnapUnitToHome(UnitView view, Vector3 home)
        {
            if (view == null)
            {
                return;
            }

            view.transform.position = new Vector3(home.x, home.y, view.transform.position.z);
            view.CaptureAnchor();
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

            if (_runtimeSwordAdditive != null)
            {
                Destroy(_runtimeSwordAdditive);
                _runtimeSwordAdditive = null;
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
