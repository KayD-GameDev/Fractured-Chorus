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
        public int[] ClearedNodeIds = Array.Empty<int>();

        /// <summary>
        /// Đường đi thực tế theo đúng thứ tự bước chân, không phải tập node đã clear.
        /// Cần cả hai vì map vẽ highlight theo thứ tự này.
        /// </summary>
        public int[] VisitedNodeIds = Array.Empty<int>();

        /// <summary>
        /// Tiến độ 3 sector của một cadence. Trước đây nằm trong CadenceRunProgress.Session (RAM),
        /// tắt game là mất, nên chuyển vào snapshot.
        /// </summary>
        public bool PulseCleared;

        public bool EchoCleared;
        public bool CanticleCleared;

        public void Clear()
        {
            Seed = 0;
            CurrentFloor = 0;
            CurrentNodeId = -1;
            ActiveSector = 0;
            HasActiveRun = false;
            ClearedNodeIds = Array.Empty<int>();
            VisitedNodeIds = Array.Empty<int>();
            PulseCleared = false;
            EchoCleared = false;
            CanticleCleared = false;
        }
    }
}
