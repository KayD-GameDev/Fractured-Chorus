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
        [SerializeField] private GuardInputController guardInput;

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

            WireGuardInput();

            _session.OnPhaseChanged += HandlePhaseChanged;
            _session.OnActionAssigned += HandleActionAssigned;
            _session.OnUnitHpChanged += HandleUnitHpChanged;
            _session.OnEncounterEnded += HandleEncounterEnded;

            if (timelineView != null)
            {
                timelineView.Bind(_timeline, _session, music, OnTimelinePlanningPause, OnRoundSegmentComplete);
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
            _boardDrag?.CancelActiveDrag();
            skillPanelView?.Hide();

            executeOverlay?.SetVisible(false);
            timelineView?.SetPlanningPauseEnabled(!_introPauseConsumed);

            if (_musicController != null && !_musicController.IsPlaying)
            {
                _musicController.PlayBossMusic();
            }

            timelineView?.BeginRoundPlayback();
        }

        private void StartExecuteSegment()
        {
            _awaitingExecute = false;
            _planningPaused = false;
            skillPanelView?.Hide();
            executeOverlay?.SetVisible(false);
            timelineView?.SetPlanningPauseEnabled(false);

            if (_musicController != null && !_musicController.IsPlaying)
            {
                _musicController.PlayBossMusic();
            }

            timelineView?.BeginRoundPlayback();
        }

        private void WireGuardInput()
        {
            if (guardInput == null)
            {
                guardInput = GetComponent<GuardInputController>();
                if (guardInput == null)
                {
                    guardInput = gameObject.AddComponent<GuardInputController>();
                }
            }

            if (_session != null && guardInput != null)
            {
                _session.GuardHeldSinceQuery = guardInput.HeldThroughBeatSince;
                _session.GuardBlockRemainingDamage = guardInput.BlockedDamageRemaining;
            }
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
            timelineView?.ResetForNextPlanningSegment();
            timelineView?.RefreshAll();
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

        private void MaybeAutoResumeAfterPlanning()
        {
            if (_planningPaused && AllPartyUnitsHaveActions())
            {
                ResumeFromPlanningPause();
            }
        }

        private bool AllPartyUnitsHaveActions()
        {
            if (_session?.Grid == null || _session.Timeline == null)
            {
                return false;
            }

            var anyAlive = false;
            foreach (var unit in _session.Grid.PlayerUnits)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                anyAlive = true;
                if (!UnitHasAssignedAction(unit))
                {
                    return false;
                }
            }

            return anyAlive;
        }

        private bool UnitHasAssignedAction(CombatUnit unit)
        {
            foreach (var entry in _session.Timeline.Agenda)
            {
                if (entry != null && entry.Unit == unit && entry.Skill != null)
                {
                    return true;
                }
            }

            return false;
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

            if (timelineView == null || !timelineView.TryGetBeatAtScreenPoint(screenPos, out var beat))
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
            MaybeAutoResumeAfterPlanning();
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
