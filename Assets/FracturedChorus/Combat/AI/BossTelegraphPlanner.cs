using FracturedChorus.Combat.Timeline;
using UnityEngine;

namespace FracturedChorus.Combat.AI
{
    public static class BossTelegraphPlanner
    {
        public static BossNoteTier RollNoteTier(int phaseIndex)
        {
            float purple, blue;
            if (phaseIndex <= 5)
            {
                purple = 0.10f;
                blue = 0.20f;
            }
            else
            {
                purple = 0.15f;
                blue = 0.25f;
            }

            var roll = Random.value;
            if (roll < purple)
            {
                return BossNoteTier.Purple;
            }

            if (roll < purple + blue)
            {
                return BossNoteTier.Blue;
            }

            return BossNoteTier.Red;
        }

        public static BossNoteTier RollEliteNoteTier()
        {
            return Random.value < 0.30f ? BossNoteTier.Blue : BossNoteTier.Red;
        }

        public static int HitsRequiredForTier(BossNoteTier tier) => (int)tier;
    }
}
