using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip bossTrack;
        [SerializeField] private MusicBeatMapSO beatMap;
        [SerializeField] private TextAsset beatMapCsv;
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
        private float _playbackSpeedMultiplier = 1f;

        public MusicBeatMapSO BeatMap => beatMap;
        public float TotalMusicalBeat => _totalMusicalBeat;
        public float PlaybackSpeedMultiplier => _playbackSpeedMultiplier;
        public float BeatDuration => 60f / bpm;
        public bool IsPlaying => _playing && source != null && source.isPlaying;
        public bool UsesBeatMap => beatMap != null && beatMap.HasData;
        public int LoopCount => _loopCount;

        private void Awake()
        {
            EnsureAudioSource();
            TryAssignDefaultClip();
            TryLoadBeatMapFromCsv();
        }

        private void TryLoadBeatMapFromCsv()
        {
            if (beatMap != null && beatMap.HasData)
            {
                return;
            }

#if UNITY_EDITOR
            if (beatMapCsv == null)
            {
                beatMapCsv = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv");
            }
#endif

            if (beatMapCsv == null || bossTrack == null)
            {
                return;
            }

            beatMap = MusicBeatMapSO.CreateRuntimeFromCsv(beatMapCsv.text, bossTrack);
        }

        private void OnValidate()
        {
            EnsureAudioSource();
        }

        private void Update()
        {
            if (!_playing || source == null || !source.isPlaying)
            {
                return;
            }

            SyncMusicalBeatFromAudio();
            HandleLoopRegions();
        }

        private void SyncMusicalBeatFromAudio()
        {
            if (UsesBeatMap)
            {
                _totalMusicalBeat = beatMap.TimeToMusicalBeat(source.time);
                return;
            }

            _totalMusicalBeat += Time.deltaTime * _playbackSpeedMultiplier / BeatDuration;
        }

        public void SetPlaybackSpeedMultiplier(float multiplier)
        {
            _playbackSpeedMultiplier = Mathf.Max(0.001f, multiplier);
            if (source != null)
            {
                source.pitch = _playbackSpeedMultiplier;
            }
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
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.pitch = _playbackSpeedMultiplier;
            source.Play();

            if (!source.isPlaying)
            {
                Debug.LogError("[CombatMusic] AudioSource.Play() failed. Check AudioListener on Main Camera.");
                _playing = false;
                return;
            }

            var syncMode = UsesBeatMap ? $"beat map ({beatMap.BeatCount} markers)" : $"{bpm} BPM";
            Debug.Log($"[CombatMusic] Playing '{bossTrack.name}' ({bossTrack.length:F1}s) sync={syncMode}.");
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

        /// <summary>Đang phát nhưng bị tạm dừng (giữ nguyên vị trí bài) — dùng cho intro-pause planning.</summary>
        public bool IsPaused => _playing && source != null && !source.isPlaying;

        /// <summary>Tạm dừng nhạc tại chỗ (không reset). Beat nhạc đóng băng theo source.time.</summary>
        public void PausePlayback()
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }
        }

        /// <summary>Phát tiếp từ chỗ đã pause.</summary>
        public void ResumePlayback()
        {
            if (_playing && source != null && !source.isPlaying)
            {
                source.UnPause();
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
            source.spatialBlend = 0f;
            source.bypassListenerEffects = false;
            source.bypassReverbZones = true;
            source.priority = 0;
        }

        private void TryAssignDefaultClip()
        {
#if UNITY_EDITOR
            if (bossTrack == null)
            {
                bossTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3");
            }

            if (beatMap == null)
            {
                beatMap = UnityEditor.AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_BeatMap.asset");
            }
#endif
        }
    }
}
