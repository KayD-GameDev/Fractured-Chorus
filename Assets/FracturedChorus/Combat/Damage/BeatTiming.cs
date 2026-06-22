namespace FracturedChorus.Combat.Damage
{
    public enum BeatTiming
    {
        Early,
        OnBeat,
        Late,
        OffBeat
    }

    public static class BeatTimingExtensions
    {
        public static float GetMultiplier(this BeatTiming timing)
        {
            return timing switch
            {
                BeatTiming.Early => 0.5f,
                BeatTiming.OnBeat => 1f,
                BeatTiming.Late => 0.25f,
                BeatTiming.OffBeat => 0.01f,
                _ => 1f
            };
        }
    }
}
