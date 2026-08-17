using FracturedChorus.Combat.Grid;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class EventChoiceTableTests
    {
        [TearDown]
        public void TearDown()
        {
            EventClaimStore.ClearRun();
            RunEventCombatMods.ClearRun();
        }

        [Test]
        public void DefaultCatalog_HasEightChoices()
        {
            var pool = EventChoiceSO.CreateDefaultCatalog();
            try
            {
                Assert.AreEqual(8, pool.Length);
                Assert.AreEqual(EventChoiceKind.NextBattleDamage, pool[0].Kind);
                Assert.AreEqual(EventChoiceKind.HealOverflowShield, pool[1].Kind);
                Assert.AreEqual(EventChoiceKind.NextBattleDefense, pool[2].Kind);
                Assert.AreEqual(EventChoiceKind.FirstNoteReduceS2, pool[3].Kind);
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        [Test]
        public void PickOffers_ReturnsThreeUnique()
        {
            var pool = EventChoiceSO.CreateDefaultCatalog();
            try
            {
                var offers = SeededOfferPicker.Pick(pool, 11, 3);
                Assert.AreEqual(3, offers.Length);
                Assert.AreNotEqual(offers[0].Id, offers[1].Id);
                Assert.AreNotEqual(offers[0].Id, offers[2].Id);
                Assert.AreNotEqual(offers[1].Id, offers[2].Id);
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        [Test]
        public void PickOffers_SameSeed_SameOrder()
        {
            var pool = EventChoiceSO.CreateDefaultCatalog();
            try
            {
                var a = SeededOfferPicker.Pick(pool, 99, 3);
                var b = SeededOfferPicker.Pick(pool, 99, 3);
                for (var i = 0; i < 3; i++)
                {
                    Assert.AreEqual(a[i].Id, b[i].Id);
                }
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        [Test]
        public void CombatMods_DamageAndDefenseStackThenConsume()
        {
            var pool = EventChoiceSO.CreateDefaultCatalog();
            try
            {
                RunEventCombatMods.ApplyChoice(pool[0]);
                RunEventCombatMods.ApplyChoice(pool[2]);
                Assert.AreEqual(1.05f, RunEventCombatMods.NextOutgoingMul, 0.0001f);
                Assert.AreEqual(0.90f, RunEventCombatMods.NextIncomingMul, 0.0001f);
                Assert.AreEqual(10.5f, RunEventCombatMods.ModifyOutgoing(GridSide.Player, 10f), 0.0001f);
                Assert.AreEqual(9f, RunEventCombatMods.ModifyIncoming(GridSide.Player, 10f), 0.0001f);
                RunEventCombatMods.ConsumeBattle();
                Assert.AreEqual(1f, RunEventCombatMods.NextOutgoingMul);
                Assert.AreEqual(1f, RunEventCombatMods.NextIncomingMul);
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        private static void DestroyCatalog(EventChoiceSO[] pool)
        {
            if (pool == null)
            {
                return;
            }

            for (var i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null)
                {
                    Object.DestroyImmediate(pool[i]);
                }
            }
        }
    }
}
