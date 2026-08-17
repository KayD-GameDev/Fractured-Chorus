using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    public static class RunEventCombatMods
    {
        public static float NextOutgoingMul { get; private set; } = 1f;
        public static float NextIncomingMul { get; private set; } = 1f;
        public static float NextCritBonus { get; private set; }
        public static float PendingHealPercent { get; private set; }
        public static bool PendingOverhealToShield { get; private set; }
        public static float PendingShieldPercent { get; private set; }
        public static int PendingPrep { get; private set; }
        public static int PendingFirstPlaceReduceS2 { get; private set; }
        public static bool FirstPlaceConsumed { get; private set; }

        public static void ApplyChoice(EventChoiceSO choice)
        {
            if (choice == null)
            {
                return;
            }

            switch (choice.Kind)
            {
                case EventChoiceKind.NextBattleDamage:
                    NextOutgoingMul += choice.Magnitude;
                    break;
                case EventChoiceKind.HealOverflowShield:
                    PendingHealPercent += choice.Magnitude;
                    PendingOverhealToShield = true;
                    break;
                case EventChoiceKind.NextBattleDefense:
                    NextIncomingMul *= Mathf.Max(0.05f, 1f - choice.Magnitude);
                    break;
                case EventChoiceKind.FirstNoteReduceS2:
                    PendingFirstPlaceReduceS2 += Mathf.Max(1, Mathf.RoundToInt(choice.Magnitude));
                    break;
                case EventChoiceKind.PrepBonus:
                    PendingPrep += Mathf.Max(1, Mathf.RoundToInt(choice.Magnitude));
                    break;
                case EventChoiceKind.StartShieldPercent:
                    PendingShieldPercent += choice.Magnitude;
                    break;
                case EventChoiceKind.NextBattleCrit:
                    NextCritBonus += choice.Magnitude;
                    break;
            }
        }

        public static void ApplyStartOfBattle(Combat.Core.CombatSession session)
        {
            if (session?.Grid == null)
            {
                return;
            }

            foreach (var unit in session.Grid.PlayerUnits)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (PendingHealPercent > 0f)
                {
                    var amount = unit.Stats.MaxHp * PendingHealPercent;
                    unit.Heal(amount, PendingOverhealToShield, unit.Stats.MaxHp);
                }

                if (PendingShieldPercent > 0f)
                {
                    unit.AddShield(Mathf.RoundToInt(unit.Stats.MaxHp * PendingShieldPercent));
                }

                if (PendingPrep > 0)
                {
                    unit.GainPrep(PendingPrep);
                }

                if (NextCritBonus > 0f)
                {
                    unit.Stats.BaseLuck += NextCritBonus;
                }
            }

            PendingHealPercent = 0f;
            PendingOverhealToShield = false;
            PendingShieldPercent = 0f;
            PendingPrep = 0;
            NextCritBonus = 0f;
        }

        public static float ModifyOutgoing(GridSide sourceSide, float damage)
        {
            if (sourceSide != GridSide.Player || Mathf.Approximately(NextOutgoingMul, 1f))
            {
                return damage;
            }

            return damage * NextOutgoingMul;
        }

        public static float ModifyIncoming(GridSide targetSide, float damage)
        {
            if (targetSide != GridSide.Player || Mathf.Approximately(NextIncomingMul, 1f))
            {
                return damage;
            }

            return damage * NextIncomingMul;
        }

        public static bool TryArmFirstPlaceReduceS2(CombatUnit unit)
        {
            if (unit == null
                || unit.Side != GridSide.Player
                || FirstPlaceConsumed
                || PendingFirstPlaceReduceS2 <= 0)
            {
                return false;
            }

            unit.SetPendingReduceS2(Mathf.Max(unit.PendingReduceS2, PendingFirstPlaceReduceS2));
            return true;
        }

        public static void ConsumeFirstPlaceReduceS2()
        {
            FirstPlaceConsumed = true;
            PendingFirstPlaceReduceS2 = 0;
        }

        public static void ConsumeBattle()
        {
            NextOutgoingMul = 1f;
            NextIncomingMul = 1f;
            NextCritBonus = 0f;
            PendingFirstPlaceReduceS2 = 0;
            FirstPlaceConsumed = false;
        }

        public static void ClearRun()
        {
            ConsumeBattle();
            PendingHealPercent = 0f;
            PendingOverhealToShield = false;
            PendingShieldPercent = 0f;
            PendingPrep = 0;
        }
    }
}
