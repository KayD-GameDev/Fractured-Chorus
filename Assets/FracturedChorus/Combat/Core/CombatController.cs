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
        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private SkillPanelUIView skillPanelView;
        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;

        private CombatSession _session;
        private BeatTimelineEngine _timeline;
        private CombatMusicController _musicController;
        private BoardDragController _boardDrag;
        private CombatUnit _armedUnit;
        private SkillDefinitionSO _armedSkill;

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

            executeOverlay?.Bind(StartRound);

            _session.OnPhaseChanged += HandlePhaseChanged;
            _session.OnActionAssigned += HandleActionAssigned;
            _session.OnUnitHpChanged += HandleUnitHpChanged;
            _session.OnEncounterEnded += HandleEncounterEnded;

            if (timelineView != null)
            {
                timelineView.Bind(_timeline, _session, OnScanBeatReached, music);
            }

            if (skillPanelView != null)
            {
                skillPanelView.Bind(_session, OnSkillArmed);
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

            executeOverlay?.Bind(StartRound);
            UpdateExecuteOverlayVisibility(_session?.Phase ?? CombatPhase.Planning);
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
            timelineView?.RefreshAll();
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
            timelineView?.RefreshAll();
        }

        private void HandleUnitHpChanged(CombatUnit unit)
        {
            // UnitView listens directly; hook for future global HUD.
        }

        private void HandleEncounterEnded()
        {
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
