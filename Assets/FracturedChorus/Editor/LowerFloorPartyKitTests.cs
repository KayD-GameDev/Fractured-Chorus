using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Units;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class LowerFloorPartyKitTests
    {
        [TearDown]
        public void ClearHandoff()
        {
            CombatEncounterHandoff.ClearAll();
        }

        [Test]
        public void FloorsOneThroughThree_StartAtLevelOne()
        {
            CombatEncounterHandoff.SetPending("battle_grunts", sourceNodeId: 4, sourceFloor: 2);
            Assert.IsTrue(CombatEncounterHandoff.IsLowerFloorStart);
        }

        [Test]
        public void FloorFourAndBoss_KeepFullKit()
        {
            CombatEncounterHandoff.SetPending("battle_grunts", sourceNodeId: 8, sourceFloor: 4);
            Assert.IsFalse(CombatEncounterHandoff.IsLowerFloorStart);

            CombatEncounterHandoff.SetPending("boss", sourceNodeId: 20, sourceFloor: 13);
            Assert.IsFalse(CombatEncounterHandoff.IsLowerFloorStart);
        }

        [Test]
        public void LevelOneStats_MatchProgressionTable()
        {
            var ren = UnitStats.CreateRenLevelOne();
            Assert.AreEqual(22f, ren.Strength);
            Assert.AreEqual(74, ren.MaxHp);

            var charlotte = UnitStats.CreateTankLevelOne();
            Assert.AreEqual(15f, charlotte.Strength);
            Assert.AreEqual(140, charlotte.MaxHp);

            var coda = UnitStats.CreateMageLevelOne();
            Assert.AreEqual(30f, coda.Magic);
            Assert.AreEqual(38, coda.MaxHp);
        }
    }
}
