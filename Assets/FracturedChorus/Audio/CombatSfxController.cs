using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatSfxController : MonoBehaviour
    {
        private const float LatePlayBudgetSec = 0.03f;

        [SerializeField] private AudioSource perfectCounterSource;
        [SerializeField] private AudioClip perfectCounterClip;
        [SerializeField] private float perfectCounterVolume = 1f;
        [Tooltip("Seconds to schedule before target DSP so clip onset lands on beat (0 if WAV already trimmed).")]
        [SerializeField] private float sfxLeadSec;
        [SerializeField] private AudioSource clashHitSource;
        [SerializeField] private AudioClip clashHitClip;
        [SerializeField] private float clashHitVolume = 1f;

        private void Awake()
        {
            EnsurePerfectCounterSource();
            EnsureClashHitSource();
            TryAssignDefaultClips();
            PrimePerfectCounterSource();
            PrimeClashHitSource();
        }

        public void PlayPerfectCounter(double targetDspTime = -1d)
        {
            if (perfectCounterClip == null)
            {
                Debug.LogWarning("[CombatSfx] perfectCounterClip not assigned — run 'Fractured Chorus/Wire Combat Music'.");
                return;
            }

            EnsurePerfectCounterSource();
            PrimePerfectCounterSource();

            perfectCounterSource.clip = perfectCounterClip;
            perfectCounterSource.volume = perfectCounterVolume;

            var now = AudioSettings.dspTime;
            var playAt = targetDspTime >= 0d ? targetDspTime - sfxLeadSec : now;
            if (playAt > now)
            {
                perfectCounterSource.Stop();
                perfectCounterSource.PlayScheduled(playAt);
                return;
            }

            var lateSec = now - playAt;
            if (lateSec > LatePlayBudgetSec)
            {
                Debug.LogWarning($"[CombatSfx] Counter SFX late {(lateSec * 1000d):F1}ms — play immediate.");
            }

            perfectCounterSource.Stop();
            perfectCounterSource.PlayScheduled(now);
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

        private void PlayClashHitImmediate(double dspStartTime = -1)
        {
            if (clashHitClip == null || clashHitSource == null)
            {
                return;
            }

            clashHitSource.clip = clashHitClip;
            clashHitSource.volume = clashHitVolume;
            if (dspStartTime >= 0d)
            {
                clashHitSource.PlayScheduled(dspStartTime);
            }
            else
            {
                clashHitSource.PlayOneShot(clashHitClip, clashHitVolume);
            }
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

        private void TryAssignDefaultClips()
        {
#if UNITY_EDITOR
            if (perfectCounterClip == null)
            {
                perfectCounterClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/Perfect sound Game.wav");
            }

            if (clashHitClip == null)
            {
                clashHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/Clash Hit.wav");
            }
#endif
        }
    }
}
