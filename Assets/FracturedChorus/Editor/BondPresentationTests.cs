using FracturedChorus.Hub;
using FracturedChorus.Meta;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class BondPresentationTests
    {
        [Test]
        public void RosterOrder_MatchesHubBondList()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    BondNpcIds.Ren,
                    BondNpcIds.Charlotte,
                    BondNpcIds.Coda,
                    BondNpcIds.Astra,
                    BondNpcIds.Ryo,
                    BondNpcIds.MeiLin
                },
                BondPresentation.RosterOrder);
        }

        [Test]
        public void VisibleChipSlots_AreTen_SixNpcs()
        {
            Assert.AreEqual(10, BondPresentation.VisibleChipSlots);
            Assert.AreEqual(6, BondPresentation.RosterOrder.Length);
            Assert.AreEqual(6, BondPresentation.RosterEntryCount);
        }

        [Test]
        public void RosterPages_TenSlots_SinglePageForSixNpcs()
        {
            Assert.AreEqual(1, BondPresentation.PageCount(10));
            Assert.AreEqual(0, BondPresentation.PageOffsetFor(5, 10));
            Assert.AreEqual(0, BondPresentation.NextPageOffset(0, 10, 1));
        }

        [Test]
        public void DisplayNames_MatchLock()
        {
            Assert.AreEqual("Ren", BondPresentation.GetDisplayName(BondNpcIds.Ren));
            Assert.AreEqual("Charlotte", BondPresentation.GetDisplayName(BondNpcIds.Charlotte));
            Assert.AreEqual("Coda", BondPresentation.GetDisplayName(BondNpcIds.Coda));
            Assert.AreEqual("Astra", BondPresentation.GetDisplayName(BondNpcIds.Astra));
            Assert.AreEqual("Ryo", BondPresentation.GetDisplayName(BondNpcIds.Ryo));
            Assert.AreEqual("Mei Lin", BondPresentation.GetDisplayName(BondNpcIds.MeiLin));
            Assert.AreEqual("???", BondPresentation.GetDisplayName("reserved"));
        }

        [Test]
        public void RoleLabel_OnlyRenIsPlayer()
        {
            Assert.AreEqual("Player", BondPresentation.GetRoleLabel(BondNpcIds.Ren));
            Assert.AreEqual(string.Empty, BondPresentation.GetRoleLabel(BondNpcIds.Charlotte));
        }

        [Test]
        public void PortraitUnlock_Arc1VisibleFour_AstraOnRoster()
        {
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ren));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Charlotte));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Coda));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Astra));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ryo));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.MeiLin));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(null));
        }

        [Test]
        public void CharlotteCopy_MatchesLock()
        {
            StringAssert.Contains("HIMA bass player", BondPresentation.GetBio(BondNpcIds.Charlotte));
            StringAssert.Contains("bass line", BondPresentation.GetQuote(BondNpcIds.Charlotte));
        }

        [Test]
        public void LinkEpisodes_DefaultFiveTitles_RankGates()
        {
            var episodes = BondLinkEpisodeCatalog.GetEpisodes(BondNpcIds.Coda);
            Assert.AreEqual(5, episodes.Length);
            Assert.AreEqual("A Usual Day", episodes[0].Title);
            Assert.AreEqual(1, episodes[0].RequiredRank);
            Assert.AreEqual("After Class", episodes[1].Title);
            Assert.AreEqual(2, episodes[1].RequiredRank);
            Assert.AreEqual("A Different Melody", episodes[2].Title);
            Assert.AreEqual("Unspoken Words", episodes[3].Title);
            Assert.AreEqual("Toward Tomorrow", episodes[4].Title);
            Assert.AreEqual(5, episodes[4].RequiredRank);
        }

        [Test]
        public void LinkEpisodes_RenPastTitles()
        {
            var episodes = BondLinkEpisodeCatalog.GetEpisodes(BondNpcIds.Ren);
            Assert.AreEqual(5, episodes.Length);
            Assert.AreEqual("The File He Gave Away", episodes[0].Title);
            Assert.AreEqual("Demo Seven", episodes[1].Title);
            Assert.AreEqual("Dead Handle", episodes[2].Title);
            Assert.AreEqual("No Name, No Face", episodes[3].Title);
            Assert.AreEqual("Still Keeping the Files", episodes[4].Title);
        }

        [Test]
        public void LinkEpisodes_CharlotteArcTitles()
        {
            var episodes = BondLinkEpisodeCatalog.GetEpisodes(BondNpcIds.Charlotte);
            Assert.AreEqual(5, episodes.Length);
            Assert.AreEqual("After the Bell", episodes[0].Title);
            Assert.AreEqual(1, episodes[0].RequiredRank);
            Assert.AreEqual("The Page He Keeps", episodes[1].Title);
            Assert.AreEqual(2, episodes[1].RequiredRank);
            Assert.AreEqual("Faded Ink", episodes[2].Title);
            Assert.AreEqual(3, episodes[2].RequiredRank);
            Assert.AreEqual("Grandfather's Mark", episodes[3].Title);
            Assert.AreEqual(4, episodes[3].RequiredRank);
            Assert.AreEqual("An Unfinished Rest", episodes[4].Title);
            Assert.AreEqual(5, episodes[4].RequiredRank);
        }

        [Test]
        public void CharlottePromoArt_FivePaths()
        {
            Assert.AreEqual(5, BondPromoCatalog.CharlotteEpisodePromoPaths.Length);
            StringAssert.Contains("ep01_after_the_bell", BondPromoCatalog.CharlotteEpisodePromoPaths[0]);
            StringAssert.Contains("ep05_unfinished_rest", BondPromoCatalog.CharlotteEpisodePromoPaths[4]);
            Assert.IsTrue(BondPromoCatalog.UsesEpisodePromos(BondNpcIds.Charlotte));
            Assert.IsTrue(BondPromoCatalog.UsesEpisodePromos(BondNpcIds.Ren));
            Assert.AreEqual(5, BondPromoCatalog.RenEpisodePromoPaths.Length);
            StringAssert.Contains("ep01_the_file_he_gave_away", BondPromoCatalog.RenEpisodePromoPaths[0]);
            StringAssert.Contains("ep05_still_keeping_the_files", BondPromoCatalog.RenEpisodePromoPaths[4]);
        }

        [Test]
        public void EpisodeHint_StoryGatedNpcsUseStoryProgress()
        {
            Assert.AreEqual(
                BondPresentation.EpisodeStoryHint,
                BondPresentation.GetEpisodeHint(BondNpcIds.Ren));
            Assert.AreEqual(
                BondPresentation.EpisodeStoryHint,
                BondPresentation.GetEpisodeHint(BondNpcIds.Astra));
            Assert.AreEqual(
                BondPresentation.EpisodeLockHint,
                BondPresentation.GetEpisodeHint(BondNpcIds.Charlotte));
        }

        [Test]
        public void EpisodeUnlock_UsesBondRank()
        {
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(1, 1));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(1, 2));
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(5, 5));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(0, 1));
        }

        [Test]
        public void DefaultPromo_UsesEpisodePlaceholder()
        {
            StringAssert.Contains(
                "bonds_episode_promo_placeholder_v1",
                BondPromoCatalog.DefaultPromoPath);
        }

        [Test]
        public void BondNav_OnlySocialStatsAndLinkImplemented()
        {
            Assert.IsTrue(BondNavIds.IsImplemented(BondNavIds.SocialStats));
            Assert.IsTrue(BondNavIds.IsImplemented(BondNavIds.Link));
            Assert.IsFalse(BondNavIds.IsImplemented(BondNavIds.Conversations));
        }
        [Test]
        public void StoryHiddenFace_LockedNotMetExceptRyoAndMeiLin()
        {
            Assert.IsFalse(BondPresentation.UsesStoryHiddenFace(BondNpcIds.Ren));
            Assert.IsFalse(BondPresentation.UsesStoryHiddenFace(BondNpcIds.Ryo));
            Assert.IsFalse(BondPresentation.UsesStoryHiddenFace(BondNpcIds.MeiLin));
            Assert.IsFalse(BondPresentation.UsesStoryHiddenFace(BondNpcIds.Astra));
        }

        [Test]
        public void RosterLock_ShowsForMeiLinNotAstra()
        {
            var meiLin = new BondProgress(BondNpcIds.MeiLin, EchoKey.Dissonance) { IsLocked = true };
            var astra = new BondProgress(BondNpcIds.Astra, EchoKey.Harmony);
            Assert.IsTrue(BondPresentation.ShouldShowRosterLock(BondNpcIds.MeiLin, meiLin));
            Assert.IsFalse(BondPresentation.ShouldShowRosterLock(BondNpcIds.Astra, astra));
        }

        [Test]
        public void NavPanelCopy_SwitchesWithTab()
        {
            BondPresentation.GetNavPanelCopy(BondNavIds.SocialStats, out var socialTitle, out var socialJp);
            Assert.AreEqual(BondPresentation.SocialStatsTitle, socialTitle);
            Assert.AreEqual(BondPresentation.SocialStatsJp, socialJp);

            BondPresentation.GetNavPanelCopy(BondNavIds.Link, out var linkTitle, out var linkJp);
            Assert.AreEqual(BondPresentation.LinkEpisodesTitle, linkTitle);
            Assert.AreEqual(BondPresentation.LinkEpisodesJp, linkJp);
        }

        [Test]
        public void BondRosterUnlock_BlocksMeiLinAndLockedBonds()
        {
            var meiLin = new BondProgress(BondNpcIds.MeiLin, EchoKey.Dissonance) { IsLocked = true };
            Assert.IsFalse(BondPresentation.IsBondRosterUnlocked(BondNpcIds.MeiLin, meiLin));

            var charlotte = new BondProgress(BondNpcIds.Charlotte, EchoKey.Harmony);
            Assert.IsTrue(BondPresentation.IsBondRosterUnlocked(BondNpcIds.Charlotte, charlotte));

            charlotte.IsLocked = true;
            Assert.IsFalse(BondPresentation.IsBondRosterUnlocked(BondNpcIds.Charlotte, charlotte));
        }

        [Test]
        public void WrapRosterIndex_Cycles()
        {
            Assert.AreEqual(1, BondsMenuUI.WrapRosterIndex(0, 1, BondPresentation.RosterEntryCount));
            Assert.AreEqual(0, BondsMenuUI.WrapRosterIndex(5, 1, BondPresentation.RosterEntryCount));
            Assert.AreEqual(5, BondsMenuUI.WrapRosterIndex(0, -1, BondPresentation.RosterEntryCount));
        }
    }
}
