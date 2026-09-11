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
        public void VisibleChipCount_IsSixNpcsPlusReserved()
        {
            Assert.AreEqual(7, BondPresentation.VisibleChipCount);
            Assert.AreEqual(6, BondPresentation.RosterOrder.Length);
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
        public void PortraitUnlock_Arc1VisibleFour()
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
        public void CharlotteCopy_MatchesMock()
        {
            StringAssert.Contains("quiet yet passionate", BondPresentation.GetBio(BondNpcIds.Charlotte));
            StringAssert.Contains("little kinder", BondPresentation.GetQuote(BondNpcIds.Charlotte));
        }

        [Test]
        public void LinkEpisodes_FiveTitles_RankGates()
        {
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes.Length);
            Assert.AreEqual("A Usual Day", BondLinkEpisodeCatalog.Episodes[0].Title);
            Assert.AreEqual(1, BondLinkEpisodeCatalog.Episodes[0].RequiredRank);
            Assert.AreEqual("After Class", BondLinkEpisodeCatalog.Episodes[1].Title);
            Assert.AreEqual(2, BondLinkEpisodeCatalog.Episodes[1].RequiredRank);
            Assert.AreEqual("A Different Melody", BondLinkEpisodeCatalog.Episodes[2].Title);
            Assert.AreEqual("Unspoken Words", BondLinkEpisodeCatalog.Episodes[3].Title);
            Assert.AreEqual("Toward Tomorrow", BondLinkEpisodeCatalog.Episodes[4].Title);
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes[4].RequiredRank);
        }

        [Test]
        public void EpisodeUnlock_UsesBondRank()
        {
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(1, 1));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(1, 2));
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(5, 5));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(0, 1));
        }
    }
}
