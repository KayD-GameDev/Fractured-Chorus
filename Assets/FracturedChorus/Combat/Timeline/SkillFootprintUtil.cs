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

    public static class SkillFootprintUtil
    {
        private static readonly List<int> ScratchNew = new();
        private static readonly List<int> ScratchOccupied = new();
        private static readonly List<int> ScratchEntry = new();

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
            Mathf.FloorToInt(centerBeat - GetActiveCenterBeatOffset(skill) + 0.5f);

        public static int GetStandingAfter(SkillDefinitionSO skill) =>
            GetStandingAfter(skill, null, null);

        public static int GetStandingAfter(SkillDefinitionSO skill, CombatUnit unit) =>
            GetStandingAfter(skill, unit, null);

        public static int GetStandingAfter(SkillDefinitionSO skill, CombatUnit unit, AgendaEntry entry)
        {
            if (entry != null && entry.StandingAfterOverride >= 0)
            {
                return entry.StandingAfterOverride;
            }

            var s2 = skill != null ? Mathf.Max(0, skill.standingBeatsAfter) : 0;
            if (unit != null && unit.PendingReduceS2 > 0)
            {
                s2 = Mathf.Max(0, s2 - unit.PendingReduceS2);
            }

            return s2;
        }

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

        public static bool FootprintInBounds(SkillDefinitionSO skill, int placementBeat, CombatUnit unit = null)
        {
            if (skill == null)
            {
                return false;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat, unit))
            {
                if (info.BeatIndex < 0 || info.BeatIndex >= CombatTimelineProfile.TotalBeats)
                {
                    return false;
                }
            }

            return true;
        }

        public static void CollectOccupiedBeats(SkillDefinitionSO skill, int placementBeat, List<int> results) =>
            CollectOccupiedBeats(skill, placementBeat, results, null);

        public static void CollectOccupiedBeats(
            SkillDefinitionSO skill,
            int placementBeat,
            List<int> results,
            CombatUnit unit)
        {
            results.Clear();
            if (skill == null)
            {
                return;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat, unit))
            {
                if (info.BeatIndex >= 0 && info.BeatIndex < CombatTimelineProfile.TotalBeats)
                {
                    results.Add(info.BeatIndex);
                }
            }
        }

        public static IEnumerable<FootprintBeatInfo> EnumerateFootprintBeats(SkillDefinitionSO skill, int placementBeat) =>
            EnumerateFootprintBeats(skill, placementBeat, null, null);

        public static IEnumerable<FootprintBeatInfo> EnumerateFootprintBeats(
            SkillDefinitionSO skill,
            int placementBeat,
            CombatUnit unit) =>
            EnumerateFootprintBeats(skill, placementBeat, unit, null);

        public static IEnumerable<FootprintBeatInfo> EnumerateFootprintBeats(
            SkillDefinitionSO skill,
            int placementBeat,
            CombatUnit unit,
            AgendaEntry entry)
        {
            if (skill == null)
            {
                yield break;
            }

            var s1 = GetStandingBefore(skill);
            var active = GetActiveBeats(skill);
            var s2 = GetStandingAfter(skill, unit, entry);

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

                ScratchEntry.Clear();
                foreach (var info in EnumerateFootprintBeats(entry.Skill, entry.BeatIndex, entry.Unit, entry))
                {
                    if (info.BeatIndex >= 0 && info.BeatIndex < CombatTimelineProfile.TotalBeats)
                    {
                        ScratchEntry.Add(info.BeatIndex);
                    }
                }

                results.AddRange(ScratchEntry);
            }
        }

        public static bool CanPlace(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int planningHorizonBeat = 0)
        {
            if (unit == null || skill == null || placementBeat < 0 || placementBeat >= CombatTimelineProfile.TotalBeats)
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

            if (!FootprintInBounds(skill, placementBeat, unit))
            {
                return false;
            }

            CollectOccupiedBeats(skill, placementBeat, ScratchNew, unit);
            if (ScratchNew.Count == 0)
            {
                return false;
            }

            CollectUnitOccupiedBeats(agenda, unit, ScratchOccupied);
            foreach (var beat in ScratchNew)
            {
                if (ScratchOccupied.Contains(beat))
                {
                    return false;
                }
            }

            return true;
        }

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
