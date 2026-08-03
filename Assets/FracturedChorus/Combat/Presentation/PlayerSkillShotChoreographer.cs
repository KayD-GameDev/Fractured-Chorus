using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class PlayerSkillShotChoreographer : MonoBehaviour
    {
        [SerializeField] private float aimHeightOffset = 0.55f;
        [SerializeField] private Transform shotParent;

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
            if (!report.IsValid || report.Target == null || !IsRenDamageSkill(report.Skill))
            {
                return;
            }

            var sourceView = UnitView.FindForUnit(report.Source);
            var targetView = UnitView.FindForUnit(report.Target);
            if (sourceView == null || targetView == null)
            {
                return;
            }

            var from = ResolveAimPoint(sourceView);
            var to = ResolveAimPoint(targetView);
            var parent = shotParent != null ? shotParent : transform;

            if (IsMeleeStockSkill(report.Skill))
            {
                RenMeleeStrikeView.Spawn(from, to, parent);
                return;
            }

            RenBulletShotView.Spawn(from, to, parent);
        }

        private static bool IsRenDamageSkill(SkillDefinitionSO skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                return false;
            }

            if (!skill.skillId.StartsWith("ren_"))
            {
                return false;
            }

            if (skill.IsGuard || skill.effectKind != SkillEffectKind.Damage)
            {
                return false;
            }

            return skill.targetType == SkillTargetType.SingleEnemy
                   || skill.targetType == SkillTargetType.AllEnemies;
        }

        private static bool IsMeleeStockSkill(SkillDefinitionSO skill)
        {
            return skill != null && skill.skillId == "ren_basic";
        }

        private Vector3 ResolveAimPoint(UnitView view)
        {
            var feet = view.FeetWorldPosition;
            return new Vector3(feet.x, feet.y + aimHeightOffset, feet.z);
        }
    }
}
