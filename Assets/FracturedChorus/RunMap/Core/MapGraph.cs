using System.Collections.Generic;

namespace FracturedChorus.RunMap.Core
{
    public sealed class MapGraph
    {
        public int Seed { get; private set; }
        public MapGenerationProfile Profile { get; private set; } = MapGenerationProfile.Default;
        public MapNodeData BossNode { get; private set; }
        public MapNodeData StartNode { get; private set; }
        public IReadOnlyList<MapNodeData> Nodes => _nodes;

        private readonly List<MapNodeData> _nodes = new List<MapNodeData>();
        private readonly Dictionary<int, MapNodeData> _byId = new Dictionary<int, MapNodeData>();
        private readonly Dictionary<(int floor, int column), MapNodeData> _byCell = new Dictionary<(int, int), MapNodeData>();

        public void Reset(int seed, MapGenerationProfile profile = null)
        {
            Seed = seed;
            Profile = profile ?? MapGenerationProfile.Default;
            _nodes.Clear();
            _byId.Clear();
            _byCell.Clear();
            BossNode = null;
            StartNode = null;
        }

        public MapNodeData AddNode(int floor, int column, MapNodeType type, bool isBoss = false)
        {
            var node = new MapNodeData
            {
                Id = _nodes.Count,
                Floor = floor,
                Column = column,
                Type = type,
                IsBoss = isBoss,
                PreBossFloor = Profile.FloorCount
            };
            _nodes.Add(node);
            _byId[node.Id] = node;

            if (!isBoss && type != MapNodeType.Start)
            {
                _byCell[(floor, column)] = node;
            }

            if (isBoss)
            {
                BossNode = node;
            }

            if (type == MapNodeType.Start)
            {
                StartNode = node;
            }

            return node;
        }

        public void Connect(int fromId, int toId)
        {
            if (!_byId.TryGetValue(fromId, out var from) || !_byId.TryGetValue(toId, out _))
            {
                return;
            }

            if (!from.Outgoing.Contains(toId))
            {
                from.Outgoing.Add(toId);
            }

            if (!_byId[toId].Incoming.Contains(fromId))
            {
                _byId[toId].Incoming.Add(fromId);
            }
        }

        public MapNodeData GetNode(int id) => _byId.TryGetValue(id, out var node) ? node : null;

        public MapNodeData FindNode(int floor, int column) =>
            _byCell.TryGetValue((floor, column), out var node) ? node : null;

        public IEnumerable<MapNodeData> NodesOnFloor(int floor)
        {
            foreach (var node in _nodes)
            {
                if (!node.IsBoss && node.Floor == floor)
                {
                    yield return node;
                }
            }
        }

        public IEnumerable<MapNodeData> StartNodes() => NodesOnFloor(1);

        public void PruneNodes(ISet<int> keepIds)
        {
            if (keepIds == null || keepIds.Count == 0)
            {
                return;
            }

            for (var i = _nodes.Count - 1; i >= 0; i--)
            {
                var node = _nodes[i];
                if (keepIds.Contains(node.Id))
                {
                    continue;
                }

                _nodes.RemoveAt(i);
                _byId.Remove(node.Id);
                if (!node.IsBoss)
                {
                    _byCell.Remove((node.Floor, node.Column));
                }
            }

            foreach (var node in _nodes)
            {
                node.Outgoing.RemoveAll(id => !keepIds.Contains(id));
                node.Incoming.RemoveAll(id => !keepIds.Contains(id));
            }

            if (BossNode != null && !keepIds.Contains(BossNode.Id))
            {
                BossNode = null;
            }

            if (StartNode != null && !keepIds.Contains(StartNode.Id))
            {
                StartNode = null;
            }
        }
    }
}
