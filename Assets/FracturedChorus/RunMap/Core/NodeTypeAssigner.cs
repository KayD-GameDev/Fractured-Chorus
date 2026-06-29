using System;
using System.Collections.Generic;

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
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    node.Type = MapNodeType.Boss;
                    continue;
                }

                switch (node.Floor)
                {
                    case 1:
                        node.Type = MapNodeType.Battle;
                        break;
                    case 9:
                        node.Type = MapNodeType.Treasure;
                        break;
                    case MapLayoutConstants.FloorCount:
                        node.Type = MapNodeType.Camp;
                        break;
                }
            }
        }

        public static void AssignRandomTypes(MapGraph graph, System.Random rng, WeightEntry[] weights = null)
        {
            weights ??= DefaultWeights;
            var maxAttempts = 800;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                foreach (var node in graph.Nodes)
                {
                    if (node.IsBoss || node.Floor == 1 || node.Floor == 9 ||
                        node.Floor == MapLayoutConstants.FloorCount)
                    {
                        continue;
                    }

                    node.Type = RollType(rng, weights);
                }

                if (ValidateRules(graph) && ValidateEliteDensity(graph))
                {
                    return;
                }
            }

            FallbackSafeTypes(graph);
            EnforceEliteDensity(graph, rng);
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

                if (node.Floor == 14 && node.Type == MapNodeType.Camp)
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
                PromoteNodesToElite(graph, rng, minElites - elites);
            }
            else if (elites > maxElites)
            {
                DemoteEliteNodes(graph, rng, elites - maxElites);
            }
        }

        private static void PromoteNodesToElite(MapGraph graph, System.Random rng, int count)
        {
            var candidates = new List<MapNodeData>();
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor < 6 || node.Floor == 9 ||
                    node.Floor == MapLayoutConstants.FloorCount)
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
                if (ValidateRules(graph) && ValidateEliteDensity(graph))
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
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Floor == 1 || node.Floor == 9 ||
                    node.Floor == MapLayoutConstants.FloorCount)
                {
                    continue;
                }

                node.Type = node.Floor % 4 == 0 ? MapNodeType.Event : MapNodeType.Battle;
            }
        }
    }
}
