using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class BondProgress
    {
        public string NpcId;
        public EchoKey EchoKey;
        public int Rank = 1;
        public int Exp;
        public bool IsLocked;

        public const int MaxRank = 10;

        private static readonly int[] RankExpThresholds =
        {
            10, 15, 22, 30, 40, 52, 66, 82, 100, 120
        };

        public BondProgress()
        {
        }

        public BondProgress(string npcId, EchoKey echoKey, int arcCap = MaxRank)
        {
            NpcId = npcId;
            EchoKey = echoKey;
            SetArcCap(arcCap);
        }

        public int ArcCap { get; private set; } = MaxRank;

        public void SetArcCap(int cap)
        {
            ArcCap = Math.Clamp(cap, 1, MaxRank);
            if (Rank > ArcCap)
            {
                Rank = ArcCap;
            }
        }

        public void AddExp(int amount)
        {
            if (amount <= 0 || IsLocked)
            {
                return;
            }

            Exp += amount;
            TryRankUp();
        }

        public bool CanRankUp() => !IsLocked && Rank < ArcCap && Rank < MaxRank && Exp >= GetThresholdForRank(Rank);

        public bool TryRankUp()
        {
            if (!CanRankUp())
            {
                return false;
            }

            Exp -= GetThresholdForRank(Rank);
            Rank++;
            TryRankUp();
            return true;
        }

        public int GetThresholdForRank(int rank)
        {
            if (rank < 1)
            {
                return RankExpThresholds[0];
            }

            var index = Math.Min(rank - 1, RankExpThresholds.Length - 1);
            return RankExpThresholds[index];
        }
    }
}
