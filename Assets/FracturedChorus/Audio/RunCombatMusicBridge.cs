using UnityEngine;

namespace FracturedChorus.Audio
{
    public sealed class RunCombatMusicBridge : MonoBehaviour, ICombatMusicSync
    {
        public bool UsesRunSession => true;
        public MusicBeatMapSO BeatMap => Session != null ? Session.BeatMap : null;
        public float TotalMusicalBeat => Session != null ? Session.TotalMusicalBeat : 0f;
        public float BeatDuration => Session != null ? Session.BeatDuration : 60f / 152f;
        public bool IsPlaying => Session != null && Session.IsPlaying;
        public float SourceTimeSec => Session?.Source != null ? Session.Source.time : 0f;
        public AudioSource Source => Session?.Source;

        private RunMusicSession Session => RunMusicSession.Instance;

        public static RunCombatMusicBridge Attach(Transform parent)
        {
            var existing = parent.GetComponentInChildren<RunCombatMusicBridge>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(nameof(RunCombatMusicBridge));
            go.transform.SetParent(parent, false);
            return go.AddComponent<RunCombatMusicBridge>();
        }

        public void PlayBossMusic()
        {
        }

        public void StopMusic()
        {
        }

        public void EnterPlanningDuck() => Session?.EnterPlanningDuck();

        public void ExitPlanningDuck() => Session?.ExitPlanningDuck();

        public void SetPlaybackSpeedMultiplier(float multiplier)
        {
            if (Session?.Source != null)
            {
                Session.Source.pitch = Mathf.Max(0.001f, multiplier);
            }
        }

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            if (Session != null)
            {
                return Session.TryGetDspTimeForMusicalBeat(musicalBeat, out dspTime);
            }

            dspTime = AudioSettings.dspTime;
            return false;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            if (Session != null)
            {
                return Session.TryGetMusicDeltaMs(musicalBeat, out deltaMs);
            }

            deltaMs = 0f;
            return false;
        }
    }
}
