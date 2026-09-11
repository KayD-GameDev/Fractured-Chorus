using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class BondPresentation
    {
        public const string Title = "BONDS";
        public const string TitleJp = "絆";
        public const string SocialStatsTitle = "SOCIAL STATS";
        public const string SocialStatsJp = "共鳴ステータス";
        public const string LinkEpisodesTitle = "LINK EPISODES";
        public const string LinkEpisodesJp = "リンクエピソード";
        public const string Location = "HIMA CITY";
        public const string LocationTagline = "Music Lives in You";
        public const string TaglineRight = "PEOPLE MAKE MUSIC.";
        public const string Wordmark = "FRACTURE CHORUS";
        public const string WordmarkSub = "MUSIC PEOPLE CONNECT THE WORLD";
        public const string NextRankLabel = "NEXT RANK";
        public const string NextRankHint = "A small step,\na closer heart.";
        public const string EpisodeLockHint = "Reach higher rank to unlock new episodes.";
        public const string PromoCaption = "Music People Connect The World";
        public const int VisibleChipCount = 7;

        public static readonly string[] RosterOrder =
        {
            BondNpcIds.Ren,
            BondNpcIds.Charlotte,
            BondNpcIds.Coda,
            BondNpcIds.Astra,
            BondNpcIds.Ryo,
            BondNpcIds.MeiLin
        };

        public static string GetDisplayName(string npcId) => npcId switch
        {
            BondNpcIds.Ren => "Ren",
            BondNpcIds.Charlotte => "Charlotte",
            BondNpcIds.Coda => "Coda",
            BondNpcIds.Astra => "Astra",
            BondNpcIds.Ryo => "Ryo",
            BondNpcIds.MeiLin => "Mei Lin",
            _ => "???"
        };

        public static string GetRoleLabel(string npcId) =>
            npcId == BondNpcIds.Ren ? "Player" : string.Empty;

        public static bool IsPortraitUnlocked(string npcId) =>
            npcId == BondNpcIds.Ren
            || npcId == BondNpcIds.Charlotte
            || npcId == BondNpcIds.Coda
            || npcId == BondNpcIds.Astra;

        public static string GetBio(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte =>
                "A quiet yet passionate girl who always stays close to music. Her melodies feel like sunlight—gentle, but strong enough to change someone's day.",
            BondNpcIds.Ren =>
                "A HIMA newcomer still learning the city's rhythm. He listens first, then steps in when the chorus starts to fracture.",
            BondNpcIds.Coda =>
                "A Harmony mage who treats every silence like a measure rest—soft-spoken, precise, and stubborn about keeping people together.",
            BondNpcIds.Astra =>
                "Campus guide by day, LUXE pulse on stage. She makes a room feel like a spotlight even when she is only pointing you down a hallway.",
            BondNpcIds.Ryo =>
                "Locked. Story progress required.",
            BondNpcIds.MeiLin =>
                "Locked. Story progress required.",
            _ => "An echo waiting to be named."
        };

        public static string GetQuote(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte => "Maybe... music can make the world a little kinder, right?",
            BondNpcIds.Ren => "If the city is out of tune, someone has to count the beats.",
            BondNpcIds.Coda => "Hold the note. The rest of us will find you.",
            BondNpcIds.Astra => "Keep your chin up. The chorus sounds better when you look at it.",
            _ => string.Empty
        };
    }
}
