using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Actions
{
    public class SkillActionCommand : ICombatAction
    {
        public SkillDefinitionSO Skill { get; }

        public int Delay => Skill != null ? Skill.delay : 0;

        public SkillActionCommand(SkillDefinitionSO skill)
        {
            Skill = skill;
        }

        public bool CanExecute(CombatContext ctx)
        {
            if (ctx?.Source == null || ctx.Skill == null || !ctx.Source.IsAlive)
            {
                return false;
            }

            return ctx.Skill.targetType switch
            {
                SkillTargetType.Self => true,
                SkillTargetType.SingleEnemy => ctx.Target != null && ctx.Target.IsAlive
                    && ctx.Target.Side != ctx.Source.Side,
                SkillTargetType.SingleAlly => ctx.Target != null && ctx.Target.IsAlive
                    && ctx.Target.Side == ctx.Source.Side,
                SkillTargetType.AllEnemies => ctx.Grid.GetOpponents(ctx.Source.Side).GetEnumerator().MoveNext(),
                _ => false
            };
        }

        public void Execute(CombatContext ctx)
        {
            if (!CanExecute(ctx))
            {
                Debug.LogWarning($"[SkillAction] Cannot execute {Skill.displayName}");
                return;
            }

            switch (ctx.Skill.targetType)
            {
                case SkillTargetType.SingleEnemy:
                case SkillTargetType.SingleAlly:
                    ApplyToTarget(ctx, ctx.Target);
                    break;
                case SkillTargetType.AllEnemies:
                    foreach (var enemy in ctx.Grid.GetOpponents(ctx.Source.Side))
                    {
                        if (enemy.IsAlive)
                        {
                            ApplyToTarget(ctx, enemy);
                        }
                    }
                    break;
                case SkillTargetType.Self:
                    ApplyToTarget(ctx, ctx.Source);
                    break;
            }
        }

        private void ApplyToTarget(CombatContext ctx, Units.CombatUnit target)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var coverMod = ctx.Grid.GetCoverModifier(ctx.Source.GridPosition, target.GridPosition);

            if (ctx.Skill.glowType == ActionGlowType.Support || ctx.Skill.IsGuard)
            {
                if (!ctx.Skill.IsGuard)
                {
                    target.Heal(ctx.Skill.baseDamage);
                    Debug.Log($"[SkillAction] {ctx.Source.DisplayName} heals {target.DisplayName} for {ctx.Skill.baseDamage}");
                }

                return;
            }

            var result = DamageCalculator.Calculate(
                ctx.Source.Stats,
                target.Stats,
                ctx.Skill.skillTier,
                ctx.BeatTiming,
                ctx.Harmony,
                coverMod);

            target.TakeDamage(result.FinalDamage);
            Debug.Log($"[SkillAction] {ctx.Source.DisplayName} -> {target.DisplayName} | " +
                      $"raw={result.RawDamage:F1} final={result.FinalDamage:F1} crit={result.IsCritical}");
        }
    }
}
