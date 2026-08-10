using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace FracturedChorus.Combat.Presentation
{
    public class EncounterDirector : MonoBehaviour
    {
        public static EncounterDirector ActiveInstance { get; private set; }

        public static bool IsPresenting
        {
            get
            {
                if (ActiveInstance != null)
                {
                    return ActiveInstance._busy;
                }

                var found = FindAnyObjectByType<EncounterDirector>();
                return found != null && found._busy;
            }
        }

        [Header("Refs")]
        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private CombatFocusDimmer focusDimmer;
        [SerializeField] private PlayerSkillShotChoreographer playerSkillShotChoreographer;
        [SerializeField] private CharlotteSkillChoreographer charlotteSkillChoreographer;
        [SerializeField] private CodaSkillChoreographer codaSkillChoreographer;
        [SerializeField] private CounterPresentationDriver counterPresentation;
        [SerializeField] private Camera focusCamera;
        [SerializeField] private CanvasGroup hideUiRoot;
        [FormerlySerializedAs("letterboxOverlay")]
        [SerializeField] private EncounterLetterboxOverlay letterboxOverlay;
        [SerializeField] private CombatMusicController musicController;

        [Header("Stage — Player/Enemy R1 C0")]
        [SerializeField] private int stageRow = 1;
        [SerializeField] private int stageColumn;
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;
        [Tooltip("Đẩy player/enemy ra xa thêm trên trục X khi vào stage encounter.")]
        [SerializeField] private float stageSpreadExtra = 0.85f;
        [SerializeField] private float stageMoveSeconds = 0.12f;
        [SerializeField] private float returnMoveSeconds = 0.12f;
        [SerializeField] private float duelSeconds = 1.5f;
        [SerializeField] [Range(0.05f, 0.95f)] private float resolveAtNormalizedTime = 0.4f;
        [Tooltip("Animator speed during encounter duel (1 = normal, 0.7 ≈ chậm nhẹ).")]
        [SerializeField] [Range(0.25f, 1f)] private float encounterAnimSpeed = 0.7f;
        [Tooltip("Animator speed khi Skill 3 / Ultimate — chậm hơn để gồng lực.")]
        [SerializeField] [Range(0.2f, 1f)] private float ultimateEncounterAnimSpeed = 0.42f;
        [SerializeField] private float ultimateAftermathHoldSeconds = 0.55f;

        [Header("Presentation")]
        [SerializeField] private bool hideOtherUnits = true;
        [FormerlySerializedAs("hideUiDuringCutscene")]
        [SerializeField] private bool hideUiDuringEncounter = true;
        [SerializeField] [Range(0f, 1f)] private float focusDimFactor = 0.12f;
        [SerializeField] private float focusFadeSeconds = 0.1f;
        [SerializeField] private bool moveCamera = true;
        [Tooltip("Orthographic size during encounter — larger = zoomed out.")]
        [SerializeField] private float cameraZoomOrtho = 5.2f;
        [SerializeField] private float cameraMoveSeconds = 0.16f;
        [SerializeField] private float swordCastHoldSeconds = 0.14f;

        private CombatSession _session;
        private bool _busy;
        private Coroutine _routine;
        private readonly List<UnitView> _hiddenViews = new();
        private readonly List<UnitView> _focusScratch = new();
        private readonly Dictionary<UnitView, Vector3> _phaseHomeFeet = new();
        private readonly Dictionary<UnitView, float> _animatorSpeedByView = new();
        private Vector3 _cameraHomePos;
        private float _cameraHomeOrtho;
        private bool _cameraCaptured;
        private float _hideUiHomeAlpha = 1f;
        private float _activeEncounterAnimSpeed = 1f;

        public bool IsBusy => _busy;

        public void CapturePhaseHomes()
        {
            _phaseHomeFeet.Clear();
            foreach (var view in FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view == null)
                {
                    continue;
                }

                _phaseHomeFeet[view] = view.FeetWorldPosition;
                view.CaptureAnchor();
            }
        }

        public void RestorePhaseHomes()
        {
            RestoreEncounterAnimSpeed();
            RestoreHidden();
            EnemyStrikeChoreographer.ActiveInstance?.ResetPresentation();

            foreach (var view in FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view == null)
                {
                    continue;
                }

                SnapViewToHome(view);
                if (view.gameObject.activeInHierarchy)
                {
                    view.PlayIdleState();
                }
            }
        }

        public bool TryGetPhaseHomeRoot(UnitView view, out Vector3 rootPosition)
        {
            rootPosition = default;
            if (view == null || !_phaseHomeFeet.TryGetValue(view, out var feet))
            {
                return false;
            }

            var rootToFeet = view.transform.position - view.FeetWorldPosition;
            rootPosition = new Vector3(feet.x + rootToFeet.x, feet.y + rootToFeet.y, view.transform.position.z);
            return true;
        }

        public void Configure(
            CombatSession session,
            BeatTimelineUIView timeline,
            CombatFocusDimmer dimmer,
            PlayerSkillShotChoreographer playerShots,
            CombatMusicController music = null,
            EncounterLetterboxOverlay letterbox = null)
        {
            _session = session;
            if (timeline != null)
            {
                timelineView = timeline;
            }

            if (dimmer != null)
            {
                focusDimmer = dimmer;
            }

            if (playerShots != null)
            {
                playerSkillShotChoreographer = playerShots;
            }

            if (music != null)
            {
                musicController = music;
            }

            if (letterbox != null)
            {
                letterboxOverlay = letterbox;
            }

            ActiveInstance = this;
            EnsureRefs();
        }

        private void OnEnable()
        {
            ActiveInstance = this;
        }

        public bool TryInterceptScanBeat(int beatIndex)
        {
            if (_busy || _session == null || timelineView == null)
            {
                return false;
            }

            if (!_session.TryGetResolvePairAtBeat(beatIndex, out var player, out var enemy))
            {
                return false;
            }

            EnsureRefs();
            _routine = StartCoroutine(RunEncounter(beatIndex, player, enemy));
            return true;
        }

        private void EnsureRefs()
        {
            if (timelineView == null)
            {
                timelineView = FindAnyObjectByType<BeatTimelineUIView>();
            }

            if (focusDimmer == null)
            {
                focusDimmer = GetComponent<CombatFocusDimmer>()
                              ?? FindAnyObjectByType<CombatFocusDimmer>();
            }

            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = GetComponent<PlayerSkillShotChoreographer>()
                                              ?? FindAnyObjectByType<PlayerSkillShotChoreographer>();
            }

            if (focusCamera == null)
            {
                focusCamera = Camera.main;
            }

            if (hideUiRoot == null && timelineView != null)
            {
                hideUiRoot = timelineView.GetComponentInParent<CanvasGroup>();
                if (hideUiRoot == null)
                {
                    var canvas = timelineView.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        hideUiRoot = canvas.GetComponent<CanvasGroup>();
                        if (hideUiRoot == null)
                        {
                            hideUiRoot = canvas.gameObject.AddComponent<CanvasGroup>();
                        }
                    }
                }
            }

            focusDimmer?.Configure(focusDimFactor, focusFadeSeconds);
            EnsureLetterbox();
            CombatImpactFeel.Ensure(transform, focusCamera != null ? focusCamera : Camera.main);
        }

        private void EnsureLetterbox()
        {
            if (letterboxOverlay == null)
            {
                letterboxOverlay = EncounterLetterboxOverlay.EnsureCreated();
            }

            if (musicController == null)
            {
                musicController = FindAnyObjectByType<CombatMusicController>();
            }
        }

        private IEnumerator RunEncounter(int beatIndex, CombatUnit playerUnit, CombatUnit enemyUnit)
        {
            _busy = true;
            timelineView.PauseForEncounter();
            EnsureLetterbox();
            if (letterboxOverlay != null)
            {
                letterboxOverlay.Show(musicController);
            }

            var playerView = UnitView.FindForUnit(playerUnit);
            var enemyView = UnitView.FindForUnit(enemyUnit);
            if (playerView == null || enemyView == null)
            {
                _session.ResolveBeatAtScan(beatIndex);
                CharlotteDomeRingView.SetEncounterHidden(false);
                CharlotteMusicOrbitShieldView.SetEncounterHidden(false);
                FinishEncounter();
                yield break;
            }

            if (_phaseHomeFeet.Count == 0)
            {
                CapturePhaseHomes();
            }

            var playerSkill = ResolvePlayerSkill(beatIndex, playerUnit);
            ApplyEncounterAnimSpeed(playerSkill, playerView, enemyView);

            CharlotteDomeRingView.SetEncounterHidden(true);
            CharlotteMusicOrbitShieldView.SetEncounterHidden(true);
            HideOthers(playerView, enemyView);
            ApplyUiHide(true);
            _focusScratch.Clear();
            _focusScratch.Add(playerView);
            _focusScratch.Add(enemyView);
            focusDimmer?.Focus(_focusScratch);

            ResolveStageFeet(out var playerStage, out var enemyStage);

            if (moveCamera)
            {
                yield return MoveCameraToStage(playerStage, enemyStage);
            }

            playerView.PlayMovingLoop();
            enemyView.PlayMovingLoop();
            yield return playerView.MoveFeetToRoutine(playerStage, stageMoveSeconds);
            yield return enemyView.MoveFeetToRoutine(enemyStage, stageMoveSeconds);

            yield return PlayDuelAndResolve(beatIndex, playerView, enemyView, playerSkill);

            if (IsUltimateSkill(playerSkill) && ultimateAftermathHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(ultimateAftermathHoldSeconds);
            }

            yield return ReturnParticipantsHome(playerView, enemyView);
            RestorePhaseHomes();

            playerView.PlayIdleState();
            enemyView.PlayIdleState();

            focusDimmer?.Release();
            ApplyUiHide(false);
            if (moveCamera)
            {
                yield return RestoreCamera();
            }

            EnsureCharlotteSkillChoreographer();
            charlotteSkillChoreographer?.FlushPendingPartyDome();
            CharlotteDomeRingView.SetEncounterHidden(false);
            CharlotteMusicOrbitShieldView.SetEncounterHidden(false);

            FinishEncounter();
        }

        private IEnumerator ReturnParticipantsHome(UnitView playerView, UnitView enemyView)
        {
            playerView.PlayMovingLoop();
            enemyView.PlayMovingLoop();

            var playerFeet = GetHomeFeet(playerView);
            var enemyFeet = GetHomeFeet(enemyView);
            var seconds = Mathf.Max(0.04f, returnMoveSeconds);

            yield return playerView.MoveFeetToRoutine(playerFeet, seconds);
            yield return enemyView.MoveFeetToRoutine(enemyFeet, seconds);

            SnapViewToHome(playerView);
            SnapViewToHome(enemyView);
        }

        private Vector3 GetHomeFeet(UnitView view)
        {
            if (view != null && _phaseHomeFeet.TryGetValue(view, out var feet))
            {
                return feet;
            }

            return ResolveHomeFeet(view);
        }

        private Vector3 ResolveHomeFeet(UnitView view)
        {
            if (view == null)
            {
                return Vector3.zero;
            }

            var unit = view.Unit;
            if (unit != null && unit.GridPosition.IsValid())
            {
                return HexBoardLayout.GetWorldPosition(unit.GridPosition, sideGap);
            }

            return view.FeetWorldPosition;
        }

        private void SnapViewToHome(UnitView view)
        {
            if (view == null)
            {
                return;
            }

            var feet = GetHomeFeet(view);
            view.SnapFeetTo(feet, view.transform.position.z);
            view.CaptureAnchor();
        }

        private IEnumerator PlayDuelAndResolve(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill)
        {
            var swordCount = ResolveSwordCount(beatIndex);
            if (swordCount > 0)
            {
                yield return PlaySwordEncounterDuel(beatIndex, playerView, enemyView, playerSkill, swordCount);
                yield break;
            }

            var countered = IsBeatFullyCountered(beatIndex);
            if (countered)
            {
                playerView.PlayCounterHold();
                enemyView.PlayBeCounteredHold();
                CharlotteCounterShieldView.TrySpawnFor(
                    playerView,
                    enemyView.FeetWorldPosition,
                    1.5f,
                    transform);
                PresentPerfectNow(playerView.Unit, ResolveNoteTier(beatIndex));
                yield return new WaitForSeconds(0.28f / ResolveActiveAnimSpeed());
                yield return CharlotteCounterShieldView.DismissAllAndWait();
            }

            EnsureCharlotteSkillChoreographer();
            if (charlotteSkillChoreographer != null
                && charlotteSkillChoreographer.Handles(playerSkill, playerView))
            {
                yield return PlayCharlotteResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            EnsureCodaSkillChoreographer();
            if (codaSkillChoreographer != null
                && codaSkillChoreographer.Handles(playerSkill, playerView))
            {
                yield return PlayCodaResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (playerSkillShotChoreographer != null
                && playerSkillShotChoreographer.IsMeleeSkill(playerSkill))
            {
                yield return PlayMeleeResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (playerSkillShotChoreographer != null
                && playerSkillShotChoreographer.IsMultiBulletSkill(playerSkill))
            {
                yield return PlayRenMultiBulletResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (playerSkill != null)
            {
                playerView.PlayAttackAnimationHold(playerSkill);
            }
            else
            {
                playerView.PlayCounterHold();
            }

            if (!countered)
            {
                enemyView.PlayCounterHold();
            }

            if (playerSkillShotChoreographer != null && playerSkill != null
                && !playerSkillShotChoreographer.IsMeleeSkill(playerSkill))
            {
                var resolveFired = false;
                yield return playerSkillShotChoreographer.PlayBulletPresentationForCutsceneRoutine(
                    playerSkill,
                    playerView,
                    ResolveAim(playerView),
                    ResolveAim(enemyView),
                    onImpact: () =>
                    {
                        if (resolveFired)
                        {
                            return;
                        }

                        resolveFired = true;
                        ResolveAndShowHp(beatIndex, playerView, enemyView);
                    });

                if (!resolveFired)
                {
                    ResolveAndShowHp(beatIndex, playerView, enemyView);
                }

                yield break;
            }

            var animSpeed = ResolveActiveAnimSpeed();
            var duel = IsUltimateSkill(playerSkill) ? Mathf.Max(duelSeconds, 2.35f) : duelSeconds;
            var wait = Mathf.Max(0.05f, duel) / animSpeed;
            var impactAt = wait * resolveAtNormalizedTime;
            if (impactAt > 0f)
            {
                yield return new WaitForSeconds(impactAt);
            }

            ResolveAndShowHp(beatIndex, playerView, enemyView);
            var tail = wait - impactAt;
            if (tail > 0f)
            {
                yield return new WaitForSeconds(tail);
            }
        }

        private IEnumerator PlaySwordEncounterDuel(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill,
            int swordCount)
        {
            var countered = IsBeatFullyCountered(beatIndex);
            enemyView.PlayCounterHold();
            if (countered)
            {
                playerView.PlayCounterHold();
            }

            if (swordCastHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(swordCastHoldSeconds / ResolveActiveAnimSpeed());
            }

            var strike = EnemyStrikeChoreographer.ActiveInstance
                         ?? FindAnyObjectByType<EnemyStrikeChoreographer>();
            Coroutine perfectAtContact = null;
            if (countered)
            {
                var contactDelay = Mathf.Max(0.01f, 0.32f * 0.55f);
                if (strike != null)
                {
                    contactDelay = strike.GetProjectileContactDelaySeconds();
                }

                perfectAtContact = StartCoroutine(PresentPerfectAfterDelay(
                    contactDelay,
                    playerView.Unit,
                    ResolveNoteTier(beatIndex)));
            }

            if (strike != null)
            {
                yield return strike.PresentEnemyVolley(
                    enemyView, playerView, swordCount, countered, beatIndex);
            }
            else if (countered)
            {
                yield return CharlotteCounterShieldView.DismissAllAndWait();
            }

            if (perfectAtContact != null)
            {
                yield return perfectAtContact;
            }

            EnsureCharlotteSkillChoreographer();
            if (countered
                && charlotteSkillChoreographer != null
                && charlotteSkillChoreographer.Handles(playerSkill, playerView))
            {
                enemyView.PlayBeCounteredHold();
                yield return PlayCharlotteResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            EnsureCodaSkillChoreographer();
            if (countered
                && codaSkillChoreographer != null
                && codaSkillChoreographer.Handles(playerSkill, playerView))
            {
                enemyView.PlayBeCounteredHold();
                yield return PlayCodaResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (countered
                && playerSkillShotChoreographer != null
                && playerSkillShotChoreographer.IsMeleeSkill(playerSkill))
            {
                enemyView.PlayBeCounteredHold();
                yield return PlayMeleeResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (countered
                && playerSkillShotChoreographer != null
                && playerSkillShotChoreographer.IsMultiBulletSkill(playerSkill))
            {
                enemyView.PlayBeCounteredHold();
                yield return PlayRenMultiBulletResolve(beatIndex, playerView, enemyView, playerSkill);
                yield break;
            }

            if (countered)
            {
                enemyView.PlayBeCounteredHold();
                if (playerSkill != null)
                {
                    playerView.PlayAttackAnimationHold(playerSkill);
                }

                if (playerSkillShotChoreographer != null && playerSkill != null
                    && !playerSkillShotChoreographer.IsMeleeSkill(playerSkill))
                {
                    var resolveFired = false;
                    yield return playerSkillShotChoreographer.PlayBulletPresentationForCutsceneRoutine(
                        playerSkill,
                        playerView,
                        ResolveAim(playerView),
                        ResolveAim(enemyView),
                        onImpact: () =>
                        {
                            if (resolveFired)
                            {
                                return;
                            }

                            resolveFired = true;
                            ResolveAndShowHp(beatIndex, playerView, enemyView);
                        });

                    if (!resolveFired)
                    {
                        ResolveAndShowHp(beatIndex, playerView, enemyView);
                    }

                    yield break;
                }

                var animSpeed = ResolveActiveAnimSpeed();
                var wait = Mathf.Max(0.05f, duelSeconds) / animSpeed;
                var impactAt = wait * resolveAtNormalizedTime;
                if (impactAt > 0f)
                {
                    yield return new WaitForSeconds(impactAt);
                }

                ResolveAndShowHp(beatIndex, playerView, enemyView);
                var tail = wait - impactAt;
                if (tail > 0f)
                {
                    yield return new WaitForSeconds(tail);
                }

                yield break;
            }

            playerView.PlayBeCounteredHold();
            ResolveAndShowHp(beatIndex, playerView, enemyView);
            if (duelSeconds > 0f)
            {
                yield return new WaitForSeconds(duelSeconds * 0.35f / ResolveActiveAnimSpeed());
            }
        }

        private IEnumerator PlayMeleeResolve(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill)
        {
            var resolveFired = false;
            yield return playerSkillShotChoreographer.PlayMeleeEngageRoutine(
                playerView,
                enemyView,
                playerSkill,
                returnHome: false,
                onImpact: () =>
                {
                    if (resolveFired)
                    {
                        return;
                    }

                    resolveFired = true;
                    ResolveAndShowHp(beatIndex, playerView, enemyView);
                });

            if (!resolveFired)
            {
                ResolveAndShowHp(beatIndex, playerView, enemyView);
            }
        }

        private IEnumerator PlayRenMultiBulletResolve(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill)
        {
            if (!IsBeatFullyCountered(beatIndex))
            {
                enemyView.PlayCounterHold();
            }

            var resolveFired = false;
            yield return playerSkillShotChoreographer.PlayBulletPresentationForCutsceneRoutine(
                playerSkill,
                playerView,
                ResolveAim(playerView),
                ResolveAim(enemyView),
                onImpact: () =>
                {
                    if (resolveFired)
                    {
                        return;
                    }

                    resolveFired = true;
                    ResolveAndShowHp(beatIndex, playerView, enemyView);
                });

            if (!resolveFired)
            {
                ResolveAndShowHp(beatIndex, playerView, enemyView);
            }
        }

        private IEnumerator PlayCharlotteResolve(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill)
        {
            EnsureCharlotteSkillChoreographer();
            if (charlotteSkillChoreographer == null)
            {
                yield break;
            }

            var resolveFired = false;
            yield return charlotteSkillChoreographer.PlaySkillRoutine(
                playerView,
                enemyView,
                playerSkill,
                returnHome: false,
                onImpact: () =>
                {
                    if (resolveFired)
                    {
                        return;
                    }

                    resolveFired = true;
                    ResolveAndShowHp(beatIndex, playerView, enemyView);
                });

            if (!resolveFired)
            {
                ResolveAndShowHp(beatIndex, playerView, enemyView);
            }
        }

        private IEnumerator PlayCodaResolve(
            int beatIndex,
            UnitView playerView,
            UnitView enemyView,
            SkillDefinitionSO playerSkill)
        {
            EnsureCodaSkillChoreographer();
            if (codaSkillChoreographer == null)
            {
                yield break;
            }

            var resolveFired = false;
            yield return codaSkillChoreographer.PlaySkillRoutine(
                playerView,
                enemyView,
                playerSkill,
                returnHome: false,
                onImpact: () =>
                {
                    if (resolveFired)
                    {
                        return;
                    }

                    resolveFired = true;
                    ResolveAndShowHp(beatIndex, playerView, enemyView);
                });

            if (!resolveFired)
            {
                ResolveAndShowHp(beatIndex, playerView, enemyView);
            }
        }

        private void EnsureCodaSkillChoreographer()
        {
            if (codaSkillChoreographer != null)
            {
                return;
            }

            codaSkillChoreographer = GetComponent<CodaSkillChoreographer>()
                                     ?? FindAnyObjectByType<CodaSkillChoreographer>();
            if (codaSkillChoreographer == null)
            {
                codaSkillChoreographer = gameObject.AddComponent<CodaSkillChoreographer>();
            }

            codaSkillChoreographer.EnsureDefaults();
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

        private void ResolveAndShowHp(int beatIndex, UnitView playerView, UnitView enemyView)
        {
            _session.ResolveBeatAtScan(beatIndex);
            PlayHpFeedback(playerView);
            PlayHpFeedback(enemyView);
        }

        private int ResolveSwordCount(int beatIndex)
        {
            if (_session?.Timeline == null)
            {
                return 0;
            }

            var telegraphs = _session.Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs == null || telegraphs.Count == 0)
            {
                return 0;
            }

            for (var i = 0; i < telegraphs.Count; i++)
            {
                var telegraph = telegraphs[i];
                if (telegraph?.Unit == null || !telegraph.Unit.IsAlive)
                {
                    continue;
                }

                if (telegraph.HitsRequired > 0)
                {
                    return Mathf.Clamp(telegraph.HitsRequired, 1, 3);
                }

                return Mathf.Clamp((int)telegraph.NoteTier, 1, 3);
            }

            return 0;
        }

        private BossNoteTier ResolveNoteTier(int beatIndex)
        {
            if (_session?.Timeline == null)
            {
                return BossNoteTier.Red;
            }

            var telegraphs = _session.Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs == null || telegraphs.Count == 0 || telegraphs[0] == null)
            {
                return BossNoteTier.Red;
            }

            return telegraphs[0].NoteTier;
        }

        private void EnsureCounterPresentation()
        {
            if (counterPresentation == null)
            {
                counterPresentation = FindAnyObjectByType<CounterPresentationDriver>();
            }
        }

        private IEnumerator PresentPerfectAfterDelay(float delaySeconds, CombatUnit unit, BossNoteTier tier)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            PresentPerfectNow(unit, tier);
        }

        private void PresentPerfectNow(CombatUnit unit, BossNoteTier tier)
        {
            EnsureCounterPresentation();
            counterPresentation?.PresentPerfectInEncounter(unit, tier);
        }

        private void ApplyEncounterAnimSpeed(SkillDefinitionSO skill, params UnitView[] views)
        {
            _animatorSpeedByView.Clear();
            _activeEncounterAnimSpeed = IsUltimateSkill(skill)
                ? Mathf.Clamp(ultimateEncounterAnimSpeed, 0.2f, 1f)
                : Mathf.Clamp(encounterAnimSpeed, 0.25f, 1f);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    continue;
                }

                _animatorSpeedByView[view] = view.AnimatorSpeed;
                view.SetAnimatorSpeed(_activeEncounterAnimSpeed);
            }
        }

        private float ResolveActiveAnimSpeed() =>
            Mathf.Max(0.01f, _activeEncounterAnimSpeed > 0.01f
                ? _activeEncounterAnimSpeed
                : encounterAnimSpeed);

        private static bool IsUltimateSkill(SkillDefinitionSO skill) =>
            skill != null && skill.slotKind == SkillSlotKind.Ultimate;

        private void RestoreEncounterAnimSpeed()
        {
            foreach (var pair in _animatorSpeedByView)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetAnimatorSpeed(pair.Value > 0.01f ? pair.Value : 1f);
                }
            }

            _animatorSpeedByView.Clear();
        }

        private SkillDefinitionSO ResolvePlayerSkill(int beatIndex, CombatUnit playerUnit)
        {
            if (_session?.Timeline?.Agenda == null)
            {
                return null;
            }

            foreach (var entry in _session.Timeline.Agenda)
            {
                if (entry?.Unit != playerUnit || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                foreach (var active in CombatCounterResolver.GetActiveBeatIndices(entry))
                {
                    if (active == beatIndex)
                    {
                        return entry.Skill;
                    }
                }
            }

            return null;
        }

        private bool IsBeatFullyCountered(int beatIndex)
        {
            if (_session?.Timeline == null)
            {
                return false;
            }

            var telegraphs = _session.Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs == null || telegraphs.Count == 0)
            {
                return false;
            }

            foreach (var telegraph in telegraphs)
            {
                if (!CombatCounterResolver.IsTelegraphFullyCountered(telegraph, _session.Timeline))
                {
                    return false;
                }
            }

            return true;
        }

        private void HideOthers(UnitView keepA, UnitView keepB)
        {
            _hiddenViews.Clear();
            if (!hideOtherUnits)
            {
                return;
            }

            foreach (var view in FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                if (view == null || view == keepA || view == keepB || !view.gameObject.activeSelf)
                {
                    continue;
                }

                view.gameObject.SetActive(false);
                _hiddenViews.Add(view);
            }
        }

        private void RestoreHidden()
        {
            for (var i = 0; i < _hiddenViews.Count; i++)
            {
                if (_hiddenViews[i] != null)
                {
                    _hiddenViews[i].gameObject.SetActive(true);
                }
            }

            _hiddenViews.Clear();
        }

        private void ApplyUiHide(bool hide)
        {
            if (!hideUiDuringEncounter || hideUiRoot == null)
            {
                return;
            }

            if (hide)
            {
                _hideUiHomeAlpha = hideUiRoot.alpha;
                hideUiRoot.alpha = 0f;
                hideUiRoot.blocksRaycasts = false;
            }
            else
            {
                hideUiRoot.alpha = _hideUiHomeAlpha;
                hideUiRoot.blocksRaycasts = true;
            }
        }

        private void ResolveStageFeet(out Vector3 playerStage, out Vector3 enemyStage)
        {
            playerStage = HexBoardLayout.GetWorldPosition(
                new GridPosition(GridSide.Player, stageRow, stageColumn), sideGap);
            enemyStage = HexBoardLayout.GetWorldPosition(
                new GridPosition(GridSide.Enemy, stageRow, stageColumn), sideGap);
            var spread = Mathf.Max(0f, stageSpreadExtra) * 0.5f;
            if (spread <= 0f)
            {
                return;
            }

            playerStage.x -= spread;
            enemyStage.x += spread;
        }

        private IEnumerator MoveCameraToStage(Vector3 playerStage, Vector3 enemyStage)
        {
            if (focusCamera == null)
            {
                yield break;
            }

            if (!_cameraCaptured)
            {
                _cameraHomePos = focusCamera.transform.position;
                _cameraHomeOrtho = focusCamera.orthographic
                    ? focusCamera.orthographicSize
                    : focusCamera.fieldOfView;
                _cameraCaptured = true;
            }

            var mid = (playerStage + enemyStage) * 0.5f;
            var target = new Vector3(mid.x, mid.y, _cameraHomePos.z);
            yield return LerpCamera(target, cameraZoomOrtho, cameraMoveSeconds);
        }

        private IEnumerator RestoreCamera()
        {
            if (focusCamera == null || !_cameraCaptured)
            {
                yield break;
            }

            yield return LerpCamera(_cameraHomePos, _cameraHomeOrtho, cameraMoveSeconds);
        }

        private IEnumerator LerpCamera(Vector3 targetPos, float targetOrthoOrFov, float seconds)
        {
            var fromPos = focusCamera.transform.position;
            var fromSize = focusCamera.orthographic
                ? focusCamera.orthographicSize
                : focusCamera.fieldOfView;
            if (seconds <= 0f)
            {
                focusCamera.transform.position = targetPos;
                if (focusCamera.orthographic)
                {
                    focusCamera.orthographicSize = targetOrthoOrFov;
                }
                else
                {
                    focusCamera.fieldOfView = targetOrthoOrFov;
                }

                yield break;
            }

            var t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                var p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
                focusCamera.transform.position = Vector3.Lerp(fromPos, targetPos, p);
                var size = Mathf.Lerp(fromSize, targetOrthoOrFov, p);
                if (focusCamera.orthographic)
                {
                    focusCamera.orthographicSize = size;
                }
                else
                {
                    focusCamera.fieldOfView = size;
                }

                yield return null;
            }
        }

        private void FinishEncounter()
        {
            CombatImpactFeel.ActiveInstance?.CancelAll();
            HideLetterboxSafe();
            _busy = false;
            _routine = null;
            timelineView?.ResumeAfterEncounter();
        }

        private void Abort()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            CombatImpactFeel.ActiveInstance?.CancelAll();
            HideLetterboxSafe();
            RestorePhaseHomes();
            focusDimmer?.ReleaseImmediate();
            ApplyUiHide(false);
            EnsureCharlotteSkillChoreographer();
            charlotteSkillChoreographer?.FlushPendingPartyDome();
            CharlotteDomeRingView.SetEncounterHidden(false);
            CharlotteMusicOrbitShieldView.SetEncounterHidden(false);
            if (focusCamera != null && _cameraCaptured)
            {
                focusCamera.transform.position = _cameraHomePos;
                if (focusCamera.orthographic)
                {
                    focusCamera.orthographicSize = _cameraHomeOrtho;
                }
                else
                {
                    focusCamera.fieldOfView = _cameraHomeOrtho;
                }
            }

            _busy = false;
            if (timelineView != null && timelineView.IsPausedForEncounter)
            {
                timelineView.ResumeAfterEncounter();
            }
        }

        private static Vector3 ResolveAim(UnitView view)
        {
            var feet = view.FeetWorldPosition;
            return new Vector3(feet.x, feet.y + 0.55f, feet.z);
        }

        private static void PlayHpFeedback(UnitView view)
        {
            var unit = view?.Unit;
            if (unit == null || !unit.LastHpChange.ShouldShowFeedback)
            {
                return;
            }

            view.PlayHpFeedback(unit.LastHpChange);
        }

        private void HideLetterboxSafe()
        {
            if (letterboxOverlay == null)
            {
                return;
            }

            letterboxOverlay.Hide();
        }

        private void OnDisable()
        {
            if (_busy)
            {
                Abort();
            }
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }
    }
}
