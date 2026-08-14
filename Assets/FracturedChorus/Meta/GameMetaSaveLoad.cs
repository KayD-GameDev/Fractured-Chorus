using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FracturedChorus.Meta
{
    public static class GameMetaSaveLoad
    {
        public const int SlotCount = 10;
        public const string LegacySaveFileName = "fc_meta_save.json";
        public const string SavesFolderName = "saves";

        private static bool s_legacyMigrated;

        public static int ActiveSlot { get; set; }

        public static string LegacySavePath => Path.Combine(Application.persistentDataPath, LegacySaveFileName);

        public static string SavesDirectory => Path.Combine(Application.persistentDataPath, SavesFolderName);

        public static string GetSlotPath(int slot) =>
            Path.Combine(SavesDirectory, $"slot_{slot:00}.json");

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
                File.WriteAllText(GetSlotPath(slot), json);
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

            try
            {
                var path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                var state = ParseSaveJson(json);
                if (state == null)
                {
                    Debug.LogError($"[Fractured Chorus] Meta save slot {slot} corrupt.");
                    return null;
                }

                ActiveSlot = slot;
                return state;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to load meta save slot {slot}: {error}");
                return null;
            }
        }

        public static bool Delete(int slot)
        {
            slot = ClampSlot(slot);

            try
            {
                var path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    return true;
                }

                File.Delete(path);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to delete meta save slot {slot}: {error}");
                return false;
            }
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
                if (File.Exists(GetSlotPath(slot)))
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

            try
            {
                if (!File.Exists(LegacySavePath))
                {
                    return;
                }

                var slotZeroPath = GetSlotPath(0);
                if (File.Exists(slotZeroPath))
                {
                    return;
                }

                Directory.CreateDirectory(SavesDirectory);
                var json = File.ReadAllText(LegacySavePath);
                var state = ParseSaveJson(json);
                if (state == null)
                {
                    Debug.LogError("[Fractured Chorus] Legacy meta save corrupt — skipping migration.");
                    return;
                }

                TrySave(state, 0);
                Debug.Log("[Fractured Chorus] Migrated legacy fc_meta_save.json to slot 0.");
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Legacy save migration failed: {error}");
            }
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

        private static SaveSlotHeader ReadHeader(int slot)
        {
            slot = ClampSlot(slot);
            var path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                return SaveSlotHeader.Empty(slot);
            }

            try
            {
                var json = File.ReadAllText(path);
                var wrapped = JsonUtility.FromJson<SaveSlotFile>(json);
                if (wrapped?.header != null && !wrapped.header.isEmpty)
                {
                    wrapped.header.slotIndex = slot;
                    return wrapped.header;
                }

                var legacy = JsonUtility.FromJson<GameMetaSaveData>(json);
                if (legacy == null)
                {
                    return SaveSlotHeader.Empty(slot);
                }

                var state = legacy.ToState();
                return BuildHeader(state, slot);
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to read save header slot {slot}: {error}");
                return SaveSlotHeader.Empty(slot);
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
                playTimeSeconds = 0
            };
        }

        private static string ResolveLocationLabel(GameMetaState state)
        {
            if (state.RunSnapshot.HasActiveRun)
            {
                return $"Cadence Run F{Mathf.Max(1, state.RunSnapshot.CurrentFloor)}";
            }

            return "Campus Hub";
        }

        private static int ClampSlot(int slot) => Mathf.Clamp(slot, 0, SlotCount - 1);
    }

    [Serializable]
    public struct SaveSlotHeader
    {
        public int slotIndex;
        public bool isEmpty;
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
                runClearedNodeIds = state.RunSnapshot.ClearedNodeIds ?? Array.Empty<int>()
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
        public int unspentStatPoints;
        public int str;
        public int ma;
        public int en;
        public int hb;

        public static LoadoutEntry FromCharacter(CharacterLoadoutEntry entry)
        {
            var skills = entry.EquippedSkillIds ?? Array.Empty<string>();
            return new LoadoutEntry
            {
                characterId = entry.CharacterId,
                skill0 = skills.Length > 0 ? skills[0] : string.Empty,
                skill1 = skills.Length > 1 ? skills[1] : string.Empty,
                skill2 = skills.Length > 2 ? skills[2] : string.Empty,
                unspentStatPoints = entry.UnspentStatPoints,
                str = entry.StrPoints,
                ma = entry.MaPoints,
                en = entry.EnPoints,
                hb = entry.HbPoints
            };
        }

        public CharacterLoadoutEntry ToCharacter()
        {
            return new CharacterLoadoutEntry(characterId)
            {
                EquippedSkillIds = new[] { skill0 ?? string.Empty, skill1 ?? string.Empty, skill2 ?? string.Empty },
                UnspentStatPoints = unspentStatPoints,
                StrPoints = str,
                MaPoints = ma,
                EnPoints = en,
                HbPoints = hb
            };
        }
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
