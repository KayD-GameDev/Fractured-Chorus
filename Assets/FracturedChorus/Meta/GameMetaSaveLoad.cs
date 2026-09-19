using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FracturedChorus.Meta
{
    public static class GameMetaSaveLoad
    {
        public const int SlotCount = 10;
        public const string LegacySaveFileName = "fc_meta_save.json";
        public const string SavesFolderName = "saves";
        public const string LegacyBackupFolderName = "_legacy_v2";
        public const string MigrationNoteFileName = "MIGRATION.txt";

        private static bool s_legacyMigrated;

        public static int ActiveSlot { get; set; }

        public static string LegacySavePath => Path.Combine(Application.persistentDataPath, LegacySaveFileName);

        public static string SavesDirectory => Path.Combine(Application.persistentDataPath, SavesFolderName);

        /// <summary>Nơi cất save JSON đời cũ sau khi đã chuyển sang bản mã hóa.</summary>
        public static string LegacyBackupDirectory => Path.Combine(SavesDirectory, LegacyBackupFolderName);

        public static string GetSlotPath(int slot) =>
            Path.Combine(SavesDirectory, $"slot_{slot:00}{SaveCrypto.EncryptedExtension}");

        public static string GetPlaintextSlotPath(int slot) =>
            Path.Combine(SavesDirectory, $"slot_{slot:00}{SaveCrypto.PlaintextExtension}");

        public static string GetLegacyBackupSlotPath(int slot) =>
            Path.Combine(LegacyBackupDirectory, $"slot_{slot:00}{SaveCrypto.PlaintextExtension}");

        public static bool TrySave(GameMetaState state) => TrySave(state, ActiveSlot);

        public static bool TrySave(GameMetaState state, int slot)
        {
            if (state == null)
            {
                Debug.LogError("[Fractured Chorus] GameMetaSaveLoad: state null.");
                return false;
            }

            slot = ClampSlot(slot);

            try
            {
                Directory.CreateDirectory(SavesDirectory);
                state.SaveVersionId = GameMetaState.SaveVersion;
                var file = new SaveSlotFile
                {
                    header = BuildHeader(state, slot),
                    data = GameMetaSaveData.FromState(state)
                };
                var json = JsonUtility.ToJson(file, prettyPrint: true);
                File.WriteAllBytes(GetSlotPath(slot), SaveCrypto.Encrypt(json));
                ActiveSlot = slot;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to save meta state slot {slot}: {error}");
                return false;
            }
        }

        public static GameMetaState LoadOrNew()
        {
            MigrateLegacySaveOnce();
            var loaded = TryLoad(ActiveSlot);
            return loaded ?? GameMetaState.CreateNew();
        }

        public static GameMetaState TryLoad(int slot)
        {
            slot = ClampSlot(slot);
            MigrateLegacySaveOnce();

            var json = ReadSlotJson(slot, out var status);
            if (json == null)
            {
                if (status != SaveBlobStatus.Empty)
                {
                    Debug.LogError($"[Fractured Chorus] Meta save slot {slot} không đọc được ({status}).");
                }

                return null;
            }

            var state = ParseSaveJson(json);
            if (state == null)
            {
                Debug.LogError($"[Fractured Chorus] Meta save slot {slot} corrupt.");
                return null;
            }

            ActiveSlot = slot;
            return state;
        }

        /// <summary>
        /// Đọc và giải mã nội dung một slot. Chấp nhận cả file mã hóa lẫn JSON thuần đời cũ
        /// để save copy tay từ bản build trước vẫn dùng được.
        /// </summary>
        private static string ReadSlotJson(int slot, out SaveBlobStatus status)
        {
            status = SaveBlobStatus.Empty;

            try
            {
                var path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    path = GetPlaintextSlotPath(slot);
                    if (!File.Exists(path))
                    {
                        return null;
                    }
                }

                var blob = File.ReadAllBytes(path);
                status = SaveCrypto.TryDecrypt(blob, out var json);
                if (status == SaveBlobStatus.Success || status == SaveBlobStatus.Plaintext)
                {
                    return json;
                }

                if (status == SaveBlobStatus.Tampered)
                {
                    Debug.LogError(
                        $"[Fractured Chorus] Slot {slot}: chữ ký HMAC không khớp — file đã bị sửa hoặc hỏng.");
                }

                return null;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to read meta save slot {slot}: {error}");
                status = SaveBlobStatus.Failed;
                return null;
            }
        }

        public static bool Delete(int slot)
        {
            slot = ClampSlot(slot);

            try
            {
                DeleteIfExists(GetSlotPath(slot));
                DeleteIfExists(GetPlaintextSlotPath(slot));
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to delete meta save slot {slot}: {error}");
                return false;
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static bool SlotExists(int slot)
        {
            slot = ClampSlot(slot);
            return File.Exists(GetSlotPath(slot)) || File.Exists(GetPlaintextSlotPath(slot));
        }

        public static SaveSlotHeader[] ListHeaders()
        {
            MigrateLegacySaveOnce();
            var headers = new SaveSlotHeader[SlotCount];
            for (var slot = 0; slot < SlotCount; slot++)
            {
                headers[slot] = ReadHeader(slot);
            }

            return headers;
        }

        public static bool HasAnySave()
        {
            MigrateLegacySaveOnce();
            for (var slot = 0; slot < SlotCount; slot++)
            {
                if (SlotExists(slot))
                {
                    return true;
                }
            }

            return false;
        }

        public static void MigrateLegacySaveOnce()
        {
            if (s_legacyMigrated)
            {
                return;
            }

            s_legacyMigrated = true;

            // ActiveSlot bị TrySave ghi đè, mà migration chạy ngầm nên phải trả lại nguyên trạng.
            var previousActiveSlot = ActiveSlot;
            try
            {
                Directory.CreateDirectory(SavesDirectory);
                MigrateRootLegacyFile();
                MigratePlaintextSlots();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Legacy save migration failed: {error}");
            }
            finally
            {
                ActiveSlot = previousActiveSlot;
            }
        }

        /// <summary>Save một-slot đời đầu (fc_meta_save.json ở gốc persistentDataPath) chuyển vào slot 0.</summary>
        private static void MigrateRootLegacyFile()
        {
            if (!File.Exists(LegacySavePath) || SlotExists(0))
            {
                return;
            }

            var state = ParseSaveJson(File.ReadAllText(LegacySavePath));
            if (state == null)
            {
                Debug.LogError("[Fractured Chorus] Legacy meta save corrupt — skipping migration.");
                return;
            }

            if (TrySave(state, 0))
            {
                Debug.Log("[Fractured Chorus] Migrated legacy fc_meta_save.json to slot 0.");
            }
        }

        /// <summary>
        /// Chuyển các slot JSON thuần sang bản mã hóa, rồi dời file gốc vào _legacy_v2/
        /// thay vì xóa — người chơi vẫn còn đường lùi nếu cần quay lại bản build cũ.
        /// </summary>
        private static void MigratePlaintextSlots()
        {
            var migrated = new List<int>();

            for (var slot = 0; slot < SlotCount; slot++)
            {
                var plaintextPath = GetPlaintextSlotPath(slot);
                if (!File.Exists(plaintextPath) || File.Exists(GetSlotPath(slot)))
                {
                    continue;
                }

                var state = ParseSaveJson(File.ReadAllText(plaintextPath));
                if (state == null)
                {
                    Debug.LogError($"[Fractured Chorus] Slot {slot}: save JSON cũ hỏng — bỏ qua migration.");
                    continue;
                }

                if (!TrySave(state, slot))
                {
                    continue;
                }

                Directory.CreateDirectory(LegacyBackupDirectory);
                var backupPath = GetLegacyBackupSlotPath(slot);
                DeleteIfExists(backupPath);
                File.Move(plaintextPath, backupPath);
                migrated.Add(slot);
            }

            if (migrated.Count == 0)
            {
                return;
            }

            WriteMigrationNote(migrated);
            Debug.Log(
                $"[Fractured Chorus] Đã mã hóa {migrated.Count} slot save. " +
                $"Bản JSON cũ nằm ở {LegacyBackupDirectory}");
        }

        private static void WriteMigrationNote(List<int> migratedSlots)
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("Fractured Chorus — save migration log");
                builder.AppendLine($"Thoi diem: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Save version dich: {GameMetaState.SaveVersion}");
                builder.AppendLine($"Slot da chuyen: {string.Join(", ", migratedSlots)}");
                builder.AppendLine();
                builder.AppendLine("Cac file .json trong thu muc nay la ban goc truoc khi ma hoa.");
                builder.AppendLine("Dung menu Editor 'Fractured Chorus/Save/Restore From Legacy Backup' de khoi phuc.");

                File.WriteAllText(Path.Combine(LegacyBackupDirectory, MigrationNoteFileName), builder.ToString());
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Không ghi được migration note: {error}");
            }
        }

        /// <summary>Giải mã một slot ra JSON đọc được (phục vụ debug / backup thủ công).</summary>
        public static string ExportSlotJson(int slot)
        {
            MigrateLegacySaveOnce();
            return ReadSlotJson(ClampSlot(slot), out _);
        }

        /// <summary>Ghi JSON đã chỉnh tay ngược vào slot, có kiểm tra parse trước khi ghi đè.</summary>
        public static bool ImportSlotJson(int slot, string json)
        {
            var state = ParseSaveJson(json);
            if (state == null)
            {
                Debug.LogError($"[Fractured Chorus] Import slot {slot} thất bại: JSON không hợp lệ.");
                return false;
            }

            return TrySave(state, ClampSlot(slot));
        }

        /// <summary>Kéo các save JSON trong _legacy_v2/ về lại slot dưới dạng mã hóa. Trả về số slot khôi phục được.</summary>
        public static int RestoreFromLegacyBackup()
        {
            if (!Directory.Exists(LegacyBackupDirectory))
            {
                return 0;
            }

            var restored = 0;
            var previousActiveSlot = ActiveSlot;

            for (var slot = 0; slot < SlotCount; slot++)
            {
                var backupPath = GetLegacyBackupSlotPath(slot);
                if (!File.Exists(backupPath))
                {
                    continue;
                }

                try
                {
                    var state = ParseSaveJson(File.ReadAllText(backupPath));
                    if (state != null && TrySave(state, slot))
                    {
                        restored++;
                    }
                }
                catch (Exception error)
                {
                    Debug.LogError($"[Fractured Chorus] Khôi phục slot {slot} từ backup thất bại: {error}");
                }
            }

            ActiveSlot = previousActiveSlot;
            return restored;
        }

        public static string Serialize(GameMetaState state)
        {
            return JsonUtility.ToJson(GameMetaSaveData.FromState(state), prettyPrint: false);
        }

        public static GameMetaState Deserialize(string json)
        {
            return ParseSaveJson(json) ?? GameMetaState.CreateNew();
        }

        public static bool DeleteSave()
        {
            var deleted = Delete(ActiveSlot);
            try
            {
                if (File.Exists(LegacySavePath))
                {
                    File.Delete(LegacySavePath);
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to delete legacy meta save: {error}");
                return false;
            }

            return deleted;
        }

        private static GameMetaState ParseSaveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var wrapped = JsonUtility.FromJson<SaveSlotFile>(json);
            if (wrapped?.data != null)
            {
                return wrapped.data.ToState();
            }

            var legacy = JsonUtility.FromJson<GameMetaSaveData>(json);
            return legacy?.ToState();
        }

        public static SaveSlotHeader ReadHeader(int slot)
        {
            slot = ClampSlot(slot);
            if (!SlotExists(slot))
            {
                return SaveSlotHeader.Empty(slot);
            }

            var json = ReadSlotJson(slot, out var status);
            if (json == null)
            {
                // File có tồn tại nhưng không giải mã nổi — báo hỏng thay vì giả vờ slot trống,
                // để người chơi không vô tình ghi đè lên save còn cứu được.
                return status == SaveBlobStatus.Empty
                    ? SaveSlotHeader.Empty(slot)
                    : SaveSlotHeader.Corrupted(slot);
            }

            try
            {
                var wrapped = JsonUtility.FromJson<SaveSlotFile>(json);
                if (wrapped?.header != null && !wrapped.header.isEmpty)
                {
                    wrapped.header.slotIndex = slot;
                    return wrapped.header;
                }

                var legacy = JsonUtility.FromJson<GameMetaSaveData>(json);
                if (legacy == null)
                {
                    return SaveSlotHeader.Corrupted(slot);
                }

                return BuildHeader(legacy.ToState(), slot);
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to read save header slot {slot}: {error}");
                return SaveSlotHeader.Corrupted(slot);
            }
        }

        private static SaveSlotHeader BuildHeader(GameMetaState state, int slot)
        {
            return new SaveSlotHeader
            {
                slotIndex = slot,
                isEmpty = false,
                dateMonth = state.Calendar.CurrentDate.Month,
                dateDay = state.Calendar.CurrentDate.Day,
                phase = (int)state.Calendar.CurrentPhase,
                locationLabel = ResolveLocationLabel(state),
                difficulty = state.Difficulty,
                notes = state.Wallet.Notes,
                playTimeSeconds = state.Playtime.TotalSecondsRounded
            };
        }

        private static string ResolveLocationLabel(GameMetaState state)
        {
            if (state.RunSnapshot.HasActiveRun)
            {
                return $"Cadence Run F{Mathf.Max(1, state.RunSnapshot.CurrentFloor)}";
            }

            return SceneLabel(state.LastSceneName);
        }

        /// <summary>
        /// Nhãn hiển thị trên danh sách slot. Scene lạ thì trả luôn tên scene còn hơn nói dối
        /// là "Campus Hub" như bản trước.
        /// </summary>
        private static string SceneLabel(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return "Campus Hub";
            }

            switch (sceneName)
            {
                case "CampusHub":
                    return "Campus Hub";
                case "PrologueVN":
                    return "Prologue";
                case "OpeningInvestigation":
                    return "Opening Investigation";
                case "FlowerShopWork":
                    return "Flower Shop";
                case "CharacterBuild":
                    return "Character Build";
                case "RunMapPrototype":
                    return "Cadence Run";
                case "CombatPrototype":
                case "CombatTutorial":
                    return "Combat";
                default:
                    return sceneName;
            }
        }

        private static int ClampSlot(int slot) => Mathf.Clamp(slot, 0, SlotCount - 1);
    }

    [Serializable]
    public struct SaveSlotHeader
    {
        public int slotIndex;
        public bool isEmpty;
        public bool isCorrupted;
        public int dateMonth;
        public int dateDay;
        public int phase;
        public string locationLabel;
        public int difficulty;
        public int notes;
        public int playTimeSeconds;

        public static SaveSlotHeader Empty(int slot)
        {
            return new SaveSlotHeader
            {
                slotIndex = slot,
                isEmpty = true,
                locationLabel = string.Empty
            };
        }

        /// <summary>Slot có file nhưng giải mã hỏng hoặc sai chữ ký — không load, cũng không nên ghi đè mù.</summary>
        public static SaveSlotHeader Corrupted(int slot)
        {
            return new SaveSlotHeader
            {
                slotIndex = slot,
                isEmpty = false,
                isCorrupted = true,
                locationLabel = string.Empty
            };
        }

        public string FormatPlayTime()
        {
            var total = Mathf.Max(0, playTimeSeconds);
            return $"{total / 3600}:{total % 3600 / 60:00}";
        }
    }

    [Serializable]
    public sealed class SaveSlotFile
    {
        public SaveSlotHeader header;
        public GameMetaSaveData data;
    }

    [Serializable]
    public sealed class GameMetaSaveData
    {
        public int saveVersion;
        public int dateMonth;
        public int dateDay;
        public int phase;
        public int slotsUsed;
        public bool morningQuizDone;
        public int notes;
        public int difficulty;
        public StatEntry[] stats = Array.Empty<StatEntry>();
        public BondEntry[] bonds = Array.Empty<BondEntry>();
        public FlagBoolEntry[] boolFlags = Array.Empty<FlagBoolEntry>();
        public FlagIntEntry[] intFlags = Array.Empty<FlagIntEntry>();
        public LoadoutEntry[] loadouts = Array.Empty<LoadoutEntry>();
        public int runSeed;
        public int runFloor;
        public int runNodeId = -1;
        public int runSector;
        public bool runActive;
        public int[] runClearedNodeIds = Array.Empty<int>();

        // --- Save version 3 ---
        public double playtimeSeconds;
        public string hubLocationId = string.Empty;
        public string hubSubLocationId = string.Empty;
        public string hubPendingActivityId = string.Empty;
        public int[] runVisitedNodeIds = Array.Empty<int>();
        public bool runPulseCleared;
        public bool runEchoCleared;
        public bool runCanticleCleared;
        public PartyVitalsEntry[] partyVitals = Array.Empty<PartyVitalsEntry>();

        // --- Save version 4 ---
        public string sceneName = string.Empty;

        public static GameMetaSaveData FromState(GameMetaState state)
        {
            var stats = new List<StatEntry>();
            foreach (SocialStatType stat in Enum.GetValues(typeof(SocialStatType)))
            {
                stats.Add(new StatEntry
                {
                    stat = (int)stat,
                    rank = state.SocialStats.GetRank(stat),
                    exp = state.SocialStats.GetExp(stat)
                });
            }

            var bonds = new List<BondEntry>();
            foreach (var pair in state.Bonds.Bonds)
            {
                var bond = pair.Value;
                bonds.Add(new BondEntry
                {
                    npcId = bond.NpcId,
                    echoKey = (int)bond.EchoKey,
                    rank = bond.Rank,
                    exp = bond.Exp,
                    arcCap = bond.ArcCap,
                    isLocked = bond.IsLocked
                });
            }

            var boolFlags = new List<FlagBoolEntry>();
            foreach (var pair in state.Flags.ExportBools())
            {
                boolFlags.Add(new FlagBoolEntry { key = pair.Key, value = pair.Value });
            }

            var intFlags = new List<FlagIntEntry>();
            foreach (var pair in state.Flags.ExportInts())
            {
                intFlags.Add(new FlagIntEntry { key = pair.Key, value = pair.Value });
            }

            var loadouts = new List<LoadoutEntry>();
            foreach (var entry in state.Loadout.Entries)
            {
                loadouts.Add(LoadoutEntry.FromCharacter(entry));
            }

            var vitals = new List<PartyVitalsEntry>();
            foreach (var entry in state.PartyVitals.Entries)
            {
                vitals.Add(new PartyVitalsEntry
                {
                    unitId = entry.UnitId,
                    hp = entry.Hp,
                    maxHp = entry.MaxHp
                });
            }

            return new GameMetaSaveData
            {
                saveVersion = GameMetaState.SaveVersion,
                dateMonth = state.Calendar.CurrentDate.Month,
                dateDay = state.Calendar.CurrentDate.Day,
                phase = (int)state.Calendar.CurrentPhase,
                slotsUsed = state.Calendar.SlotsUsedToday,
                morningQuizDone = state.Calendar.MorningQuizDone,
                notes = state.Wallet.Notes,
                difficulty = state.Difficulty,
                stats = stats.ToArray(),
                bonds = bonds.ToArray(),
                boolFlags = boolFlags.ToArray(),
                intFlags = intFlags.ToArray(),
                loadouts = loadouts.ToArray(),
                runSeed = state.RunSnapshot.Seed,
                runFloor = state.RunSnapshot.CurrentFloor,
                runNodeId = state.RunSnapshot.CurrentNodeId,
                runSector = state.RunSnapshot.ActiveSector,
                runActive = state.RunSnapshot.HasActiveRun,
                runClearedNodeIds = state.RunSnapshot.ClearedNodeIds ?? Array.Empty<int>(),
                playtimeSeconds = state.Playtime.TotalSeconds,
                hubLocationId = state.HubLocation.LastLocationId ?? string.Empty,
                hubSubLocationId = state.HubLocation.LastSubLocationId ?? string.Empty,
                hubPendingActivityId = state.HubLocation.PendingActivityId ?? string.Empty,
                runVisitedNodeIds = state.RunSnapshot.VisitedNodeIds ?? Array.Empty<int>(),
                runPulseCleared = state.RunSnapshot.PulseCleared,
                runEchoCleared = state.RunSnapshot.EchoCleared,
                runCanticleCleared = state.RunSnapshot.CanticleCleared,
                partyVitals = vitals.ToArray(),
                sceneName = state.LastSceneName ?? string.Empty
            };
        }

        public GameMetaState ToState()
        {
            var state = GameMetaState.CreateNew();
            state.SaveVersionId = saveVersion;
            state.Calendar.ResetForNewDay(new GameDate(dateMonth, dateDay));
            state.Calendar.CurrentPhase = (DayPhase)Mathf.Clamp(phase, 0, 2);
            state.Calendar.SlotsUsedToday = Mathf.Max(0, slotsUsed);
            state.Calendar.MorningQuizDone = morningQuizDone;
            state.Wallet.Notes = Mathf.Max(0, notes);
            state.Difficulty = difficulty;
            state.LastSceneName = sceneName ?? string.Empty;

            if (stats != null)
            {
                foreach (var entry in stats)
                {
                    if (!Enum.IsDefined(typeof(SocialStatType), entry.stat))
                    {
                        continue;
                    }

                    state.SocialStats.ImportRank((SocialStatType)entry.stat, entry.rank, entry.exp);
                }
            }

            state.Bonds = new BondState();
            if (bonds != null)
            {
                foreach (var entry in bonds)
                {
                    var bond = new BondProgress(entry.npcId, (EchoKey)entry.echoKey, entry.arcCap)
                    {
                        Rank = Mathf.Clamp(entry.rank, 1, entry.arcCap),
                        Exp = entry.exp,
                        IsLocked = entry.isLocked
                    };
                    state.Bonds.ImportBond(bond);
                }
            }

            state.Flags = new StoryFlags();
            if (boolFlags != null)
            {
                foreach (var entry in boolFlags)
                {
                    if (!string.IsNullOrWhiteSpace(entry.key))
                    {
                        state.Flags.ImportBool(entry.key, entry.value);
                    }
                }
            }

            if (intFlags != null)
            {
                foreach (var entry in intFlags)
                {
                    if (!string.IsNullOrWhiteSpace(entry.key))
                    {
                        state.Flags.ImportInt(entry.key, entry.value);
                    }
                }
            }

            state.Loadout = new PartyLoadoutState();
            if (loadouts != null)
            {
                foreach (var entry in loadouts)
                {
                    state.Loadout.ImportEntry(entry.ToCharacter());
                }
            }

            state.RunSnapshot.Seed = runSeed;
            state.RunSnapshot.CurrentFloor = runFloor;
            state.RunSnapshot.CurrentNodeId = runNodeId;
            state.RunSnapshot.ActiveSector = runSector;
            state.RunSnapshot.HasActiveRun = runActive;
            state.RunSnapshot.ClearedNodeIds = runClearedNodeIds ?? Array.Empty<int>();
            state.RunSnapshot.VisitedNodeIds = runVisitedNodeIds ?? Array.Empty<int>();
            state.RunSnapshot.PulseCleared = runPulseCleared;
            state.RunSnapshot.EchoCleared = runEchoCleared;
            state.RunSnapshot.CanticleCleared = runCanticleCleared;

            state.Playtime.TotalSeconds = playtimeSeconds > 0d ? playtimeSeconds : 0d;
            state.HubLocation.SetLocation(hubLocationId, hubSubLocationId);
            state.HubLocation.SetPendingActivity(hubPendingActivityId);

            state.PartyVitals.Clear();
            if (partyVitals != null)
            {
                foreach (var entry in partyVitals)
                {
                    state.PartyVitals.Set(entry.unitId, entry.hp, entry.maxHp);
                }
            }

            return state;
        }
    }

    [Serializable]
    public struct LoadoutEntry
    {
        public string characterId;
        public string skill0;
        public string skill1;
        public string skill2;
        public string skill3;
        public string skill4;
        public int unspentStatPoints;
        public int str;
        public int ma;
        public int en;
        public int hb;
        public int level;
        public int exp;

        public static LoadoutEntry FromCharacter(CharacterLoadoutEntry entry)
        {
            var skills = entry.EquippedSkillIds ?? Array.Empty<string>();
            return new LoadoutEntry
            {
                characterId = entry.CharacterId,
                skill0 = skills.Length > 0 ? skills[0] : string.Empty,
                skill1 = skills.Length > 1 ? skills[1] : string.Empty,
                skill2 = skills.Length > 2 ? skills[2] : string.Empty,
                skill3 = skills.Length > 3 ? skills[3] : string.Empty,
                skill4 = skills.Length > 4 ? skills[4] : string.Empty,
                unspentStatPoints = entry.UnspentStatPoints,
                str = entry.StrPoints,
                ma = entry.MaPoints,
                en = entry.EnPoints,
                hb = entry.HbPoints,
                level = entry.Level,
                exp = entry.Exp
            };
        }

        public CharacterLoadoutEntry ToCharacter()
        {
            return new CharacterLoadoutEntry(characterId)
            {
                EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(
                    new[]
                    {
                        skill0 ?? string.Empty,
                        skill1 ?? string.Empty,
                        skill2 ?? string.Empty,
                        skill3 ?? string.Empty,
                        skill4 ?? string.Empty
                    }),
                UnspentStatPoints = unspentStatPoints,
                StrPoints = str,
                MaPoints = ma,
                EnPoints = en,
                HbPoints = hb,
                // level = 0 nghĩa là save version 2 chưa có trường này.
                Level = level > 0 ? level : CharacterLoadoutEntry.DefaultLevel,
                Exp = exp
            };
        }
    }

    [Serializable]
    public struct PartyVitalsEntry
    {
        public string unitId;
        public int hp;
        public int maxHp;
    }

    [Serializable]
    public struct StatEntry
    {
        public int stat;
        public int rank;
        public int exp;
    }

    [Serializable]
    public struct BondEntry
    {
        public string npcId;
        public int echoKey;
        public int rank;
        public int exp;
        public int arcCap;
        public bool isLocked;
    }

    [Serializable]
    public struct FlagBoolEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public struct FlagIntEntry
    {
        public string key;
        public int value;
    }
}
