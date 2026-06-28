using System.Collections.Generic;
using System.Linq;

namespace FracturedChorus.RunMap.Core
{
    public static class PathValidator
    {
        public static bool HasPathToBoss(MapGraph graph)
        {
            if (graph.BossNode == null)
            {
                return false;
            }

            foreach (var start in graph.StartNodes())
            {
                if (CanReach(graph, start.Id, graph.BossNode.Id))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanReach(MapGraph graph, int fromId, int toId)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(fromId);
            visited.Add(fromId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (id == toId)
                {
                    return true;
                }

                var node = graph.GetNode(id);
                if (node == null)
                {
                    continue;
                }

                foreach (var next in node.Outgoing)
                {
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }

        public static HashSet<(int floor, int column)> CollectActiveCells(IReadOnlyList<int[]> paths)
        {
            var active = new HashSet<(int, int)>();
            foreach (var path in paths)
            {
                for (var floor = 0; floor < path.Length; floor++)
                {
                    active.Add((floor + 1, path[floor]));
                }
            }

            return active;
        }

        public static List<(int fromId, int toId)> MergeEdges(MapGraph graph)
        {
            var edges = new HashSet<(int, int)>();
            foreach (var node in graph.Nodes)
            {
                foreach (var to in node.Outgoing)
                {
                    edges.Add((node.Id, to));
                }
            }

            return edges.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ToList();
        }
    }
}
