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
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;
        public ActionGlowType glowType = ActionGlowType.Attack;
        public int baseDamage = 10;

        public int GetAvCost()
        {
            return SkillAvCosts.GetCost(slotKind);
        }

        public bool IsGuard => slotKind == SkillSlotKind.Guard || glowType == ActionGlowType.Guard;
        public bool IsAttack => !IsGuard && glowType != ActionGlowType.Support;
    }
}
