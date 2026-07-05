using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Timeline;

namespace FracturedChorus.Combat.Block
{
    public class BlockBarrierTracker
    {
        public const int MaxEffectiveBlocksPerPhase = 7;

        private readonly List<BlockBarrier> _barriers = new();
        private readonly Dictionary<int, int> _effectiveBlocksUsedPerPhase = new();

        public IReadOnlyList<BlockBarrier> Barriers => _barriers;

        public event Action OnBarriersChanged;

        public bool HasBarrierAtBeat(int beatIndex) =>
            _barriers.Any(b => b.BeatIndex == beatIndex);

        public bool TryPlaceBarrier(int beatIndex)
        {
            if (HasBarrierAtBeat(beatIndex))
            {
                return false;
            }

            _barriers.Add(new BlockBarrier(beatIndex));
            OnBarriersChanged?.Invoke();
            return true;
        }

        /// <summary>Best timing-based reduction for enemy beat E, or null if block invalid.</summary>
        public BlockTiming? TryGetBlockTiming(int enemyBeat, BeatTimelineEngine timeline)
        {
            if (timeline == null || CombatCounterResolver.HasCounterOnBeat(timeline, enemyBeat))
            {
                return null;
            }

            if (!CombatCounterResolver.HasStandingOverlapOnBeat(timeline, enemyBeat))
            {
                return null;
            }

            var phase = TimelineConstants.GetPhaseIndex(enemyBeat);
            _effectiveBlocksUsedPerPhase.TryGetValue(phase, out var used);
            if (used >= MaxEffectiveBlocksPerPhase)
            {
                return null;
            }

            BlockTiming? bestTiming = null;
            var bestScore = 0;
            foreach (var barrier in _barriers)
            {
                var timing = BlockTimingExtensions.Resolve(barrier.BeatIndex, enemyBeat);
                var score = timing switch
                {
                    BlockTiming.OnBeat => 3,
                    BlockTiming.Early => 2,
                    BlockTiming.Late => 2,
                    _ => 0
                };

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTiming = timing;
            }

            if (bestTiming == null || bestScore == 0)
            {
                return null;
            }

            _effectiveBlocksUsedPerPhase[phase] = used + 1;
            return bestTiming;
        }

        public void Clear()
        {
            _barriers.Clear();
            _effectiveBlocksUsedPerPhase.Clear();
            OnBarriersChanged?.Invoke();
        }
    }
}
