using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.RunMap.Core;

namespace FracturedChorus.RunMap
{
    public static class RunMapRunSave
    {
        public static void Persist(MapGraph graph, RunState state)
        {
            if (!FlushToSession(graph, state))
            {
                return;
            }

            GameMetaSession.Save();
        }

        /// <summary>
        /// Chỉ đổ tiến độ run vào session, không ghi file. Dùng khi SaveToSlot đã sắp ghi đĩa
        /// và đang bắn event Saving — gọi Persist() lúc đó sẽ ghi hai lần.
        /// </summary>
        public static bool FlushToSession(MapGraph graph, RunState state)
        {
            if (graph == null || state == null || !GameMetaSession.HasSession)
            {
                return false;
            }

            var snap = GameMetaSession.Current.RunSnapshot;
            snap.HasActiveRun = true;
            snap.Seed = graph.Seed;
            snap.CurrentNodeId = state.CurrentNodeId;
            snap.CurrentFloor = state.CurrentFloor;
            snap.ActiveSector = (int)graph.Profile.Sector;
            snap.ClearedNodeIds = CollectClearedNodeIds(graph);
            snap.VisitedNodeIds = CollectVisitedNodeIds(state);

            var progress = CadenceRunProgress.Session;
            snap.PulseCleared = progress.PulseCleared;
            snap.EchoCleared = progress.EchoCleared;
            snap.CanticleCleared = progress.CanticleCleared;
            return true;
        }

        public static bool TryRestore(MapGraph graph, RunState state)
        {
            if (graph == null || state == null || !GameMetaSession.HasSession)
            {
                return false;
            }

            var snap = GameMetaSession.Current.RunSnapshot;
            if (!snap.HasActiveRun || snap.Seed != graph.Seed)
            {
                return false;
            }

            var node = graph.GetNode(snap.CurrentNodeId);
            if (node == null)
            {
                return false;
            }

            ApplyClearedNodes(graph, snap.ClearedNodeIds);
            RestoreSectorProgress(snap);

            // ImportVisited trước EnterNode để node hiện tại nằm đúng cuối đường đi.
            state.ImportVisited(graph, snap.VisitedNodeIds);
            state.EnterNode(node);
            return true;
        }

        private static void RestoreSectorProgress(RunSnapshot snap)
        {
            var sector = System.Enum.IsDefined(typeof(PinkySectorId), snap.ActiveSector)
                ? (PinkySectorId)snap.ActiveSector
                : PinkySectorId.Pulse;

            CadenceRunProgress.Session.ImportSectorClears(
                snap.Seed,
                sector,
                snap.PulseCleared,
                snap.EchoCleared,
                snap.CanticleCleared);
        }

        private static int[] CollectVisitedNodeIds(RunState state)
        {
            var visited = state.VisitedPath;
            if (visited == null || visited.Count == 0)
            {
                return System.Array.Empty<int>();
            }

            var ids = new int[visited.Count];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = visited[i];
            }

            return ids;
        }

        private static int[] CollectClearedNodeIds(MapGraph graph)
        {
            var cleared = new List<int>();
            foreach (var node in graph.Nodes)
            {
                if (node != null && node.Cleared)
                {
                    cleared.Add(node.Id);
                }
            }

            return cleared.ToArray();
        }

        private static void ApplyClearedNodes(MapGraph graph, int[] clearedNodeIds)
        {
            if (graph == null || clearedNodeIds == null)
            {
                return;
            }

            foreach (var nodeId in clearedNodeIds)
            {
                var node = graph.GetNode(nodeId);
                if (node != null)
                {
                    node.Cleared = true;
                }
            }
        }
    }
}
