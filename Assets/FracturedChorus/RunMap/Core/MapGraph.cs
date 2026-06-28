using System.Collections.Generic;
using System.Linq;

namespace FracturedChorus.RunMap.Core
{
    public sealed class MapGraph
    {
        public int Seed { get; private set; }
        public MapNodeData BossNode { get; private set; }
        public IReadOnlyList<MapNodeData> Nodes => _nodes;

        private readonly List<MapNodeData> _nodes = new List<MapNodeData>();
        private readonly Dictionary<int, MapNodeData> _byId = new Dictionary<int, MapNodeData>();

        public void Reset(int seed)
        {
            Seed = seed;
            _nodes.Clear();
            _byId.Clear();
            BossNode = null;
        }

        public MapNodeData AddNode(int floor, int column, MapNodeType type, bool isBoss = false)
        {
            var node = new MapNodeData
            {
                Id = _nodes.Count,
                Floor = floor,
                Column = column,
                Type = type,
                IsBoss = isBoss
            };
            _nodes.Add(node);
            _byId[node.Id] = node;
            if (isBoss)
            {
                BossNode = node;
            }

            return node;
        }

        public void Connect(int fromId, int toId)
        {
            if (!_byId.TryGetValue(fromId, out var from) || !_byId.TryGetValue(toId, out var to))
            {
                return;
            }

            if (!from.Outgoing.Contains(toId))
            {
                from.Outgoing.Add(toId);
            }

            if (!to.Incoming.Contains(fromId))
            {
                to.Incoming.Add(fromId);
            }
        }

        public MapNodeData GetNode(int id) => _byId.TryGetValue(id, out var node) ? node : null;

        public IEnumerable<MapNodeData> NodesOnFloor(int floor) =>
            _nodes.Where(n => !n.IsBoss && n.Floor == floor);

        public IEnumerable<MapNodeData> StartNodes() => NodesOnFloor(1);

        public MapNodeData FindNode(int floor, int column) =>
            _nodes.FirstOrDefault(n => !n.IsBoss && n.Floor == floor && n.Column == column);
    }
}
