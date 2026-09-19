using FracturedChorus.Meta;
using UnityEngine;

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
        public const string EpisodeStoryHint = "Advance the story to unlock new episodes.";
        public static readonly Color PromoLockedTint = new Color(0.42f, 0.42f, 0.45f, 1f);
        public const string PromoCaption = "Music People Connect The World";
        public const int VisibleChipSlots = 10;

        public const string StoryHiddenCardPath =
            "Assets/FracturedChorus/Art/UI/Bonds/Cards/bond_card_story_hidden_v1.png";

        public static readonly string[] RosterOrder =
        {
            BondNpcIds.Ren,
            BondNpcIds.Charlotte,
            BondNpcIds.Coda,
            BondNpcIds.Astra,
            BondNpcIds.Ryo,
            BondNpcIds.MeiLin
        };

        public static int RosterEntryCount => RosterOrder.Length;

        public static int PageCount(int slotCount)
        {
            if (slotCount <= 0)
            {
                return 1;
            }

            return (RosterEntryCount + slotCount - 1) / slotCount;
        }

        public static int PageOffsetFor(int rosterIndex, int slotCount)
        {
            if (slotCount <= 0)
            {
                return 0;
            }

            var clamped = rosterIndex < 0 ? 0 : rosterIndex;
            if (clamped >= RosterEntryCount)
            {
                clamped = RosterEntryCount - 1;
            }

            return clamped / slotCount * slotCount;
        }

        public static int NextPageOffset(int pageOffset, int slotCount, int delta)
        {
            var pages = PageCount(slotCount);
            if (slotCount <= 0 || pages <= 0)
            {
                return 0;
            }

            var page = pageOffset / slotCount;
            var next = page + delta;
            if (next < 0)
            {
                next = 0;
            }
            else if (next >= pages)
            {
                next = pages - 1;
            }

            return next * slotCount;
        }

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

        public static bool UsesStoryHiddenFace(string npcId) =>
            !IsPortraitUnlocked(npcId)
            && npcId != BondNpcIds.Ryo
            && npcId != BondNpcIds.MeiLin;

        public static bool ShouldShowRosterLock(string npcId, BondProgress bond)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return false;
            }

            if (bond == null)
            {
                return !IsPortraitUnlocked(npcId);
            }

            return !IsBondRosterUnlocked(npcId, bond);
        }

        public static bool IsBondRosterUnlocked(string npcId, BondProgress bond)
        {
            if (!IsPortraitUnlocked(npcId) || bond == null)
            {
                return false;
            }

            return !bond.IsLocked;
        }

        public static string GetBio(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte =>
                "A HIMA bass player who anchors the party like a steady downbeat—composed on the surface, devoted to the score beneath it. She reads silence the way others read lyrics.",
            BondNpcIds.Ren =>
                "A new transfer at HIMA still learning Lumina's tempo. He watches more than he speaks—until the chorus fractures and he steps in with a violet note.",
            BondNpcIds.Coda =>
                "HIMA's Harmony mage—mid-teens, soft-spoken, and stubborn about keeping the group in tune. Every silence is a rest she refuses to let stay empty.",
            BondNpcIds.Astra =>
                "LUXE pulse on stage, HIMA campus guide off it. Golden-eyed and polished—she can make a hallway feel like a spotlight.",
            BondNpcIds.Ryo =>
                "Hamilton Police junior officer—quick to sweat, quicker to show up when Mei Lin calls. Still learning which cases stay on the right side of the law.",
            BondNpcIds.MeiLin =>
                "Hamilton PD inspector—stern, tired, always three steps ahead of the report on her desk. The Opening crime scene never really left her.",
            _ => "An echo waiting to be named."
        };

        public static string GetQuote(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte =>
                "If the melody wavers, hold the bass line. Someone has to remember how the song goes.",
            BondNpcIds.Ren => "Every shot is a note. Music breaks the silence.",
            BondNpcIds.Coda => "Hold the note. The rest of us will find you.",
            BondNpcIds.Astra => "Keep your chin up. The chorus sounds better when you look at it.",
            BondNpcIds.Ryo => "W-we're not supposed to be here… but if someone's in trouble, I can't just walk away.",
            BondNpcIds.MeiLin => "Listen carefully. People lie. The scene doesn't.",
            _ => string.Empty
        };

        public static string GetEpisodeHint(string npcId) =>
            npcId == BondNpcIds.Ren || npcId == BondNpcIds.Astra
                ? EpisodeStoryHint
                : EpisodeLockHint;

        public static void GetNavPanelCopy(int navIndex, out string title, out string subtitleJp)
        {
            if (navIndex == BondNavIds.Link)
            {
                title = LinkEpisodesTitle;
                subtitleJp = LinkEpisodesJp;
                return;
            }

            title = SocialStatsTitle;
            subtitleJp = SocialStatsJp;
        }
    }
}
