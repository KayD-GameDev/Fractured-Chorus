using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.RunMap.Core;

namespace FracturedChorus.RunMap
{
    public static class RunMapRunSave
    {
        public static void Persist(MapGraph graph, RunState state)
        {
            if (graph == null || state == null || !GameMetaSession.HasSession)
            {
                return;
            }

            var snap = GameMetaSession.Current.RunSnapshot;
            snap.HasActiveRun = true;
            snap.Seed = graph.Seed;
            snap.CurrentNodeId = state.CurrentNodeId;
            snap.CurrentFloor = state.CurrentFloor;
            snap.ActiveSector = (int)graph.Profile.Sector;
            snap.ClearedNodeIds = CollectClearedNodeIds(graph);
            GameMetaSession.Save();
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

            ApplyClearedNodes(graph, snap.ClearedNodeIds);

            var node = graph.GetNode(snap.CurrentNodeId);
            if (node == null)
            {
                return false;
            }

            state.EnterNode(node);
            return true;
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
