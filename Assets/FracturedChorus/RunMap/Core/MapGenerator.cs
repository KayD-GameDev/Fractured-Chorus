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
            var startColumns = BuildStartColumns(rng, pathCount, profile);

            for (var i = 0; i < startColumns.Count && paths.Count < pathCount; i++)
            {
                TryAddPath(paths, signatures, GenerateSinglePath(rng, startColumns[i], profile));
            }

            var attempts = 0;
            while (paths.Count < pathCount && attempts < MaxGenerationAttempts)
            {
                attempts++;
                var center = BossColumnFor(profile);
                var startCol = center + (rng.Next(3) - 1);
                TryAddPath(paths, signatures, GenerateSinglePath(rng, startCol, profile));
            }

            attempts = 0;
            while (paths.Count < pathCount && paths.Count > 0 && attempts < MaxGenerationAttempts)
            {
                attempts++;
                TryAddPath(paths, signatures, MutatePath(rng, paths[rng.Next(paths.Count)], profile));
            }

            EnsureSymmetricPaths(paths, signatures, pathCount, profile.ColumnCount);
            return paths;
        }

        private static List<int[]> GeneratePaths(Random rng, int pathCount) =>
            GeneratePaths(rng, pathCount, MapGenerationProfile.Default);

        private static List<int> BuildStartColumns(Random rng, int pathCount, MapGenerationProfile profile)
        {
            var center = BossColumnFor(profile);
            var startPool = new List<int> { center };
            if (center - 1 >= 1)
            {
                startPool.Add(center - 1);
            }

            if (center + 1 < profile.ColumnCount - 1)
            {
                startPool.Add(center + 1);
            }

            Shuffle(rng, startPool);
            var assigned = new List<int>(pathCount);

            for (var i = 0; i < pathCount; i++)
            {
                assigned.Add(startPool[i % startPool.Count]);
            }

            return assigned;
        }

        private static int PickSymmetricStart(Random rng, List<int> startPool, int center, int columnCount)
        {
            var pick = startPool[rng.Next(startPool.Count)];
            return rng.NextDouble() < 0.5 ? pick : MirrorColumn(pick, columnCount);
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

        private static int[] GenerateSinglePath(Random rng, int startCol, MapGenerationProfile profile)
        {
            var center = BossColumnFor(profile);
            startCol = ClampColumn(startCol, profile.ColumnCount);
            if (Math.Abs(startCol - center) > MapLayoutConstants.MaxDriftFromCenter)
            {
                startCol = center;
            }

            var path = new int[profile.FloorCount];
            path[0] = startCol;

            for (var floor = 1; floor < profile.FloorCount; floor++)
            {
                var options = GetNeighborColumns(path[floor - 1], profile.ColumnCount, center);
                if (options.Count == 0)
                {
                    return null;
                }

                var converge = floor >= (int)(profile.FloorCount * 0.55f);
                path[floor] = floor >= profile.FloorCount - 2 || converge
                    ? PickColumnTowardCenter(rng, options, center)
                    : PickNextColumn(rng, path[floor - 1], options, center);
            }

            return path;
        }

        private static int PickNextColumn(Random rng, int previousColumn, List<int> options, int centerColumn)
        {
            var candidates = new List<int>();
            var bestScore = float.MinValue;

            foreach (var option in options)
            {
                var lateral = Math.Abs(option - previousColumn);
                if (lateral > MapLayoutConstants.MaxColumnConnectDelta)
                {
                    continue;
                }

                var centerDistance = Math.Abs(option - centerColumn);
                if (centerDistance > MapLayoutConstants.MaxDriftFromCenter)
                {
                    continue;
                }

                var score = lateral == 0 ? MapLayoutConstants.CenterColumnBiasWeight : 1.5f;
                score /= 1f + centerDistance * 0.25f;

                if (Math.Abs(option - centerColumn) > Math.Abs(previousColumn - centerColumn))
                {
                    score *= 0.35f;
                }

                if (score > bestScore + 0.01f)
                {
                    bestScore = score;
                    candidates.Clear();
                    candidates.Add(option);
                }
                else if (Math.Abs(score - bestScore) <= 0.01f)
                {
                    candidates.Add(option);
                }
            }

            if (candidates.Count == 0)
            {
                return PickColumnTowardCenter(rng, options, centerColumn);
            }

            return candidates[rng.Next(candidates.Count)];
        }

        private static int[] GenerateSinglePath(Random rng, int startCol) =>
            GenerateSinglePath(rng, startCol, MapGenerationProfile.Default);

        private static int[] MutatePath(Random rng, int[] source, MapGenerationProfile profile)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }

            var path = (int[])source.Clone();
            var mutationCount = rng.Next(1, 2);

            for (var m = 0; m < mutationCount; m++)
            {
                var floor = rng.Next(1, path.Length);
                var options = GetNeighborColumns(path[floor - 1], profile.ColumnCount, BossColumnFor(profile));
                if (options.Count == 0)
                {
                    continue;
                }

                options.RemoveAll(col => col == path[floor]);
                if (options.Count == 0)
                {
                    continue;
                }

                path[floor] = PickNextColumn(rng, path[floor - 1], options, BossColumnFor(profile));

                for (var f = floor + 1; f < path.Length; f++)
                {
                    var nextOptions = GetNeighborColumns(path[f - 1], profile.ColumnCount, BossColumnFor(profile));
                    if (nextOptions.Count == 0)
                    {
                        return null;
                    }

                    if (!nextOptions.Contains(path[f]))
                    {
                        var center = BossColumnFor(profile);
                        var converge = f >= (int)(profile.FloorCount * 0.55f);
                        path[f] = f >= profile.FloorCount - 2 || converge
                            ? PickColumnTowardCenter(rng, nextOptions, center)
                            : PickNextColumn(rng, path[f - 1], nextOptions, center);
                    }
                }
            }

            return path;
        }

        private static int[] MutatePath(Random rng, int[] source) =>
            MutatePath(rng, source, MapGenerationProfile.Default);

        private static List<int> GetNeighborColumns(int column, int columnCount, int centerColumn)
        {
            var options = GetNeighborColumns(column, columnCount);
            var filtered = options
                .Where(col => Math.Abs(col - centerColumn) <= MapLayoutConstants.MaxDriftFromCenter)
                .ToList();
            return filtered.Count > 0 ? filtered : options;
        }

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

        private static List<int> GetNeighborColumns(int column) =>
            GetNeighborColumns(column, MapLayoutConstants.ColumnCount);

        private static int PickColumnTowardCenter(Random rng, List<int> options, int centerColumn)
        {
            if (options.Count == 1)
            {
                return options[0];
            }

            options.Sort();
            var total = 0f;
            var weights = new float[options.Count];

            for (var i = 0; i < options.Count; i++)
            {
                var weight = 1f / (1f + Math.Abs(options[i] - centerColumn));
                weights[i] = weight;
                total += weight;
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

        private static int ClampColumn(int column, int columnCount) =>
            Math.Max(0, Math.Min(columnCount - 1, column));

        private static int ClampColumn(int column) =>
            ClampColumn(column, MapLayoutConstants.ColumnCount);

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
            int pathCount,
            int columnCount)
        {
            var index = 0;
            while (paths.Count < pathCount && index < paths.Count)
            {
                TryAddPath(paths, signatures, MirrorPath(paths[index], columnCount));
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
