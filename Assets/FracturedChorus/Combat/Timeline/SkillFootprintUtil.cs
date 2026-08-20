using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
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
        private static readonly List<int> ScratchSwap = new();

        public static int GetStandingBefore(SkillDefinitionSO skill) =>
            skill != null ? Mathf.Max(0, skill.standingBeatsBefore) : 0;

        public static int GetActiveBeats(SkillDefinitionSO skill) =>
            GetActiveBeats(skill, null, null);

        public static int GetActiveBeats(SkillDefinitionSO skill, CombatUnit unit) =>
            GetActiveBeats(skill, unit, null);

        public static int GetActiveBeats(SkillDefinitionSO skill, CombatUnit unit, AgendaEntry entry)
        {
            if (entry != null && entry.ActiveBeatsOverride > 0)
            {
                return entry.ActiveBeatsOverride;
            }

            var active = skill != null ? Mathf.Max(1, skill.activeBeats) : 1;
            if (entry == null && unit != null && unit.Side == GridSide.Player
                && RunEventCombatMods.PendingPlaceCounterPlus > 0)
            {
                return active + RunEventCombatMods.PendingPlaceCounterPlus;
            }

            return active;
        }

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

        public static bool FootprintInBounds(SkillDefinitionSO skill, int placementBeat, CombatUnit unit = null) =>
            FootprintInBounds(skill, placementBeat, unit, null);

        public static bool FootprintInBounds(
            SkillDefinitionSO skill,
            int placementBeat,
            CombatUnit unit,
            AgendaEntry entry)
        {
            if (skill == null)
            {
                return false;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat, unit, entry))
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
            CombatUnit unit) =>
            CollectOccupiedBeats(skill, placementBeat, results, unit, null);

        public static void CollectOccupiedBeats(
            SkillDefinitionSO skill,
            int placementBeat,
            List<int> results,
            CombatUnit unit,
            AgendaEntry entry)
        {
            results.Clear();
            if (skill == null)
            {
                return;
            }

            foreach (var info in EnumerateFootprintBeats(skill, placementBeat, unit, entry))
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
            var active = GetActiveBeats(skill, unit, entry);
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

        public static void CollectUnitOccupiedBeats(IReadOnlyList<AgendaEntry> agenda, CombatUnit unit, List<int> results) =>
            CollectUnitOccupiedBeats(agenda, unit, results, null);

        public static void CollectUnitOccupiedBeats(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            List<int> results,
            AgendaEntry ignore)
        {
            results.Clear();
            if (agenda == null || unit == null)
            {
                return;
            }

            foreach (var entry in agenda)
            {
                if (entry?.Unit != unit || entry.Skill == null || entry == ignore)
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

        public static bool TryGetEntryAtBeat(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            int beat,
            out AgendaEntry entry,
            out FootprintBeatRole role)
        {
            entry = null;
            role = default;
            if (agenda == null || unit == null || beat < 0 || beat >= CombatTimelineProfile.TotalBeats)
            {
                return false;
            }

            foreach (var candidate in agenda)
            {
                if (candidate?.Unit != unit || candidate.Skill == null)
                {
                    continue;
                }

                foreach (var info in EnumerateFootprintBeats(
                    candidate.Skill, candidate.BeatIndex, candidate.Unit, candidate))
                {
                    if (info.BeatIndex != beat)
                    {
                        continue;
                    }

                    entry = candidate;
                    role = info.Role;
                    return true;
                }
            }

            return false;
        }

        public static bool ActivePhasesOverlap(
            SkillDefinitionSO ghostSkill,
            int ghostPlacement,
            CombatUnit unit,
            AgendaEntry entry)
        {
            if (ghostSkill == null || entry?.Skill == null)
            {
                return false;
            }

            foreach (var ghostBeat in EnumerateFootprintBeats(ghostSkill, ghostPlacement, unit))
            {
                if (ghostBeat.Role != FootprintBeatRole.Active)
                {
                    continue;
                }

                foreach (var occupied in EnumerateFootprintBeats(
                    entry.Skill, entry.BeatIndex, entry.Unit, entry))
                {
                    if (occupied.Role == FootprintBeatRole.Active
                        && occupied.BeatIndex == ghostBeat.BeatIndex)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetActivePhaseOverlapEntry(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            out AgendaEntry entry)
        {
            entry = null;
            if (agenda == null || unit == null || skill == null)
            {
                return false;
            }

            foreach (var candidate in agenda)
            {
                if (candidate?.Unit != unit || candidate.Skill == null)
                {
                    continue;
                }

                if (!ActivePhasesOverlap(skill, placementBeat, unit, candidate))
                {
                    continue;
                }

                if (entry != null)
                {
                    entry = null;
                    return false;
                }

                entry = candidate;
            }

            return entry != null;
        }

        public static AgendaEntry FindSingleOverlappingEntry(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat)
        {
            if (agenda == null || unit == null || skill == null)
            {
                return null;
            }

            CollectOccupiedBeats(skill, placementBeat, ScratchNew, unit);
            if (ScratchNew.Count == 0)
            {
                return null;
            }

            AgendaEntry found = null;
            foreach (var entry in agenda)
            {
                if (entry?.Unit != unit || entry.Skill == null)
                {
                    continue;
                }

                var overlaps = false;
                foreach (var info in EnumerateFootprintBeats(entry.Skill, entry.BeatIndex, entry.Unit, entry))
                {
                    if (ScratchNew.Contains(info.BeatIndex))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = entry;
            }

            return found;
        }

        public static bool FootprintsOverlap(
            SkillDefinitionSO skillA,
            int beatA,
            CombatUnit unitA,
            AgendaEntry entryA,
            SkillDefinitionSO skillB,
            int beatB,
            CombatUnit unitB,
            AgendaEntry entryB)
        {
            ScratchSwap.Clear();
            foreach (var info in EnumerateFootprintBeats(skillA, beatA, unitA, entryA))
            {
                ScratchSwap.Add(info.BeatIndex);
            }

            foreach (var info in EnumerateFootprintBeats(skillB, beatB, unitB, entryB))
            {
                if (ScratchSwap.Contains(info.BeatIndex))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanPlace(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int planningHorizonBeat = 0) =>
            CanPlace(agenda, unit, skill, placementBeat, planningHorizonBeat, null);

        public static bool CanPlace(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int planningHorizonBeat,
            AgendaEntry ignore) =>
            CanPlace(agenda, unit, skill, placementBeat, planningHorizonBeat, ignore, null);

        public static bool CanPlace(
            IReadOnlyList<AgendaEntry> agenda,
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int planningHorizonBeat,
            AgendaEntry ignore,
            AgendaEntry placingEntry)
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

            if (!FootprintInBounds(skill, placementBeat, unit, placingEntry))
            {
                return false;
            }

            CollectOccupiedBeats(skill, placementBeat, ScratchNew, unit, placingEntry);
            if (ScratchNew.Count == 0)
            {
                return false;
            }

            CollectUnitOccupiedBeats(agenda, unit, ScratchOccupied, ignore);
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
