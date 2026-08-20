using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta.Economy;
using FracturedChorus.RunMap;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class ShopChoiceTests
    {
        [TearDown]
        public void TearDown()
        {
            PartyRunHpStore.Clear();
            RunEventCombatMods.ClearRun();
        }

        [Test]
        public void DefaultOffers_HasFivePurchasableKinds()
        {
            var offers = ShopChoiceCatalog.CreateOffers(previewAllAvailable: true);
            Assert.AreEqual(5, offers.Length);
            Assert.AreEqual(ShopChoiceKind.HealPotion, offers[0].Kind);
            Assert.AreEqual(ShopChoiceKind.Prep, offers[1].Kind);
            Assert.AreEqual(ShopChoiceKind.Armor, offers[2].Kind);
            Assert.AreEqual(ShopChoiceKind.Revive, offers[3].Kind);
            Assert.AreEqual(ShopChoiceKind.PlaceCounterPlus1, offers[4].Kind);
        }

        [Test]
        public void EmptyStore_HealAndReviveUnavailable()
        {
            var offers = ShopChoiceCatalog.CreateOffers();
            Assert.IsFalse(offers[0].Available);
            Assert.IsFalse(offers[3].Available);
        }

        [Test]
        public void ApplyPrep_AddsPendingPrep()
        {
            RunEventCombatMods.AddPrep(EconomyTable.ShopPrepAmount);
            Assert.AreEqual(EconomyTable.ShopPrepAmount, RunEventCombatMods.PendingPrep);
        }

        [Test]
        public void ApplyArmor_AddsPendingShield()
        {
            RunEventCombatMods.AddArmorShieldPercent(EconomyTable.ShopArmorShieldPercent);
            Assert.AreEqual(EconomyTable.ShopArmorShieldPercent, RunEventCombatMods.PendingShieldPercent, 0.001f);
        }

        [Test]
        public void PlaceCounter_ConsumesOnUse()
        {
            RunEventCombatMods.AddPlaceCounterPlus(1);
            Assert.AreEqual(1, RunEventCombatMods.PendingPlaceCounterPlus);
            Assert.IsTrue(RunEventCombatMods.TryConsumePlaceCounterPlus());
            Assert.AreEqual(0, RunEventCombatMods.PendingPlaceCounterPlus);
            Assert.IsFalse(RunEventCombatMods.TryConsumePlaceCounterPlus());
        }
    }
}
