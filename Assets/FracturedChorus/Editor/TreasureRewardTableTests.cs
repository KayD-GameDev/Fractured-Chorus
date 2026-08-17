using FracturedChorus.Data;
using FracturedChorus.RunMap;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class TreasureRewardTableTests
    {
        [TearDown]
        public void TearDown()
        {
            TreasureClaimStore.ClearRun();
        }

        [Test]
        public void PickOffers_SameSeed_SameOrder()
        {
            var pool = TreasureRewardSO.CreateDefaultCatalog();
            try
            {
                var a = TreasureRewardTableSO.PickOffers(pool, 42, 3);
                var b = TreasureRewardTableSO.PickOffers(pool, 42, 3);

                Assert.AreEqual(3, a.Length);
                Assert.AreEqual(a.Length, b.Length);
                for (var i = 0; i < a.Length; i++)
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
        public void PickOffers_ReturnsUniqueIds()
        {
            var pool = TreasureRewardSO.CreateDefaultCatalog();
            try
            {
                var offers = TreasureRewardTableSO.PickOffers(pool, 7, 3);
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
        public void DefaultCatalog_HasHealPotionAndPlaceCounter()
        {
            var pool = TreasureRewardSO.CreateDefaultCatalog();
            try
            {
                Assert.AreEqual(TreasureRewardKind.HealPotion, pool[0].Kind);
                Assert.AreEqual(TreasureRewardKind.PlaceCounterPlus1, pool[1].Kind);
                Assert.AreEqual(TreasureRewardKind.Notes, pool[2].Kind);
                Assert.AreEqual(TreasureRewardSO.CadenceFlaskId, pool[0].Id);
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        [Test]
        public void ClaimStore_RecordsPendingApply()
        {
            var pool = TreasureRewardSO.CreateDefaultCatalog();
            try
            {
                TreasureClaimStore.Record(pool[0], 12, 6);

                Assert.AreEqual(1, TreasureClaimStore.All.Count);
                Assert.IsTrue(TreasureClaimStore.Last.HasValue);
                Assert.AreEqual(TreasureRewardSO.CadenceFlaskId, TreasureClaimStore.Last.Value.RewardId);
                Assert.AreEqual(TreasureRewardKind.HealPotion, TreasureClaimStore.Last.Value.Kind);
                Assert.AreEqual(12, TreasureClaimStore.Last.Value.NodeId);
            }
            finally
            {
                DestroyCatalog(pool);
            }
        }

        private static void DestroyCatalog(TreasureRewardSO[] pool)
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
