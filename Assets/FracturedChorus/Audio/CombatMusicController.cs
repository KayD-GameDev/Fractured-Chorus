using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip bossTrack;
        [SerializeField] private MusicBeatMapSO beatMap;
        [SerializeField] private float fallbackBpm = 152f;

        [Header("Loop (bar aligned)")]
        [Tooltip("Bar the loop jumps back to. Bar b starts at musical beat b * MusicBeatMapSO.BeatsPerBar.")]
        [SerializeField] private int loopStartBar;
        [Tooltip("Bar the loop jumps from. -1 resolves to the last full bar in the clip.")]
        [SerializeField] private int loopEndBar = -1;
        [SerializeField] private float loopFadeSec = 0.05f;
        [SerializeField] private float loopDetectLeadSec = 0.05f;

        [Header("Planning Duck")]
        [SerializeField] [Range(0f, 1f)] private float duckVolume = 0.7f;
        [SerializeField] private float duckCutoffHz = 900f;
        [SerializeField] private float duckFadeSec = 0.25f;

        private const float OpenCutoffHz = 22000f;

        private AudioLowPassFilter _lowPass;
        private float _totalMusicalBeat;
        private float _loopBeatAccum;
        private int _loopCount;
        private bool _playing;
        private bool _ducked;
        private float _playbackSpeedMultiplier = 1f;
        private float _mixVolume = 1f;
        private float _loopFade = 1f;
        private Coroutine _duckRoutine;
        private Coroutine _loopJumpRoutine;

        public MusicBeatMapSO BeatMap => beatMap;
        public float TotalMusicalBeat => _totalMusicalBeat;
        public float PlaybackSpeedMultiplier => _playbackSpeedMultiplier;
        public float BeatDuration => UsesBeatMap ? beatMap.BeatSpanSec : 60f / Mathf.Max(0.01f, fallbackBpm);
        public bool IsPlaying => _playing && source != null && source.isPlaying;
        public bool UsesBeatMap => beatMap != null && beatMap.HasData;
        public bool IsDucked => _ducked;
        public int LoopCount => _loopCount;
        public float SourceTimeSec => source != null ? source.time : 0f;

        /// <summary>Musical beats consumed by loop jumps, so TotalMusicalBeat never runs backwards.</summary>
        public float LoopBeatOffset => _loopBeatAccum;

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            dspTime = AudioSettings.dspTime;
            if (source == null || !UsesBeatMap)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            var pitch = Mathf.Abs(source.pitch) > 0.0001f ? source.pitch : 1f;
            dspTime = AudioSettings.dspTime + (targetAudioTime - source.time) / pitch;
            return true;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            deltaMs = 0f;
            if (source == null || !UsesBeatMap)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            var pitch = Mathf.Abs(source.pitch) > 0.0001f ? source.pitch : 1f;
            deltaMs = (source.time - targetAudioTime) * 1000f / pitch;
            return true;
        }

        private void Awake()
        {
            EnsureAudioSource();
            EnsureLowPass();
            TryAssignDefaultClip();
            TryLoadBeatMapFromResources();
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
            HandleLoopRegion();
        }

        private void TryLoadBeatMapFromResources()
        {
            if (beatMap != null && beatMap.HasData)
            {
                return;
            }

            beatMap = Resources.Load<MusicBeatMapSO>("Music/EternalSpark_BossRemix_BeatMap");
        }

        private void SyncMusicalBeatFromAudio()
        {
            if (UsesBeatMap)
            {
                _totalMusicalBeat = beatMap.TimeToMusicalBeat(source.time) + _loopBeatAccum;
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
            EnsureLowPass();
            StopAllCoroutines();
            _duckRoutine = null;
            _loopJumpRoutine = null;

            _totalMusicalBeat = 0f;
            _loopBeatAccum = 0f;
            _loopCount = 0;
            _playing = true;
            _ducked = false;
            _mixVolume = 1f;
            _loopFade = 1f;

            source.clip = bossTrack;
            source.loop = false;
            source.time = 0f;
            source.spatialBlend = 0f;
            source.pitch = _playbackSpeedMultiplier;
            ApplyVolume();
            ApplyCutoff(OpenCutoffHz);
            source.Play();

            if (!source.isPlaying)
            {
                Debug.LogError("[CombatMusic] AudioSource.Play() failed. Check AudioListener on Main Camera.");
                _playing = false;
                return;
            }

            var syncMode = UsesBeatMap
                ? $"{beatMap.Bpm} BPM, first beat {beatMap.FirstBeatOffsetSec:F3}s, {beatMap.TotalBeatsForClip()} beats"
                : $"{fallbackBpm} BPM (no beat map)";
            Debug.Log($"[CombatMusic] Playing '{bossTrack.name}' ({bossTrack.length:F1}s) sync={syncMode}.");
        }

        public void StopMusic()
        {
            _playing = false;
            _ducked = false;
            StopAllCoroutines();
            _duckRoutine = null;
            _loopJumpRoutine = null;

            if (source != null)
            {
                source.Stop();
            }

            _mixVolume = 1f;
            _loopFade = 1f;
            ApplyCutoff(OpenCutoffHz);
        }

        /// <summary>Planning window: soften the boss track without ever pausing it.</summary>
        public void EnterPlanningDuck()
        {
            if (_ducked)
            {
                return;
            }

            _ducked = true;
            StartDuckFade(duckVolume, duckCutoffHz);
        }

        public void ExitPlanningDuck()
        {
            if (!_ducked)
            {
                return;
            }

            _ducked = false;
            StartDuckFade(1f, OpenCutoffHz);
        }

        private void StartDuckFade(float targetVolume, float targetCutoff)
        {
            if (!isActiveAndEnabled)
            {
                _mixVolume = targetVolume;
                ApplyVolume();
                ApplyCutoff(targetCutoff);
                return;
            }

            if (_duckRoutine != null)
            {
                StopCoroutine(_duckRoutine);
            }

            _duckRoutine = StartCoroutine(DuckFadeRoutine(targetVolume, targetCutoff));
        }

        private IEnumerator DuckFadeRoutine(float targetVolume, float targetCutoff)
        {
            EnsureLowPass();
            var startVolume = _mixVolume;
            var startCutoff = _lowPass != null ? _lowPass.cutoffFrequency : OpenCutoffHz;
            var duration = Mathf.Max(0.01f, duckFadeSec);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                _mixVolume = Mathf.Lerp(startVolume, targetVolume, t);
                ApplyVolume();
                ApplyCutoff(Mathf.Lerp(startCutoff, targetCutoff, t));
                yield return null;
            }

            _mixVolume = targetVolume;
            ApplyVolume();
            ApplyCutoff(targetCutoff);
            _duckRoutine = null;
        }

        private int ResolveLoopEndBar()
        {
            if (loopEndBar >= 0)
            {
                return loopEndBar;
            }

            if (!UsesBeatMap || bossTrack == null)
            {
                return -1;
            }

            return (beatMap.TotalBeatsForClip() - 1) / MusicBeatMapSO.BeatsPerBar;
        }

        private void HandleLoopRegion()
        {
            if (!UsesBeatMap || _loopJumpRoutine != null)
            {
                return;
            }

            var endBar = ResolveLoopEndBar();
            if (endBar <= loopStartBar)
            {
                return;
            }

            var endBeat = endBar * MusicBeatMapSO.BeatsPerBar;
            if (source.time < beatMap.MusicalBeatToTime(endBeat) - loopDetectLeadSec)
            {
                return;
            }

            var startBeat = loopStartBar * MusicBeatMapSO.BeatsPerBar;
            _loopBeatAccum += endBeat - startBeat;
            _loopCount++;
            _loopJumpRoutine = StartCoroutine(LoopJumpRoutine(beatMap.MusicalBeatToTime(startBeat)));
        }

        private IEnumerator LoopJumpRoutine(float targetTime)
        {
            var half = Mathf.Max(0.005f, loopFadeSec * 0.5f);

            for (var elapsed = 0f; elapsed < half; elapsed += Time.unscaledDeltaTime)
            {
                _loopFade = 1f - Mathf.Clamp01(elapsed / half);
                ApplyVolume();
                yield return null;
            }

            _loopFade = 0f;
            ApplyVolume();
            source.time = Mathf.Clamp(targetTime, 0f, Mathf.Max(0f, bossTrack.length - 0.01f));

            for (var elapsed = 0f; elapsed < half; elapsed += Time.unscaledDeltaTime)
            {
                _loopFade = Mathf.Clamp01(elapsed / half);
                ApplyVolume();
                yield return null;
            }

            _loopFade = 1f;
            ApplyVolume();
            _loopJumpRoutine = null;
        }

        private void ApplyVolume()
        {
            if (source != null)
            {
                source.volume = Mathf.Clamp01(_mixVolume * _loopFade);
            }
        }

        private void ApplyCutoff(float hz)
        {
            if (_lowPass != null)
            {
                _lowPass.cutoffFrequency = hz;
            }
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

        private void EnsureLowPass()
        {
            if (_lowPass != null || source == null)
            {
                return;
            }

            _lowPass = source.GetComponent<AudioLowPassFilter>();
            if (_lowPass == null)
            {
                _lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
            }

            _lowPass.cutoffFrequency = OpenCutoffHz;
            _lowPass.lowpassResonanceQ = 1f;
        }

        private void TryAssignDefaultClip()
        {
#if UNITY_EDITOR
            if (bossTrack == null)
            {
                bossTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix.mp3");
            }

            if (beatMap == null)
            {
                beatMap = UnityEditor.AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix_BeatMap.asset");
            }
#endif
        }
    }
}
