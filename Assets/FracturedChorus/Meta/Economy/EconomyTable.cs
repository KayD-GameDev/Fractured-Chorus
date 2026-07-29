using UnityEngine;

namespace FracturedChorus.Meta.Economy
{
    public static class EconomyTable
    {
        public const int TreasureMin = 50;
        public const int TreasureMax = 120;
        public const int CampHealCost = 30;
        public const int RelayCost = 50;
        public const int HubHealCost = 40;

        public static int BattleReward(int floor) => 40 + Mathf.Max(0, floor) * 5;

        public static int EliteReward(int floor) => 80 + Mathf.Max(0, floor) * 8;

        public static int BossReward(int floor) => 200 + Mathf.Max(0, floor) * 15;

        public static int TreasureReward(int floorSeed)
        {
            var span = TreasureMax - TreasureMin + 1;
            if (span <= 1)
            {
                return TreasureMin;
            }

            var offset = Mathf.Abs(floorSeed) % span;
            return TreasureMin + offset;
        }
    }
}
