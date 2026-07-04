using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "PinkyVaultConfig", menuName = "Fractured Chorus/Pinky Vault Config")]
    public class PinkyVaultConfigSO : ScriptableObject
    {
        public SectorConfig pulse = SectorConfig.Default(PinkySectorId.Pulse);
        public SectorConfig echo = SectorConfig.Default(PinkySectorId.Echo);
        public SectorConfig canticle = SectorConfig.Default(PinkySectorId.Canticle);

        public SectorConfig GetSector(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => pulse,
            PinkySectorId.Echo => echo,
            PinkySectorId.Canticle => canticle,
            _ => pulse
        };

        public MapGenerationProfile ProfileFor(PinkySectorId sector)
        {
            var cfg = GetSector(sector);
            return new MapGenerationProfile
            {
                ColumnCount = cfg.columnCount,
                FloorCount = cfg.floorCount,
                BossFloor = cfg.bossFloor,
                PathCount = cfg.pathCount,
                Sector = sector
            };
        }

        public NodeTypeAssigner.WeightEntry[] WeightsFor(PinkySectorId sector)
        {
            var cfg = GetSector(sector);
            return new[]
            {
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Battle, Weight = cfg.battleWeight },
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Elite, Weight = cfg.eliteWeight },
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Event, Weight = cfg.eventWeight },
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Relay, Weight = cfg.relayWeight },
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Camp, Weight = cfg.campWeight },
                new NodeTypeAssigner.WeightEntry { Type = MapNodeType.Treasure, Weight = cfg.treasureWeight }
            };
        }

        [System.Serializable]
        public struct SectorConfig
        {
            public string title;
            public string bossLabel;
            public int columnCount;
            public int floorCount;
            public int bossFloor;
            public int pathCount;
            public int previewSeed;
            public bool loadBossScene;

            [Range(0f, 1f)] public float battleWeight;
            [Range(0f, 1f)] public float eliteWeight;
            [Range(0f, 1f)] public float eventWeight;
            [Range(0f, 1f)] public float relayWeight;
            [Range(0f, 1f)] public float campWeight;
            [Range(0f, 1f)] public float treasureWeight;

            public static SectorConfig Default(PinkySectorId sector) => sector switch
            {
                PinkySectorId.Pulse => new SectorConfig
                {
                    title = "Part 1 · Pulse Lane",
                    bossLabel = "Mimi — The Pulse",
                    columnCount = 7,
                    floorCount = 10,
                    bossFloor = 11,
                    pathCount = 6,
                    previewSeed = 101,
                    loadBossScene = false,
                    battleWeight = 0.26f,
                    eliteWeight = 0.32f,
                    eventWeight = 0.17f,
                    relayWeight = 0.05f,
                    campWeight = 0.06f,
                    treasureWeight = 0.14f
                },
                PinkySectorId.Echo => new SectorConfig
                {
                    title = "Part 2 · Echo Lane",
                    bossLabel = "Kiki — The Echo",
                    columnCount = 7,
                    floorCount = 10,
                    bossFloor = 11,
                    pathCount = 6,
                    previewSeed = 202,
                    loadBossScene = false,
                    battleWeight = 0.26f,
                    eliteWeight = 0.32f,
                    eventWeight = 0.17f,
                    relayWeight = 0.05f,
                    campWeight = 0.06f,
                    treasureWeight = 0.14f
                },
                PinkySectorId.Canticle => new SectorConfig
                {
                    title = "Part 3 · Canticle Lane",
                    bossLabel = "Astra — Chart Lord",
                    columnCount = 7,
                    floorCount = 12,
                    bossFloor = 13,
                    pathCount = 6,
                    previewSeed = 303,
                    loadBossScene = true,
                    battleWeight = 0.26f,
                    eliteWeight = 0.32f,
                    eventWeight = 0.17f,
                    relayWeight = 0.05f,
                    campWeight = 0.06f,
                    treasureWeight = 0.14f
                },
                _ => Default(PinkySectorId.Pulse)
            };
        }
    }
}
