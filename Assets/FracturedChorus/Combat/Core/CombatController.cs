using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace FracturedChorus.Combat.Core

{

    public class CombatController : MonoBehaviour

    {

        private const string DeployLabel = "Deploy";

        private const string ExecuteLabel = "Execute";



        [SerializeField] private BeatTimelineUIView timelineView;

        [SerializeField] private SkillPanelUIView skillPanelView;

        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;
        [SerializeField] private CombatResultOverlayUIView resultOverlay;
        [SerializeField] private BlockInputController blockInput;

        private CombatSession _session;

        private BeatTimelineEngine _timeline;

        private CombatMusicController _musicController;

        private BoardDragController _boardDrag;

        private bool _planningPaused;

        private bool _awaitingExecute;

        private bool _introPauseConsumed;

        private CombatUnit _relocateUnit;
        private SkillDefinitionSO _relocateSkill;
        private int _relocateFromBeat = -1;
        private bool _coverMusicLatch;



        public void Initialize(CombatSession session, BeatTimelineEngine timeline,

            BeatTimelineUIView timelineUi, SkillPanelUIView skillPanel, CombatMusicController music = null,

            CombatExecuteOverlayUIView executeOverlayView = null, BoardDragController boardDrag = null)

        {

            _session = session;

            _timeline = timeline;

            timelineView = timelineUi;

            skillPanelView = skillPanel;

            _musicController = music;

            _boardDrag = boardDrag != null ? boardDrag : GetComponent<BoardDragController>();

            if (executeOverlayView != null)

            {

                executeOverlay = executeOverlayView;

            }

            else if (executeOverlay == null)

            {

                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();

            }



            BindDeployButton();



            WireBlockInput();
            EnsureResultOverlay();

            _session.OnPhaseChanged += HandlePhaseChanged;

            _session.OnActionAssigned += HandleActionAssigned;

            _session.OnUnitHpChanged += HandleUnitHpChanged;

            _session.OnEncounterEnded += HandleEncounterEnded;

            _session.Cover.OnChanged += HandleCoverChanged;
            _coverMusicLatch = false;



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



            timelineView?.RefreshAll();

            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);
            ApplySlotFloorVisibilityForCurrentPhase();
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



            BindDeployButton();

            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);

        }



        private void BindDeployButton()

        {

            executeOverlay?.Bind(StartRound);

            executeOverlay?.SetLabel(DeployLabel);

        }



        public void StartRound()

        {

            if (_session == null || _session.Phase != CombatPhase.Planning || _session.IsEncounterOver)

            {

                return;

            }



            if (_session.AllowPlayerReposition)

            {

                StartDeployRound();

                return;

            }



            if (_awaitingExecute)

            {

                StartExecuteSegment();

            }

        }



        private void StartDeployRound()

        {

            _planningPaused = false;

            _awaitingExecute = false;

            SetCoverActivateAllowed(false);

            _session.LockPlayerReposition();

            _session.PrepareTelegraphsForCurrentSegment();

            _boardDrag?.CancelActiveDrag();

            _boardDrag?.SetSlotFloorsVisible(false);

            skillPanelView?.Hide();



            executeOverlay?.SetVisible(false);

            timelineView?.RefreshTelegraphsAndSlots();

            timelineView?.SetPlanningPauseEnabled(!_introPauseConsumed);



            if (_musicController != null && !_musicController.IsPlaying)
            {
                _musicController.PlayBossMusic();
            }

            timelineView?.BeginRoundPlayback(continueFromHold: false);

        }



        private void StartExecuteSegment()

        {

            _awaitingExecute = false;

            _planningPaused = false;

            SetCoverActivateAllowed(false);

            _session.Cover.BeginWindowIfPending();

            skillPanelView?.Hide();

            executeOverlay?.SetVisible(false);

            timelineView?.SetPlanningPauseEnabled(false);



            if (_musicController != null)
            {
                if (_musicController.IsPaused)
                {
                    _musicController.ResumePlayback();
                }
                else if (!_musicController.IsPlaying)
                {
                    _musicController.PlayBossMusic();
                }
            }



            timelineView?.BeginRoundPlayback(continueFromHold: true);

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
            if (_session == null || _session.Phase != CombatPhase.Planning || _session.AllowPlayerReposition)
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
            if (_session == null || _session.AllowPlayerReposition || _session.Timeline == null)
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



        private void UpdateExecuteOverlayVisibility(CombatPhase phase)

        {

            if (_session == null || _session.IsEncounterOver)

            {

                executeOverlay?.SetVisible(false);

                return;

            }



            var showDeploy = phase == CombatPhase.Planning && _session.AllowPlayerReposition;

            var showExecute = phase == CombatPhase.Planning && _awaitingExecute && !_session.AllowPlayerReposition;

            executeOverlay?.SetVisible(showDeploy || showExecute);



            if (showDeploy)

            {

                executeOverlay?.Bind(StartRound);

                executeOverlay?.SetLabel(DeployLabel);

            }

            else if (showExecute)

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



            _introPauseConsumed = true;

            _planningPaused = false;

            _awaitingExecute = true;

            SetCoverActivateAllowed(true);

            _session.EndRoundSegment();

            timelineView?.HoldAtRoundEnd();

            timelineView?.RefreshTelegraphsAndSlots();

            RefreshCoverHud();

            if (TimelineConstants.GetSegmentStartBeat(_session.RoundSegmentIndex) >= TimelineConstants.TotalBeats)
            {
                _awaitingExecute = false;
                executeOverlay?.SetVisible(false);
                return;
            }

            UpdateExecuteOverlayVisibility(_session.Phase);

        }



        public void OnTimelinePlanningPause()

        {

            _planningPaused = true;

            SetCoverActivateAllowed(true);

            executeOverlay?.Bind(ResumeFromPlanningPause);

            executeOverlay?.SetLabel(ExecuteLabel);

            executeOverlay?.SetVisible(true);

        }



        public void ResumeFromPlanningPause()

        {

            if (!_planningPaused)

            {

                return;

            }



            _planningPaused = false;

            SetCoverActivateAllowed(false);

            _session?.Cover.BeginWindowIfPending();

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

            var hud = Object.FindAnyObjectByType<CoverHudView>();

            hud?.Refresh();

        }



        private bool AssignSkillAtScreenPoint(CombatUnit unit, SkillDefinitionSO skill, Vector2 screenPos)

        {

            if (_session == null || _session.Phase != CombatPhase.Planning || _session.AllowPlayerReposition)

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



            timelineView.RefreshBeat(placementBeat);

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, placementBeat, unit))

            {

                if (info.Role == FootprintBeatRole.Active)

                {

                    timelineView.RefreshBeat(info.BeatIndex);

                }

            }

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



            if (phase == CombatPhase.Planning && _session != null && _session.AllowPlayerReposition)

            {

                _awaitingExecute = false;

                skillPanelView?.Hide();

                ApplySlotFloorVisibilityForCurrentPhase();

            }



            if (phase == CombatPhase.Victory || phase == CombatPhase.Defeat)
            {
                ShowResultOverlay(phase == CombatPhase.Victory);
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
            _awaitingExecute = false;
            StopCoverMusicIfNeeded();
            skillPanelView?.Hide();
            timelineView?.StopTimelinePlayback();
            executeOverlay?.SetVisible(false);
            _musicController?.StopMusic();
            var victory = _session != null && _session.Phase == CombatPhase.Victory;
            if (victory)
            {
                CombatEncounterHandoff.SetResult(true);
            }

            ShowResultOverlay(victory);
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



        private void HandleCoverChanged()

        {

            if (_session == null)

            {

                return;

            }



            var active = _session.Cover.IsActive;

            if (active && !_coverMusicLatch)

            {

                _coverMusicLatch = true;

                _musicController?.PlayRenCoverMusic();

                return;

            }



            if (!active && _coverMusicLatch)

            {

                StopCoverMusicIfNeeded();

            }

        }



        private void StopCoverMusicIfNeeded()

        {

            if (!_coverMusicLatch && !(_musicController?.IsCoverMusicActive ?? false))

            {

                return;

            }



            _coverMusicLatch = false;

            _musicController?.StopRenCoverMusic();

        }



        /// <summary>
        /// Player hex floors only during Deploy reposition; enemy floors stay hidden.
        /// </summary>
        private void ApplySlotFloorVisibilityForCurrentPhase()

        {

            var showPlayerFloors = _session != null

                && _session.Phase == CombatPhase.Planning

                && _session.AllowPlayerReposition;

            _boardDrag?.SetSlotFloorsVisible(false, GridSide.Enemy);

            _boardDrag?.SetSlotFloorsVisible(showPlayerFloors, GridSide.Player);

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

