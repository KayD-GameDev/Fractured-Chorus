using System.Collections.Generic;

namespace FracturedChorus.RunMap.Core
{
    public sealed class RunState
    {
        public int Seed { get; set; }
        public int CurrentNodeId { get; set; } = -1;
        public int CurrentFloor { get; set; }
        public readonly List<int> VisitedPath = new List<int>();

        public void BeginRun(int seed)
        {
            Seed = seed;
            CurrentNodeId = -1;
            CurrentFloor = 0;
            VisitedPath.Clear();
        }

        public void EnterNode(MapNodeData node)
        {
            if (node == null)
            {
                return;
            }

            CurrentNodeId = node.Id;
            CurrentFloor = node.IsBoss ? MapLayoutConstants.BossFloor : node.Floor;
            node.Visited = true;
            if (!VisitedPath.Contains(node.Id))
            {
                VisitedPath.Add(node.Id);
            }
        }

        public bool CanTravelTo(MapGraph graph, MapNodeData target)
        {
            if (target == null || target.Cleared)
            {
                return false;
            }

            if (CurrentNodeId < 0)
            {
                return target.IsStart;
            }

            var current = graph.GetNode(CurrentNodeId);
            if (current == null)
            {
                return target.IsStart;
            }

            return current.Outgoing.Contains(target.Id);
        }
    }
}
