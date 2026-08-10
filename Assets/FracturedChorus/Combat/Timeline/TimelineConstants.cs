namespace FracturedChorus.Combat.Timeline
{
    public static class TimelineConstants
    {
        public const int Phase1SlotCount = 22;
        public const int LaterPhaseSlotCount = 22;

        /// <summary>Eternal Spark Boss Remix @ 152 BPM, first beat 1.161s, 268.29s clip.</summary>
        public const int TotalBeats = 677;

        public static int PhaseCount =>
            1 + (TotalBeats - Phase1SlotCount + LaterPhaseSlotCount - 1) / LaterPhaseSlotCount;

        /// <summary>Fallback when UI has not reported visible slot count yet.</summary>
        public const int DefaultVisibleBeatHint = 20;

        /// <summary>Quái chỉ được phép ra đòn từ beat index này — beat 3 = index 3.</summary>
        public const int EnemyFirstAttackBeat = 3;

        /// <summary>Reaction buffer between a planning horizon and the first impact the boss may land.</summary>
        public const int EnemySpawnBufferBeatsAfterHorizon = 3;

        /// <summary>
        /// Runtime floor for enemy note impacts (intro beats are empty).
        /// </summary>
        public static int EnemyNoteFloorBeat { get; set; }

        /// <summary>Beat nhỏ nhất cho impact của quái trong phase — phase start + buffer.</summary>
        public static int GetMinEnemyImpactBeat(int phaseStartBeat)
        {
            var floor = System.Math.Max(EnemyFirstAttackBeat, EnemyNoteFloorBeat);
            return System.Math.Max(floor, phaseStartBeat + EnemySpawnBufferBeatsAfterHorizon);
        }

        /// <summary>Timeline phases executed per round segment before returning to Planning.</summary>
        public const int RoundPhaseCount = 1;

        /// <summary>How many phases of boss notes to keep pre-spawned ahead of the current phase (1 = current phrase only).</summary>
        public const int TelegraphLookaheadPhases = 1;

        /// <summary>Boss Remix first-beat offset — keep in sync with MusicBeatMapSO.</summary>
        public const float BossRemixFirstBeatOffsetSec = 1.161f;

        public const float BossRemixBpm = 152f;

        /// <summary>Intro length in musical beats (timing). Notes spawn only after intro ends.</summary>
        public const int CombatIntroBeatCount = 12;

        /// <summary>Full-music intro = offset + 12 beats @ 152 BPM (~5.90s).</summary>
        public const float CombatIntroDurationSec =
            BossRemixFirstBeatOffsetSec + CombatIntroBeatCount * (60f / BossRemixBpm);

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

        /// <summary>Beat indices after which a phase divider is drawn (every 22 beats).</summary>
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

        /// <summary>First beat of each phase: 0, 22, 44, …</summary>
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
