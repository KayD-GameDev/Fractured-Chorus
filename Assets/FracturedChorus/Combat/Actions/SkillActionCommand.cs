using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
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

            return ctx.Skill.effectKind switch
            {
                SkillEffectKind.Shield => true,
                SkillEffectKind.DelayBossNote => true,
                SkillEffectKind.Heal => ctx.Target != null && ctx.Target.IsAlive
                    && ctx.Target.Side == ctx.Source.Side,
                SkillEffectKind.ReduceS2 => ctx.Target != null && ctx.Target.IsAlive
                    && ctx.Target.Side == ctx.Source.Side,
                _ => ctx.Skill.targetType switch
                {
                    SkillTargetType.Self => true,
                    SkillTargetType.SingleEnemy => ctx.Target != null && ctx.Target.IsAlive
                        && ctx.Target.Side != ctx.Source.Side,
                    SkillTargetType.SingleAlly => ctx.Target != null && ctx.Target.IsAlive
                        && ctx.Target.Side == ctx.Source.Side,
                    SkillTargetType.AllEnemies => ctx.Grid.GetOpponents(ctx.Source.Side).GetEnumerator().MoveNext(),
                    _ => false
                }
            };
        }

        public void Execute(CombatContext ctx)
        {
            if (!CanExecute(ctx))
            {
                Debug.LogWarning($"[SkillAction] Cannot execute {Skill.displayName}");
                return;
            }

            switch (ctx.Skill.effectKind)
            {
                case SkillEffectKind.Heal:
                    ApplyHeal(ctx);
                    break;
                case SkillEffectKind.Shield:
                    ApplyShield(ctx);
                    if (ctx.Target != null && ctx.Target.Side != ctx.Source.Side)
                    {
                        ApplyDamageToTarget(ctx, ctx.Target);
                    }
                    break;
                case SkillEffectKind.ReduceS2:
                    ApplyReduceS2(ctx);
                    break;
                case SkillEffectKind.DelayBossNote:
                    ApplyDelayBossNote(ctx);
                    break;
                case SkillEffectKind.Damage:
                default:
                    ApplyDamageTargets(ctx);
                    break;
            }
        }

        private void ApplyHeal(CombatContext ctx)
        {
            if (ctx.Entry != null && ctx.Entry.EffectPayloadApplied)
            {
                return;
            }

            var target = ctx.Target ?? ctx.Source;
            var amount = ctx.Skill.ResolveEffectValue(false) + ctx.Source.Stats.Magic * 0.5f;
            if (ctx.IsEmpowered && ctx.Skill.empowerEffectValue > 0)
            {
                amount += ctx.Skill.empowerEffectValue;
            }

            if (ctx.Grid != null)
            {
                amount *= ctx.Grid.GetHealPotencyModifier(ctx.Source.GridPosition);
            }

            var overheal = ctx.IsEmpowered && ctx.Skill.empowerOverhealToShield;
            var cap = ctx.Skill.empowerOverhealShieldCap;
            target.Heal(amount, overheal, cap);
            if (ctx.Entry != null)
            {
                ctx.Entry.EffectPayloadApplied = true;
            }

            Debug.Log(
                $"[SkillAction] {ctx.Source.DisplayName} heals {target.DisplayName} for {amount:F0}" +
                (ctx.IsEmpowered ? " (empowered)" : string.Empty));
        }

        private void ApplyShield(CombatContext ctx)
        {
            if (ctx.Entry != null && ctx.Entry.EffectPayloadApplied)
            {
                return;
            }

            var amount = ctx.Skill.ResolveEffectValue(ctx.IsEmpowered);
            ctx.Source.AddShield(amount);
            if (ctx.Entry != null)
            {
                ctx.Entry.EffectPayloadApplied = true;
            }

            Debug.Log(
                $"[SkillAction] {ctx.Source.DisplayName} gains Shield {amount}" +
                (ctx.IsEmpowered && ctx.Skill.empowerGuardChargeOnPerfect
                    ? " (empowered · GuardCharge on OnBeat block in S)"
                    : ctx.IsEmpowered ? " (empowered)" : string.Empty));
        }

        private void ApplyDelayBossNote(CombatContext ctx)
        {
            if (ctx.Entry != null && ctx.Entry.EffectPayloadApplied)
            {
                return;
            }

            if (ctx.Timeline == null || ctx.Entry == null)
            {
                return;
            }

            var delay = Mathf.Max(1, ctx.Skill.ResolveEffectValue(ctx.IsEmpowered));
            var sEnd = ctx.Entry.BeatIndex + SkillFootprintUtil.GetActiveBeats(ctx.Skill) - 1;
            var phase = TimelineConstants.GetPhaseIndex(ctx.Entry.BeatIndex);
            TimelineConstants.GetPhaseBeatRange(phase, out var startBeat, out var count);
            var moved = ctx.Timeline.DelayImpactTelegraphsAfterBeat(sEnd, startBeat + count, delay);
            if (ctx.Entry != null)
            {
                ctx.Entry.EffectPayloadApplied = true;
            }

            Debug.Log(
                $"[SkillAction] {ctx.Source.DisplayName} DelayBossNote +{delay} after S@{sEnd} → {moved.Count} notes" +
                (ctx.IsEmpowered ? " (empowered)" : string.Empty));
        }

        private void ApplyReduceS2(CombatContext ctx)
        {
            if (ctx.Entry != null && ctx.Entry.EffectPayloadApplied)
            {
                return;
            }

            var amount = Mathf.Max(1, ctx.Skill.ResolveEffectValue(false));
            if (ctx.IsEmpowered && ctx.Skill.empowerPartyReduceS2 && ctx.Grid != null)
            {
                foreach (var ally in ctx.Grid.GetAllies(ctx.Source.Side))
                {
                    if (ally != null && ally.IsAlive)
                    {
                        ally.SetPendingReduceS2(Mathf.Max(ally.PendingReduceS2, amount));
                    }
                }
            }
            else if (ctx.Target != null)
            {
                ctx.Target.SetPendingReduceS2(Mathf.Max(ctx.Target.PendingReduceS2, amount));
            }

            if (ctx.IsEmpowered && ctx.Skill.empowerGiftPrepToTarget && ctx.Target != null)
            {
                ctx.Target.GainPrep(1);
            }

            if (ctx.Entry != null)
            {
                ctx.Entry.EffectPayloadApplied = true;
            }

            Debug.Log(
                $"[SkillAction] {ctx.Source.DisplayName} ReduceS2 -{amount}" +
                (ctx.IsEmpowered ? " (party/gift)" : $" → {ctx.Target?.DisplayName}"));
        }

        private void ApplyDamageTargets(CombatContext ctx)
        {
            switch (ctx.Skill.targetType)
            {
                case SkillTargetType.SingleEnemy:
                case SkillTargetType.SingleAlly:
                    ApplyDamageToTarget(ctx, ctx.Target);
                    break;
                case SkillTargetType.AllEnemies:
                    foreach (var enemy in ctx.Grid.GetOpponents(ctx.Source.Side))
                    {
                        if (enemy.IsAlive)
                        {
                            ApplyDamageToTarget(ctx, enemy);
                        }
                    }
                    break;
                case SkillTargetType.Self:
                    ApplyDamageToTarget(ctx, ctx.Source);
                    break;
            }
        }

        private void ApplyDamageToTarget(CombatContext ctx, Units.CombatUnit target)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var coverMod = ctx.Grid != null
                ? ctx.Grid.GetCoverModifier(ctx.Source.GridPosition, target.GridPosition)
                : 1f;
            var attackerElement = ctx.Source.Stats.Element;
            if (ctx.IsEmpowered && ctx.Skill.empowerForceHarmony)
            {
                attackerElement = HarmonyElement.Harmony;
            }

            var harmony = HarmonyElementResolver.GetRelation(attackerElement, target.Stats.Element);

            var damageType = ctx.Skill != null ? ctx.Skill.damageType : DamageType.Physical;
            var result = DamageCalculator.Calculate(
                ctx.Source.Stats,
                target.Stats,
                ctx.Skill.skillTier,
                damageType,
                ctx.BeatTiming,
                harmony,
                coverMod);

            var finalDamage = result.FinalDamage;
            if (ctx.IsEmpowered &&
                ctx.Skill.empowerExtraHits > 0 &&
                ctx.Skill.empowerDamageMultiplier > 1f &&
                !CombatCounterResolver.ActiveWindowHasImpactNote(ctx.Entry, ctx.Timeline))
            {
                finalDamage *= ctx.Skill.empowerDamageMultiplier;
            }
            else if (ctx.IsEmpowered &&
                     ctx.Skill.empowerDamageMultiplier > 1f &&
                     ctx.Skill.empowerExtraHits <= 0 &&
                     !ctx.Skill.empowerForceHarmony)
            {
                finalDamage *= ctx.Skill.empowerDamageMultiplier;
            }

            if (ctx.CoverOutgoingMultiplier > 0f &&
                !Mathf.Approximately(ctx.CoverOutgoingMultiplier, 1f))
            {
                finalDamage *= ctx.CoverOutgoingMultiplier;
            }

            target.TakeDamage(finalDamage, result.IsCritical);
            var atkStat = damageType == DamageType.Magical
                ? ctx.Source.Stats.Magic
                : ctx.Source.Stats.Strength;
            var atkLabel = damageType == DamageType.Magical ? "ma" : "str";
            Debug.Log($"[SkillAction] {ctx.Source.DisplayName} -> {target.DisplayName} | " +
                      $"rand={result.SkillRandomRoll:F2}×{atkLabel}={atkStat:F0} " +
                      $"raw={result.RawDamage:F1} en×={result.EnduranceFactor:F2} " +
                      $"pos×={coverMod:F2} final={finalDamage:F1} crit={result.IsCritical} mult={result.CritDamageMultiplier:F2}" +
                      (ctx.IsEmpowered ? " empowered" : string.Empty));
        }
    }
}
