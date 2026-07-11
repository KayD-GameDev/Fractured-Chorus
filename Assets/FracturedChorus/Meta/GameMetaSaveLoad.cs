using System;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Meta
{
    public static class GameMetaSaveLoad
    {
        public const string SaveFileName = "fc_meta_save.json";

        public static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool TrySave(GameMetaState state)
        {
            if (state == null)
            {
                Debug.LogError("[Fractured Chorus] GameMetaSaveLoad: state null.");
                return false;
            }

            try
            {
                var dto = GameMetaSaveData.FromState(state);
                var json = JsonUtility.ToJson(dto, prettyPrint: true);
                System.IO.File.WriteAllText(SavePath, json);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to save meta state: {error}");
                return false;
            }
        }

        public static GameMetaState LoadOrNew()
        {
            if (!System.IO.File.Exists(SavePath))
            {
                return GameMetaState.CreateNew();
            }

            try
            {
                var json = System.IO.File.ReadAllText(SavePath);
                var dto = JsonUtility.FromJson<GameMetaSaveData>(json);

                if (dto == null)
                {
                    Debug.LogError("[Fractured Chorus] Meta save corrupt — starting new game.");
                    return GameMetaState.CreateNew();
                }

                return dto.ToState();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to load meta save: {error}");
                return GameMetaState.CreateNew();
            }
        }

        public static string Serialize(GameMetaState state)
        {
            return JsonUtility.ToJson(GameMetaSaveData.FromState(state), prettyPrint: false);
        }

        public static GameMetaState Deserialize(string json)
        {
            var dto = JsonUtility.FromJson<GameMetaSaveData>(json);
            return dto?.ToState() ?? GameMetaState.CreateNew();
        }

        public static bool DeleteSave()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath))
                {
                    return true;
                }

                System.IO.File.Delete(SavePath);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to delete meta save: {error}");
                return false;
            }
        }
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
        public StatEntry[] stats = Array.Empty<StatEntry>();
        public BondEntry[] bonds = Array.Empty<BondEntry>();
        public FlagBoolEntry[] boolFlags = Array.Empty<FlagBoolEntry>();
        public FlagIntEntry[] intFlags = Array.Empty<FlagIntEntry>();
        public int runSeed;
        public int runFloor;
        public int runNodeId = -1;
        public int runSector;
        public bool runActive;

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

            return new GameMetaSaveData
            {
                saveVersion = GameMetaState.SaveVersion,
                dateMonth = state.Calendar.CurrentDate.Month,
                dateDay = state.Calendar.CurrentDate.Day,
                phase = (int)state.Calendar.CurrentPhase,
                slotsUsed = state.Calendar.SlotsUsedToday,
                morningQuizDone = state.Calendar.MorningQuizDone,
                stats = stats.ToArray(),
                bonds = bonds.ToArray(),
                boolFlags = boolFlags.ToArray(),
                intFlags = intFlags.ToArray(),
                runSeed = state.RunSnapshot.Seed,
                runFloor = state.RunSnapshot.CurrentFloor,
                runNodeId = state.RunSnapshot.CurrentNodeId,
                runSector = state.RunSnapshot.ActiveSector,
                runActive = state.RunSnapshot.HasActiveRun
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

            state.RunSnapshot.Seed = runSeed;
            state.RunSnapshot.CurrentFloor = runFloor;
            state.RunSnapshot.CurrentNodeId = runNodeId;
            state.RunSnapshot.ActiveSector = runSector;
            state.RunSnapshot.HasActiveRun = runActive;

            return state;
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
