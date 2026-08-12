namespace FracturedChorus.Combat.Timeline
{
    public static class CombatTimelineProfile
    {
        public const int BossTotalBeats = 677;
        public const int RunTotalBeats = 689;
        public const int BossIntroBeatCount = 12;
        public const int RunIntroBeatCount = 0;

        public static int TotalBeats { get; private set; } = BossTotalBeats;
        public static int CombatIntroBeatCount { get; private set; } = BossIntroBeatCount;
        public static float CombatIntroDurationSec { get; private set; } =
            TimelineConstants.BossRemixFirstBeatOffsetSec
            + BossIntroBeatCount * (60f / TimelineConstants.BossRemixBpm);

        public static void ApplyBoss()
        {
            TotalBeats = BossTotalBeats;
            CombatIntroBeatCount = BossIntroBeatCount;
            CombatIntroDurationSec = TimelineConstants.BossRemixFirstBeatOffsetSec
                + BossIntroBeatCount * (60f / TimelineConstants.BossRemixBpm);
        }

        public static void ApplyRun()
        {
            TotalBeats = RunTotalBeats;
            CombatIntroBeatCount = RunIntroBeatCount;
            CombatIntroDurationSec = 0f;
        }
    }
}
