using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public enum RunMusicMode
    {
        Map,
        Combat,
        Planning,
        BossPaused
    }

    public sealed class RunMusicSession : MonoBehaviour
    {
        public static RunMusicSession Instance { get; private set; }

        [SerializeField] private AudioClip candenceClip;
        [SerializeField] private MusicBeatMapSO beatMap;
        [SerializeField] private float mapVolume = 0.4f;
        [SerializeField] private float combatVolume = 1f;
        [SerializeField] private float duckVolume = 0.7f;
        [SerializeField] private float duckCutoffHz = 900f;
        [SerializeField] private float fadeSec = 0.25f;
        [SerializeField] private float loopFadeSec = 0.05f;

        private AudioSource _source;
        private AudioLowPassFilter _lowPass;
        private float _totalMusicalBeat;
        private float _loopBeatAccum;
        private float _pausedMusicalBeat;
        private bool _playing;
        private bool _pausedForBoss;
        private RunMusicMode _mode = RunMusicMode.Map;
        private Coroutine _fadeRoutine;
        private Coroutine _loopRoutine;
        private float _targetVolume = 0.4f;

        public bool IsActive => _playing;
        public MusicBeatMapSO BeatMap => beatMap;
        public float TotalMusicalBeat => _totalMusicalBeat;
        public float BeatDuration => beatMap != null && beatMap.HasData ? beatMap.BeatSpanSec : 60f / 152f;
        public bool IsPlaying => _playing && _source != null && _source.isPlaying;
        public AudioSource Source => _source;

        public static RunMusicSession Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject(nameof(RunMusicSession));
            DontDestroyOnLoad(go);
            return go.AddComponent<RunMusicSession>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudio();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Begin(AudioClip clip = null, MusicBeatMapSO map = null)
        {
            if (_playing && !_pausedForBoss)
            {
                return;
            }

            if (clip != null)
            {
                candenceClip = clip;
            }

            if (map != null)
            {
                beatMap = map;
            }

            TryLoadDefaults();
            if (candenceClip == null || beatMap == null)
            {
                Debug.LogError("[RunMusic] Candence clip or beat map missing.");
                return;
            }

            EnsureAudio();
            _playing = true;
            _pausedForBoss = false;
            _loopBeatAccum = 0f;
            _totalMusicalBeat = 0f;
            _source.clip = candenceClip;
            _source.time = 0f;
            _source.loop = false;
            _source.pitch = 1f;
            _source.Play();
            SetMode(RunMusicMode.Map, immediate: true);
        }

        public void Stop()
        {
            _playing = false;
            _pausedForBoss = false;
            StopAllCoroutines();
            _fadeRoutine = null;
            _loopRoutine = null;
            if (_source != null)
            {
                _source.Stop();
            }
        }

        public void SetMode(RunMusicMode mode, bool immediate = false)
        {
            _mode = mode;
            if (!_playing || _source == null)
            {
                return;
            }

            if (mode == RunMusicMode.BossPaused)
            {
                SyncBeat();
                _source.Pause();
                return;
            }

            if (!_source.isPlaying)
            {
                _source.UnPause();
            }

            var targetVol = mode switch
            {
                RunMusicMode.Map => mapVolume,
                RunMusicMode.Combat => combatVolume,
                RunMusicMode.Planning => duckVolume,
                _ => mapVolume
            };
            var targetCutoff = mode == RunMusicMode.Planning ? duckCutoffHz : 22000f;
            StartFade(targetVol, targetCutoff, immediate);
        }

        public void PauseForBoss()
        {
            if (!_playing)
            {
                return;
            }

            SyncBeat();
            _pausedMusicalBeat = _totalMusicalBeat;
            _pausedForBoss = true;
            SetMode(RunMusicMode.BossPaused, immediate: true);
        }

        public void ResumeFromBoss()
        {
            if (!_playing || beatMap == null || _source == null)
            {
                return;
            }

            _pausedForBoss = false;
            var audioTime = beatMap.MusicalBeatToTime(_pausedMusicalBeat - _loopBeatAccum);
            _source.time = Mathf.Clamp(audioTime, 0f, Mathf.Max(0f, candenceClip.length - 0.01f));
            if (!_source.isPlaying)
            {
                _source.UnPause();
            }

            SetMode(RunMusicMode.Map, immediate: false);
        }

        public void EnterPlanningDuck() => SetMode(RunMusicMode.Planning);

        public void ExitPlanningDuck() => SetMode(RunMusicMode.Combat);

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            dspTime = AudioSettings.dspTime;
            if (_source == null || beatMap == null)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            var pitch = Mathf.Abs(_source.pitch) > 0.0001f ? _source.pitch : 1f;
            dspTime = AudioSettings.dspTime + (targetAudioTime - _source.time) / pitch;
            return true;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            deltaMs = 0f;
            if (_source == null || beatMap == null)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            var pitch = Mathf.Abs(_source.pitch) > 0.0001f ? _source.pitch : 1f;
            deltaMs = (_source.time - targetAudioTime) * 1000f / pitch;
            return true;
        }

        private void Update()
        {
            if (!_playing || _source == null || _pausedForBoss)
            {
                return;
            }

            if (_source.isPlaying)
            {
                SyncBeat();
                TryLoop();
            }
        }

        private void SyncBeat()
        {
            if (beatMap == null || _source == null)
            {
                return;
            }

            _totalMusicalBeat = beatMap.TimeToMusicalBeat(_source.time) + _loopBeatAccum;
        }

        private void TryLoadDefaults()
        {
#if UNITY_EDITOR
            candenceClip ??= UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3");
            beatMap ??= UnityEditor.AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(
                "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset");
#endif
            beatMap ??= Resources.Load<MusicBeatMapSO>("Music/EternalSpark_Candence_BeatMap");
        }

        private void EnsureAudio()
        {
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.loop = false;
                _source.spatialBlend = 0f;
            }

            _lowPass ??= gameObject.GetComponent<AudioLowPassFilter>()
                         ?? gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.cutoffFrequency = 22000f;
        }

        private void StartFade(float targetVolume, float targetCutoff, bool immediate)
        {
            _targetVolume = targetVolume;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(targetVolume, targetCutoff, immediate));
        }

        private IEnumerator FadeRoutine(float targetVolume, float targetCutoff, bool immediate)
        {
            EnsureAudio();
            var startVol = _source.volume;
            var startCutoff = _lowPass.cutoffFrequency;
            if (immediate)
            {
                _source.volume = targetVolume;
                _lowPass.cutoffFrequency = targetCutoff;
                _fadeRoutine = null;
                yield break;
            }

            for (var t = 0f; t < fadeSec; t += Time.unscaledDeltaTime)
            {
                var a = Mathf.Clamp01(t / fadeSec);
                _source.volume = Mathf.Lerp(startVol, targetVolume, a);
                _lowPass.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, a);
                yield return null;
            }

            _source.volume = targetVolume;
            _lowPass.cutoffFrequency = targetCutoff;
            _fadeRoutine = null;
        }

        private void TryLoop()
        {
            if (beatMap == null || candenceClip == null || _loopRoutine != null)
            {
                return;
            }

            var endBar = (beatMap.TotalBeatsForClip() - 1) / MusicBeatMapSO.BeatsPerBar;
            if (endBar <= 0)
            {
                return;
            }

            var endBeat = endBar * MusicBeatMapSO.BeatsPerBar;
            if (_source.time < beatMap.MusicalBeatToTime(endBeat) - 0.05f)
            {
                return;
            }

            var startBeat = 0;
            _loopBeatAccum += endBeat - startBeat;
            _loopRoutine = StartCoroutine(LoopJumpRoutine(beatMap.MusicalBeatToTime(startBeat)));
        }

        private IEnumerator LoopJumpRoutine(float targetTime)
        {
            var half = Mathf.Max(0.005f, loopFadeSec * 0.5f);
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(_targetVolume, 0f, Mathf.Clamp01(t / half));
                yield return null;
            }

            _source.time = Mathf.Clamp(targetTime, 0f, Mathf.Max(0f, candenceClip.length - 0.01f));
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(0f, _targetVolume, Mathf.Clamp01(t / half));
                yield return null;
            }

            _loopRoutine = null;
        }
    }
}
