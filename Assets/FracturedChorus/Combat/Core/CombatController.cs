using System;
using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Formation;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace FracturedChorus.Combat.Core

{

    public class CombatController : MonoBehaviour

    {

        private const string ExecuteLabel = "Execute";



        [SerializeField] private BeatTimelineUIView timelineView;

        [SerializeField] private SkillPanelUIView skillPanelView;

        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;
        [SerializeField] private CombatResultOverlayUIView resultOverlay;
        [SerializeField] private BlockInputController blockInput;
        [SerializeField] private DeployFormationHintView deployFormationHint;

        private CombatSession _session;
        private bool _combatTutorialStarted;
        private string _activeEncounterId;

        private BeatTimelineEngine _timeline;

        private CombatMusicController _musicController;
        private CombatSfxController _combatSfx;

        private BoardDragController _boardDrag;

        private bool _planningPaused;

        private bool _deployAnnounced;

        private CombatUnit _relocateUnit;
        private SkillDefinitionSO _relocateSkill;
        private int _relocateFromBeat = -1;
        private Coroutine _segmentCompleteRoutine;
        private Coroutine _encounterEndRoutine;
        private Coroutine _combatIntroRoutine;
        private bool _pendingEncounterResult;



        public void SetActiveEncounter(string encounterId)
        {
            _activeEncounterId = encounterId;
        }

        public void Initialize(CombatSession session, BeatTimelineEngine timeline,

            BeatTimelineUIView timelineUi, SkillPanelUIView skillPanel, CombatMusicController music = null,

            CombatExecuteOverlayUIView executeOverlayView = null, BoardDragController boardDrag = null)

        {

            _session = session;

            _timeline = timeline;

            timelineView = timelineUi;

            skillPanelView = skillPanel;

            _musicController = music;
            _combatSfx = music != null ? music.GetComponent<CombatSfxController>() : null;
            if (_combatSfx == null)
            {
                _combatSfx = FindAnyObjectByType<CombatSfxController>();
            }

            _boardDrag = boardDrag != null ? boardDrag : GetComponent<BoardDragController>();

            if (executeOverlayView != null)

            {

                executeOverlay = executeOverlayView;

            }

            else if (executeOverlay == null)

            {

                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();

            }



            BindExecuteButton();



            WireBlockInput();
            EnsureResultOverlay();
            EnsureDeployFormationHint();

            _session.OnPhaseChanged += HandlePhaseChanged;

            _session.OnActionAssigned += HandleActionAssigned;

            _session.OnUnitHpChanged += HandleUnitHpChanged;

            _session.OnEncounterEnded += HandleEncounterEnded;




            if (timelineView != null)

            {

                CombatSfxController combatSfx = null;
                if (music != null)
                {
                    combatSfx = music.GetComponent<CombatSfxController>();
                }

                var presentation = GetComponent<CounterPresentationDriver>();
                if (presentation == null)
                {
                    presentation = FindAnyObjectByType<CounterPresentationDriver>();
                }

                presentation?.Configure(combatSfx, timelineView);
                timelineView.Bind(_timeline, _session, music, OnTimelinePlanningPause, OnRoundSegmentComplete, combatSfx,
                    presentation);
                timelineView.SetSkillRelocateHandlers(BeginRelocateSkill, UpdateRelocateSkill, EndRelocateSkill);
                timelineView.BindBlockBarriers(_session.BlockBarriers);
                timelineView.SetLaneAvatarClickHandler(OnLaneAvatarClicked);

            }



            if (skillPanelView != null)

            {

                skillPanelView.Bind(
                    _session,
                    AssignSkillAtScreenPoint,
                    PreviewSkillDrop,
                    HideSkillDropPreview,
                    () => timelineView != null && timelineView.IsPlaybackActive);

            }

            _boardDrag?.SetSkillPanelOpenPredicate(
                () => skillPanelView == null || skillPanelView.CanOpenSkillPanelNow());
            _boardDrag?.SetDeployCellClickHandler(OpenFormationHintFromDeployCell);

            timelineView?.RefreshAll();

            StartCombatIntro();
        }

        /// <summary>
        /// Fight start: full music + scan for CombatIntroDurationSec, then duck into Planning.
        /// Mid-fight planning windows skip this intro.
        /// </summary>
        private void StartCombatIntro()
        {
            if (_musicController != null && !_musicController.IsPlaying)
            {
                _musicController.PlayBossMusic();
            }

            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);
            ApplySlotFloorVisibilityForCurrentPhase();
            RefreshDeployFormationHint();
            _session?.SetTimelineRunning(true);

            if (timelineView != null)
            {
                timelineView.BeginIntroPlayback(
                    TimelineConstants.CombatIntroDurationSec,
                    OnCombatIntroComplete);
                return;
            }

            if (_combatIntroRoutine != null)
            {
                StopCoroutine(_combatIntroRoutine);
            }

            _combatIntroRoutine = StartCoroutine(CombatIntroFallbackRoutine());
        }

        private IEnumerator CombatIntroFallbackRoutine()
        {
            var wait = Mathf.Max(0f, TimelineConstants.CombatIntroDurationSec);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            OnCombatIntroComplete();
            _combatIntroRoutine = null;
        }

        private void OnCombatIntroComplete()
        {
            if (_session == null || _session.IsEncounterOver)
            {
                return;
            }

            _session.EndCombatIntro();
            _session.SetTimelineRunning(false);
            _musicController?.EnterPlanningDuck();
            PlayPlanningTransitionSfx();
            timelineView?.RefreshTelegraphsAndSlots();
            UpdateExecuteOverlayVisibility(_session.Phase);
            ApplySlotFloorVisibilityForCurrentPhase();
            RefreshDeployFormationHint();
            TryStartCombatTutorial();
        }

        private void PlayPlanningTransitionSfx()
        {
            EnsureCombatSfx()?.PlayPlanningTransition();
        }

        private void PlaySkillPlaceSfx()
        {
            EnsureCombatSfx()?.PlaySkillPlace();
        }

        private CombatSfxController EnsureCombatSfx()
        {
            if (_combatSfx == null)
            {
                _combatSfx = _musicController != null
                    ? _musicController.GetComponent<CombatSfxController>()
                    : FindAnyObjectByType<CombatSfxController>();
            }

            return _combatSfx;
        }

        private void Start()

        {

            if (_boardDrag == null)

            {

                _boardDrag = GetComponent<BoardDragController>();

            }



            if (executeOverlay == null)

            {

                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();

            }



            BindExecuteButton();

            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);

        }



        private void BindExecuteButton()

        {

            executeOverlay?.Bind(StartRound);

            executeOverlay?.SetLabel(ExecuteLabel);

        }



        public void StartRound()

        {

            if (_session == null || !_session.IsPlanningWindowOpen)

            {

                return;

            }

            StartExecuteSegment();
        }

        public event Action PlayerDeployed;

        private void StartExecuteSegment()

        {

            var firstSegment = !_deployAnnounced;

            _planningPaused = false;

            _session.SetTimelineRunning(true);

            SetCoverActivateAllowed(false);

            _session.Cover.BeginWindowIfPending();

            deployFormationHint?.Hide();

            _boardDrag?.CancelActiveDrag();

            _boardDrag?.SetSlotFloorsVisible(false);

            skillPanelView?.Hide();

            executeOverlay?.SetVisible(false);

            timelineView?.RefreshTelegraphsAndSlots();



            if (_musicController != null)
            {
                if (!_musicController.IsPlaying)
                {
                    _musicController.PlayBossMusic();
                }

                _musicController.ExitPlanningDuck();
            }

            if (timelineView != null && timelineView.IsPausedForPlanning)
            {
                timelineView.ResumeRoundPlayback();
            }
            else
            {
                timelineView?.BeginRoundPlayback(continueFromHold: !firstSegment);
            }

            if (firstSegment)
            {
                _deployAnnounced = true;
                PlayerDeployed?.Invoke();
            }

        }



        private void WireBlockInput()
        {
            if (blockInput == null)
            {
                blockInput = GetComponent<BlockInputController>();
                if (blockInput == null)
                {
                    blockInput = gameObject.AddComponent<BlockInputController>();
                }
            }

            if (timelineView != null && _session != null && blockInput != null)
            {
                CombatSfxController combatSfx = null;
                if (_musicController != null)
                {
                    combatSfx = _musicController.GetComponent<CombatSfxController>();
                }

                if (combatSfx == null)
                {
                    combatSfx = FindAnyObjectByType<CombatSfxController>();
                }

                blockInput.Initialize(timelineView, _session.BlockBarriers, _timeline, combatSfx);
            }
        }

        private void OnLaneAvatarClicked(CombatUnit unit)
        {
            FocusPlayerUnit(unit, UnitView.FindForUnit(unit));
        }

        public void FocusPlayerUnit(CombatUnit unit, UnitView view = null)
        {
            if (_session == null || !_session.IsPlanningWindowOpen)
            {
                return;
            }

            if (timelineView != null && timelineView.IsPlaybackActive)
            {
                return;
            }

            if (view == null)
            {
                view = UnitView.FindForUnit(unit);
            }

            timelineView?.SetSelectedLaneUnit(unit);
            skillPanelView?.ToggleForUnit(unit, view);
        }

        private bool BeginRelocateSkill(CombatUnit unit, int beatIndex)
        {
            if (_session == null || !_session.IsPlanningWindowOpen || _session.Timeline == null)
            {
                return false;
            }

            var entry = _session.Timeline.FindPlayerEntry(unit, beatIndex);
            if (entry?.Skill == null)
            {
                return false;
            }

            _relocateUnit = unit;
            _relocateSkill = entry.Skill;
            _relocateFromBeat = beatIndex;

            if (!_session.TryRemovePlayerAction(unit, beatIndex))
            {
                ClearRelocateState();
                return false;
            }

            RefreshBeatsForSkillFootprint(unit, entry.Skill, beatIndex);
            timelineView?.PrepareLaneMarkerRelocate(unit, beatIndex);
            timelineView?.SoftHideFootprintsForRelocate(unit);
            return true;
        }

        private void UpdateRelocateSkill(Vector2 screenPos)
        {
            if (_relocateSkill == null || timelineView == null)
            {
                return;
            }

            if (timelineView.IsScreenPointInViewport(screenPos))
            {
                timelineView.ShowDropGhost(_relocateUnit, _relocateSkill, screenPos);
            }
            else
            {
                timelineView.HideDropGhost();
            }
        }

        private void EndRelocateSkill(Vector2 screenPos)
        {
            if (_relocateSkill == null)
            {
                timelineView?.HideDropGhost();
                return;
            }

            var unit = _relocateUnit;
            var skill = _relocateSkill;
            var fromBeat = _relocateFromBeat;

            timelineView?.HideDropGhost();
            timelineView?.ClearLaneMarkerRelocatePrepare();

            // Thả trong viewport + beat hợp lệ → đặt lại vị trí mới.
            if (timelineView != null && timelineView.IsScreenPointInViewport(screenPos)
                && timelineView.TryGetPlacementBeatAtScreenPoint(screenPos, skill, out var beat)
                && _session.TryAssignPlayerAction(unit, skill, beat))
            {
                PlaySkillPlaceSfx();
                ClearRelocateState();
                RefreshBeatsForSkillFootprint(unit, skill, beat);
                timelineView.RefreshLaneMarkers();
                return;
            }

            // Kéo ra ngoài timeline → xóa skill (đã remove lúc BeginRelocate).
            if (timelineView == null || !timelineView.IsScreenPointInViewport(screenPos))
            {
                ClearRelocateState();
                RefreshBeatsForSkillFootprint(unit, skill, fromBeat);
                timelineView?.RefreshAll();
                timelineView?.RefreshLaneMarkers();
                return;
            }

            // Trong viewport nhưng không đặt được → trả về beat cũ.
            if (!_session.TryAssignPlayerAction(unit, skill, fromBeat))
            {
                var fallback = _session.Timeline != null
                    ? _session.Timeline.FindFirstAssignableBeat(unit, skill)
                    : -1;
                if (fallback < 0 || !_session.TryAssignPlayerAction(unit, skill, fallback))
                {
                    Debug.LogError(
                        $"[Combat] Relocate restore failed for {unit?.DisplayName} {skill?.displayName} fromBeat={fromBeat}");
                }
                else
                {
                    RefreshBeatsForSkillFootprint(unit, skill, fallback);
                }
            }
            else
            {
                RefreshBeatsForSkillFootprint(unit, skill, fromBeat);
            }

            ClearRelocateState();
            timelineView?.RefreshAll();
            timelineView?.RefreshLaneMarkers();
        }

        private void ClearRelocateState()
        {
            timelineView?.HideDropGhost();
            timelineView?.ClearLaneMarkerRelocatePrepare();
            _relocateUnit = null;
            _relocateSkill = null;
            _relocateFromBeat = -1;
        }



        public void RefreshExecuteOverlayVisibility()
        {
            if (_session == null)
            {
                executeOverlay?.SetVisible(false);
                deployFormationHint?.Hide();
                return;
            }

            UpdateExecuteOverlayVisibility(_session.Phase);
            RefreshDeployFormationHint();
        }

        private void UpdateExecuteOverlayVisibility(CombatPhase phase)

        {

            if (_session == null || _session.IsEncounterOver)

            {

                executeOverlay?.SetVisible(false);

                return;

            }



            var coachBlocking = TutorialCoachView.FindAnyVisible();
            var showExecute = !coachBlocking && _session.IsPlanningWindowOpen;

            executeOverlay?.SetVisible(showExecute);

            if (showExecute)

            {

                executeOverlay?.Bind(StartRound);

                executeOverlay?.SetLabel(ExecuteLabel);

            }

        }



        public void OnRoundSegmentComplete()
        {
            if (_session == null || _session.IsEncounterOver)
            {
                return;
            }

            if (_segmentCompleteRoutine != null)
            {
                StopCoroutine(_segmentCompleteRoutine);
            }

            _segmentCompleteRoutine = StartCoroutine(CompleteRoundSegmentAfterPresentations());
        }

        private IEnumerator CompleteRoundSegmentAfterPresentations()
        {
            yield return WaitForStrikePresentations();

            if (_session == null || _session.IsEncounterOver)
            {
                _segmentCompleteRoutine = null;
                yield break;
            }

            _musicController?.EnterPlanningDuck();
            _planningPaused = false;
            SetCoverActivateAllowed(true);
            _session.EndRoundSegment();
            timelineView?.HoldAtRoundEnd();
            timelineView?.RefreshTelegraphsAndSlots();
            RefreshCoverHud();

            if (TimelineConstants.GetSegmentStartBeat(_session.RoundSegmentIndex) >= TimelineConstants.TotalBeats)
            {
                executeOverlay?.SetVisible(false);
                _segmentCompleteRoutine = null;
                yield break;
            }

            UpdateExecuteOverlayVisibility(_session.Phase);
            PlayPlanningTransitionSfx();
            RefreshDeployFormationHint();
            _segmentCompleteRoutine = null;
        }

        /// <summary>
        /// Wait until end-of-phase counter / damage / strike animations finish before opening the next Planning.
        /// </summary>
        private IEnumerator WaitForStrikePresentations()
        {
            yield return null;

            const float timeoutSec = 10f;
            var deadline = Time.unscaledTime + timeoutSec;

            while (Time.unscaledTime < deadline)
            {
                var choreographer = EnemyStrikeChoreographer.ActiveInstance;
                if (choreographer == null)
                {
                    choreographer = FindAnyObjectByType<EnemyStrikeChoreographer>();
                }

                if (choreographer == null || !choreographer.IsBusy)
                {
                    yield return null;
                    choreographer = EnemyStrikeChoreographer.ActiveInstance;
                    if (choreographer == null)
                    {
                        choreographer = FindAnyObjectByType<EnemyStrikeChoreographer>();
                    }

                    if (choreographer == null || !choreographer.IsBusy)
                    {
                        yield break;
                    }
                }

                yield return null;
            }
        }



        public void OnTimelinePlanningPause()
        {
            _planningPaused = true;
            _session?.SetTimelineRunning(false);
            SetCoverActivateAllowed(true);
            executeOverlay?.Bind(ResumeFromPlanningPause);
            executeOverlay?.SetLabel(ExecuteLabel);
            executeOverlay?.SetVisible(true);
            PlayPlanningTransitionSfx();
            RefreshDeployFormationHint();
        }



        public void ResumeFromPlanningPause()

        {

            if (!_planningPaused)

            {

                return;

            }

            _planningPaused = false;

            _session?.SetTimelineRunning(true);

            SetCoverActivateAllowed(false);

            _session?.Cover.BeginWindowIfPending();

            skillPanelView?.Hide();

            executeOverlay?.SetVisible(false);

            timelineView?.ResumeRoundPlayback();
        }



        private void SetCoverActivateAllowed(bool allowed)

        {

            if (_session == null)

            {

                return;

            }



            _session.AllowCoverActivate = allowed;

            RefreshCoverHud();

        }



        private static void RefreshCoverHud()

        {

            var hud = UnityEngine.Object.FindAnyObjectByType<CoverHudView>();

            hud?.Refresh();

        }



        private bool AssignSkillAtScreenPoint(CombatUnit unit, SkillDefinitionSO skill, Vector2 screenPos)

        {

            if (_session == null || !_session.IsPlanningWindowOpen)

            {

                return false;

            }



            if (unit == null || skill == null)

            {

                return false;

            }

            if (timelineView == null || !timelineView.TryGetPlacementBeatAtScreenPoint(screenPos, skill, out var beat))

            {

                return false;

            }

            if (!_session.TryAssignPlayerAction(unit, skill, beat))
            {
                return false;
            }

            PlaySkillPlaceSfx();
            RefreshBeatsForSkillFootprint(unit, skill, beat);

            timelineView?.RefreshLaneMarkers();

            return true;

        }



        private void RefreshBeatsForSkillFootprint(CombatUnit unit, SkillDefinitionSO skill, int placementBeat)
        {
            if (timelineView == null || skill == null)
            {
                return;
            }

            var beats = new System.Collections.Generic.HashSet<int> { placementBeat };
            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, placementBeat, unit))
            {
                if (info.Role == FootprintBeatRole.Active)
                {
                    beats.Add(info.BeatIndex);
                }
            }

            timelineView.RefreshBeatsAndBossNotes(beats);
        }



        private void PreviewSkillDrop(CombatUnit unit, SkillDefinitionSO skill, Vector2 screenPos)

        {

            timelineView?.ShowDropGhost(unit, skill, screenPos);

        }



        private void HideSkillDropPreview()

        {

            timelineView?.HideDropGhost();

        }



        private void HandlePhaseChanged(CombatPhase phase)

        {

            timelineView?.SetPhase(phase);

            timelineView?.RefreshAll();

            UpdateExecuteOverlayVisibility(phase);



            if (_session != null && _session.IsPlanningWindowOpen)

            {

                skillPanelView?.Hide();

                ApplySlotFloorVisibilityForCurrentPhase();
                RefreshDeployFormationHint();
                TryStartCombatTutorial();

            }
            else
            {
                deployFormationHint?.Hide();
            }



            if (phase == CombatPhase.Victory || phase == CombatPhase.Defeat)
            {
                executeOverlay?.SetVisible(false);
            }
        }



        private void HandleActionAssigned(AgendaEntry entry)

        {

            timelineView?.RefreshBeat(entry.BeatIndex);

            timelineView?.RefreshLaneMarkers();

        }



        private void HandleUnitHpChanged(CombatUnit unit)
        {
            if (unit == null || !unit.LastHpChange.ShouldShowFeedback)
            {
                return;
            }

            if (EnemyStrikeChoreographer.TryDeferHpFeedback(unit, unit.LastHpChange))
            {
                return;
            }

            var view = UnitView.FindForUnit(unit);
            if (view != null)
            {
                view.PlayHpFeedback(unit.LastHpChange);
                return;
            }

            var heal = unit.LastHpChange.Kind == HpChangeKind.Heal;
            var cam = Camera.main;
            var fallback = cam != null
                ? cam.ViewportToWorldPoint(new Vector3(0.5f, 0.55f, 10f))
                : Vector3.zero;
            DamageNumberPopupView.Spawn(
                fallback,
                unit.LastHpChange.Amount,
                heal,
                unit.LastHpChange.IsCritical);
            Debug.LogWarning(
                $"[DamageNumbers] No UnitView for {unit.DisplayName} — spawned at fallback.");
        }



        private void HandleEncounterEnded()
        {
            _planningPaused = false;
            skillPanelView?.Hide();
            timelineView?.StopTimelinePlayback();
            executeOverlay?.SetVisible(false);
            _musicController?.StopMusic();
            var victory = _session != null && _session.Phase == CombatPhase.Victory;
            if (victory)
            {
                CombatEncounterHandoff.SetResult(true);
            }

            if (_encounterEndRoutine != null)
            {
                StopCoroutine(_encounterEndRoutine);
            }

            _pendingEncounterResult = true;
            _encounterEndRoutine = StartCoroutine(ShowResultAfterPresentations(victory));
        }

        private IEnumerator ShowResultAfterPresentations(bool victory)
        {
            yield return WaitForStrikePresentations();
            if (!_pendingEncounterResult)
            {
                _encounterEndRoutine = null;
                yield break;
            }

            _pendingEncounterResult = false;
            ShowResultOverlay(victory);
            _encounterEndRoutine = null;
        }

        private void EnsureResultOverlay()
        {
            if (resultOverlay != null)
            {
                resultOverlay.Bind(OnResultContinue, OnResultRetry);
                return;
            }

            resultOverlay = FindAnyObjectByType<CombatResultOverlayUIView>();
            if (resultOverlay == null)
            {
                Canvas canvas = null;
                if (executeOverlay != null)
                {
                    canvas = executeOverlay.GetComponentInParent<Canvas>();
                }

                if (canvas == null && timelineView != null)
                {
                    canvas = timelineView.GetComponentInParent<Canvas>();
                }

                if (canvas == null)
                {
                    canvas = FindAnyObjectByType<Canvas>();
                }

                if (canvas != null)
                {
                    resultOverlay = CombatResultOverlayUIView.EnsureOnCanvas(canvas.transform);
                }
            }

            resultOverlay?.Bind(OnResultContinue, OnResultRetry);
        }

        private void ShowResultOverlay(bool victory)
        {
            EnsureResultOverlay();
            if (resultOverlay == null)
            {
                Debug.LogWarning("[Combat] Result overlay missing — cannot show VICTORY/DEFEAT UI.");
                return;
            }

            var reward = victory ? CombatEncounterHandoff.PendingRewardSummary : null;
            if (victory && string.IsNullOrEmpty(reward))
            {
                CombatEncounterHandoff.SetResult(true);
                reward = CombatEncounterHandoff.PendingRewardSummary;
            }

            resultOverlay.Show(victory, reward);
        }

        private void OnResultContinue()
        {
            var victory = _session != null && _session.Phase == CombatPhase.Victory;
            if (victory
                && (EncounterCatalog.IsTutorial(_activeEncounterId)
                    || CombatPrototypeBootstrap.IsCombatTutorialSceneStatic()))
            {
                ExitTutorialToRunMap();
                return;
            }

            if (victory)
            {
                PartyRunHpStore.CaptureFromSession(_session);
            }
            else
            {
                PartyRunHpStore.RestoreFullAtCamp();
            }

            CombatEncounterHandoff.SetResult(victory);
            if (victory)
            {
                CadenceMapController.MarkBossVictoryPending();
            }

            var sceneName = string.IsNullOrWhiteSpace(CombatEncounterHandoff.ReturnSceneName)
                ? RunMapSceneCatalog.RunMapPrototype
                : CombatEncounterHandoff.ReturnSceneName;
            if (!RunMapSceneLoader.LoadByName(sceneName))
            {
                Debug.LogError($"[Combat] Failed to load return scene '{sceneName}'.");
            }
        }

        private void OnResultRetry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitTutorialToRunMap()
        {
            PartyRunHpStore.CaptureFromSession(_session);
            CombatEncounterHandoff.SetResult(true);
            var sceneName = string.IsNullOrWhiteSpace(CombatEncounterHandoff.ReturnSceneName)
                ? RunMapSceneCatalog.RunMapPrototype
                : CombatEncounterHandoff.ReturnSceneName;
            if (string.Equals(sceneName, RunMapSceneCatalog.CampusHub, StringComparison.OrdinalIgnoreCase))
            {
                sceneName = RunMapSceneCatalog.RunMapPrototype;
            }

            if (!RunMapSceneLoader.LoadByName(sceneName))
            {
                Debug.LogError($"[Combat] Tutorial exit failed to load '{sceneName}'.");
            }
        }






        /// <summary>
        /// Player hex floors show through every planning window; enemy floors stay hidden.
        /// </summary>
        private void ApplySlotFloorVisibilityForCurrentPhase()

        {

            var showPlayerFloors = _session != null && _session.IsPlanningWindowOpen;

            _boardDrag?.SetSlotFloorsVisible(false, GridSide.Enemy);

            _boardDrag?.SetSlotFloorsVisible(showPlayerFloors, GridSide.Player);

        }



        private void EnsureDeployFormationHint()
        {
            if (deployFormationHint != null)
            {
                return;
            }

            deployFormationHint = FindAnyObjectByType<DeployFormationHintView>();
            if (deployFormationHint != null)
            {
                return;
            }

            Canvas canvas = null;
            if (executeOverlay != null)
            {
                canvas = executeOverlay.GetComponentInParent<Canvas>();
            }

            if (canvas == null && timelineView != null)
            {
                canvas = timelineView.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            if (canvas != null)
            {
                deployFormationHint = DeployFormationHintView.EnsureOnCanvas(canvas.transform);
            }
        }

        private void RefreshDeployFormationHint()
        {
            EnsureDeployFormationHint();
            if (deployFormationHint == null)
            {
                return;
            }

            if (_session == null
                || !_session.IsPlanningWindowOpen
                || TutorialDirector.SuppressFormationHint)
            {
                deployFormationHint.Hide();
            }
        }

        private void OpenFormationHintFromDeployCell()
        {
            if (_session == null
                || !_session.IsPlanningWindowOpen
                || TutorialDirector.SuppressFormationHint)
            {
                return;
            }

            EnsureDeployFormationHint();
            deployFormationHint?.ShowForDeploy(BossFormationRuntime.Active);
        }

        private void TryStartCombatTutorial()
        {
            if (_combatTutorialStarted || _session == null || !_session.IsPlanningWindowOpen)
            {
                return;
            }

            _combatTutorialStarted = true;
            if (EncounterCatalog.IsTutorial(_activeEncounterId)
                || CombatPrototypeBootstrap.IsCombatTutorialSceneStatic())
            {
                FindAnyObjectByType<CombatFocusDimmer>()?.ReleaseImmediate();
                TutorialDirector.Ensure().StartCadenceIntroTrack();
                return;
            }

            TutorialDirector.Ensure().StartCombatTrack();
        }

        private void OnDestroy()

        {

            if (_session == null)

            {

                return;

            }



            _session.OnPhaseChanged -= HandlePhaseChanged;

            _session.OnActionAssigned -= HandleActionAssigned;

            _session.OnUnitHpChanged -= HandleUnitHpChanged;

            _session.OnEncounterEnded -= HandleEncounterEnded;

        }

    }

}

