using FracturedChorus.Combat.Cover;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Qte;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class AstraTvMoodStateTests
    {
        private AstraStageTvConfig _config;
        private SkillDefinitionSO _skill;
        private UnitPresetSO _playerPreset;
        private UnitPresetSO _bossPreset;

        [SetUp]
        public void SetUp()
        {
            AstraTvMoodState.Clear();
            _config = ScriptableObject.CreateInstance<AstraStageTvConfig>();
            _config.MoodDurationPhases = 2;
            _config.JoyS2ReduceBeats = 1;
            _config.JoyExtraRedCoreNotes = 1;
            _config.AngerQteWindowMult = 0.55f;
            _config.LoveOutgoingMult = 1.25f;
            _config.HateCoverCostMult = 2f;
            _config.SorrowExtraMiniAttacks = 1;

            _skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            _skill.standingBeatsAfter = 3;

            _playerPreset = ScriptableObject.CreateInstance<UnitPresetSO>();
            _playerPreset.displayName = "Ren";
            _playerPreset.role = UnitRole.Dps;
            _playerPreset.stats = new UnitStats { MaxHp = 80, HeartBeat = 180 };

            _bossPreset = ScriptableObject.CreateInstance<UnitPresetSO>();
            _bossPreset.displayName = "Astra";
            _bossPreset.role = UnitRole.Boss;
            _bossPreset.stats = new UnitStats { MaxHp = 200, HeartBeat = 120 };
        }

        [TearDown]
        public void TearDown()
        {
            AstraTvMoodState.Clear();
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_skill);
            Object.DestroyImmediate(_playerPreset);
            Object.DestroyImmediate(_bossPreset);
        }

        [Test]
        public void ShouldReroll_EveryTwoCompletedSegments()
        {
            AstraTvMoodState.Apply(AstraTvMood.Joy, 0, _config, null);
            Assert.IsFalse(AstraTvMoodState.ShouldReroll(0));
            Assert.IsFalse(AstraTvMoodState.ShouldReroll(1));
            Assert.IsTrue(AstraTvMoodState.ShouldReroll(2));
            AstraTvMoodState.MarkRolled(2);
            Assert.IsFalse(AstraTvMoodState.ShouldReroll(2));
            Assert.IsTrue(AstraTvMoodState.ShouldReroll(4));
        }

        [Test]
        public void CoversSegment_LastsConfiguredDuration()
        {
            AstraTvMoodState.Apply(AstraTvMood.Anger, 2, _config, null);
            Assert.IsFalse(AstraTvMoodState.CoversSegment(1));
            Assert.IsTrue(AstraTvMoodState.CoversSegment(2));
            Assert.IsTrue(AstraTvMoodState.CoversSegment(3));
            Assert.IsFalse(AstraTvMoodState.CoversSegment(4));
        }

        [Test]
        public void Joy_ReducesS2WithoutPendingBuff()
        {
            var unit = new CombatUnit(_playerPreset, GridSide.Player);
            Assert.AreEqual(0, unit.PendingReduceS2);
            Assert.AreEqual(3, SkillFootprintUtil.GetStandingAfter(_skill, unit));

            AstraTvMoodState.Apply(AstraTvMood.Joy, 0, _config, null);
            Assert.AreEqual(1, AstraTvMoodState.JoyS2ReduceBeats);
            Assert.AreEqual(1, AstraTvMoodState.ExtraRedCoreNotes);
            Assert.AreEqual(2, SkillFootprintUtil.GetStandingAfter(_skill, unit));
            Assert.AreEqual(0, unit.PendingReduceS2);
        }

        [Test]
        public void Hate_DoublesCoverActivateCost()
        {
            var cover = new CoverRuntime();
            Assert.AreEqual(CoverConstants.ActivateCost, cover.ActivateCost);

            AstraTvMoodState.Apply(AstraTvMood.Hate, 0, _config, null);
            Assert.AreEqual(CoverConstants.ActivateCost * 2, cover.ActivateCost);
            cover.DebugSetGauge(CoverConstants.GaugeCap);
            Assert.IsFalse(cover.CanActivate(true));
        }

        [Test]
        public void Love_SpotlightLeakAndBossOutgoing()
        {
            var spotlight = new CombatUnit(_playerPreset, GridSide.Player);
            var boss = new CombatUnit(_bossPreset, GridSide.Enemy);
            var other = new CombatUnit(_playerPreset, GridSide.Player);

            AstraTvMoodState.Apply(AstraTvMood.Love, 0, _config, spotlight);
            Assert.AreEqual(spotlight, AstraTvMoodState.ResolveLeakTarget(other));
            Assert.AreEqual(1.25f, AstraTvMoodState.ResolveOutgoingMult(spotlight, boss));
            Assert.AreEqual(1f, AstraTvMoodState.ResolveOutgoingMult(other, boss));
        }

        [Test]
        public void Anger_ShrinksQteWindows()
        {
            var profile = ScriptableObject.CreateInstance<CombatQteProfileSO>();
            profile.shrinkDuration = 0.75f;
            profile.perfectWindowSec = 0.06f;
            profile.goodWindowSec = 0.14f;
            try
            {
                Assert.AreEqual(CombatQteGrade.Perfect, profile.Evaluate(0.75f, 1f));
                Assert.AreEqual(CombatQteGrade.Good, profile.Evaluate(0.75f + 0.05f, 0.55f));
                Assert.AreEqual(CombatQteGrade.Miss, profile.Evaluate(0.75f + 0.10f, 0.55f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Sorrow_SetsExtraMiniAttacksOnly()
        {
            AstraTvMoodState.Apply(AstraTvMood.Sorrow, 0, _config, null);
            Assert.AreEqual(1, AstraTvMoodState.ExtraMiniAttacks);
            Assert.AreEqual(0, AstraTvMoodState.ExtraRedCoreNotes);
            Assert.AreEqual(0, AstraTvMoodState.JoyS2ReduceBeats);
            Assert.AreEqual(1f, AstraTvMoodState.CoverCostMult);
        }

        [Test]
        public void MoodSpriteResourcePath_MatchesLockedFace()
        {
            Assert.IsNull(AstraTvMoodState.MoodSpriteResourcePath);
            AstraTvMoodState.Apply(AstraTvMood.Joy, 0, _config, null);
            Assert.AreEqual("UI/Combat/Buffs/astra_tv_mood_joy_v1", AstraTvMoodState.MoodSpriteResourcePath);
            AstraTvMoodState.Apply(AstraTvMood.Hate, 0, _config, null);
            Assert.AreEqual("UI/Combat/Buffs/astra_tv_mood_hate_v1", AstraTvMoodState.MoodSpriteResourcePath);
            AstraTvMoodState.Clear();
            Assert.IsNull(AstraTvMoodState.MoodSpriteResourcePath);
        }
    }
}
