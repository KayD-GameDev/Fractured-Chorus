using System.IO;
using System.Text;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FracturedChorus.Tests
{
    public class SaveCryptoTests
    {
        private const string Sample = "{\"header\":{\"slotIndex\":3},\"data\":{\"saveVersion\":3}}";

        [Test]
        public void Encrypt_RoundTripsBackToSameJson()
        {
            var blob = SaveCrypto.Encrypt(Sample);

            Assert.IsTrue(SaveCrypto.LooksEncrypted(blob), "Blob phải có magic FCS1.");
            Assert.AreEqual(SaveBlobStatus.Success, SaveCrypto.TryDecrypt(blob, out var json));
            Assert.AreEqual(Sample, json);
        }

        [Test]
        public void Encrypt_UsesFreshIv_SoTwoWritesDiffer()
        {
            var first = SaveCrypto.Encrypt(Sample);
            var second = SaveCrypto.Encrypt(Sample);

            Assert.AreNotEqual(
                System.Convert.ToBase64String(first),
                System.Convert.ToBase64String(second),
                "IV ngẫu nhiên mỗi lần ghi nên hai blob không được giống nhau.");
        }

        [Test]
        public void TryDecrypt_DetectsTamperedCiphertext()
        {
            var blob = SaveCrypto.Encrypt(Sample);
            blob[blob.Length - 1] ^= 0xFF;

            Assert.AreEqual(SaveBlobStatus.Tampered, SaveCrypto.TryDecrypt(blob, out var json));
            Assert.IsNull(json);
        }

        [Test]
        public void TryDecrypt_DetectsTamperedIv()
        {
            var blob = SaveCrypto.Encrypt(Sample);
            // Byte thứ 5 trở đi là IV: sửa IV mà MAC vẫn cũ thì phải bị bắt.
            blob[6] ^= 0x5A;

            Assert.AreEqual(SaveBlobStatus.Tampered, SaveCrypto.TryDecrypt(blob, out _));
        }

        [Test]
        public void TryDecrypt_AcceptsLegacyPlaintext()
        {
            var blob = Encoding.UTF8.GetBytes(Sample);

            Assert.AreEqual(SaveBlobStatus.Plaintext, SaveCrypto.TryDecrypt(blob, out var json));
            Assert.AreEqual(Sample, json);
        }

        [Test]
        public void TryDecrypt_TreatsEmptyBlobAsEmpty()
        {
            Assert.AreEqual(SaveBlobStatus.Empty, SaveCrypto.TryDecrypt(new byte[0], out _));
            Assert.AreEqual(SaveBlobStatus.Empty, SaveCrypto.TryDecrypt(null, out _));
        }
    }

    public class SaveSlotIoTests
    {
        // Slot cuối ít dùng nhất; vẫn backup file thật để không phá save của người đang test.
        private const int TestSlot = GameMetaSaveLoad.SlotCount - 1;

        private byte[] _backup;
        private int _previousActiveSlot;

        [SetUp]
        public void SetUp()
        {
            // Đốt cờ one-shot trước để migration không chen ngang giữa các assert.
            GameMetaSaveLoad.MigrateLegacySaveOnce();

            _previousActiveSlot = GameMetaSaveLoad.ActiveSlot;
            var path = GameMetaSaveLoad.GetSlotPath(TestSlot);
            _backup = File.Exists(path) ? File.ReadAllBytes(path) : null;
            GameMetaSaveLoad.Delete(TestSlot);
        }

        [TearDown]
        public void TearDown()
        {
            GameMetaSaveLoad.Delete(TestSlot);
            if (_backup != null)
            {
                Directory.CreateDirectory(GameMetaSaveLoad.SavesDirectory);
                File.WriteAllBytes(GameMetaSaveLoad.GetSlotPath(TestSlot), _backup);
            }

            GameMetaSaveLoad.ActiveSlot = _previousActiveSlot;
        }

        [Test]
        public void SaveThenLoad_RestoresProgressData()
        {
            var state = BuildSampleState();

            Assert.IsTrue(GameMetaSaveLoad.TrySave(state, TestSlot));
            Assert.IsTrue(GameMetaSaveLoad.SlotExists(TestSlot));

            var loaded = GameMetaSaveLoad.TryLoad(TestSlot);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(GameMetaState.SaveVersion, loaded.SaveVersionId);
            Assert.AreEqual(1234, loaded.Wallet.Notes);
            Assert.AreEqual(9182, loaded.RunSnapshot.Seed);
            Assert.AreEqual(new[] { 3, 7, 11 }, loaded.RunSnapshot.VisitedNodeIds);
            Assert.IsTrue(loaded.RunSnapshot.PulseCleared);
            Assert.IsFalse(loaded.RunSnapshot.CanticleCleared);
        }

        [Test]
        public void SaveThenLoad_RestoresPartyVitalsAndLevels()
        {
            var state = BuildSampleState();

            Assert.IsTrue(GameMetaSaveLoad.TrySave(state, TestSlot));
            var loaded = GameMetaSaveLoad.TryLoad(TestSlot);

            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.PartyVitals.TryGet(PartyCharacterIds.Ren, out var hp, out var maxHp));
            Assert.AreEqual(42, hp);
            Assert.AreEqual(180, maxHp);

            var ren = loaded.Loadout.GetOrCreate(PartyCharacterIds.Ren);
            Assert.AreEqual(21, ren.Level);
            Assert.AreEqual(640, ren.Exp);
        }

        [Test]
        public void SaveThenLoad_RestoresHubLocationAndPlaytime()
        {
            var state = BuildSampleState();

            Assert.IsTrue(GameMetaSaveLoad.TrySave(state, TestSlot));
            var loaded = GameMetaSaveLoad.TryLoad(TestSlot);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("downtown", loaded.HubLocation.LastLocationId);
            Assert.AreEqual("flower_shop", loaded.HubLocation.LastSubLocationId);
            Assert.AreEqual(3725d, loaded.Playtime.TotalSeconds, 0.5d);
        }

        [Test]
        public void ListHeaders_ShowsPlaytimeAndFlagsCorruptSlot()
        {
            var state = BuildSampleState();
            Assert.IsTrue(GameMetaSaveLoad.TrySave(state, TestSlot));

            var header = GameMetaSaveLoad.ListHeaders()[TestSlot];
            Assert.IsFalse(header.isEmpty);
            Assert.IsFalse(header.isCorrupted);
            Assert.AreEqual("1:02", header.FormatPlayTime());

            var path = GameMetaSaveLoad.GetSlotPath(TestSlot);
            var blob = File.ReadAllBytes(path);
            blob[blob.Length - 1] ^= 0xFF;
            File.WriteAllBytes(path, blob);

            LogAssert.ignoreFailingMessages = true;
            var corrupted = GameMetaSaveLoad.ListHeaders()[TestSlot];
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(corrupted.isCorrupted, "Slot hỏng phải báo CORRUPTED thay vì hiện trống.");
            Assert.IsFalse(corrupted.isEmpty);
        }

        [Test]
        public void ExportThenImportJson_PreservesState()
        {
            Assert.IsTrue(GameMetaSaveLoad.TrySave(BuildSampleState(), TestSlot));

            var json = GameMetaSaveLoad.ExportSlotJson(TestSlot);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));

            GameMetaSaveLoad.Delete(TestSlot);
            Assert.IsTrue(GameMetaSaveLoad.ImportSlotJson(TestSlot, json));

            var loaded = GameMetaSaveLoad.TryLoad(TestSlot);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(9182, loaded.RunSnapshot.Seed);
        }

        [Test]
        public void TryLoad_ReadsLegacyPlaintextSlotFile()
        {
            var json = JsonUtility.ToJson(
                new SaveSlotFile
                {
                    header = SaveSlotHeader.Empty(TestSlot),
                    data = GameMetaSaveData.FromState(BuildSampleState())
                },
                prettyPrint: false);

            Directory.CreateDirectory(GameMetaSaveLoad.SavesDirectory);
            var plaintextPath = GameMetaSaveLoad.GetPlaintextSlotPath(TestSlot);
            File.WriteAllText(plaintextPath, json);

            try
            {
                var loaded = GameMetaSaveLoad.TryLoad(TestSlot);
                Assert.IsNotNull(loaded, "Save JSON đời cũ vẫn phải đọc được.");
                Assert.AreEqual(1234, loaded.Wallet.Notes);
            }
            finally
            {
                DeleteIfExists(plaintextPath);
                DeleteIfExists(GameMetaSaveLoad.GetLegacyBackupSlotPath(TestSlot));
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static GameMetaState BuildSampleState()
        {
            var state = GameMetaState.CreateNew();
            state.Wallet.Notes = 1234;
            state.Difficulty = 2;

            state.RunSnapshot.Seed = 9182;
            state.RunSnapshot.HasActiveRun = true;
            state.RunSnapshot.CurrentFloor = 4;
            state.RunSnapshot.CurrentNodeId = 11;
            state.RunSnapshot.ClearedNodeIds = new[] { 3, 7 };
            state.RunSnapshot.VisitedNodeIds = new[] { 3, 7, 11 };
            state.RunSnapshot.PulseCleared = true;
            state.RunSnapshot.EchoCleared = false;
            state.RunSnapshot.CanticleCleared = false;

            state.PartyVitals.Set(PartyCharacterIds.Ren, 42, 180);
            state.PartyVitals.Set(PartyCharacterIds.Charlotte, 150, 150);

            var ren = state.Loadout.GetOrCreate(PartyCharacterIds.Ren);
            ren.Level = 21;
            ren.Exp = 640;

            state.HubLocation.SetLocation("downtown", "flower_shop");
            state.Playtime.TotalSeconds = 3725d;

            return state;
        }
    }

    public class SaveMigrationTests
    {
        /// <summary>
        /// Save version 2 không có level/exp, mảng visited hay party vitals — load lại không được
        /// đẩy nhân vật về Lv0 hoặc ném exception.
        /// </summary>
        [Test]
        public void LegacyVersion2Payload_FillsDefaultsInsteadOfZeroes()
        {
            var legacy = new GameMetaSaveData
            {
                saveVersion = 2,
                dateMonth = 6,
                dateDay = 12,
                notes = 500,
                difficulty = 1,
                runSeed = 77,
                runActive = true,
                runNodeId = 5,
                runClearedNodeIds = new[] { 1, 2 },
                loadouts = new[]
                {
                    new LoadoutEntry
                    {
                        characterId = PartyCharacterIds.Ren,
                        skill0 = "skill_a",
                        skill1 = string.Empty,
                        skill2 = string.Empty
                    }
                }
            };

            var state = JsonUtility.FromJson<GameMetaSaveData>(JsonUtility.ToJson(legacy)).ToState();

            Assert.AreEqual(2, state.SaveVersionId);
            Assert.AreEqual(500, state.Wallet.Notes);
            Assert.AreEqual(0, state.RunSnapshot.VisitedNodeIds.Length);
            Assert.AreEqual(0d, state.Playtime.TotalSeconds);
            Assert.IsFalse(state.HubLocation.HasLocation);

            var ren = state.Loadout.GetOrCreate(PartyCharacterIds.Ren);
            Assert.AreEqual(CharacterLoadoutEntry.DefaultLevel, ren.Level);
            Assert.AreEqual("skill_a", ren.EquippedSkillIds[0]);
        }

        [Test]
        public void Version3Payload_KeepsExplicitLevel()
        {
            var data = new GameMetaSaveData
            {
                saveVersion = 3,
                loadouts = new[]
                {
                    new LoadoutEntry { characterId = PartyCharacterIds.Coda, level = 7, exp = 120 }
                }
            };

            var coda = data.ToState().Loadout.GetOrCreate(PartyCharacterIds.Coda);
            Assert.AreEqual(7, coda.Level);
            Assert.AreEqual(120, coda.Exp);
        }
    }

    public class PartyIdResolutionTests
    {
        [Test]
        public void ResolveCharacterId_MapsLegacyDpsPresetToRen()
        {
            // UnitPreset_Ren.asset từng có unitId "DPS"; save cũ vẫn phải trỏ về Ren.
            Assert.AreEqual(PartyCharacterIds.Ren, PartyLoadoutApplicator.ResolveCharacterId("DPS"));
            Assert.AreEqual(PartyCharacterIds.Ren, PartyLoadoutApplicator.ResolveCharacterId("ren"));
            Assert.AreEqual(PartyCharacterIds.Ren, PartyLoadoutApplicator.ResolveCharacterId("Ren_Alt"));
        }

        [Test]
        public void ResolveCharacterId_ReturnsNullForUnknownId()
        {
            Assert.IsNull(PartyLoadoutApplicator.ResolveCharacterId("slime_enemy_01"));
            Assert.IsNull(PartyLoadoutApplicator.ResolveCharacterId(string.Empty));
            Assert.IsNull(PartyLoadoutApplicator.ResolveCharacterId((string)null));
        }
    }

    public class PartyVitalsStateTests
    {
        [Test]
        public void Set_OverwritesExistingUnitInsteadOfDuplicating()
        {
            var vitals = new PartyVitalsState();
            vitals.Set(PartyCharacterIds.Ren, 100, 200);
            vitals.Set(PartyCharacterIds.Ren, 55, 200);

            Assert.AreEqual(1, vitals.Entries.Count);
            Assert.IsTrue(vitals.TryGet(PartyCharacterIds.Ren, out var hp, out var maxHp));
            Assert.AreEqual(55, hp);
            Assert.AreEqual(200, maxHp);
        }

        [Test]
        public void Clear_EmptiesAllEntries()
        {
            var vitals = new PartyVitalsState();
            vitals.Set(PartyCharacterIds.Coda, 10, 90);
            vitals.Clear();

            Assert.AreEqual(0, vitals.Entries.Count);
            Assert.IsFalse(vitals.TryGet(PartyCharacterIds.Coda, out _, out _));
        }
    }

    public class PlaytimeStateTests
    {
        [Test]
        public void Format_ShowsHoursAndPaddedMinutes()
        {
            var playtime = new PlaytimeState();
            playtime.Add(3725d);

            Assert.AreEqual(3725, playtime.TotalSecondsRounded);
            Assert.AreEqual("1:02", playtime.Format());
        }

        [Test]
        public void Reset_ClearsAccumulatedTime()
        {
            var playtime = new PlaytimeState();
            playtime.Add(600d);
            playtime.Reset();

            Assert.AreEqual(0, playtime.TotalSecondsRounded);
        }
    }
}
