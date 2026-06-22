using FracturedChorus.Combat.Damage;
using UnityEngine;

namespace FracturedChorus.Combat.Timeline
{
    public static class BeatTimingResolver
    {
        public static BeatTiming Resolve(int playerBeatIndex, int enemyTelegraphBeatIndex)
        {
            if (enemyTelegraphBeatIndex < 0)
            {
                return BeatTiming.OnBeat;
            }

            var delta = playerBeatIndex - enemyTelegraphBeatIndex;
            return delta switch
            {
                0 => BeatTiming.OnBeat,
                -1 => BeatTiming.Early,
                1 => BeatTiming.Late,
                _ => BeatTiming.OffBeat
            };
        }

        public static float ApplyGuardReduction(float incomingDamage, BeatTiming guardTiming)
        {
            var beatMult = guardTiming.GetMultiplier();
            return incomingDamage - incomingDamage * beatMult;
        }
    }
}
