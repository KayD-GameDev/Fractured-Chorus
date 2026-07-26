using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatSfxController : MonoBehaviour
    {
        private const float ScheduleAheadEpsilonSec = 0.002f;
        private const string PerfectCounterClipPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound Game.wav";
        private const string PerfectBlockClipPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound SFX.wav";
        private const string PerfectBlockResourcePath = "Audio/SFX/Perfect sound SFX";
        private const string ClashHitClipPath = "Assets/FracturedChorus/Audio/SFX/Clash Hit.wav";

        [SerializeField] private AudioSource perfectCounterSource;
        [SerializeField] private AudioClip perfectCounterClip;
        [SerializeField] private float perfectCounterVolume = 1f;
        [SerializeField] private AudioSource perfectBlockSource;
        [SerializeField] private AudioClip perfectBlockClip;
        [SerializeField] private float perfectBlockVolume = 1f;
        [Tooltip("Seconds to schedule before target DSP so clip onset lands on beat (0 if WAV already trimmed).")]
        [SerializeField] private float sfxLeadSec;
        [SerializeField] private AudioSource clashHitSource;
        [SerializeField] private AudioClip clashHitClip;
        [SerializeField] private float clashHitVolume = 1f;

        private void Awake()
        {
            EnsurePerfectCounterSource();
            EnsurePerfectBlockSource();
            EnsureClashHitSource();
            TryAssignDefaultClips();
            PrimePerfectCounterSource();
            PrimePerfectBlockSource();
            PrimeClashHitSource();
        }

        public void PlayPerfectCounter(double targetDspTime = -1d)
        {
            EnsureClip(ref perfectCounterClip, null, PerfectCounterClipPath);
            PlayClip(
                ref perfectCounterSource,
                perfectCounterClip,
                perfectCounterVolume,
                "PerfectCounterSfx",
                "perfectCounterClip",
                targetDspTime);
        }

        public void PlayPerfectBlock(double targetDspTime = -1d)
        {
            EnsureClip(ref perfectBlockClip, PerfectBlockResourcePath, PerfectBlockClipPath);
            if (perfectBlockClip == null)
            {
                Debug.LogWarning("[CombatSfx] perfectBlockClip missing — fallback counter clip.");
                PlayPerfectCounter(-1d);
                return;
            }

            EnsurePerfectBlockSource();
            if (perfectBlockSource != null)
            {
                perfectBlockSource.spatialBlend = 0f;
                perfectBlockSource.mute = false;
                perfectBlockSource.volume = perfectBlockVolume;
                perfectBlockSource.PlayOneShot(perfectBlockClip, perfectBlockVolume);
                Debug.Log($"[CombatSfx] PlayPerfectBlock len={perfectBlockClip.length:F2}s vol={perfectBlockVolume}");
                return;
            }

            var listener = FindAnyObjectByType<AudioListener>();
            var pos = listener != null ? listener.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(perfectBlockClip, pos, perfectBlockVolume);
            Debug.Log($"[CombatSfx] PlayPerfectBlock via PlayClipAtPoint len={perfectBlockClip.length:F2}s");
            _ = targetDspTime;
        }

        public void PlayClashHit()
        {
            if (clashHitClip == null)
            {
                return;
            }

            EnsureClashHitSource();
            PrimeClashHitSource();
            PlayClashHitImmediate();
        }

        private void PlayClip(
            ref AudioSource source,
            AudioClip clip,
            float volume,
            string sourceName,
            string clipFieldName,
            double targetDspTime)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[CombatSfx] {clipFieldName} not assigned — run 'Fractured Chorus/Wire Combat Music'.");
                return;
            }

            if (source == null)
            {
                source = FindOrCreateSfxSource(sourceName);
            }

            source.spatialBlend = 0f;
            source.mute = false;
            source.volume = volume;

            var now = AudioSettings.dspTime;
            var playAt = targetDspTime >= 0d ? targetDspTime - sfxLeadSec : now;
            if (playAt > now + ScheduleAheadEpsilonSec)
            {
                source.clip = clip;
                source.Stop();
                source.PlayScheduled(playAt);
                return;
            }

            source.PlayOneShot(clip, volume);
        }

        private void PlayClashHitImmediate()
        {
            if (clashHitClip == null || clashHitSource == null)
            {
                return;
            }

            clashHitSource.PlayOneShot(clashHitClip, clashHitVolume);
        }

        private void PrimePerfectCounterSource()
        {
            if (perfectCounterSource == null || perfectCounterClip == null)
            {
                return;
            }

            perfectCounterSource.clip = perfectCounterClip;
            perfectCounterSource.volume = perfectCounterVolume;
        }

        private void PrimePerfectBlockSource()
        {
            if (perfectBlockSource == null || perfectBlockClip == null)
            {
                return;
            }

            perfectBlockSource.clip = perfectBlockClip;
            perfectBlockSource.volume = perfectBlockVolume;
        }

        private void PrimeClashHitSource()
        {
            if (clashHitSource == null || clashHitClip == null)
            {
                return;
            }

            clashHitSource.clip = clashHitClip;
            clashHitSource.volume = clashHitVolume;
        }

        private void EnsurePerfectCounterSource()
        {
            if (perfectCounterSource != null)
            {
                return;
            }

            perfectCounterSource = FindOrCreateSfxSource("PerfectCounterSfx");
        }

        private void EnsurePerfectBlockSource()
        {
            if (perfectBlockSource != null)
            {
                return;
            }

            perfectBlockSource = FindOrCreateSfxSource("PerfectBlockSfx");
        }

        private void EnsureClashHitSource()
        {
            if (clashHitSource != null)
            {
                return;
            }

            clashHitSource = FindOrCreateSfxSource("ClashHitSfx");
        }

        private AudioSource FindOrCreateSfxSource(string name)
        {
            var existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent<AudioSource>(out var existingSource))
            {
                return existingSource;
            }

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.bypassReverbZones = true;
            source.priority = 0;
            return source;
        }

        private static void EnsureClip(ref AudioClip clip, string resourcePath, string editorAssetPath)
        {
            if (clip != null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(resourcePath))
            {
                clip = Resources.Load<AudioClip>(resourcePath);
            }

#if UNITY_EDITOR
            if (clip == null && !string.IsNullOrEmpty(editorAssetPath))
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(editorAssetPath);
            }
#endif
        }

        private void TryAssignDefaultClips()
        {
            EnsureClip(ref perfectBlockClip, PerfectBlockResourcePath, PerfectBlockClipPath);
#if UNITY_EDITOR
            if (perfectCounterClip == null)
            {
                perfectCounterClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(PerfectCounterClipPath);
            }

            if (clashHitClip == null)
            {
                clashHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ClashHitClipPath);
            }
#endif
        }
    }
}
