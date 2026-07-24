using System;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Hub
{
    public enum TownPinIcon
    {
        School = 0,
        Shop = 1,
        Flower = 2,
        Shrine = 3,
        Vault = 4
    }

    [Serializable]
    public sealed class TownSubLocation
    {
        public string Id;
        public string Label;
        public string ActivityId;
        public DayPhase[] AllowedPhases;
    }

    [Serializable]
    public sealed class TownLocationDefinition
    {
        public string Id;
        public string DisplayName;
        public TownPinIcon PinIcon;
        public Vector2 AnchorNormalized;
        public DayPhase[] AvailablePhases;
        public DayOfWeek[] AllowedWeekdays;
        public TownSubLocation[] SubLocations;
        public string RequiredFlag;
    }

    public static class TownLocationCatalog
    {
        public static TownLocationDefinition[] CreateDefault()
        {
            return new[]
            {
                new TownLocationDefinition
                {
                    Id = "hima",
                    DisplayName = "HIMA School",
                    PinIcon = TownPinIcon.School,
                    AnchorNormalized = new Vector2(0.72f, 0.28f),
                    AvailablePhases = new[] { DayPhase.Day, DayPhase.Evening },
                    SubLocations = new[]
                    {
                        new TownSubLocation
                        {
                            Id = "library",
                            Label = "Library — Study",
                            ActivityId = "study",
                            AllowedPhases = new[] { DayPhase.Day }
                        },
                        new TownSubLocation
                        {
                            Id = "music_room",
                            Label = "Music Room — Practice",
                            ActivityId = "practice",
                            AllowedPhases = new[] { DayPhase.Day }
                        },
                        new TownSubLocation
                        {
                            Id = "courtyard_rest",
                            Label = "Courtyard — Rest",
                            ActivityId = "rest_evening",
                            AllowedPhases = new[] { DayPhase.Evening }
                        }
                    }
                },
                new TownLocationDefinition
                {
                    Id = "convenience",
                    DisplayName = "Convenience Store",
                    PinIcon = TownPinIcon.Shop,
                    AnchorNormalized = new Vector2(0.42f, 0.48f),
                    AvailablePhases = new[] { DayPhase.Day, DayPhase.Evening },
                    SubLocations = new[]
                    {
                        new TownSubLocation
                        {
                            Id = "part_time",
                            Label = "Part-time Shift",
                            ActivityId = "convenience_job",
                            AllowedPhases = new[] { DayPhase.Day, DayPhase.Evening }
                        }
                    }
                },
                new TownLocationDefinition
                {
                    Id = "flower_shop",
                    DisplayName = "Flower Shop",
                    PinIcon = TownPinIcon.Flower,
                    AnchorNormalized = new Vector2(0.38f, 0.58f),
                    AvailablePhases = new[] { DayPhase.Day },
                    AllowedWeekdays = new[] { DayOfWeek.Wednesday, DayOfWeek.Saturday },
                    SubLocations = new[]
                    {
                        new TownSubLocation
                        {
                            Id = "flower_work",
                            Label = "Arrange Flowers",
                            ActivityId = "flower_job",
                            AllowedPhases = new[] { DayPhase.Day }
                        }
                    }
                },
                new TownLocationDefinition
                {
                    Id = "shrine",
                    DisplayName = "Hill Shrine",
                    PinIcon = TownPinIcon.Shrine,
                    AnchorNormalized = new Vector2(0.78f, 0.62f),
                    AvailablePhases = new[] { DayPhase.Day, DayPhase.Evening },
                    SubLocations = new[]
                    {
                        new TownSubLocation
                        {
                            Id = "shrine_rest",
                            Label = "Quiet Rest",
                            ActivityId = "rest_evening",
                            AllowedPhases = new[] { DayPhase.Day, DayPhase.Evening }
                        }
                    }
                },
                new TownLocationDefinition
                {
                    Id = "cadence_gate",
                    DisplayName = "Cadence Gate",
                    PinIcon = TownPinIcon.Vault,
                    AnchorNormalized = new Vector2(0.55f, 0.35f),
                    AvailablePhases = new[] { DayPhase.Evening },
                    RequiredFlag = StoryFlagIds.VaultQuestActive,
                    SubLocations = new[]
                    {
                        new TownSubLocation
                        {
                            Id = "vault_run",
                            Label = "Cadence Remediation",
                            ActivityId = "dungeon_run",
                            AllowedPhases = new[] { DayPhase.Evening }
                        }
                    }
                }
            };
        }
    }
}
