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
        private const string ResumeLabel = "Continue";

        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private SkillPanelUIView skillPanelView;
        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;
        [SerializeField] private GuardInputController guardInput;

        private CombatSession _session;
        private BeatTimelineEngine _timeline;
        private CombatMusicController _musicController;
        private BoardDragController _boardDrag;
        private CombatUnit _armedUnit;
        private SkillDefinitionSO _armedSkill;
        private bool _planningPaused;

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
                timelineView.Bind(_timeline, _session, OnScanBeatReached, music, OnTimelinePlanningPause, ConfirmPlanning);
            }

            if (skillPanelView != null)
            {
                skillPanelView.Bind(_session, OnSkillArmed, AssignSkillAtScreenPoint, PreviewSkillDrop, HideSkillDropPreview);
                skillPanelView.VisibilityChanged += OnSkillPanelVisibilityChanged;
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

        /// <summary>Bind nút giữa về StartRound và ép nhãn "Deploy" (không phụ thuộc giá trị serialize có thể cũ).</summary>
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

            if (!_session.AllowPlayerReposition)
            {
                return;
            }

            _planningPaused = false;
            _session.LockPlayerReposition();
            _boardDrag?.CancelActiveDrag();
            ClearArmedSkill();
            skillPanelView?.Hide();

            executeOverlay?.SetVisible(false);
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
            var show = phase == CombatPhase.Planning
                       && _session != null
                       && _session.AllowPlayerReposition
                       && !_session.IsEncounterOver;
            executeOverlay?.SetVisible(show);
        }

        private void OnSkillPanelVisibilityChanged(bool visible)
        {
            timelineView?.SetSkillPanelOpen(visible);
        }

        public void ConfirmPlanning()
        {
            ClearArmedSkill();
            _session?.ConfirmPlanningAndExecute();
            timelineView?.RefreshAll();
        }

        /// <summary>Timeline gọi khi scan vừa qua nốt đầu tiên: mở planning cho player set up skill.</summary>
        public void OnTimelinePlanningPause()
        {
            _planningPaused = true;
            executeOverlay?.Bind(ResumeFromPlanningPause);
            executeOverlay?.SetLabel(ResumeLabel);
            executeOverlay?.SetVisible(true);
        }

        /// <summary>Phát tiếp bài nhạc sau intro-pause (bấm Continue hoặc auto khi cả đội đã xếp skill).</summary>
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

        private void OnScanBeatReached(int beatIndex)
        {
            if (_session == null || _session.Phase != CombatPhase.Planning)
            {
                return;
            }

            TryAssignArmedAtBeat(beatIndex);
        }

        private bool OnSkillArmed(CombatUnit unit, SkillDefinitionSO skill)
        {
            if (_session == null || _session.Phase != CombatPhase.Planning || _session.AllowPlayerReposition)
            {
                return false;
            }

            if (!_session.PhaseAv.CanAfford(skill.GetAvCost()))
            {
                return false;
            }

            _armedUnit = unit;
            _armedSkill = skill;
            timelineView?.RefreshPhaseAvLabel();
            return true;
        }

        /// <summary>Kéo-thả skill: gán vào beat dưới con trỏ trên timeline (lane của unit).</summary>
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

            _armedUnit = unit;
            _armedSkill = skill;
            return TryAssignArmedAtBeat(beat);
        }

        private void PreviewSkillDrop(CombatUnit unit, Vector2 screenPos)
        {
            timelineView?.ShowDropGhost(unit, screenPos);
        }

        private void HideSkillDropPreview()
        {
            timelineView?.HideDropGhost();
        }

        private bool TryAssignArmedAtBeat(int beatIndex)
        {
            if (_armedUnit == null || _armedSkill == null || _session == null)
            {
                return false;
            }

            if (!_session.TryAssignPlayerAction(_armedUnit, _armedSkill, beatIndex))
            {
                return false;
            }

            ClearArmedSkill();
            timelineView?.RefreshBeat(beatIndex);
            timelineView?.RefreshLaneMarkers();
            return true;
        }

        private void ClearArmedSkill()
        {
            _armedUnit = null;
            _armedSkill = null;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            timelineView?.SetPhase(phase);
            timelineView?.RefreshAll();
            UpdateExecuteOverlayVisibility(phase);

            if (phase == CombatPhase.Planning && _session != null && _session.AllowPlayerReposition)
            {
                ClearArmedSkill();
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
            // PartyMemberCardView listens to CombatUnit.OnHpChanged directly.
        }

        private void HandleEncounterEnded()
        {
            _planningPaused = false;
            ClearArmedSkill();
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

            if (skillPanelView != null)
            {
                skillPanelView.VisibilityChanged -= OnSkillPanelVisibilityChanged;
            }
        }
    }
}
