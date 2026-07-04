using System;
using System.Collections.Generic;
using System.Linq;

using FracturedChorus.Data;

namespace FracturedChorus.RunMap.Core
{
    public static class NodeTypeAssigner
    {
        /// <summary>Elite nodes as share of all non-boss map nodes (StS-style act density).</summary>
        public const float EliteDensityMin = 0.25f;
        public const float EliteDensityMax = 0.35f;

        public struct WeightEntry
        {
            public MapNodeType Type;
            public float Weight;
        }

        public static readonly WeightEntry[] DefaultWeights =
        {
            new WeightEntry { Type = MapNodeType.Battle, Weight = 0.26f },
            new WeightEntry { Type = MapNodeType.Elite, Weight = 0.32f },
            new WeightEntry { Type = MapNodeType.Event, Weight = 0.17f },
            new WeightEntry { Type = MapNodeType.Relay, Weight = 0.05f },
            new WeightEntry { Type = MapNodeType.Camp, Weight = 0.06f },
            new WeightEntry { Type = MapNodeType.Treasure, Weight = 0.14f }
        };

        public static WeightEntry[] WeightsFromTemplate(MapTemplateSO template)
        {
            if (template == null)
            {
                return DefaultWeights;
            }

            return new[]
            {
                new WeightEntry { Type = MapNodeType.Battle, Weight = template.battleWeight },
                new WeightEntry { Type = MapNodeType.Elite, Weight = template.eliteWeight },
                new WeightEntry { Type = MapNodeType.Event, Weight = template.eventWeight },
                new WeightEntry { Type = MapNodeType.Relay, Weight = template.relayWeight },
                new WeightEntry { Type = MapNodeType.Camp, Weight = template.campWeight },
                new WeightEntry { Type = MapNodeType.Treasure, Weight = template.treasureWeight }
            };
        }

        public static void ApplyFixedFloors(MapGraph graph)
        {
            ApplyFixedFloors(graph, graph?.Profile ?? MapGenerationProfile.Default);
        }

        public static void ApplyFixedFloors(MapGraph graph, MapGenerationProfile profile)
        {
            var treasureFloor = profile.TreasureFloor;
            var campFloor = profile.FloorCount;
            var centerColumn = (profile.ColumnCount - 1) / 2;

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    node.Type = MapNodeType.Boss;
                    continue;
                }

                if (node.Floor == 1)
                {
                    node.Type = MapNodeType.Battle;
                }
            }

            AssignSingleFloorSpecial(graph, treasureFloor, MapNodeType.Treasure, centerColumn);
            AssignSingleFloorSpecial(graph, campFloor, MapNodeType.Camp, centerColumn);
        }

        private static void AssignSingleFloorSpecial(
            MapGraph graph,
            int floor,
            MapNodeType type,
            int centerColumn)
        {
            MapNodeData chosen = null;
            var bestDistance = int.MaxValue;

            foreach (var node in graph.NodesOnFloor(floor))
            {
                var distance = Math.Abs(node.Column - centerColumn);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                chosen = node;
            }

            if (chosen != null)
            {
                chosen.Type = type;
            }
        }

        public static void AssignRandomTypes(MapGraph graph, System.Random rng, WeightEntry[] weights = null)
        {
            AssignRandomTypes(graph, rng, weights, graph?.Profile ?? MapGenerationProfile.Default);
        }

        public static void AssignRandomTypes(
            MapGraph graph,
            System.Random rng,
            WeightEntry[] weights,
            MapGenerationProfile profile)
        {
            weights ??= DefaultWeights;
            var maxAttempts = 800;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                AssignDiverseFloorTypes(graph, rng, weights, profile);

                if (ValidateRules(graph, profile) && ValidateEliteDensity(graph) && ValidateFloorTypeDiversity(graph, profile))
                {
                    return;
                }
            }

            FallbackSafeTypes(graph, profile);
            EnforceEliteDensity(graph, rng, profile);
            EnforceFloorTypeDiversity(graph, rng, profile);
        }

        private static void AssignDiverseFloorTypes(
            MapGraph graph,
            System.Random rng,
            WeightEntry[] weights,
            MapGenerationProfile profile)
        {
            var treasureFloor = profile.TreasureFloor;
            var campFloor = profile.FloorCount;
            var nodesByFloor = new Dictionary<int, List<MapNodeData>>();

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor == 1)
                {
                    continue;
                }

                if (!nodesByFloor.TryGetValue(node.Floor, out var list))
                {
                    list = new List<MapNodeData>();
                    nodesByFloor[node.Floor] = list;
                }

                list.Add(node);
            }

            foreach (var pair in nodesByFloor)
            {
                var nodes = pair.Value.OrderBy(n => n.Column).ToList();
                var assignable = new List<MapNodeData>();
                foreach (var node in nodes)
                {
                    if (node.Floor == treasureFloor && node.Type == MapNodeType.Treasure)
                    {
                        continue;
                    }

                    if (node.Floor == campFloor && node.Type == MapNodeType.Camp)
                    {
                        continue;
                    }

                    assignable.Add(node);
                }

                if (assignable.Count == 0)
                {
                    continue;
                }

                var pool = BuildUniqueTypePool(rng, weights, assignable.Count);
                for (var i = 0; i < assignable.Count; i++)
                {
                    assignable[i].Type = pool[i];
                }
            }
        }

        private static List<MapNodeType> BuildUniqueTypePool(System.Random rng, WeightEntry[] weights, int count)
        {
            var ordered = weights
                .OrderByDescending(entry => entry.Weight)
                .Select(entry => entry.Type)
                .ToList();
            var pool = new List<MapNodeType>(count);
            var guard = 0;

            while (pool.Count < count && guard < 128)
            {
                guard++;
                var type = RollType(rng, weights);
                if (!pool.Contains(type))
                {
                    pool.Add(type);
                }
            }

            var fallbackIndex = 0;
            while (pool.Count < count && fallbackIndex < ordered.Count * 2)
            {
                var type = ordered[fallbackIndex % ordered.Count];
                if (!pool.Contains(type))
                {
                    pool.Add(type);
                }

                fallbackIndex++;
            }

            return pool;
        }

        public static bool ValidateFloorTypeDiversity(MapGraph graph, MapGenerationProfile profile)
        {
            var treasureFloor = profile.TreasureFloor;
            var campFloor = profile.FloorCount;
            var typesByFloor = new Dictionary<int, HashSet<MapNodeType>>();

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor == 1)
                {
                    continue;
                }

                if (!typesByFloor.TryGetValue(node.Floor, out var types))
                {
                    types = new HashSet<MapNodeType>();
                    typesByFloor[node.Floor] = types;
                }

                if (!types.Add(node.Type))
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnforceFloorTypeDiversity(MapGraph graph, System.Random rng, MapGenerationProfile profile)
        {
            if (ValidateFloorTypeDiversity(graph, profile))
            {
                return;
            }

            AssignDiverseFloorTypes(graph, rng, DefaultWeights, profile);
        }

        private static MapNodeType RollType(System.Random rng, WeightEntry[] weights)
        {
            var roll = (float)rng.NextDouble();
            var cumulative = 0f;
            foreach (var entry in weights)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative)
                {
                    return entry.Type;
                }
            }

            return MapNodeType.Battle;
        }

        public static bool ValidateRules(MapGraph graph)
        {
            return ValidateRules(graph, graph?.Profile ?? MapGenerationProfile.Default);
        }

        public static bool ValidateRules(MapGraph graph, MapGenerationProfile profile)
        {
            var campFloor = profile.FloorCount;
            var preCampFloor = System.Math.Max(1, campFloor - 1);

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    continue;
                }

                if (node.Floor < 6 && (node.Type == MapNodeType.Elite || node.Type == MapNodeType.Camp))
                {
                    return false;
                }

                if (node.Floor == preCampFloor && node.Type == MapNodeType.Camp)
                {
                    return false;
                }
            }

            foreach (var node in graph.Nodes)
            {
                if (node.Outgoing.Count < 2)
                {
                    continue;
                }

                var destTypes = new HashSet<MapNodeType>();
                foreach (var toId in node.Outgoing)
                {
                    var dest = graph.GetNode(toId);
                    if (dest == null)
                    {
                        continue;
                    }

                    if (!destTypes.Add(dest.Type))
                    {
                        return false;
                    }
                }
            }

            foreach (var node in graph.Nodes)
            {
                foreach (var toId in node.Outgoing)
                {
                    var next = graph.GetNode(toId);
                    if (next == null || next.IsBoss)
                    {
                        continue;
                    }

                    if (IsRestrictedPair(node.Type, next.Type))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool ValidateEliteDensity(MapGraph graph)
        {
            var total = 0;
            var elites = 0;

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    continue;
                }

                total++;
                if (node.Type == MapNodeType.Elite)
                {
                    elites++;
                }
            }

            if (total == 0)
            {
                return true;
            }

            var ratio = (float)elites / total;
            return ratio >= EliteDensityMin && ratio <= EliteDensityMax;
        }

        private static void EnforceEliteDensity(MapGraph graph, System.Random rng)
        {
            EnforceEliteDensity(graph, rng, graph?.Profile ?? MapGenerationProfile.Default);
        }

        private static void EnforceEliteDensity(MapGraph graph, System.Random rng, MapGenerationProfile profile)
        {
            if (ValidateEliteDensity(graph))
            {
                return;
            }

            var total = 0;
            var elites = 0;
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    continue;
                }

                total++;
                if (node.Type == MapNodeType.Elite)
                {
                    elites++;
                }
            }

            if (total == 0)
            {
                return;
            }

            var minElites = (int)System.Math.Ceiling(total * EliteDensityMin);
            var maxElites = (int)System.Math.Floor(total * EliteDensityMax);

            if (elites < minElites)
            {
                PromoteNodesToElite(graph, rng, minElites - elites, profile);
            }
            else if (elites > maxElites)
            {
                DemoteEliteNodes(graph, rng, elites - maxElites);
            }
        }

        private static void PromoteNodesToElite(MapGraph graph, System.Random rng, int count)
        {
            PromoteNodesToElite(graph, rng, count, graph?.Profile ?? MapGenerationProfile.Default);
        }

        private static void PromoteNodesToElite(
            MapGraph graph,
            System.Random rng,
            int count,
            MapGenerationProfile profile)
        {
            var treasureFloor = profile.TreasureFloor;
            var campFloor = profile.FloorCount;
            var candidates = new List<MapNodeData>();
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor < 6 || node.Floor == treasureFloor ||
                    node.Floor == campFloor)
                {
                    continue;
                }

                if (node.Type == MapNodeType.Battle || node.Type == MapNodeType.Event)
                {
                    candidates.Add(node);
                }
            }

            Shuffle(candidates, rng);

            foreach (var node in candidates)
            {
                if (count <= 0)
                {
                    break;
                }

                var previousType = node.Type;
                node.Type = MapNodeType.Elite;
                if (ValidateRules(graph, profile) && ValidateEliteDensity(graph))
                {
                    count--;
                    continue;
                }

                node.Type = previousType;
            }
        }

        private static void DemoteEliteNodes(MapGraph graph, System.Random rng, int count)
        {
            var candidates = new List<MapNodeData>();
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Type != MapNodeType.Elite)
                {
                    continue;
                }

                candidates.Add(node);
            }

            Shuffle(candidates, rng);

            foreach (var node in candidates)
            {
                if (count <= 0)
                {
                    break;
                }

                node.Type = MapNodeType.Battle;
                if (ValidateRules(graph) && ValidateEliteDensity(graph))
                {
                    count--;
                    continue;
                }

                node.Type = MapNodeType.Elite;
            }
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool IsRestrictedPair(MapNodeType a, MapNodeType b)
        {
            if (a == MapNodeType.Elite && b == MapNodeType.Elite)
            {
                return true;
            }

            if (a == MapNodeType.Relay && b == MapNodeType.Relay)
            {
                return true;
            }

            if (a == MapNodeType.Camp && b == MapNodeType.Camp)
            {
                return true;
            }

            return false;
        }

        private static void FallbackSafeTypes(MapGraph graph)
        {
            FallbackSafeTypes(graph, graph?.Profile ?? MapGenerationProfile.Default);
        }

        private static void FallbackSafeTypes(MapGraph graph, MapGenerationProfile profile)
        {
            var treasureFloor = profile.TreasureFloor;
            var campFloor = profile.FloorCount;

            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor == 1 || node.Type == MapNodeType.Treasure ||
                    node.Type == MapNodeType.Camp)
                {
                    continue;
                }

                node.Type = node.Floor % 4 == 0 ? MapNodeType.Event : MapNodeType.Battle;
            }

            EnforceFloorTypeDiversity(graph, new System.Random(17), profile);
        }
    }
}
