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

        private CombatSession _session;
        private BeatTimelineEngine _timeline;
        private CombatUnit _armedUnit;
        private SkillDefinitionSO _armedSkill;

        public CombatSession Session => _session;

        public void Initialize(CombatSession session, BeatTimelineEngine timeline,
            BeatTimelineUIView timelineUi, SkillPanelUIView skillPanel, CombatMusicController music = null)
        {
            _session = session;
            _timeline = timeline;
            timelineView = timelineUi;
            skillPanelView = skillPanel;

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
            if (_session == null || _session.Phase != CombatPhase.Planning)
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
