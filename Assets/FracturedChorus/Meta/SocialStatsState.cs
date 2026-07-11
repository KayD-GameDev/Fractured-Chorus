using System;
using System.Collections.Generic;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class SocialStatsState
    {
        private static readonly int[] RankExpThresholds =
        {
            15, 25, 40, 60, 85, 115, 150, 190, 235, 120
        };

        public const int MaxRank = 10;

        private readonly Dictionary<SocialStatType, int> _exp = new Dictionary<SocialStatType, int>();
        private readonly Dictionary<SocialStatType, int> _rank = new Dictionary<SocialStatType, int>();

        public SocialStatsState()
        {
            foreach (SocialStatType stat in Enum.GetValues(typeof(SocialStatType)))
            {
                _exp[stat] = 0;
                _rank[stat] = 1;
            }
        }

        public int GetRank(SocialStatType stat) => _rank.TryGetValue(stat, out var rank) ? rank : 1;

        public int GetExp(SocialStatType stat) => _exp.TryGetValue(stat, out var exp) ? exp : 0;

        public void AddExp(SocialStatType stat, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _exp[stat] = GetExp(stat) + amount;
            TryRankUp(stat);
        }

        public bool TryRankUp(SocialStatType stat)
        {
            var rank = GetRank(stat);

            if (rank >= MaxRank)
            {
                return false;
            }

            var threshold = GetThresholdForRank(rank);
            if (GetExp(stat) < threshold)
            {
                return false;
            }

            _exp[stat] = GetExp(stat) - threshold;
            _rank[stat] = rank + 1;
            TryRankUp(stat);
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

        public IReadOnlyDictionary<SocialStatType, int> ExportRanks()
        {
            return new Dictionary<SocialStatType, int>(_rank);
        }

        public IReadOnlyDictionary<SocialStatType, int> ExportExp()
        {
            return new Dictionary<SocialStatType, int>(_exp);
        }

        public void ImportRank(SocialStatType stat, int rank, int exp)
        {
            _rank[stat] = Math.Clamp(rank, 1, MaxRank);
            _exp[stat] = Math.Max(0, exp);
        }
    }
}
