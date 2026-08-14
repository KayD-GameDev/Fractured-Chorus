using UnityEngine;

namespace FracturedChorus.Audio
{
    public static class CombatAudioSpectrum
    {
        public static AudioSource ResolvePlayingSource(ICombatMusicSync music)
        {
            if (IsPlaying(music?.Source))
            {
                return music.Source;
            }

            var session = RunMusicSession.Instance;
            if (IsPlaying(session?.Source))
            {
                return session.Source;
            }

            var controller = Object.FindAnyObjectByType<CombatMusicController>();
            if (IsPlaying(controller?.Source))
            {
                return controller.Source;
            }

            return music?.Source;
        }

        public static bool TryFill(AudioSource source, float[] buffer, FFTWindow window)
        {
            if (buffer == null || buffer.Length == 0 || !IsPlaying(source))
            {
                return false;
            }

            source.GetSpectrumData(buffer, 0, window);
            var peak = 0f;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] > peak)
                {
                    peak = buffer[i];
                }
            }

            return peak > 0.000001f;
        }

        public static void FillBeatPulse(float[] buffer, float musicalBeat)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return;
            }

            var pulse = 0.22f + 0.78f * Mathf.Abs(Mathf.Sin(musicalBeat * Mathf.PI));
            for (var i = 0; i < buffer.Length; i++)
            {
                var falloff = 1f / (1f + i * 0.035f);
                var wobble = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(i * 0.37f + musicalBeat * 2.4f));
                buffer[i] = pulse * falloff * wobble * 0.08f;
            }
        }

        private static bool IsPlaying(AudioSource source)
        {
            return source != null && source.isPlaying;
        }
    }
}
