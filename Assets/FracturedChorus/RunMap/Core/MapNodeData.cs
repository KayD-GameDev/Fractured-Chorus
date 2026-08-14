using System;
using System.Collections.Generic;

namespace FracturedChorus.RunMap.Core
{
    [Serializable]
    public sealed class MapNodeData
    {
        public int Id;
        public int Floor;
        public int Column;
        public MapNodeType Type;
        public bool IsBoss;
        public bool Cleared;
        public bool Visited;

        public readonly List<int> Outgoing = new List<int>();
        public readonly List<int> Incoming = new List<int>();

        public bool IsStart => Type == MapNodeType.Start;
        public bool IsFloorEntry => Floor == 1 && Type != MapNodeType.Start;
        public bool IsSavePoint => MapNodeCatalog.IsSavePoint(Type, IsBoss, Cleared);
        public int PreBossFloor { get; set; } = MapLayoutConstants.FloorCount;

        public bool IsPreBoss => Floor == PreBossFloor;
    }
}
