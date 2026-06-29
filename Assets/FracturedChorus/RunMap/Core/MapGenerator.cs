using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FracturedChorus.Data;

namespace FracturedChorus.RunMap.Core
{
    /// <summary>
    /// StS-style map generation: template grid → path gen ×N → prune → fixed floors → random types → boss F16.
    /// Ref: docs/diagrams + scripts/build_fc_diagrams_drawio.py
    /// </summary>
    public static class MapGenerator
    {
        private const int BossColumn = 3;
        private const int MaxGenerationAttempts = 400;

        public static MapGraph Generate(int seed, int pathCount = MapLayoutConstants.DefaultPathCount, NodeTypeAssigner.WeightEntry[] weights = null)
        {
            var rng = new Random(seed);
            var paths = GeneratePaths(rng, pathCount);
            var graph = BuildGraphFromPaths(seed, paths);
            NodeTypeAssigner.ApplyFixedFloors(graph);
            NodeTypeAssigner.AssignRandomTypes(graph, rng, weights);
            return graph;
        }

        /// <summary>Demo map cố định — khớp STS_PATHS trong build_fc_diagrams_drawio.py.</summary>
        public static MapGraph GenerateDemoReference(int seed = 42)
        {
            var graph = BuildGraphFromPaths(seed, ReferencePaths());
            ApplyReferenceLocations(graph);
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

        private static MapGraph BuildGraphFromPaths(int seed, IReadOnlyList<int[]> paths)
        {
            var graph = new MapGraph();
            graph.Reset(seed);

            foreach (var (floor, column) in PathValidator.CollectActiveCells(paths).OrderBy(c => c.floor).ThenBy(c => c.column))
            {
                graph.AddNode(floor, column, MapNodeType.Battle);
            }

            WirePathEdges(graph, paths);

            var boss = graph.AddNode(MapLayoutConstants.BossFloor, BossColumn, MapNodeType.Boss, isBoss: true);
            foreach (var preBoss in graph.NodesOnFloor(MapLayoutConstants.FloorCount))
            {
                graph.Connect(preBoss.Id, boss.Id);
            }

            return graph;
        }

        private static List<int[]> GeneratePaths(Random rng, int pathCount)
        {
            var paths = new List<int[]>(pathCount);
            var signatures = new HashSet<string>();
            var startColumns = BuildStartColumns(rng, pathCount);

            for (var i = 0; i < startColumns.Count && paths.Count < pathCount; i++)
            {
                TryAddPath(paths, signatures, GenerateSinglePath(rng, startColumns[i]));
            }

            var attempts = 0;
            while (paths.Count < pathCount && attempts < MaxGenerationAttempts)
            {
                attempts++;
                TryAddPath(paths, signatures, GenerateSinglePath(rng, rng.Next(0, MapLayoutConstants.ColumnCount)));
            }

            attempts = 0;
            while (paths.Count < pathCount && paths.Count > 0 && attempts < MaxGenerationAttempts)
            {
                attempts++;
                TryAddPath(paths, signatures, MutatePath(rng, paths[rng.Next(paths.Count)]));
            }

            return paths;
        }

        private static List<int> BuildStartColumns(Random rng, int pathCount)
        {
            var innerColumns = Enumerable.Range(1, MapLayoutConstants.ColumnCount - 2).ToList();
            Shuffle(rng, innerColumns);

            var uniqueStarts = Math.Min(
                rng.Next(MapLayoutConstants.MinStartNodes, MapLayoutConstants.MaxStartNodes + 1),
                innerColumns.Count);

            var startPool = innerColumns.Take(uniqueStarts).ToList();
            var assigned = new List<int>(pathCount);

            for (var i = 0; i < pathCount; i++)
            {
                assigned.Add(rng.NextDouble() < 0.75 || assigned.Count == 0
                    ? startPool[rng.Next(startPool.Count)]
                    : rng.Next(0, MapLayoutConstants.ColumnCount));
            }

            return assigned;
        }

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

        private static int[] GenerateSinglePath(Random rng, int startCol)
        {
            startCol = ClampColumn(startCol);
            var path = new int[MapLayoutConstants.FloorCount];
            path[0] = startCol;

            for (var floor = 1; floor < MapLayoutConstants.FloorCount; floor++)
            {
                var options = GetNeighborColumns(path[floor - 1]);
                if (options.Count == 0)
                {
                    return null;
                }

                path[floor] = floor >= MapLayoutConstants.FloorCount - 2
                    ? PickColumnTowardCenter(rng, options, BossColumn)
                    : options[rng.Next(options.Count)];
            }

            return path;
        }

        private static int[] MutatePath(Random rng, int[] source)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }

            var path = (int[])source.Clone();
            var mutationCount = rng.Next(1, 4);

            for (var m = 0; m < mutationCount; m++)
            {
                var floor = rng.Next(1, path.Length);
                var options = GetNeighborColumns(path[floor - 1]);
                if (options.Count == 0)
                {
                    continue;
                }

                options.RemoveAll(col => col == path[floor]);
                if (options.Count == 0)
                {
                    continue;
                }

                path[floor] = options[rng.Next(options.Count)];

                for (var f = floor + 1; f < path.Length; f++)
                {
                    var nextOptions = GetNeighborColumns(path[f - 1]);
                    if (nextOptions.Count == 0)
                    {
                        return null;
                    }

                    if (!nextOptions.Contains(path[f]))
                    {
                        path[f] = f >= MapLayoutConstants.FloorCount - 2
                            ? PickColumnTowardCenter(rng, nextOptions, BossColumn)
                            : nextOptions[rng.Next(nextOptions.Count)];
                    }
                }
            }

            return path;
        }

        private static List<int> GetNeighborColumns(int column)
        {
            var options = new List<int>(3);
            for (var delta = -1; delta <= 1; delta++)
            {
                var col = column + delta;
                if (col >= 0 && col < MapLayoutConstants.ColumnCount)
                {
                    options.Add(col);
                }
            }

            return options;
        }

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

        private static int ClampColumn(int column) =>
            Math.Max(0, Math.Min(MapLayoutConstants.ColumnCount - 1, column));

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
