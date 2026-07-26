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
        private int _guardCharges;

        public IReadOnlyList<BlockBarrier> Barriers => _barriers;
        public int GuardCharges => _guardCharges;

        public event Action OnBarriersChanged;

        public bool HasBarrierAtBeat(int beatIndex) =>
            _barriers.Any(b => b.BeatIndex == beatIndex);

        public bool TryFindNearbyImpactBeat(int beatIndex, BeatTimelineEngine timeline, out int impactBeat)
        {
            impactBeat = -1;
            if (timeline == null || beatIndex < 0)
            {
                return false;
            }

            for (var delta = 0; delta <= 1; delta++)
            {
                var candidates = delta == 0
                    ? new[] { beatIndex }
                    : new[] { beatIndex - delta, beatIndex + delta };
                foreach (var candidate in candidates)
                {
                    if (candidate < 0)
                    {
                        continue;
                    }

                    var telegraphs = timeline.GetImpactTelegraphsAtBeat(candidate);
                    if (telegraphs == null || telegraphs.Count == 0)
                    {
                        continue;
                    }

                    if (CombatCounterResolver.HasCounterOnBeat(timeline, candidate))
                    {
                        continue;
                    }

                    impactBeat = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool CanPlaceBarrier(int beatIndex, BeatTimelineEngine timeline)
        {
            if (HasBarrierAtBeat(beatIndex) || timeline == null || beatIndex < 0)
            {
                return false;
            }

            if (CombatCounterResolver.HasCounterOnBeat(timeline, beatIndex))
            {
                return false;
            }

            return TryFindNearbyImpactBeat(beatIndex, timeline, out _);
        }

        public bool TryPlaceBarrier(int beatIndex, BeatTimelineEngine timeline = null)
        {
            if (timeline != null && !CanPlaceBarrier(beatIndex, timeline))
            {
                return false;
            }

            if (HasBarrierAtBeat(beatIndex))
            {
                return false;
            }

            _barriers.Add(new BlockBarrier(beatIndex));
            OnBarriersChanged?.Invoke();
            return true;
        }

        public void AddGuardCharge(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            _guardCharges += amount;
            OnBarriersChanged?.Invoke();
        }

        public BlockTiming ConsumeGuardChargeRemap(BlockTiming timing)
        {
            if (_guardCharges <= 0)
            {
                return timing;
            }

            if (timing is not (BlockTiming.Early or BlockTiming.Late))
            {
                return timing;
            }

            _guardCharges--;
            OnBarriersChanged?.Invoke();
            return BlockTiming.OnBeat;
        }

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
            _guardCharges = 0;
            OnBarriersChanged?.Invoke();
        }
    }
}
