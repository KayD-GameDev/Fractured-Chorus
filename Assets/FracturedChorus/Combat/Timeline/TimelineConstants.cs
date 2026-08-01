namespace FracturedChorus.Combat.Timeline
{
    public static class TimelineConstants
    {
        public const int Phase1SlotCount = 16;
        public const int LaterPhaseSlotCount = 16;

        /// <summary>Eternal Spark Boss Remix @ 152 BPM, first beat 1.161s, 268.29s clip.</summary>
        public const int TotalBeats = 677;

        public static int PhaseCount =>
            1 + (TotalBeats - Phase1SlotCount + LaterPhaseSlotCount - 1) / LaterPhaseSlotCount;

        /// <summary>Fallback when UI has not reported visible slot count yet.</summary>
        public const int DefaultVisibleBeatHint = 20;

        /// <summary>Quái chỉ được phép ra đòn (đặt telegraph) từ beat này trở đi — "beat thứ 3" = index 2.</summary>
        public const int EnemyFirstAttackBeat = 2;

        /// <summary>Beat index cuối intro trước planning-pause (beat thứ 6 = index 6).</summary>
        public const int IntroPlanningPauseAfterBeatIndex = 6;

        /// <summary>Beat execute đầu tiên sau intro-pause.</summary>
        public const int IntroExecuteStartBeatIndex = IntroPlanningPauseAfterBeatIndex + 1;

        /// <summary>Vùng bắt đầu boss có thể spawn impact: execute start + buffer (segment 0 intro).</summary>
        public const int EnemySpawnBufferBeatsAfterHorizon = 3;

        public const int IntroEnemySpawnZoneStartBeat = IntroExecuteStartBeatIndex + EnemySpawnBufferBeatsAfterHorizon;

        /// <summary>Beat nhỏ nhất cho impact của quái trong phase — phase start + buffer (segment 0 intro dùng IntroEnemySpawnZoneStartBeat).</summary>
        public static int GetMinEnemyImpactBeat(int phaseStartBeat)
        {
            if (phaseStartBeat <= 0)
            {
                return IntroEnemySpawnZoneStartBeat;
            }

            return phaseStartBeat + EnemySpawnBufferBeatsAfterHorizon;
        }

        /// <summary>Timeline phases executed per round segment before returning to Execute.</summary>
        public const int RoundPhaseCount = 2;

        public static int GetRoundEndBeatExclusive()
        {
            GetPhaseBeatRange(RoundPhaseCount - 1, out var startBeat, out var count);
            return startBeat + count;
        }

        public static int GetSegmentBeatCount() => GetRoundEndBeatExclusive();

        public static int GetSegmentStartBeat(int segmentIndex) => segmentIndex * GetSegmentBeatCount();

        public static int GetSegmentEndBeatExclusive(int segmentIndex)
        {
            return GetSegmentStartBeat(segmentIndex) + GetSegmentBeatCountForSegment(segmentIndex);
        }

        /// <summary>Beat count for a segment — clamped at song end.</summary>
        public static int GetSegmentBeatCountForSegment(int segmentIndex)
        {
            var start = GetSegmentStartBeat(segmentIndex);
            return System.Math.Min(GetSegmentBeatCount(), System.Math.Max(0, TotalBeats - start));
        }

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
