using System;
using System.IO;
using FracturedChorus.Combat.Difficulty;
using FracturedChorus.Meta;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    /// <summary>
    /// Chốt số theo bảng độ khó trong GDD §4 và docs/combat/DIFFICULTY.md. Ba bậc dùng chung luật
    /// trận, chỉ khác tỷ lệ và lệch level, nên bất kỳ thay đổi nào ở đây phải sửa tài liệu trước.
    /// </summary>
    public class DifficultyMultiplierTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void OnBeat_MatchesGddRow()
        {
            var mult = DifficultyRuntime.Get(DifficultyRuntime.OnBeat);

            Assert.AreEqual(-2, mult.EnemyLevelOffset);
            Assert.AreEqual(13, mult.RecommendedPartyLevel);
            Assert.AreEqual(0.85f, mult.EnemyHp, Tolerance);
            Assert.AreEqual(0.85f, mult.EnemyDamage, Tolerance);
            Assert.AreEqual(0.8f, mult.PierceFrontBias, Tolerance);
            Assert.AreEqual(1.1f, mult.NotesEarn, Tolerance);
            Assert.AreEqual(0f, mult.EarlyLateBlockPenalty, Tolerance);
        }

        [Test]
        public void Cadence_IsTheBaseline()
        {
            var mult = DifficultyRuntime.Get(DifficultyRuntime.Cadence);

            Assert.AreEqual(0, mult.EnemyLevelOffset);
            Assert.AreEqual(DifficultyRuntime.CadencePartyTargetLevel, mult.RecommendedPartyLevel);
            Assert.AreEqual(1f, mult.EnemyHp, Tolerance);
            Assert.AreEqual(1f, mult.EnemyDamage, Tolerance);
            Assert.AreEqual(1f, mult.PierceFrontBias, Tolerance);
            Assert.AreEqual(1f, mult.NotesEarn, Tolerance);
            Assert.AreEqual(0f, mult.EarlyLateBlockPenalty, Tolerance);
        }

        [Test]
        public void OffBeat_MatchesGddRow()
        {
            var mult = DifficultyRuntime.Get(DifficultyRuntime.OffBeat);

            Assert.AreEqual(2, mult.EnemyLevelOffset);
            Assert.AreEqual(17, mult.RecommendedPartyLevel);
            Assert.AreEqual(1.15f, mult.EnemyHp, Tolerance);
            Assert.AreEqual(1.2f, mult.EnemyDamage, Tolerance);
            Assert.AreEqual(1.15f, mult.PierceFrontBias, Tolerance);
            Assert.AreEqual(1f, mult.NotesEarn, Tolerance);
            Assert.AreEqual(0.1f, mult.EarlyLateBlockPenalty, Tolerance);
        }

        /// <summary>Tỷ lệ sau khi gộp lệch level — On Beat còn ~¾ Cadence, Off Beat hơn ~30%.</summary>
        [Test]
        public void ResolvedRatios_FoldLevelOffsetIntoHpAndDamage()
        {
            var onBeat = DifficultyRuntime.Get(DifficultyRuntime.OnBeat);
            Assert.AreEqual(0.748f, onBeat.ResolvedEnemyHp, Tolerance);
            Assert.AreEqual(0.765f, onBeat.ResolvedEnemyDamage, Tolerance);

            var cadence = DifficultyRuntime.Get(DifficultyRuntime.Cadence);
            Assert.AreEqual(1f, cadence.ResolvedEnemyHp, Tolerance);
            Assert.AreEqual(1f, cadence.ResolvedEnemyDamage, Tolerance);

            var offBeat = DifficultyRuntime.Get(DifficultyRuntime.OffBeat);
            Assert.AreEqual(1.288f, offBeat.ResolvedEnemyHp, Tolerance);
            Assert.AreEqual(1.32f, offBeat.ResolvedEnemyDamage, Tolerance);
        }

        [Test]
        public void EffectiveBossLevel_ShiftsWithOffset()
        {
            Assert.AreEqual(16, DifficultyRuntime.Get(DifficultyRuntime.OnBeat).EffectiveBossLevel);
            Assert.AreEqual(18, DifficultyRuntime.Get(DifficultyRuntime.Cadence).EffectiveBossLevel);
            Assert.AreEqual(20, DifficultyRuntime.Get(DifficultyRuntime.OffBeat).EffectiveBossLevel);
        }

        [Test]
        public void UnknownTier_FallsBackToCadence()
        {
            var mult = DifficultyRuntime.Get(99);

            Assert.AreEqual(0, mult.EnemyLevelOffset);
            Assert.AreEqual(1f, mult.EnemyHp, Tolerance);
        }
    }

    /// <summary>
    /// GDD: người chơi chọn bậc lúc bắt đầu, sau đó bậc khóa theo file lưu. Bộ test này giữ đúng
    /// hai vế đó — chốt lúc New Game, và không bị đổi bởi setting ở menu sau này.
    /// </summary>
    public class DifficultyLockTests
    {
        private const int TestSlot = GameMetaSaveLoad.SlotCount - 1;

        private byte[] _backup;
        private int _previousActiveSlot;
        private Func<int> _previousProvider;

        [SetUp]
        public void SetUp()
        {
            GameMetaSaveLoad.MigrateLegacySaveOnce();

            _previousActiveSlot = GameMetaSaveLoad.ActiveSlot;
            _previousProvider = GameMetaSession.DefaultDifficultyProvider;
            var path = GameMetaSaveLoad.GetSlotPath(TestSlot);
            _backup = File.Exists(path) ? File.ReadAllBytes(path) : null;
            GameMetaSaveLoad.Delete(TestSlot);
        }

        [TearDown]
        public void TearDown()
        {
            GameMetaSession.PendingNewGameDifficulty = null;
            GameMetaSession.DefaultDifficultyProvider = _previousProvider;

            GameMetaSaveLoad.Delete(TestSlot);
            if (_backup != null)
            {
                Directory.CreateDirectory(GameMetaSaveLoad.SavesDirectory);
                File.WriteAllBytes(GameMetaSaveLoad.GetSlotPath(TestSlot), _backup);
            }

            GameMetaSaveLoad.ActiveSlot = _previousActiveSlot;
        }

        [Test]
        public void BeginNewGame_WritesPendingDifficultyIntoSave()
        {
            GameMetaSession.DefaultDifficultyProvider = () => DifficultyRuntime.Cadence;
            GameMetaSession.PendingNewGameDifficulty = DifficultyRuntime.OffBeat;

            GameMetaSession.BeginNewGame(TestSlot);

            Assert.AreEqual(DifficultyRuntime.OffBeat, GameMetaSession.Current.Difficulty);
            Assert.IsNull(
                GameMetaSession.PendingNewGameDifficulty,
                "Dùng xong phải xoá, không thì ván sau thừa hưởng bậc cũ.");

            var loaded = GameMetaSaveLoad.TryLoad(TestSlot);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(DifficultyRuntime.OffBeat, loaded.Difficulty);
        }

        [Test]
        public void BeginNewGame_WithoutPending_ReadsMenuSelection()
        {
            GameMetaSession.PendingNewGameDifficulty = null;
            GameMetaSession.DefaultDifficultyProvider = () => DifficultyRuntime.OnBeat;

            GameMetaSession.BeginNewGame(TestSlot);

            Assert.AreEqual(DifficultyRuntime.OnBeat, GameMetaSession.Current.Difficulty);
        }

        /// <summary>
        /// Cuối Prologue state bị thay mới. Bậc đã khóa phải đi theo, kể cả khi người chơi ghé
        /// Config đổi chip giữa chừng.
        /// </summary>
        [Test]
        public void BeginHubAfterOpening_KeepsLockedDifficulty()
        {
            GameMetaSession.PendingNewGameDifficulty = DifficultyRuntime.OffBeat;
            GameMetaSession.BeginNewGame(TestSlot);

            GameMetaSession.DefaultDifficultyProvider = () => DifficultyRuntime.OnBeat;
            GameMetaSession.BeginHubAfterOpening();

            Assert.AreEqual(DifficultyRuntime.OffBeat, GameMetaSession.Current.Difficulty);
        }

        [Test]
        public void BeginHubEnRouteToHima_KeepsLockedDifficulty()
        {
            GameMetaSession.PendingNewGameDifficulty = DifficultyRuntime.OnBeat;
            GameMetaSession.BeginNewGame(TestSlot);

            GameMetaSession.DefaultDifficultyProvider = () => DifficultyRuntime.OffBeat;
            GameMetaSession.BeginHubEnRouteToHima();

            Assert.AreEqual(DifficultyRuntime.OnBeat, GameMetaSession.Current.Difficulty);
        }
    }
}
