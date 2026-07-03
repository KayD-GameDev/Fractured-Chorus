namespace FracturedChorus.Combat.Timeline
{
    public static class TimelineConstants
    {
        public const int Phase1SlotCount = 16;
        public const int LaterPhaseSlotCount = 16;

        /// <summary>Eternal Spark beat map: 618 CSV markers + t=0 pad (MusicBeatMapSO.ParseCsvTimes).</summary>
        public const int TotalBeats = 619;

        public const int PhaseCount = 39;

        /// <summary>Fallback when UI has not reported visible slot count yet.</summary>
        public const int DefaultVisibleBeatHint = 20;

        /// <summary>Quái chỉ được phép ra đòn (đặt telegraph) từ beat này trở đi — "beat thứ 3" = index 2.</summary>
        public const int EnemyFirstAttackBeat = 2;

        /// <summary>Beat indices after which a phase divider is drawn (between 15|16, 25|26, …).</summary>
        public static bool IsPhaseDividerAfter(int beatIndex)
        {
            if (beatIndex == Phase1SlotCount - 1)
            {
                return true;
            }

            if (beatIndex < Phase1SlotCount)
            {
                return false;
            }

            var indexInLater = beatIndex - Phase1SlotCount;
            return indexInLater % LaterPhaseSlotCount == LaterPhaseSlotCount - 1 && beatIndex < TotalBeats - 1;
        }

        public static int GetPhaseIndex(int beatIndex)
        {
            if (beatIndex < Phase1SlotCount)
            {
                return 0;
            }

            return 1 + (beatIndex - Phase1SlotCount) / LaterPhaseSlotCount;
        }

        /// <summary>Beat range [startBeat, startBeat + count) for a timeline phase index.</summary>
        public static void GetPhaseBeatRange(int phaseIndex, out int startBeat, out int count)
        {
            if (phaseIndex <= 0)
            {
                startBeat = 0;
                count = Phase1SlotCount;
                return;
            }

            startBeat = Phase1SlotCount + (phaseIndex - 1) * LaterPhaseSlotCount;
            count = LaterPhaseSlotCount;
            if (startBeat + count > TotalBeats)
            {
                count = System.Math.Max(0, TotalBeats - startBeat);
            }
        }

        /// <summary>First beat of each phase: 0, 15, 25, 35, …</summary>
        public static bool IsFirstBeatOfPhase(int beatIndex)
        {
            if (beatIndex == 0 || beatIndex == Phase1SlotCount)
            {
                return true;
            }

            if (beatIndex < Phase1SlotCount)
            {
                return false;
            }

            return (beatIndex - Phase1SlotCount) % LaterPhaseSlotCount == 0;
        }
    }
}
