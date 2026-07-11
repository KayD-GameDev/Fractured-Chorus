using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class RunSnapshot
    {
        public int Seed;
        public int CurrentFloor;
        public int CurrentNodeId = -1;
        public int ActiveSector;
        public bool HasActiveRun;

        public void Clear()
        {
            Seed = 0;
            CurrentFloor = 0;
            CurrentNodeId = -1;
            ActiveSector = 0;
            HasActiveRun = false;
        }
    }
}
