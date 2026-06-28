using System;
using System.Collections.Generic;

namespace FracturedChorus.RunMap.Core
{
    public static class NodeTypeAssigner
    {
        public struct WeightEntry
        {
            public MapNodeType Type;
            public float Weight;
        }

        public static readonly WeightEntry[] DefaultWeights =
        {
            new WeightEntry { Type = MapNodeType.Battle, Weight = 0.45f },
            new WeightEntry { Type = MapNodeType.Elite, Weight = 0.16f },
            new WeightEntry { Type = MapNodeType.Event, Weight = 0.22f },
            new WeightEntry { Type = MapNodeType.Relay, Weight = 0.05f },
            new WeightEntry { Type = MapNodeType.Camp, Weight = 0.06f },
            new WeightEntry { Type = MapNodeType.Treasure, Weight = 0.06f }
        };

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
            var maxAttempts = 500;

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

                if (ValidateRules(graph))
                {
                    return;
                }
            }

            FallbackSafeTypes(graph);
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
