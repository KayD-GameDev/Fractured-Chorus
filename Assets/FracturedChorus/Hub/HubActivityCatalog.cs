using System;
using System.Collections.Generic;
using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public readonly struct HubActivityOption
    {
        public HubActivityOption(string id, string label, DayPhase phase, Action<GameMetaState> apply)
        {
            Id = id;
            Label = label;
            Phase = phase;
            Apply = apply;
        }

        public string Id { get; }
        public string Label { get; }
        public DayPhase Phase { get; }
        public Action<GameMetaState> Apply { get; }
    }

    public static class HubActivityCatalog
    {
        public static IReadOnlyList<HubActivityOption> GetForPhase(DayPhase phase)
        {
            var list = new List<HubActivityOption>();

            foreach (var option in All)
            {
                if (option.Phase == phase)
                {
                    list.Add(option);
                }
            }

            return list;
        }

        private static readonly HubActivityOption[] All =
        {
            new HubActivityOption(
                "study",
                "Study — Thư viện (+Cadence)",
                DayPhase.Day,
                state => state.AddStatExp(SocialStatType.Cadence, 8)),
            new HubActivityOption(
                "practice",
                "Practice — Phòng nhạc (+Harmony)",
                DayPhase.Day,
                state => state.AddStatExp(SocialStatType.Harmony, 8)),
            new HubActivityOption(
                "rest_evening",
                "Rest — Nghỉ ngơi (+Rhythm)",
                DayPhase.Evening,
                state => state.AddStatExp(SocialStatType.Rhythm, 5)),
            new HubActivityOption(
                "rest_evening",
                "Rest — Nghỉ ngơi (+Rhythm)",
                DayPhase.Day,
                state => state.AddStatExp(SocialStatType.Rhythm, 5)),
            new HubActivityOption(
                "convenience_job",
                "CV Tiện lợi (+Harmony/Cadence)",
                DayPhase.Day,
                state =>
                {
                    state.AddStatExp(SocialStatType.Harmony, 10);
                    state.AddStatExp(SocialStatType.Cadence, 4);
                }),
            new HubActivityOption(
                "convenience_job",
                "CV Tiện lợi (+Harmony/Cadence)",
                DayPhase.Evening,
                state =>
                {
                    state.AddStatExp(SocialStatType.Harmony, 10);
                    state.AddStatExp(SocialStatType.Cadence, 4);
                }),
            new HubActivityOption(
                "flower_job",
                "Shop hoa (+Resonance/Harmony)",
                DayPhase.Day,
                state =>
                {
                    state.AddStatExp(SocialStatType.Resonance, 10);
                    state.AddStatExp(SocialStatType.Harmony, 4);
                }),
            new HubActivityOption(
                "dungeon_run",
                "Cadence Remediation (Evening)",
                DayPhase.Evening,
                state =>
                {
                    if (!state.Flags.Has(StoryFlagIds.VaultQuestActive))
                    {
                        return;
                    }

                    state.RunSnapshot.HasActiveRun = true;
                })
        };

        public static bool TryGet(string activityId, DayPhase phase, out HubActivityOption option)
        {
            foreach (var entry in All)
            {
                if (entry.Id == activityId && entry.Phase == phase)
                {
                    option = entry;
                    return true;
                }
            }

            foreach (var entry in All)
            {
                if (entry.Id == activityId)
                {
                    option = entry;
                    return true;
                }
            }

            option = default;
            return false;
        }
    }
}
