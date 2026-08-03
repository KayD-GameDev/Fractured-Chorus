using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CharlotteVfxChoreographer : MonoBehaviour
    {
        [SerializeField] private float aimHeightOffset = 0.45f;
        [SerializeField] private Transform vfxParent;

        private CombatSession _session;

        public void Configure(CombatSession session)
        {
            Unsubscribe();
            _session = session;
            if (_session == null)
            {
                return;
            }

            _session.OnPlayerSkillResolved += HandlePlayerSkillResolved;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.OnPlayerSkillResolved -= HandlePlayerSkillResolved;
            }

            _session = null;
        }

        private void HandlePlayerSkillResolved(PlayerSkillResolvedReport report)
        {
            if (!report.IsValid || !IsCharlotteSkill(report.Skill))
            {
                return;
            }

            var sourceView = UnitView.FindForUnit(report.Source);
            if (sourceView == null)
            {
                return;
            }

            var parent = vfxParent != null ? vfxParent : transform;
            var impact = sourceView.FeetWorldPosition + Vector3.right * 1.6f + Vector3.up * aimHeightOffset;
            if (report.Target != null)
            {
                var targetView = UnitView.FindForUnit(report.Target);
                if (targetView != null)
                {
                    impact = targetView.FeetWorldPosition + Vector3.up * aimHeightOffset;
                }
            }

            // Đòn đánh: nốt văng tại target.
            if (report.Skill.effectKind == SkillEffectKind.Damage
                || report.Skill.slotKind == SkillSlotKind.BasicAttack
                || report.Skill.slotKind == SkillSlotKind.Skill)
            {
                CharlotteVfxView.SpawnNoteScatter(impact, parent);
            }

            // Skill tạo khiên: aura quanh Charlotte đến khi Shield = 0.
            if (report.Skill.effectKind == SkillEffectKind.Shield || report.Source.Shield > 0)
            {
                if (report.Skill.effectKind == SkillEffectKind.Shield)
                {
                    CharlotteVfxView.EnsurePersistentAura(report.Source, sourceView, parent);
                }
            }
        }

        public static void PlayCounterIfCharlotte(CombatUnit unit, Transform parent = null)
        {
            if (!IsCharlotteUnit(unit))
            {
                return;
            }

            var view = UnitView.FindForUnit(unit);
            if (view == null)
            {
                return;
            }

            CharlotteVfxView.SpawnCounterFrontShield(view, parent);
        }

        private static bool IsCharlotteSkill(SkillDefinitionSO skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                return false;
            }

            var id = skill.skillId;
            return id == "Charlott_basic"
                   || id == "tank_basic"
                   || id == "tank_skill"
                   || id == "tank_ult"
                   || id.StartsWith("tank_")
                   || id.StartsWith("Charlott");
        }

        private static bool IsCharlotteUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return false;
            }

            var view = UnitView.FindForUnit(unit);
            if (view != null)
            {
                var key = view.DemoUnitKey ?? string.Empty;
                if (key == "Charlott" || key == "charlotte" || key == "tank")
                {
                    return true;
                }
            }

            var name = unit.DisplayName ?? string.Empty;
            return name.IndexOf("Charlotte", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Charlott", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
