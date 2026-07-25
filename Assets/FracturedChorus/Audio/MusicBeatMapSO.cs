using UnityEngine;

namespace FracturedChorus.Audio
{
    [CreateAssetMenu(fileName = "MusicBeatMap", menuName = "Fractured Chorus/Music Beat Map")]
    public class MusicBeatMapSO : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private float[] beatTimesSec;
        [SerializeField] private float fallbackBpm = 148f;

        public AudioClip Clip => clip;
        public float[] BeatTimesSec => beatTimesSec;
        public bool HasData => beatTimesSec != null && beatTimesSec.Length > 0;
        public int BeatCount => beatTimesSec?.Length ?? 0;

        public void SetData(AudioClip audioClip, float[] times)
        {
            clip = audioClip;
            beatTimesSec = times;
        }

        public static MusicBeatMapSO CreateRuntimeFromCsv(string csvText, AudioClip audioClip)
        {
            var map = CreateInstance<MusicBeatMapSO>();
            map.SetData(audioClip, ParseCsvTimes(csvText));
            return map;
        }

        public static float[] ParseCsvTimes(string csvText)
        {
            var times = new System.Collections.Generic.List<float>();
            var lines = csvText.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || (i == 0 && line.ToLowerInvariant().Contains("time")))
                {
                    continue;
                }

                var parts = line.Split(',');
                if (parts.Length >= 2 &&
                    float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var sec))
                {
                    times.Add(sec);
                }
            }

            if (times.Count == 0)
            {
                return System.Array.Empty<float>();
            }

            times.Sort();
            if (times[0] > 0.001f)
            {
                times.Insert(0, 0f);
            }

            return times.ToArray();
        }

        public float GetBeatSpanSec(int beatIndex)
        {
            if (!HasData)
            {
                return 60f / fallbackBpm;
            }

            var last = beatTimesSec.Length - 1;
            if (beatIndex < 0)
            {
                beatIndex = 0;
            }

            if (beatIndex >= last)
            {
                var tail = last > 0 ? beatTimesSec[last] - beatTimesSec[last - 1] : 60f / fallbackBpm;
                return tail > 0f ? tail : 60f / fallbackBpm;
            }

            var span = beatTimesSec[beatIndex + 1] - beatTimesSec[beatIndex];
            return span > 0f ? span : 60f / fallbackBpm;
        }

        public float AverageBeatSpanSec()
        {
            if (!HasData || beatTimesSec.Length < 2)
            {
                return 60f / fallbackBpm;
            }

            var total = beatTimesSec[beatTimesSec.Length - 1] - beatTimesSec[0];
            var span = total / (beatTimesSec.Length - 1);
            return span > 0f ? span : 60f / fallbackBpm;
        }

        public float TimeToMusicalBeat(float audioTimeSec)
        {
            if (!HasData)
            {
                return audioTimeSec / (60f / fallbackBpm);
            }

            if (audioTimeSec <= beatTimesSec[0])
            {
                if (beatTimesSec.Length < 2 || beatTimesSec[0] <= 0f)
                {
                    return 0f;
                }

                var span = beatTimesSec[1] - beatTimesSec[0];
                return span > 0f ? audioTimeSec / span : 0f;
            }

            var lastIndex = beatTimesSec.Length - 1;
            if (audioTimeSec >= beatTimesSec[lastIndex])
            {
                var tailSpan = lastIndex > 0
                    ? beatTimesSec[lastIndex] - beatTimesSec[lastIndex - 1]
                    : 60f / fallbackBpm;
                if (tailSpan <= 0f)
                {
                    tailSpan = 60f / fallbackBpm;
                }

                return lastIndex + (audioTimeSec - beatTimesSec[lastIndex]) / tailSpan;
            }

            var low = 0;
            var high = lastIndex;
            while (low < high - 1)
            {
                var mid = (low + high) >> 1;
                if (beatTimesSec[mid] <= audioTimeSec)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            var t0 = beatTimesSec[low];
            var t1 = beatTimesSec[high];
            var span01 = t1 - t0;
            if (span01 <= 0.0001f)
            {
                return low;
            }

            var frac = (audioTimeSec - t0) / span01;
            return low + frac;
        }

        public float MusicalBeatToTime(float musicalBeat)
        {
            if (!HasData)
            {
                return musicalBeat * (60f / fallbackBpm);
            }

            if (musicalBeat <= 0f)
            {
                if (beatTimesSec.Length < 2 || beatTimesSec[0] <= 0f)
                {
                    return 0f;
                }

                var span = beatTimesSec[1] - beatTimesSec[0];
                return span > 0f ? musicalBeat * span : 0f;
            }

            var lastIndex = beatTimesSec.Length - 1;
            if (musicalBeat >= lastIndex)
            {
                var tailSpan = lastIndex > 0
                    ? beatTimesSec[lastIndex] - beatTimesSec[lastIndex - 1]
                    : 60f / fallbackBpm;
                if (tailSpan <= 0f)
                {
                    tailSpan = 60f / fallbackBpm;
                }

                return beatTimesSec[lastIndex] + (musicalBeat - lastIndex) * tailSpan;
            }

            var low = Mathf.FloorToInt(musicalBeat);
            var high = low + 1;
            var frac = musicalBeat - low;
            var t0 = beatTimesSec[low];
            var t1 = beatTimesSec[high];
            var span01 = t1 - t0;
            if (span01 <= 0.0001f)
            {
                return t0;
            }

            return t0 + frac * span01;
        }
    }
}
