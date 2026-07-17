using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Core
{
    public static class CombatCounterResolver
    {
        /// <summary>Player active beat overlaps enemy impact telegraph at beat E.</summary>
        public static bool IsCounterEntry(AgendaEntry entry, EnemyTelegraph telegraph)
        {
            if (entry?.Unit == null || entry.Skill == null || telegraph == null || entry.Skill.IsGuard)
            {
                return false;
            }

            if (entry.Unit.Side != GridSide.Player)
            {
                return false;
            }

            return GetActiveBeatIndices(entry).Contains(telegraph.BeatIndex);
        }

        public static int CountCountersAtBeat(BeatTimelineEngine timeline, int beatIndex)
        {
            if (timeline == null)
            {
                return 0;
            }

            var telegraphs = timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs.Count == 0)
            {
                return 0;
            }

            var count = 0;
            foreach (var entry in timeline.Agenda)
            {
                if (entry.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                count += GetCounterHitContribution(entry, beatIndex, timeline);
            }

            return count;
        }

        public static int GetCounterHitContribution(
            AgendaEntry entry,
            int beatIndex,
            BeatTimelineEngine timeline)
        {
            if (entry?.Skill == null || !GetActiveBeatIndices(entry).Contains(beatIndex))
            {
                return 0;
            }

            var hits = 1;
            if (!entry.IsEmpowered || entry.Skill.empowerExtraHits <= 0 || timeline == null)
            {
                return hits;
            }

            var firstNoteBeat = FindFirstActiveImpactBeat(entry, timeline);
            if (firstNoteBeat == beatIndex)
            {
                hits += entry.Skill.empowerExtraHits;
            }

            return hits;
        }

        public static bool ActiveWindowHasImpactNote(AgendaEntry entry, BeatTimelineEngine timeline)
        {
            return FindFirstActiveImpactBeat(entry, timeline) >= 0;
        }

        public static int FindFirstActiveImpactBeat(AgendaEntry entry, BeatTimelineEngine timeline)
        {
            if (entry?.Skill == null || timeline == null)
            {
                return -1;
            }

            foreach (var activeBeat in GetActiveBeatIndices(entry))
            {
                if (timeline.GetImpactTelegraphsAtBeat(activeBeat).Count > 0)
                {
                    return activeBeat;
                }
            }

            return -1;
        }

        public static bool HasCounterOnBeat(BeatTimelineEngine timeline, int beatIndex) =>
            CountCountersAtBeat(timeline, beatIndex) > 0;

        public static void CollectCounteringPlayerUnits(
            BeatTimelineEngine timeline,
            int beatIndex,
            List<CombatUnit> results)
        {
            results.Clear();
            if (timeline == null || beatIndex < 0)
            {
                return;
            }

            var telegraphs = timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs.Count == 0)
            {
                return;
            }

            foreach (var entry in timeline.Agenda)
            {
                if (entry?.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                if (!GetActiveBeatIndices(entry).Contains(beatIndex))
                {
                    continue;
                }

                var countersTelegraph = false;
                foreach (var telegraph in telegraphs)
                {
                    if (IsCounterEntry(entry, telegraph))
                    {
                        countersTelegraph = true;
                        break;
                    }
                }

                if (countersTelegraph && !results.Contains(entry.Unit))
                {
                    results.Add(entry.Unit);
                }
            }
        }

        public static void CollectCounteredEnemyUnits(
            BeatTimelineEngine timeline,
            int beatIndex,
            List<CombatUnit> results)
        {
            results.Clear();
            if (timeline == null || beatIndex < 0 || !HasCounterOnBeat(timeline, beatIndex))
            {
                return;
            }

            foreach (var telegraph in timeline.GetImpactTelegraphsAtBeat(beatIndex))
            {
                if (telegraph?.Unit == null || telegraph.Unit.Side != GridSide.Enemy || !telegraph.Unit.IsAlive)
                {
                    continue;
                }

                if (!results.Contains(telegraph.Unit))
                {
                    results.Add(telegraph.Unit);
                }
            }
        }

        public static bool IsTelegraphFullyCountered(EnemyTelegraph telegraph, BeatTimelineEngine timeline)
        {
            if (telegraph == null || timeline == null)
            {
                return false;
            }

            return GetRemainingHits(telegraph, timeline) <= 0;
        }

        /// <summary>Hits still needed to cancel — spawn HitsRequired minus current Active counters on that beat.</summary>
        public static int GetRemainingHits(EnemyTelegraph telegraph, BeatTimelineEngine timeline)
        {
            if (telegraph == null)
            {
                return 0;
            }

            var required = telegraph.HitsRequired > 0 ? telegraph.HitsRequired : 1;
            if (timeline == null)
            {
                return required;
            }

            return Mathf.Max(0, required - CountCountersAtBeat(timeline, telegraph.BeatIndex));
        }

        /// <summary>Preview remaining hits if <paramref name="pendingSkill"/> Active window covers the telegraph beat.</summary>
        public static int GetRemainingHitsAfterPending(
            EnemyTelegraph telegraph,
            BeatTimelineEngine timeline,
            SkillDefinitionSO pendingSkill,
            int pendingPlacementBeat,
            CombatUnit pendingUnit)
        {
            var remaining = GetRemainingHits(telegraph, timeline);
            if (telegraph == null || pendingSkill == null || remaining <= 0)
            {
                return remaining;
            }

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(pendingSkill, pendingPlacementBeat, pendingUnit))
            {
                if (info.Role == FootprintBeatRole.Active && info.BeatIndex == telegraph.BeatIndex)
                {
                    return Mathf.Max(0, remaining - 1);
                }
            }

            return remaining;
        }

        /// <summary>
        /// Visual tier from remaining hits: 3→Purple, 2→Blue, 1→Red, 0→fully covered (no tier).
        /// </summary>
        public static bool TryGetDisplayTier(int remainingHits, out BossNoteTier tier)
        {
            if (remainingHits <= 0)
            {
                tier = BossNoteTier.Red;
                return false;
            }

            tier = remainingHits switch
            {
                1 => BossNoteTier.Red,
                2 => BossNoteTier.Blue,
                _ => BossNoteTier.Purple
            };
            return true;
        }

        public static CombatUnit ResolvePlayerCounterTarget(AgendaEntry entry, BeatTimelineEngine timeline)
        {
            if (entry?.Skill == null || timeline == null)
            {
                return null;
            }

            if (entry.Skill.targetType != SkillTargetType.SingleEnemy)
            {
                return null;
            }

            foreach (var telegraph in timeline.Telegraphs)
            {
                if (telegraph == null || telegraph.IsWindupOnly || telegraph.Unit == null || !telegraph.Unit.IsAlive)
                {
                    continue;
                }

                if (IsCounterEntry(entry, telegraph))
                {
                    return telegraph.Unit;
                }
            }

            return null;
        }

        public static bool HasStandingOverlapOnBeat(BeatTimelineEngine timeline, int beatIndex)
        {
            if (timeline == null)
            {
                return false;
            }

            foreach (var entry in timeline.Agenda)
            {
                if (entry.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null)
                {
                    continue;
                }

                foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(entry.Skill, entry.BeatIndex))
                {
                    if (info.BeatIndex != beatIndex)
                    {
                        continue;
                    }

                    if (info.Role == FootprintBeatRole.StandingBefore || info.Role == FootprintBeatRole.StandingAfter)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static IEnumerable<int> GetActiveBeatIndices(AgendaEntry entry)
        {
            if (entry?.Skill == null)
            {
                yield break;
            }

            var active = SkillFootprintUtil.GetActiveBeats(entry.Skill);
            for (var i = 0; i < active; i++)
            {
                yield return entry.BeatIndex + i;
            }
        }
    }
}
