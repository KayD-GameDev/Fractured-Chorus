using System.Collections.Generic;

namespace FracturedChorus.RunMap.Core
{
    public sealed class RunState
    {
        public int Seed { get; private set; }
        public int CurrentNodeId { get; private set; } = -1;
        public int CurrentFloor { get; private set; }
        public IReadOnlyList<int> VisitedPath => _visitedOrder;

        private readonly List<int> _visitedOrder = new List<int>();
        private readonly HashSet<int> _visitedIds = new HashSet<int>();

        public void BeginRun(int seed)
        {
            Seed = seed;
            CurrentNodeId = -1;
            CurrentFloor = 0;
            _visitedOrder.Clear();
            _visitedIds.Clear();
        }

        public bool IsVisited(int nodeId) => _visitedIds.Contains(nodeId);

        public void EnterNode(MapNodeData node)
        {
            if (node == null)
            {
                return;
            }

            CurrentNodeId = node.Id;
            CurrentFloor = node.IsBoss ? MapLayoutConstants.BossFloor : node.Floor;
            node.Visited = true;

            if (_visitedIds.Add(node.Id))
            {
                _visitedOrder.Add(node.Id);
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
            return current != null && current.Outgoing.Contains(target.Id);
        }

        /// <summary>Cho phép chọn lại node boss hiện tại để mở cổng trận.</summary>
        public bool CanSelectNode(MapGraph graph, MapNodeData target)
        {
            if (target == null || target.Cleared)
            {
                return false;
            }

            if (CurrentNodeId == target.Id && (target.IsBoss || target.Type == MapNodeType.Boss))
            {
                return true;
            }

            return CanTravelTo(graph, target);
        }
    }
}
