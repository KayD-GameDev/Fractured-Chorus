using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Timeline
{
    public enum FootprintBeatRole
    {
        StandingBefore,
        Active,
        StandingAfter
    }

    public readonly struct FootprintBeatInfo
    {
        public int BeatIndex { get; }
        public FootprintBeatRole Role { get; }

        public FootprintBeatInfo(int beatIndex, FootprintBeatRole role)
        {
            BeatIndex = beatIndex;
            Role = role;
        }
    }

    /// <summary>
    /// S1 · S · S2 footprint helpers — placement beat = start of Using (S) phase.
    /// </summary>
    public static class SkillFootprintUtil
    {
        public static int GetStandingBefore(SkillDefinitionSO skill) =>
            skill != null ? Mathf.Max(0, skill.standingBeatsBefore) : 0;

        public static int GetActiveBeats(SkillDefinitionSO skill) =>
            skill != null ? Mathf.Max(1, skill.activeBeats) : 1;

        public static bool UsesGapCenterAnchor(SkillDefinitionSO skill) =>
            skill != null && GetActiveBeats(skill) % 2 == 0;

        public static float GetActiveCenterBeatOffset(SkillDefinitionSO skill) =>
            (GetActiveBeats(skill) - 1) * 0.5f;

        public static float GetActiveVisualCenterBeat(SkillDefinitionSO skill, int placementBeat) =>
            placementBeat + GetActiveCenterBeatOffset(skill);

        public static int ResolvePlacementBeatFromCenter(SkillDefinitionSO skill, float centerBeat) =>
            Mathf.RoundToInt(centerBeat - GetActiveCenterBeatOffset(skill));

        public static int GetStandingAfter(SkillDefinitionSO skill) =>
            skill != null ? Mathf.Max(0, skill.standingBeatsAfter) : 0;

        /// <summary>Earliest placement beat (start of S phase) — needs room for S1 standing beats before.</summary>
        public static int GetMinimumPlacementBeat(SkillDefinitionSO skill, int planningHorizonBeat = 0)
        {
            var s1 = GetStandingBefore(skill);
            return Mathf.Max(s1, planningHorizonBeat + s1);
        }

        public static bool FootprintHasS1BeforeHorizon(SkillDefinitionSO skill, int placementBeat, int planningHorizonBeat)
        {
            if (skill == null || planningHorizonBeat <= 0)
            {
                return false;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat))
            {
                if (info.Role == FootprintBeatRole.StandingBefore && info.BeatIndex < planningHorizonBeat)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every beat index occupied by this skill when placed at placementBeat.</summary>
        public static void CollectOccupiedBeats(SkillDefinitionSO skill, int placementBeat, List<int> results)
        {
            results.Clear();
            if (skill == null)
            {
                return;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat))
            {
                if (info.BeatIndex >= 0 && info.BeatIndex < TimelineConstants.TotalBeats)
                {
                    results.Add(info.BeatIndex);
                }
            }
        }

        public static IEnumerable<FootprintBeatInfo> EnumerateFootprintBeats(SkillDefinitionSO skill, int placementBeat)
        {
            if (skill == null)
            {
                yield break;
            }

            var s1 = GetStandingBefore(skill);
            var active = GetActiveBeats(skill);
            var s2 = GetStandingAfter(skill);

            for (var i = s1; i >= 1; i--)
            {
                yield return new FootprintBeatInfo(placementBeat - i, FootprintBeatRole.StandingBefore);
            }

            for (var i = 0; i < active; i++)
            {
                yield return new FootprintBeatInfo(placementBeat + i, FootprintBeatRole.Active);
            }

            for (var i = 0; i < s2; i++)
            {
                yield return new FootprintBeatInfo(placementBeat + active + i, FootprintBeatRole.StandingAfter);
            }
        }

        /// <summary>All beats already occupied by agenda entries for the same unit.</summary>
        public static void CollectUnitOccupiedBeats(IReadOnlyList<AgendaEntry> agenda, CombatUnit unit, List<int> results)
        {
            results.Clear();
            if (agenda == null || unit == null)
            {
                return;
            }

            foreach (var entry in agenda)
            {
                if (entry?.Unit != unit || entry.Skill == null)
                {
                    continue;
                }

                CollectOccupiedBeats(entry.Skill, entry.BeatIndex, _scratchBeats);
                results.AddRange(_scratchBeats);
            }
        }

        private static readonly List<int> _scratchBeats = new();
        private static readonly List<int> _scratchNew = new();

        public static bool CanPlace(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int planningHorizonBeat = 0)
        {
            if (unit == null || skill == null || placementBeat < 0 || placementBeat >= TimelineConstants.TotalBeats)
            {
                return false;
            }

            if (placementBeat < GetMinimumPlacementBeat(skill, planningHorizonBeat))
            {
                return false;
            }

            if (FootprintHasS1BeforeHorizon(skill, placementBeat, planningHorizonBeat))
            {
                return false;
            }

            CollectOccupiedBeats(skill, placementBeat, _scratchNew);
            if (_scratchNew.Count == 0)
            {
                return false;
            }

            CollectUnitOccupiedBeats(agenda, unit, _scratchBeats);
            foreach (var beat in _scratchNew)
            {
                if (_scratchBeats.Contains(beat))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Impact beat for enemy telegraph (start of Using / S phase).</summary>
        public static int GetImpactBeat(SkillDefinitionSO skill, int telegraphBeatIndex, bool isWindup)
        {
            if (!isWindup)
            {
                return telegraphBeatIndex;
            }

            return telegraphBeatIndex + GetStandingBefore(skill);
        }

        public static bool IsImpactTelegraph(EnemyTelegraph telegraph)
        {
            return telegraph != null && !telegraph.IsWindupOnly;
        }
    }
}
