using FracturedChorus.Combat.Units;
using System;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    /// <summary>
    /// Arena law from the locked Stage TV face. Duration is in Execute segments (1 segment = 1 phase).
    /// </summary>
    public static class AstraTvMoodState
    {
        public static AstraTvMood Mood { get; private set; } = AstraTvMood.None;
        public static int MoodStartSegment { get; private set; }
        public static int DurationSegments { get; private set; } = 2;
        public static int LastRolledSegment { get; private set; } = -1;

        public static int JoyS2ReduceBeats { get; private set; }
        public static float AngerQteWindowMult { get; private set; } = 1f;
        public static float CoverCostMult { get; private set; } = 1f;
        public static CombatUnit Spotlight { get; private set; }
        public static float SpotlightOutgoingMult { get; private set; } = 1f;
        public static int ExtraRedCoreNotes { get; private set; }
        public static int ExtraMiniAttacks { get; private set; }

        public static event Action OnChanged;

        public static bool HasMood => Mood != AstraTvMood.None;

        public static bool CoversSegment(int segmentIndex)
        {
            if (!HasMood || DurationSegments <= 0)
            {
                return false;
            }

            return segmentIndex >= MoodStartSegment
                   && segmentIndex < MoodStartSegment + DurationSegments;
        }

        public static bool ShouldReroll(int segmentIndex)
        {
            if (DurationSegments <= 0 || segmentIndex <= 0)
            {
                return false;
            }

            return segmentIndex % DurationSegments == 0 && LastRolledSegment != segmentIndex;
        }

        public static void MarkRolled(int segmentIndex)
        {
            LastRolledSegment = segmentIndex;
        }

        public static void Clear()
        {
            Mood = AstraTvMood.None;
            MoodStartSegment = 0;
            DurationSegments = 2;
            LastRolledSegment = -1;
            JoyS2ReduceBeats = 0;
            AngerQteWindowMult = 1f;
            CoverCostMult = 1f;
            Spotlight = null;
            SpotlightOutgoingMult = 1f;
            ExtraRedCoreNotes = 0;
            ExtraMiniAttacks = 0;
            OnChanged?.Invoke();
        }

        public static float ResolveOutgoingMult(CombatUnit source, CombatUnit target)
        {
            if (source == null || target == null || Spotlight == null || !source.IsAlive)
            {
                return 1f;
            }

            if (source != Spotlight || target.Role != UnitRole.Boss)
            {
                return 1f;
            }

            return SpotlightOutgoingMult > 0f ? SpotlightOutgoingMult : 1f;
        }

        public static CombatUnit ResolveLeakTarget(CombatUnit fallback)
        {
            if (Spotlight != null && Spotlight.IsAlive)
            {
                return Spotlight;
            }

            return fallback;
        }

        public static void Apply(
            AstraTvMood mood,
            int startSegment,
            AstraStageTvConfig config,
            CombatUnit spotlight)
        {
            var duration = config != null ? Mathf.Max(1, config.MoodDurationPhases) : 2;
            Mood = mood;
            MoodStartSegment = Mathf.Max(0, startSegment);
            DurationSegments = duration;
            LastRolledSegment = MoodStartSegment;
            JoyS2ReduceBeats = mood == AstraTvMood.Joy && config != null
                ? Mathf.Max(0, config.JoyS2ReduceBeats)
                : 0;
            AngerQteWindowMult = mood == AstraTvMood.Anger && config != null
                ? Mathf.Clamp(config.AngerQteWindowMult, 0.2f, 1f)
                : 1f;
            CoverCostMult = mood == AstraTvMood.Hate && config != null
                ? Mathf.Max(1f, config.HateCoverCostMult)
                : 1f;
            Spotlight = mood == AstraTvMood.Love ? spotlight : null;
            SpotlightOutgoingMult = mood == AstraTvMood.Love && config != null
                ? Mathf.Max(1f, config.LoveOutgoingMult)
                : 1f;
            ExtraRedCoreNotes = mood == AstraTvMood.Joy && config != null
                ? Mathf.Max(0, config.JoyExtraRedCoreNotes)
                : 0;
            ExtraMiniAttacks = mood == AstraTvMood.Sorrow && config != null
                ? Mathf.Max(0, config.SorrowExtraMiniAttacks)
                : 0;
            OnChanged?.Invoke();
        }

        public static string MoodSpriteResourcePath
        {
            get
            {
                return Mood switch
                {
                    AstraTvMood.Joy => "UI/Combat/Buffs/astra_tv_mood_joy_v1",
                    AstraTvMood.Anger => "UI/Combat/Buffs/astra_tv_mood_anger_v1",
                    AstraTvMood.Love => "UI/Combat/Buffs/astra_tv_mood_love_v1",
                    AstraTvMood.Hate => "UI/Combat/Buffs/astra_tv_mood_hate_v1",
                    AstraTvMood.Sorrow => "UI/Combat/Buffs/astra_tv_mood_sorrow_v1",
                    _ => null
                };
            }
        }

        public static AstraTvMood FromFaceIndex(int faceIndex)
        {
            if (faceIndex < 0 || faceIndex > (int)AstraTvMood.Sorrow)
            {
                return AstraTvMood.None;
            }

            return (AstraTvMood)faceIndex;
        }
    }
}
