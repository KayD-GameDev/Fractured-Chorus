namespace FracturedChorus.Combat.Block
{
    public enum BlockTiming
    {
        OnBeat,
        Early,
        Late,
        OffBeat
    }

    public static class BlockTimingExtensions
    {
        public static BlockTiming Resolve(int barrierBeat, int enemyImpactBeat)
        {
            var delta = barrierBeat - enemyImpactBeat;
            return delta switch
            {
                0 => BlockTiming.OnBeat,
                -1 => BlockTiming.Early,
                1 => BlockTiming.Late,
                _ => BlockTiming.OffBeat
            };
        }

        /// <summary>Fraction of incoming damage reduced (0 = none, 0.68 = 68% reduction).</summary>
        public static float GetDamageReduction(this BlockTiming timing)
        {
            return timing switch
            {
                BlockTiming.OnBeat => 0.68f,
                BlockTiming.Early => 0.25f,
                BlockTiming.Late => 0.10f,
                _ => 0f
            };
        }
    }
}
