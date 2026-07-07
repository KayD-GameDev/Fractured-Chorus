using FracturedChorus.Audio;

using FracturedChorus.Combat.Core;

using FracturedChorus.Combat.Timeline;

using FracturedChorus.Combat.Units;

using FracturedChorus.Data;

using FracturedChorus.UI;

using UnityEngine;



namespace FracturedChorus.Combat.Core

{

    public class CombatController : MonoBehaviour

    {

        private const string DeployLabel = "Deploy";

        private const string ExecuteLabel = "Execute";



        [SerializeField] private BeatTimelineUIView timelineView;

        [SerializeField] private SkillPanelUIView skillPanelView;

        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;

        [SerializeField] private BlockInputController blockInput;



        private CombatSession _session;

        private BeatTimelineEngine _timeline;

        private CombatMusicController _musicController;

        private BoardDragController _boardDrag;

        private bool _planningPaused;

        private bool _awaitingExecute;

        private bool _introPauseConsumed;



        public CombatSession Session => _session;



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

                timelineView.Bind(_timeline, _session, music, OnTimelinePlanningPause, OnRoundSegmentComplete, combatSfx);
                timelineView.SetSkillRemoveHandler(RemoveSkillAtBeat);
                timelineView.BindBlockBarriers(_session.BlockBarriers);
                timelineView.SetLaneAvatarClickHandler(OnLaneAvatarClicked);

            }



            if (skillPanelView != null)

            {

                skillPanelView.Bind(_session, AssignSkillAtScreenPoint, PreviewSkillDrop, HideSkillDropPreview);

            }



            timelineView?.RefreshAll();

            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);
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

            _session.LockPlayerReposition();

            _session.PrepareTelegraphsForCurrentSegment();

            _boardDrag?.CancelActiveDrag();

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
                blockInput.Initialize(timelineView, _session.BlockBarriers);
            }
        }

        private void OnLaneAvatarClicked(CombatUnit unit)
        {
            FocusPlayerUnit(unit);
        }

        public void FocusPlayerUnit(CombatUnit unit, UnitView view = null)
        {
            if (_session == null || _session.Phase != CombatPhase.Planning || _session.AllowPlayerReposition)
            {
                return;
            }

            timelineView?.SetSelectedLaneUnit(unit);
            skillPanelView?.ToggleForUnit(unit, view);
        }

        private void RemoveSkillAtBeat(CombatUnit unit, int beatIndex)
        {
            if (_session == null || _session.AllowPlayerReposition)
            {
                return;
            }

            if (!_session.TryRemovePlayerAction(unit, beatIndex))
            {
                return;
            }

            timelineView?.RefreshAll();
            timelineView?.RefreshLaneMarkers();
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

            _session.EndRoundSegment();

            timelineView?.HoldAtRoundEnd();

            timelineView?.RefreshTelegraphsAndSlots();

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

            executeOverlay?.SetVisible(false);

            timelineView?.ResumeRoundPlayback();

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



            if (!_session.PhaseAv.CanAfford(skill.GetAvCost()))

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



            timelineView?.RefreshBeat(beat);

            timelineView?.RefreshLaneMarkers();

            return true;

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

            }



            if (phase == CombatPhase.Victory)

            {

                Debug.Log("[Combat] Victory!");

            }

            else if (phase == CombatPhase.Defeat)

            {

                Debug.Log("[Combat] Defeat!");

            }

        }



        private void HandleActionAssigned(AgendaEntry entry)

        {

            timelineView?.RefreshBeat(entry.BeatIndex);

            timelineView?.RefreshLaneMarkers();

        }



        private void HandleUnitHpChanged(CombatUnit unit)

        {

        }



        private void HandleEncounterEnded()

        {

            _planningPaused = false;

            _awaitingExecute = false;

            skillPanelView?.Hide();

            timelineView?.StopTimelinePlayback();

            executeOverlay?.SetVisible(false);

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

