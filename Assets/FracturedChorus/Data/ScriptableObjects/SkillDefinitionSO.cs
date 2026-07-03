using FracturedChorus.Combat.Damage;
using UnityEngine;

namespace FracturedChorus.Data
{
    public enum SkillTargetType
    {
        SingleEnemy,
        SingleAlly,
        Self,
        AllEnemies
    }

    public enum ActionGlowType
    {
        Attack,
        Rush,
        Support,
        Guard
    }

    [CreateAssetMenu(fileName = "Skill", menuName = "Fractured Chorus/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public string skillId;
        public string displayName;
        [TextArea] public string description;
        public SkillSlotKind slotKind = SkillSlotKind.BasicAttack;
        public int avCost;
        public int delay = 3;
        public int skillTier = 1;
        [Tooltip("Legacy — combat dùng Strength Type từ Unit Stat Block.")]
        public DamageType damageType = DamageType.Physical;
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;
        public ActionGlowType glowType = ActionGlowType.Attack;
        public int baseDamage = 10;

        [Header("Timeline footprint — S1 standing · S using · S2 standing")]
        [Tooltip("Standing Phase 1 (wind-up): số beat đứng yên TRƯỚC khi ra đòn. Chống spam skill giữa các beat.")]
        [Min(0)] public int standingBeatsBefore = 1;
        [Tooltip("Using Skill Phase: số beat đòn thực sự active (đếm counter hit vs nốt boss).")]
        [Min(1)] public int activeBeats = 1;
        [Tooltip("Standing Phase 2 (recovery): số beat đứng yên SAU đòn trước khi được plan tiếp.")]
        [Min(0)] public int standingBeatsAfter = 1;

        /// <summary>Tổng số beat skill chiếm trên timeline: S1 + S + S2.</summary>
        public int TotalFootprintBeats => standingBeatsBefore + Mathf.Max(1, activeBeats) + standingBeatsAfter;

        public int GetAvCost()
        {
            return SkillAvCosts.GetCost(slotKind);
        }

        public bool IsGuard => slotKind == SkillSlotKind.Guard || glowType == ActionGlowType.Guard;
        public bool IsAttack => !IsGuard && glowType != ActionGlowType.Support;
    }
}
