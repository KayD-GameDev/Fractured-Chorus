using FracturedChorus.Combat.Difficulty;
using FracturedChorus.Meta;
using FracturedChorus.Meta.Economy;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatRewardService
    {
        public static string GrantVictoryNotes(string encounterId)
        {
            if (!GameMetaSession.HasSession)
            {
                return null;
            }

            var state = GameMetaSession.Current;
            var floor = Mathf.Max(0, state.RunSnapshot.CurrentFloor);
            var baseAmount = ResolveBaseAmount(encounterId, floor);
            var mult = DifficultyRuntime.Get(state.Difficulty).NotesEarn;
            var granted = Mathf.Max(0, Mathf.RoundToInt(baseAmount * mult));
            state.Wallet.Add(granted);
            GameMetaSession.Save();
            return $"+{granted} Notes";
        }

        private static int ResolveBaseAmount(string encounterId, int floor)
        {
            if (string.IsNullOrEmpty(encounterId))
            {
                return EconomyTable.BattleReward(floor);
            }

            if (encounterId == EncounterCatalog.BossDespair
                || encounterId.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EconomyTable.BossReward(floor);
            }

            if (encounterId == EncounterCatalog.EliteGrunts
                || encounterId.IndexOf("Elite", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EconomyTable.EliteReward(floor);
            }

            return EconomyTable.BattleReward(floor);
        }
    }
}
