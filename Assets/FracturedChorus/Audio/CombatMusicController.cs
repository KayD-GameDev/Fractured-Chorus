using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip bossTrack;
        [SerializeField] private float bpm = 148f;
        [SerializeField] private float introEndSec = 24f;
        [SerializeField] private float loopEndSec = 122f;
        [SerializeField] private float firstPassEndSec = 244.8f;
        [SerializeField] private float crossfadeSec = 0.05f;
        [SerializeField] private float loopDetectLeadSec = 0.05f;

        private float _totalMusicalBeat;
        private int _loopCount;
        private bool _inLoopBody;
        private bool _playing;
        private Coroutine _crossfadeRoutine;

        public float TotalMusicalBeat => _totalMusicalBeat;
        public float BeatDuration => 60f / bpm;
        public bool IsPlaying => _playing && source != null && source.isPlaying;
        public int LoopCount => _loopCount;

        private void Awake()
        {
            EnsureAudioSource();
            TryAssignDefaultClip();
        }

        private void Update()
        {
            if (!_playing || source == null || !source.isPlaying)
            {
                return;
            }

            _totalMusicalBeat += Time.deltaTime / BeatDuration;
            HandleLoopRegions();
        }

        public void PlayBossMusic()
        {
            if (bossTrack == null)
            {
                Debug.LogWarning("[CombatMusic] No boss track assigned.");
                return;
            }

            EnsureAudioSource();
            StopAllCoroutines();
            _crossfadeRoutine = null;

            _totalMusicalBeat = 0f;
            _loopCount = 0;
            _inLoopBody = false;
            _playing = true;

            source.clip = bossTrack;
            source.loop = false;
            source.time = 0f;
            source.Play();
        }

        public void StopMusic()
        {
            _playing = false;
            StopAllCoroutines();
            _crossfadeRoutine = null;

            if (source != null)
            {
                source.Stop();
            }
        }

        private void HandleLoopRegions()
        {
            var passEnd = ResolveFirstPassEndSec();

            if (!_inLoopBody)
            {
                if (source.time >= passEnd - loopDetectLeadSec)
                {
                    EnterLoopBody();
                }

                return;
            }

            if (source.time >= loopEndSec - loopDetectLeadSec)
            {
                _loopCount++;
                StartCrossfadeJump(introEndSec);
            }
        }

        private void EnterLoopBody()
        {
            _inLoopBody = true;
            _loopCount = 1;
            StartCrossfadeJump(introEndSec);
        }

        private float ResolveFirstPassEndSec()
        {
            if (bossTrack == null)
            {
                return firstPassEndSec;
            }

            return Mathf.Min(firstPassEndSec, bossTrack.length);
        }

        private void StartCrossfadeJump(float targetTime)
        {
            if (_crossfadeRoutine != null)
            {
                StopCoroutine(_crossfadeRoutine);
            }

            _crossfadeRoutine = StartCoroutine(CrossfadeJumpRoutine(targetTime));
        }

        private IEnumerator CrossfadeJumpRoutine(float targetTime)
        {
            if (source == null)
            {
                yield break;
            }

            var peakVolume = source.volume;
            var duration = Mathf.Max(0.01f, crossfadeSec);
            var half = duration * 0.5f;

            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                source.volume = peakVolume * (1f - t / half);
                yield return null;
            }

            source.time = Mathf.Clamp(targetTime, 0f, bossTrack != null ? bossTrack.length - 0.01f : targetTime);
            source.volume = 0f;

            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                source.volume = peakVolume * (t / half);
                yield return null;
            }

            source.volume = peakVolume;
            _crossfadeRoutine = null;
        }

        private void EnsureAudioSource()
        {
            if (source != null)
            {
                return;
            }

            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
        }

        private void TryAssignDefaultClip()
        {
            if (bossTrack != null)
            {
                return;
            }

#if UNITY_EDITOR
            bossTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3");
#endif
        }
    }
}
