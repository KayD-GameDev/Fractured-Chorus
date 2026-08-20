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
        public const int ShopHealPotionCost = 40;
        public const int ShopPrepCost = 35;
        public const int ShopArmorCost = 45;
        public const int ShopReviveCost = 50;
        public const int ShopPlaceCounterCost = 40;
        public const float ShopArmorShieldPercent = 0.25f;
        public const int ShopPrepAmount = 1;

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
