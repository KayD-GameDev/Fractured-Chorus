using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;

namespace FracturedChorus.RunMap.Core
{
    /// <summary>
    /// StS-style map generation: template grid → path gen ×N → prune → fixed floors → random types → boss F16.
    /// Ref: docs/diagrams + scripts/build_fc_diagrams_drawio.py
    /// </summary>
    public static class MapGenerator
    {
        private const int MaxGenerationAttempts = 400;

        private static int BossColumnFor(MapGenerationProfile profile) =>
            (profile.ColumnCount - 1) / 2;

        public static MapGraph Generate(int seed, int pathCount = MapLayoutConstants.DefaultPathCount, NodeTypeAssigner.WeightEntry[] weights = null)
        {
            return Generate(seed, MapGenerationProfile.Default, weights, pathCount);
        }

        public static MapGraph Generate(
            int seed,
            MapGenerationProfile profile,
            NodeTypeAssigner.WeightEntry[] weights = null,
            int? pathCountOverride = null)
        {
            profile ??= MapGenerationProfile.Default;
            var rng = new Random(seed);
            var pathCount = pathCountOverride ?? profile.PathCount;
            var paths = GeneratePaths(rng, pathCount, profile);
            var graph = BuildGraphFromPaths(seed, paths, profile);
            return FinalizeGeneratedGraph(graph, profile, rng, weights);
        }

        public static MapGraph GenerateSector(
            PinkySectorId sector,
            int seed,
            NodeTypeAssigner.WeightEntry[] weights = null,
            PinkyVaultConfigSO vaultConfig = null)
        {
            var profile = vaultConfig != null
                ? vaultConfig.ProfileFor(sector)
                : MapGenerationProfile.ForSector(sector);
            weights ??= vaultConfig?.WeightsFor(sector);
            return Generate(seed, profile, weights);
        }

        /// <summary>Demo map cố định — khớp STS_PATHS trong build_fc_diagrams_drawio.py.</summary>
        public static MapGraph GenerateDemoReference(int seed = 42)
        {
            var graph = BuildGraphFromPaths(seed, ReferencePaths());
            ApplyReferenceLocations(graph);
            AttachStartNode(graph);
            return graph;
        }

        public static MapGraph GenerateFromTemplate(MapTemplateSO template, int seed)
        {
            if (template != null && template.useReferenceDemoOnPlay)
            {
                return GenerateDemoReference(seed);
            }

            var pathCount = template != null ? template.pathCount : MapLayoutConstants.DefaultPathCount;
            var weights = template != null ? NodeTypeAssigner.WeightsFromTemplate(template) : null;
            return Generate(seed, pathCount, weights);
        }

        private static MapGraph BuildGraphFromPaths(int seed, IReadOnlyList<int[]> paths, MapGenerationProfile profile)
        {
            var graph = new MapGraph();
            graph.Reset(seed, profile);

            foreach (var (floor, column) in PathValidator.CollectActiveCells(paths).OrderBy(c => c.floor).ThenBy(c => c.column))
            {
                graph.AddNode(floor, column, MapNodeType.Battle);
            }

            WirePathEdges(graph, paths);
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss || node.Type == MapNodeType.Start)
                {
                    continue;
                }

                if (node.Floor < 1 || node.Floor > MapLayoutConstants.ExclusivePrefixFloors)
                {
                    continue;
                }

                if (node.Outgoing.Count <= 1)
                {
                    continue;
                }

                for (var i = node.Outgoing.Count - 1; i >= 1; i--)
                {
                    var toId = node.Outgoing[i];
                    node.Outgoing.RemoveAt(i);
                    graph.GetNode(toId)?.Incoming.Remove(node.Id);
                }
            }

            PathValidator.RemoveDanglingNodes(graph);

            var bossColumn = BossColumnFor(profile);
            var boss = graph.AddNode(profile.BossFloor, bossColumn, MapNodeType.Boss, isBoss: true);
            foreach (var preBoss in graph.NodesOnFloor(profile.FloorCount))
            {
                graph.Connect(preBoss.Id, boss.Id);
            }

            PathValidator.PruneToBossReachable(graph);
            return graph;
        }

        private static void AttachStartNode(MapGraph graph)
        {
            if (graph.StartNode != null)
            {
                return;
            }

            var centerCol = BossColumnFor(graph.Profile);
            var start = graph.AddNode(0, centerCol, MapNodeType.Start);
            var linked = 0;
            foreach (var entry in graph.NodesOnFloor(1))
            {
                graph.Connect(start.Id, entry.Id);
                linked++;
            }

            if (linked == 0)
            {
                UnityEngine.Debug.LogWarning("[Fractured Chorus] Start node has no F1 connections.");
            }
        }

        private static MapGraph FinalizeGeneratedGraph(
            MapGraph graph,
            MapGenerationProfile profile,
            System.Random rng,
            NodeTypeAssigner.WeightEntry[] weights)
        {
            NodeTypeAssigner.ApplyFixedFloors(graph, profile);
            NodeTypeAssigner.AssignRandomTypes(graph, rng, weights, profile);
            AttachStartNode(graph);
            return graph;
        }

        private static MapGraph BuildGraphFromPaths(int seed, IReadOnlyList<int[]> paths) =>
            BuildGraphFromPaths(seed, paths, MapGenerationProfile.Default);

        private static List<int[]> GeneratePaths(Random rng, int pathCount, MapGenerationProfile profile)
        {
            var paths = new List<int[]>(pathCount);
            var signatures = new HashSet<string>();
            var reservedPrefix = new HashSet<(int floor, int column)>();
            var startColumns = BuildStartColumns(rng, pathCount, profile);

            for (var i = 0; i < startColumns.Count && paths.Count < pathCount; i++)
            {
                TryAddExclusivePath(
                    paths,
                    signatures,
                    reservedPrefix,
                    GenerateSinglePath(rng, startColumns[i], profile, reservedPrefix));
            }

            var attempts = 0;
            while (paths.Count < pathCount && attempts < MaxGenerationAttempts)
            {
                attempts++;
                TryAddExclusivePath(
                    paths,
                    signatures,
                    reservedPrefix,
                    GenerateSinglePath(rng, rng.Next(profile.ColumnCount), profile, reservedPrefix));
            }

            EnsureSymmetricPaths(paths, signatures, reservedPrefix, pathCount, profile.ColumnCount);
            return paths;
        }

        private static List<int[]> GeneratePaths(Random rng, int pathCount) =>
            GeneratePaths(rng, pathCount, MapGenerationProfile.Default);

        private static List<int> BuildStartColumns(Random rng, int pathCount, MapGenerationProfile profile)
        {
            var columnCount = Math.Max(1, profile.ColumnCount);
            var count = Math.Clamp(pathCount, MapLayoutConstants.MinStartNodes, MapLayoutConstants.MaxStartNodes);
            count = Math.Min(count, columnCount);

            var pool = new List<int>(columnCount);
            for (var column = 0; column < columnCount; column++)
            {
                pool.Add(column);
            }

            Shuffle(rng, pool);
            var assigned = new List<int>(count);
            foreach (var column in pool)
            {
                if (assigned.Count >= count)
                {
                    break;
                }

                if (assigned.Exists(existing => Math.Abs(existing - column) <= 1))
                {
                    continue;
                }

                assigned.Add(column);
            }

            foreach (var column in pool)
            {
                if (assigned.Count >= count)
                {
                    break;
                }

                if (!assigned.Contains(column))
                {
                    assigned.Add(column);
                }
            }

            return assigned;
        }

        private static List<int> BuildStartColumns(Random rng, int pathCount) =>
            BuildStartColumns(rng, pathCount, MapGenerationProfile.Default);

        private static bool TryAddPath(List<int[]> paths, HashSet<string> signatures, int[] path)
        {
            if (path == null)
            {
                return false;
            }

            var signature = PathSignature(path);
            if (!signatures.Add(signature))
            {
                return false;
            }

            paths.Add(path);
            return true;
        }

        private static bool TryAddExclusivePath(
            List<int[]> paths,
            HashSet<string> signatures,
            HashSet<(int floor, int column)> reservedPrefix,
            int[] path)
        {
            if (PrefixConflicts(path, reservedPrefix))
            {
                return false;
            }

            if (!TryAddPath(paths, signatures, path))
            {
                return false;
            }

            ReservePrefix(path, reservedPrefix);
            return true;
        }

        private static bool PrefixConflicts(int[] path, HashSet<(int floor, int column)> reservedPrefix)
        {
            if (path == null || reservedPrefix == null)
            {
                return path == null;
            }

            foreach (var floor in ExclusiveFloors(path.Length))
            {
                if (reservedPrefix.Contains((floor, path[floor])))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ReservePrefix(int[] path, HashSet<(int floor, int column)> reservedPrefix)
        {
            if (path == null || reservedPrefix == null)
            {
                return;
            }

            foreach (var floor in ExclusiveFloors(path.Length))
            {
                reservedPrefix.Add((floor, path[floor]));
            }
        }

        private static IEnumerable<int> ExclusiveFloors(int pathLength)
        {
            var prefix = Math.Min(MapLayoutConstants.ExclusivePrefixFloors, pathLength);
            for (var floor = 0; floor < prefix; floor++)
            {
                yield return floor;
            }

            var last = pathLength - 1;
            if (last >= prefix)
            {
                yield return last;
            }
        }

        private static int[] GenerateSinglePath(
            Random rng,
            int startCol,
            MapGenerationProfile profile,
            HashSet<(int floor, int column)> reservedPrefix = null)
        {
            startCol = ClampColumn(startCol, profile.ColumnCount);
            if (reservedPrefix != null && reservedPrefix.Contains((0, startCol)))
            {
                return null;
            }

            var path = new int[profile.FloorCount];
            path[0] = startCol;

            for (var floor = 1; floor < profile.FloorCount; floor++)
            {
                var options = GetNeighborColumns(path[floor - 1], profile.ColumnCount);
                if (reservedPrefix != null && IsReservedFloor(floor, profile.FloorCount))
                {
                    options.RemoveAll(column => reservedPrefix.Contains((floor, column)));
                }

                if (options.Count == 0)
                {
                    return null;
                }

                path[floor] = PickNextColumn(rng, path[floor - 1], options);
            }

            return path;
        }

        private static int PickNextColumn(Random rng, int previousColumn, List<int> options)
        {
            if (options == null || options.Count == 0)
            {
                return previousColumn;
            }

            if (options.Count == 1)
            {
                return options[0];
            }

            var total = 0f;
            var weights = new float[options.Count];
            for (var i = 0; i < options.Count; i++)
            {
                var lateral = Math.Abs(options[i] - previousColumn);
                if (lateral > MapLayoutConstants.MaxColumnConnectDelta)
                {
                    weights[i] = 0f;
                    continue;
                }

                weights[i] = lateral == 0 ? 1f : 2f;
                total += weights[i];
            }

            if (total <= 0f)
            {
                return options[rng.Next(options.Count)];
            }

            var roll = (float)rng.NextDouble() * total;
            var cumulative = 0f;
            for (var i = 0; i < options.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return options[i];
                }
            }

            return options[options.Count - 1];
        }

        private static bool IsReservedFloor(int pathIndex, int floorCount) =>
            pathIndex < MapLayoutConstants.ExclusivePrefixFloors || pathIndex == floorCount - 1;

        private static int[] GenerateSinglePath(Random rng, int startCol) =>
            GenerateSinglePath(rng, startCol, MapGenerationProfile.Default);

        private static List<int> GetNeighborColumns(int column, int columnCount)
        {
            var options = new List<int>(3);
            for (var delta = -1; delta <= 1; delta++)
            {
                var col = column + delta;
                if (col >= 0 && col < columnCount)
                {
                    options.Add(col);
                }
            }

            return options;
        }

        private static int ClampColumn(int column, int columnCount) =>
            Math.Max(0, Math.Min(columnCount - 1, column));

        private static string PathSignature(int[] path)
        {
            var builder = new StringBuilder(path.Length * 2);
            for (var i = 0; i < path.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append('-');
                }

                builder.Append(path[i]);
            }

            return builder.ToString();
        }

        private static void Shuffle(Random rng, IList<int> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void WireProximityEdges(MapGraph graph, int maxColumnDelta)
        {
            foreach (var node in graph.Nodes.ToList())
            {
                if (node.IsBoss || node.Floor <= 1)
                {
                    continue;
                }

                foreach (var parent in graph.NodesOnFloor(node.Floor - 1))
                {
                    if (Math.Abs(parent.Column - node.Column) > maxColumnDelta)
                    {
                        continue;
                    }

                    graph.Connect(parent.Id, node.Id);
                }
            }
        }

        private static void EnsureSymmetricPaths(
            List<int[]> paths,
            HashSet<string> signatures,
            HashSet<(int floor, int column)> reservedPrefix,
            int pathCount,
            int columnCount)
        {
            var index = 0;
            while (paths.Count < pathCount && index < paths.Count)
            {
                TryAddExclusivePath(paths, signatures, reservedPrefix, MirrorPath(paths[index], columnCount));
                index++;
            }
        }

        private static int[] MirrorPath(int[] path, int columnCount)
        {
            var mirrored = new int[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                mirrored[i] = MirrorColumn(path[i], columnCount);
            }

            return mirrored;
        }

        private static int MirrorColumn(int column, int columnCount) =>
            columnCount - 1 - column;

        private static void WirePathEdges(MapGraph graph, IReadOnlyList<int[]> paths)
        {
            var edgeSet = new HashSet<(int floor, int fromCol, int toCol)>();

            foreach (var path in paths)
            {
                for (var floor = 0; floor < path.Length - 1; floor++)
                {
                    edgeSet.Add((floor + 1, path[floor], path[floor + 1]));
                }
            }

            foreach (var (floor, fromCol, toCol) in edgeSet)
            {
                var from = graph.FindNode(floor, fromCol);
                var to = graph.FindNode(floor + 1, toCol);
                if (from != null && to != null)
                {
                    graph.Connect(from.Id, to.Id);
                }
            }
        }

        private static void ApplyReferenceLocations(MapGraph graph)
        {
            var locations = ReferenceLocations();
            foreach (var node in graph.Nodes)
            {
                if (node.IsStart || node.Type == MapNodeType.Start)
                {
                    continue;
                }

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
                    default:
                        node.Type = locations.TryGetValue((node.Floor, node.Column), out var code)
                            ? StsCodeToType(code)
                            : MapNodeType.Battle;
                        break;
                }
            }
        }

        private static MapNodeType StsCodeToType(string code) => code switch
        {
            "M" => MapNodeType.Battle,
            "?" => MapNodeType.Event,
            "E" => MapNodeType.Elite,
            "Rest" => MapNodeType.Camp,
            "Shop" => MapNodeType.Relay,
            "T" => MapNodeType.Treasure,
            "Boss" => MapNodeType.Boss,
            _ => MapNodeType.Battle
        };

        public static List<int[]> ReferencePaths() => new List<int[]>
        {
            new[] { 1, 1, 2, 2, 3, 3, 4, 4, 2, 2, 3, 3, 4, 3, 3 },
            new[] { 2, 3, 3, 4, 4, 5, 5, 4, 3, 4, 4, 5, 5, 4, 4 },
            new[] { 3, 2, 2, 1, 2, 2, 3, 2, 1, 2, 2, 1, 2, 2, 2 },
            new[] { 4, 4, 5, 5, 6, 5, 4, 5, 5, 6, 5, 4, 3, 4, 5 },
            new[] { 0, 1, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0 },
            new[] { 5, 4, 4, 3, 2, 3, 2, 3, 4, 3, 2, 3, 3, 2, 1 }
        };

        public static Dictionary<(int floor, int col), string> ReferenceLocations() =>
            new Dictionary<(int, int), string>
            {
                { (2, 1), "?" }, { (2, 2), "M" }, { (2, 3), "E" }, { (2, 4), "M" },
                { (3, 2), "Shop" }, { (3, 3), "M" }, { (3, 4), "?" },
                { (4, 1), "Rest" }, { (4, 2), "M" }, { (4, 3), "M" }, { (4, 4), "E" },
                { (5, 2), "?" }, { (5, 3), "M" }, { (5, 4), "?" }, { (5, 5), "Rest" },
                { (6, 1), "Rest" }, { (6, 2), "M" }, { (6, 3), "M" }, { (6, 4), "M" }, { (6, 5), "?" },
                { (7, 2), "M" }, { (7, 3), "?" }, { (7, 4), "Rest" },
                { (8, 1), "?" }, { (8, 2), "Rest" }, { (8, 3), "M" }, { (8, 4), "M" }, { (8, 5), "?" },
                { (10, 1), "?" }, { (10, 2), "M" }, { (10, 3), "M" }, { (10, 4), "E" },
                { (11, 2), "E" }, { (11, 3), "?" }, { (11, 4), "?" }, { (11, 5), "M" },
                { (12, 1), "Rest" }, { (12, 2), "M" }, { (12, 3), "M" },
                { (13, 2), "?" }, { (13, 3), "Shop" }, { (13, 4), "M" },
                { (14, 2), "Rest" }, { (14, 3), "M" }, { (14, 4), "M" }
            };
    }
}
