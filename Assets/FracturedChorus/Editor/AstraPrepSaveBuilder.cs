#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FracturedChorus.Data;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class AstraPrepSaveBuilder
    {
        public const string VaultConfigPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Presets/PinkyVaultConfig_Default.asset";

        public const string BundledRelativePath = "StreamingAssets/Saves/slot_01.fcsav";

        [MenuItem("Fractured Chorus/Save/Write Astra Prep Slot")]
        public static void WriteBundledSave()
        {
            var state = BuildState();
            var bytes = GameMetaSaveLoad.EncryptSlot(state, GameMetaSaveLoad.BundledPrepSlot);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var output = Path.Combine(projectRoot, "Assets", BundledRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
            File.WriteAllBytes(output, bytes);
            Debug.Log(
                $"[Fractured Chorus] Astra prep save → {output} " +
                $"(F{state.RunSnapshot.CurrentFloor} node {state.RunSnapshot.CurrentNodeId}, seed {state.RunSnapshot.Seed}).");
        }

        public static GameMetaState BuildState()
        {
            var config = AssetDatabase.LoadAssetAtPath<PinkyVaultConfigSO>(VaultConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException($"Không thấy Pinky vault config tại {VaultConfigPath}.");
            }

            var sector = PinkySectorId.Canticle;
            var sectorConfig = config.GetSector(sector);
            if (sectorConfig.floorCount != 12 || sectorConfig.bossFloor != 13)
            {
                throw new InvalidOperationException(
                    $"Canticle phải là F1–F12, boss F13. Đang là F{sectorConfig.floorCount} / boss F{sectorConfig.bossFloor}.");
            }

            var graph = MapGenerator.GenerateSector(sector, sectorConfig.previewSeed, config.WeightsFor(sector), config);
            var path = FindPathToPreBossCamp(graph);
            if (path == null || path.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Seed {sectorConfig.previewSeed} không có đường từ start tới camp F12 nối boss.");
            }

            var camp = graph.GetNode(path[path.Count - 1]);
            if (camp == null || camp.Floor != 12 || camp.Type != MapNodeType.Camp || graph.BossNode == null
                || !camp.Outgoing.Contains(graph.BossNode.Id))
            {
                throw new InvalidOperationException("Node cuối đường đi không phải camp F12 trước Astra.");
            }

            var state = GameMetaState.CreateNew();
            ApplyStoryFlags(state);
            ApplyParty(state);
            ApplyRun(state, graph, path, camp);
            state.LastSceneName = RunMapSceneCatalog.RunMapPrototype;
            return state;
        }

        private static void ApplyStoryFlags(GameMetaState state)
        {
            state.SetFlag(StoryFlagIds.ContractSigned);
            state.SetFlag(StoryFlagIds.OpeningInvestigationDone);
            state.SetFlag(StoryFlagIds.RenArrivedHima);
            state.SetFlag(StoryFlagIds.HimaEnrollmentDone);
            state.SetFlag(StoryFlagIds.VaultQuestActive);
            state.SetFlag(StoryFlagIds.MimiEncountered);
            state.SetFlag(StoryFlagIds.CharlotteReunited);
            state.SetFlag(StoryFlagIds.CodaMet);
            state.SetFlag(StoryFlagIds.CodaRescue);
            state.SetFlag(StoryFlagIds.TutorialHubDone);
            state.SetFlag(StoryFlagIds.TutorialHubPostRescueDone);
            state.SetFlag(StoryFlagIds.TutorialMapDone);
            state.SetFlag(StoryFlagIds.TutorialCombatDone);
            state.SetFlag(StoryFlagIds.TutorialCadenceIntroDone);
        }

        private static void ApplyParty(GameMetaState state)
        {
            // Preset combat đã là Lv15. Ba điểm này là phần Lv16–18, cộng thêm lúc vào trận.
            SetMember(
                state,
                PartyCharacterIds.Ren,
                new[] { "ren_basic", "ren_skill", "ren_ult" },
                str: 1,
                ma: 0,
                en: 1,
                hb: 1);
            SetMember(
                state,
                PartyCharacterIds.Charlotte,
                new[] { "Charlott_basic", "tank_skill", "tank_ult" },
                str: 1,
                ma: 0,
                en: 1,
                hb: 1);
            SetMember(
                state,
                PartyCharacterIds.Coda,
                new[] { "mage_basic", "mage_skill", "mage_ult" },
                str: 0,
                ma: 1,
                en: 1,
                hb: 1);
        }

        private static void SetMember(
            GameMetaState state,
            string characterId,
            string[] skills,
            int str,
            int ma,
            int en,
            int hb)
        {
            var entry = state.Loadout.GetOrCreate(characterId);
            entry.Level = 18;
            entry.Exp = 0;
            entry.UnspentStatPoints = 0;
            entry.StrPoints = str;
            entry.MaPoints = ma;
            entry.EnPoints = en;
            entry.HbPoints = hb;
            entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(skills);
        }

        private static void ApplyRun(GameMetaState state, MapGraph graph, List<int> path, MapNodeData camp)
        {
            var cleared = new List<int>();
            for (var i = 1; i < path.Count - 1; i++)
            {
                cleared.Add(path[i]);
            }

            var snap = state.RunSnapshot;
            snap.HasActiveRun = true;
            snap.Seed = graph.Seed;
            snap.ActiveSector = (int)PinkySectorId.Canticle;
            snap.CurrentNodeId = camp.Id;
            snap.CurrentFloor = camp.Floor;
            snap.VisitedNodeIds = path.ToArray();
            snap.ClearedNodeIds = cleared.ToArray();
            snap.PulseCleared = true;
            snap.EchoCleared = true;
            snap.CanticleCleared = false;
        }

        private static List<int> FindPathToPreBossCamp(MapGraph graph)
        {
            if (graph?.StartNode == null || graph.BossNode == null)
            {
                return null;
            }

            var campFloor = graph.Profile.FloorCount;
            foreach (var node in graph.Nodes)
            {
                if (node == null || node.Floor != campFloor || node.Type != MapNodeType.Camp)
                {
                    continue;
                }

                if (!node.Outgoing.Contains(graph.BossNode.Id))
                {
                    continue;
                }

                var path = FindPath(graph, graph.StartNode.Id, node.Id);
                if (path != null)
                {
                    return path;
                }
            }

            return null;
        }

        private static List<int> FindPath(MapGraph graph, int startId, int goalId)
        {
            var previous = new Dictionary<int, int> { [startId] = -1 };
            var queue = new Queue<int>();
            queue.Enqueue(startId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (id == goalId)
                {
                    return Reconstruct(previous, goalId);
                }

                var node = graph.GetNode(id);
                if (node == null)
                {
                    continue;
                }

                foreach (var nextId in node.Outgoing)
                {
                    if (previous.ContainsKey(nextId))
                    {
                        continue;
                    }

                    var next = graph.GetNode(nextId);
                    if (next == null || next.IsBoss)
                    {
                        continue;
                    }

                    previous[nextId] = id;
                    queue.Enqueue(nextId);
                }
            }

            return null;
        }

        private static List<int> Reconstruct(Dictionary<int, int> previous, int goalId)
        {
            var path = new List<int>();
            var cursor = goalId;
            while (cursor >= 0)
            {
                path.Add(cursor);
                cursor = previous[cursor];
            }

            path.Reverse();
            return path;
        }
    }
}
#endif
