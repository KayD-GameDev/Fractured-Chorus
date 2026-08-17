using System.Collections.Generic;
using FracturedChorus.Data;

namespace FracturedChorus.RunMap
{
    public static class TreasureClaimStore
    {
        public readonly struct Claim
        {
            public Claim(string rewardId, string title, TreasureRewardKind kind, int nodeId, int floor)
            {
                RewardId = rewardId;
                Title = title;
                Kind = kind;
                NodeId = nodeId;
                Floor = floor;
            }

            public string RewardId { get; }
            public string Title { get; }
            public TreasureRewardKind Kind { get; }
            public int NodeId { get; }
            public int Floor { get; }
        }

        private static readonly List<Claim> Claims = new List<Claim>();

        public static IReadOnlyList<Claim> All => Claims;
        public static Claim? Last { get; private set; }

        public static void Record(TreasureRewardSO reward, int nodeId, int floor)
        {
            if (reward == null)
            {
                return;
            }

            var claim = new Claim(reward.Id, reward.Title, reward.Kind, nodeId, floor);
            Claims.Add(claim);
            Last = claim;
        }

        public static void ClearRun()
        {
            Claims.Clear();
            Last = null;
        }
    }
}
