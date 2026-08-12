using UnityEngine;

namespace FracturedChorus.Audio
{
    public interface ICombatMusicSync
    {
        MusicBeatMapSO BeatMap { get; }
        float TotalMusicalBeat { get; }
        float BeatDuration { get; }
        bool IsPlaying { get; }
        float SourceTimeSec { get; }
        AudioSource Source { get; }
        bool UsesRunSession { get; }
        void PlayBossMusic();
        void StopMusic();
        void EnterPlanningDuck();
        void ExitPlanningDuck();
        void SetPlaybackSpeedMultiplier(float multiplier);
        bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime);
        bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs);
    }
}
