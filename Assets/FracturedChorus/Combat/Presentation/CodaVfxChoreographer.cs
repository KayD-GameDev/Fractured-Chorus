using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CodaVfxChoreographer : MonoBehaviour
    {
        [SerializeField] private float aimHeightOffset = 0.55f;
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
            if (!report.IsValid || !IsCodaSkill(report.Skill))
            {
                return;
            }

            var sourceView = UnitView.FindForUnit(report.Source);
            if (sourceView == null)
            {
                return;
            }

            var from = ResolveAimPoint(sourceView);
            var to = from + Vector3.right * 3.5f;
            if (report.Target != null)
            {
                var targetView = UnitView.FindForUnit(report.Target);
                if (targetView != null)
                {
                    to = ResolveAimPoint(targetView);
                }
            }

            var parent = vfxParent;
            var id = report.Skill.skillId;

            if (id == "mage_basic" || report.Skill.slotKind == SkillSlotKind.BasicAttack)
            {
                CodaMagicVfxView.SpawnCrescentSlash(from, to, parent);
                return;
            }

            if (id == "mage_skill" || report.Skill.slotKind == SkillSlotKind.Skill)
            {
                CodaMagicVfxView.SpawnBeam(from, to, parent);
                return;
            }

            if (id == "mage_ult" || report.Skill.slotKind == SkillSlotKind.Ultimate)
            {
                CodaMagicVfxView.SpawnTripleBeam(from, to, parent);
            }
        }

        private static bool IsCodaSkill(SkillDefinitionSO skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                return false;
            }

            var id = skill.skillId;
            return id == "mage_basic"
                   || id == "mage_skill"
                   || id == "mage_ult"
                   || id.StartsWith("mage_")
                   || id.StartsWith("coda_");
        }

        private Vector3 ResolveAimPoint(UnitView view)
        {
            var feet = view.FeetWorldPosition;
            return new Vector3(feet.x, feet.y + aimHeightOffset, feet.z);
        }
    }
}
