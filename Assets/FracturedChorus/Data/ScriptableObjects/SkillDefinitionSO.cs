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

    public enum SkillEffectKind
    {
        Damage = 0,
        Heal = 1,
        Shield = 2,
        DelayBossNote = 3,
        ReduceS2 = 4,
        CycleShift = 5
    }

    [CreateAssetMenu(fileName = "Skill", menuName = "Fractured Chorus/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public string skillId;
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;
        public SkillSlotKind slotKind = SkillSlotKind.BasicAttack;
        public int avCost;
        public int delay = 3;
        public int skillTier = 1;
        [Tooltip("Legacy — combat uses Strength Type from Unit Stat Block.")]
        public DamageType damageType = DamageType.Physical;
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;
        public ActionGlowType glowType = ActionGlowType.Attack;
        public int baseDamage = 10;

        [Header("Timeline footprint — S1 standing · S using · S2 standing")]
        [Tooltip("Standing Phase 1 (wind-up): beats idle BEFORE the attack lands. Prevents skill spam between beats.")]
        [Min(0)] public int standingBeatsBefore = 1;
        [Tooltip("Using Skill Phase: beats the attack is actually active (counts counter hits vs boss notes).")]
        [Min(1)] public int activeBeats = 1;
        [Tooltip("Standing Phase 2 (recovery): beats idle AFTER the attack before planning again.")]
        [Min(0)] public int standingBeatsAfter = 1;

        [Header("Effect")]
        public SkillEffectKind effectKind = SkillEffectKind.Damage;
        public int effectValue;
        public bool grantsCycleShift;

        [Header("Prep empower")]
        public bool usesPrepEmpower;
        public int prepEmpowerThreshold = 1;
        public int prepEmpowerCost = 1;
        public int empowerEffectValue;
        public float empowerDamageMultiplier = 1f;
        public int empowerExtraHits;
        public bool empowerForceHarmony;
        public bool empowerKeepDelayTier;
        public bool empowerOverhealToShield;
        public int empowerOverhealShieldCap = 30;
        public bool empowerPartyReduceS2;
        public bool empowerGiftPrepToTarget;
        public bool empowerGuardChargeOnPerfect;

        public int TotalFootprintBeats => standingBeatsBefore + Mathf.Max(1, activeBeats) + standingBeatsAfter;

        public int GetAvCost()
        {
            return SkillAvCosts.GetCost(slotKind);
        }

        public bool IsGuard => slotKind == SkillSlotKind.Guard || glowType == ActionGlowType.Guard;
        public bool IsAttack => !IsGuard && glowType != ActionGlowType.Support;

        public int ResolveEffectValue(bool empowered)
        {
            if (empowered && empowerEffectValue > 0)
            {
                return empowerEffectValue;
            }

            if (effectValue > 0)
            {
                return effectValue;
            }

            return baseDamage;
        }
    }
}
