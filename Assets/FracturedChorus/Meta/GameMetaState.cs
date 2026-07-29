using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class GameMetaState
    {
        public const int SaveVersion = 2;

        public int SaveVersionId = SaveVersion;
        public CalendarState Calendar = new CalendarState();
        public SocialStatsState SocialStats = new SocialStatsState();
        public BondState Bonds = new BondState();
        public StoryFlags Flags = new StoryFlags();
        public RunSnapshot RunSnapshot = new RunSnapshot();
        public WalletState Wallet = new WalletState();
        public PartyLoadoutState Loadout = new PartyLoadoutState();
        public int Difficulty;

        public static GameMetaState CreateNew()
        {
            var state = new GameMetaState();
            state.Calendar.ResetForNewDay(GameDate.Arc1Start);
            state.Flags.SetBool(StoryFlagIds.LuminaCaseOpen, true);
            return state;
        }

        public static GameMetaState CreateHubStart()
        {
            var state = CreateNew();
            state.Flags.SetBool(StoryFlagIds.RenArrivedHima, true);
            return state;
        }

        public void CompleteMorningQuiz()
        {
            Calendar.CompleteMorningQuiz();
        }

        public bool ConsumeActivitySlot()
        {
            return Calendar.ConsumeActivitySlot();
        }

        public void AdvanceDay()
        {
            Calendar.AdvanceDay();
            OnDayAdvanced();
        }

        public void AddStatExp(SocialStatType stat, int amount)
        {
            SocialStats.AddExp(stat, amount);
        }

        public void SetFlag(string flagId, bool value = true)
        {
            Flags.SetBool(flagId, value);
            ApplyFlagSideEffects(flagId, value);
        }

        public bool HasFlag(string flagId) => Flags.Has(flagId);

        public BondProgress GetBond(string npcId) => Bonds.GetOrCreate(npcId);

        private void OnDayAdvanced()
        {
            CheckVaultDeadline();

            if (Calendar.IsArcComplete)
            {
                return;
            }
        }

        private void CheckVaultDeadline()
        {
            if (!Flags.Has(StoryFlagIds.VaultQuestActive))
            {
                return;
            }

            if (Flags.Has(StoryFlagIds.VaultCleared) || Flags.Has(StoryFlagIds.VaultMissedDeadline))
            {
                return;
            }

            if (Calendar.CurrentDate > GameDate.VaultDeadline)
            {
                Flags.SetBool(StoryFlagIds.VaultMissedDeadline, true);
            }
        }

        private void ApplyFlagSideEffects(string flagId, bool value)
        {
            if (!value)
            {
                return;
            }

            switch (flagId)
            {
                case StoryFlagIds.VaultQuestActive:
                    break;
                case StoryFlagIds.VaultCleared:
                    if (Calendar.CurrentDate <= GameDate.VaultDeadline)
                    {
                        Flags.SetBool(StoryFlagIds.VaultClearedOnTime, true);
                    }
                    else
                    {
                        Flags.SetBool(StoryFlagIds.VaultClearedLate, true);
                    }

                    UnlockPostVaultBondCaps();
                    break;
                case StoryFlagIds.CodaMet:
                    Bonds.GetOrCreate(BondNpcIds.Coda).SetArcCap(4);
                    break;
                case StoryFlagIds.CharlotteReunited:
                    Bonds.GetOrCreate(BondNpcIds.Charlotte).SetArcCap(3);
                    break;
            }
        }

        private void UnlockPostVaultBondCaps()
        {
            Bonds.GetOrCreate(BondNpcIds.Charlotte).SetArcCap(BondProgress.MaxRank);
            Bonds.GetOrCreate(BondNpcIds.Coda).SetArcCap(BondProgress.MaxRank);
            Bonds.GetOrCreate(BondNpcIds.Ryo).SetArcCap(3);
            Bonds.GetOrCreate(BondNpcIds.MeiLin).SetArcCap(3);
        }
    }
}
