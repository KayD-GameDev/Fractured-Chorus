using UnityEngine;

namespace FracturedChorus.Audio
{
    [CreateAssetMenu(fileName = "MusicBeatMap", menuName = "Fractured Chorus/Music Beat Map")]
    public class MusicBeatMapSO : ScriptableObject
    {
        public const int BeatsPerBar = 4;

        [SerializeField] private AudioClip clip;
        [SerializeField] private float bpm = 152f;
        [SerializeField] private float firstBeatOffsetSec;

        public AudioClip Clip => clip;
        public float Bpm => bpm;
        public float FirstBeatOffsetSec => firstBeatOffsetSec;
        public bool HasData => bpm > 0.01f;
        public float BeatSpanSec => 60f / Mathf.Max(0.01f, bpm);

        /// <summary>Beat slots that fit in the clip, counting the beat at firstBeatOffsetSec as index 0.</summary>
        public int TotalBeatsForClip()
        {
            if (clip == null)
            {
                return 1;
            }

            var span = Mathf.Max(0f, clip.length - firstBeatOffsetSec);
            return Mathf.Max(1, Mathf.FloorToInt(span / BeatSpanSec) + 1);
        }

        public float GetBeatSpanSec(int beatIndex) => BeatSpanSec;

        public float AverageBeatSpanSec() => BeatSpanSec;

        public float TimeToMusicalBeat(float audioTimeSec) => (audioTimeSec - firstBeatOffsetSec) / BeatSpanSec;

        public float MusicalBeatToTime(float musicalBeat) => firstBeatOffsetSec + musicalBeat * BeatSpanSec;

        public static float SnapUpToBar(float musicalBeat) => Mathf.Ceil(musicalBeat / BeatsPerBar) * BeatsPerBar;

        /// <summary>Next integer beat at or after musicalBeat (shortest beat-aligned wait).</summary>
        public static float SnapUpToBeat(float musicalBeat) => Mathf.Ceil(musicalBeat);

#if UNITY_EDITOR
        public void EditorSetData(AudioClip audioClip, float beatsPerMinute, float offsetSec)
        {
            clip = audioClip;
            bpm = beatsPerMinute;
            firstBeatOffsetSec = offsetSec;
        }
#endif
    }
}
