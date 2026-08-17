using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.RunMap;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class CampChoiceTests
    {
        [TearDown]
        public void TearDown()
        {
            PartyRunHpStore.Clear();
        }

        [Test]
        public void EmptyStore_HealAndReviveUnavailable_ContinueAvailable()
        {
            var offers = CampChoiceCatalog.CreateOffers();
            Assert.AreEqual(3, offers.Length);
            Assert.IsFalse(offers[0].Available);
            Assert.IsFalse(offers[1].Available);
            Assert.IsTrue(offers[2].Available);
            Assert.AreEqual(CampChoiceKind.Continue, offers[2].Kind);
        }

        [Test]
        public void DamagedLiving_CanHeal_DoesNotReviveDead()
        {
            PartyRunHpStore.Write("ren", 40, 100);
            PartyRunHpStore.Write("coda", 0, 80);

            Assert.IsTrue(PartyRunHpStore.CanHealLiving());
            Assert.IsTrue(PartyRunHpStore.CanRevive());

            Assert.AreEqual(1, PartyRunHpStore.HealLivingPercent(0.5f));
            Assert.IsTrue(PartyRunHpStore.TryGet("ren", out var renHp, out _));
            Assert.AreEqual(90, renHp);
            Assert.IsTrue(PartyRunHpStore.TryGet("coda", out var codaHp, out _));
            Assert.AreEqual(0, codaHp);
        }

        [Test]
        public void ReviveOne_SetsOneHp_DoesNotHealOthers()
        {
            PartyRunHpStore.Write("ren", 40, 100);
            PartyRunHpStore.Write("coda", 0, 80);

            Assert.IsTrue(PartyRunHpStore.ReviveOne(1));
            Assert.IsTrue(PartyRunHpStore.TryGet("coda", out var codaHp, out _));
            Assert.AreEqual(1, codaHp);
            Assert.IsTrue(PartyRunHpStore.TryGet("ren", out var renHp, out _));
            Assert.AreEqual(40, renHp);
            Assert.IsFalse(PartyRunHpStore.CanRevive());
        }

        [Test]
        public void FullParty_HealUnavailable()
        {
            PartyRunHpStore.Write("ren", 100, 100);
            Assert.IsFalse(PartyRunHpStore.CanHealLiving());
            Assert.IsFalse(CampChoiceCatalog.CreateOffers()[0].Available);
        }
    }
}
