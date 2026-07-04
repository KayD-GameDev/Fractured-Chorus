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

        public static void PruneToBossReachable(MapGraph graph)
        {
            if (graph?.BossNode == null)
            {
                return;
            }

            var bossId = graph.BossNode.Id;
            var forward = CollectForwardReachable(graph);
            var backward = CollectBackwardReachable(graph, bossId);
            var keep = new HashSet<int>(forward);
            keep.IntersectWith(backward);
            keep.Add(bossId);
            graph.PruneNodes(keep);
        }

        public static void RemoveDanglingNodes(MapGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            const int maxPasses = 32;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var remove = new HashSet<int>();
                foreach (var node in graph.Nodes)
                {
                    if (node.IsBoss || node.Floor <= 1)
                    {
                        continue;
                    }

                    if (node.Incoming.Count == 0)
                    {
                        remove.Add(node.Id);
                    }
                }

                if (remove.Count == 0)
                {
                    return;
                }

                var keep = new HashSet<int>();
                foreach (var node in graph.Nodes)
                {
                    if (!remove.Contains(node.Id))
                    {
                        keep.Add(node.Id);
                    }
                }

                graph.PruneNodes(keep);
            }
        }

        private static HashSet<int> CollectForwardReachable(MapGraph graph)
        {
            var reachable = new HashSet<int>();
            var queue = new Queue<int>();

            foreach (var start in graph.StartNodes())
            {
                if (reachable.Add(start.Id))
                {
                    queue.Enqueue(start.Id);
                }
            }

            while (queue.Count > 0)
            {
                var node = graph.GetNode(queue.Dequeue());
                if (node == null)
                {
                    continue;
                }

                foreach (var nextId in node.Outgoing)
                {
                    if (reachable.Add(nextId))
                    {
                        queue.Enqueue(nextId);
                    }
                }
            }

            return reachable;
        }

        private static HashSet<int> CollectBackwardReachable(MapGraph graph, int bossId)
        {
            var reachable = new HashSet<int> { bossId };
            var queue = new Queue<int>();
            queue.Enqueue(bossId);

            while (queue.Count > 0)
            {
                var node = graph.GetNode(queue.Dequeue());
                if (node == null)
                {
                    continue;
                }

                foreach (var prevId in node.Incoming)
                {
                    if (reachable.Add(prevId))
                    {
                        queue.Enqueue(prevId);
                    }
                }
            }

            return reachable;
        }
    }
}
