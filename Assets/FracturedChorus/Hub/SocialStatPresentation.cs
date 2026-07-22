using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class SocialStatPresentation
    {
        public static readonly SocialStatType[] OrderedStats =
        {
            SocialStatType.Resonance,
            SocialStatType.Cadence,
            SocialStatType.Pulse,
            SocialStatType.Harmony,
            SocialStatType.Rhythm
        };

        public static string GetDisplayName(SocialStatType stat) => stat.ToString();

        public static string GetFlavor(SocialStatType stat) => stat switch
        {
            SocialStatType.Resonance => "Build deeper bonds through empathy.",
            SocialStatType.Cadence => "Understand timing and flow.",
            SocialStatType.Pulse => "Find strength in commitment.",
            SocialStatType.Harmony => "Unite through shared purpose.",
            SocialStatType.Rhythm => "Keep steady. Drive forward.",
            _ => string.Empty
        };
    }
}
