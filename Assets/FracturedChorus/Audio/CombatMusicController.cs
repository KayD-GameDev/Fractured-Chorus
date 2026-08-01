using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip bossTrack;
        [SerializeField] private MusicBeatMapSO beatMap;
        [SerializeField] private float bpm = 152f;
        [SerializeField] private float introEndSec = 24f;
        [SerializeField] private float loopEndSec = 122f;
        [SerializeField] private float firstPassEndSec = 244.8f;
        [SerializeField] private float crossfadeSec = 0.05f;
        [SerializeField] private float loopDetectLeadSec = 0.05f;

        [Header("Planning BGM")]
        [SerializeField] private AudioSource planningSource;
        [SerializeField] private AudioClip planningClip;
        [SerializeField] private float planningStartSec = 17f;
        [SerializeField] private float planningVolume = 0.25f;

        [Header("Planning Transition SFX")]
        [SerializeField] private AudioSource transitionSource;
        [SerializeField] private AudioClip planningTransitionClip;
        [SerializeField] private float planningTransitionVolume = 1f;

        [Header("Ren Cover")]
        [SerializeField] private AudioSource coverSource;
        [SerializeField] private AudioClip coverClip;
        [SerializeField] private float coverStartSec = 96.5f;
        [SerializeField] private float coverVolume = 1f;
        [SerializeField] private float coverBossDuckVolume = 0.2f;

        private float _totalMusicalBeat;
        private int _loopCount;
        private bool _inLoopBody;
        private bool _playing;
        private Coroutine _crossfadeRoutine;
        private Coroutine _planningEnterRoutine;
        private float _playbackSpeedMultiplier = 1f;
        private bool _planningMusicActive;
        private float _planningResumeTimeSec = -1f;
        private bool _coverMusicActive;
        private float _bossVolumeBeforeCover = 1f;

        public MusicBeatMapSO BeatMap => beatMap;
        public bool IsCoverMusicActive => _coverMusicActive;
        public float TotalMusicalBeat => _totalMusicalBeat;
        public float PlaybackSpeedMultiplier => _playbackSpeedMultiplier;
        public float BeatDuration => 60f / bpm;
        public bool IsPlaying => _playing && source != null && source.isPlaying;
        public bool UsesBeatMap => beatMap != null && beatMap.HasData;
        public int LoopCount => _loopCount;
        public float SourceTimeSec => source != null ? source.time : 0f;

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            dspTime = AudioSettings.dspTime;
            if (source == null || !UsesBeatMap)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat);
            var pitch = Mathf.Abs(source.pitch) > 0.0001f ? source.pitch : 1f;
            var deltaSec = (targetAudioTime - source.time) / pitch;
            dspTime = AudioSettings.dspTime + deltaSec;
            return true;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            deltaMs = 0f;
            if (source == null || !UsesBeatMap)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat);
            var pitch = Mathf.Abs(source.pitch) > 0.0001f ? source.pitch : 1f;
            deltaMs = (source.time - targetAudioTime) * 1000f / pitch;
            return true;
        }

        private void Awake()
        {
            EnsureAudioSource();
            TryAssignDefaultClip();
            TryLoadBeatMapFromResources();
            PreloadPlanningTransition();
        }

        private void TryLoadBeatMapFromResources()
        {
            if (beatMap != null && beatMap.HasData)
            {
                return;
            }

            beatMap = Resources.Load<MusicBeatMapSO>("Music/EternalSpark_BossRemix_BeatMap");
        }

        private void OnValidate()
        {
            EnsureAudioSource();
        }

        private void Update()
        {
            if (_planningMusicActive && planningSource != null && planningSource.isPlaying)
            {
                HandlePlanningLoopRegion();
            }

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
            CancelPlanningEnterRoutine();
            StopAllCoroutines();
            _crossfadeRoutine = null;

            _totalMusicalBeat = 0f;
            _loopCount = 0;
            _inLoopBody = false;
            _playing = true;
            _planningResumeTimeSec = -1f;

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
            CancelPlanningEnterRoutine();
            StopAllCoroutines();
            _crossfadeRoutine = null;

            if (source != null)
            {
                source.Stop();
            }

            StopPlanningMusic();
            StopRenCoverMusic();
        }

        /// <summary>Overlay Ren Cover từ coverStartSec (1:36.5); duck boss, không đụng beat sync.</summary>
        public void PlayRenCoverMusic()
        {
            if (coverClip == null)
            {
                TryAssignDefaultClip();
            }

            if (coverClip == null)
            {
                Debug.LogWarning("[CombatMusic] No Ren cover clip assigned.");
                return;
            }

            EnsureCoverSource();
            StopPlanningMusic();

            if (!_coverMusicActive && source != null)
            {
                _bossVolumeBeforeCover = source.volume;
                source.volume = Mathf.Clamp01(coverBossDuckVolume);
            }

            try
            {
                coverSource.clip = coverClip;
                coverSource.loop = false;
                coverSource.volume = coverVolume;
                coverSource.spatialBlend = 0f;
                var start = Mathf.Clamp(coverStartSec, 0f, Mathf.Max(0f, coverClip.length - 0.01f));
                coverSource.Play();
                if (coverSource.isPlaying)
                {
                    coverSource.time = start;
                }

                _coverMusicActive = true;
                Debug.Log(
                    $"[CombatMusic] Ren Cover '{coverClip.name}' from {start:F1}s (len {coverClip.length:F1}s).");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CombatMusic] Failed to play Ren Cover: " + e);
                StopRenCoverMusic();
            }
        }

        public void StopRenCoverMusic()
        {
            if (!_coverMusicActive && (coverSource == null || !coverSource.isPlaying))
            {
                _coverMusicActive = false;
                return;
            }

            _coverMusicActive = false;
            if (coverSource != null && coverSource.isPlaying)
            {
                coverSource.Stop();
            }

            if (source != null)
            {
                source.volume = _bossVolumeBeforeCover > 0f ? _bossVolumeBeforeCover : 1f;
            }
        }

        /// <summary>Đang phát nhưng bị tạm dừng (giữ nguyên vị trí bài) — dùng cho intro-pause planning.</summary>
        public bool IsPaused => _playing && source != null && !source.isPlaying;

        /// <summary>Vào planning: pause boss → transition SFX → silent BGM @17s.</summary>
        public void EnterPlanningPhase()
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }

            StopPlanningMusic();
            CancelPlanningEnterRoutine();
            _planningEnterRoutine = StartCoroutine(EnterPlanningPhaseRoutine());
        }

        private IEnumerator EnterPlanningPhaseRoutine()
        {
            if (planningTransitionClip != null)
            {
                PlayPlanningTransitionSound();
                while (transitionSource != null && transitionSource.isPlaying)
                {
                    yield return null;
                }
            }

            PlayPlanningMusic(forceRestart: true);
            _planningEnterRoutine = null;
        }

        private void CancelPlanningEnterRoutine()
        {
            if (_planningEnterRoutine == null)
            {
                return;
            }

            StopCoroutine(_planningEnterRoutine);
            _planningEnterRoutine = null;
        }

        /// <summary>Tạm dừng nhạc tại chỗ (không reset). Beat nhạc đóng băng theo source.time.</summary>
        public void PausePlayback()
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }

            PlayPlanningMusic();
        }

        /// <summary>Pause track only — no planning stinger (tutorial scan freeze).</summary>
        public void PauseTrackOnly()
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }
        }

        /// <summary>Phát tiếp từ chỗ đã pause.</summary>
        public void ResumePlayback()
        {
            CancelPlanningEnterRoutine();
            if (transitionSource != null && transitionSource.isPlaying)
            {
                transitionSource.Stop();
            }

            StopPlanningMusic();
            if (_playing && source != null && !source.isPlaying)
            {
                source.UnPause();
            }
        }

        /// <summary>Planning BGM lần đầu bắt đầu @17s; các lần planning sau tiếp tục từ vị trí đã dừng lần trước.</summary>
        public void PlayPlanningMusic(bool forceRestart = false)
        {
            if (planningClip == null)
            {
                return;
            }

            EnsurePlanningSource();
            if (!forceRestart && _planningMusicActive && planningSource != null && planningSource.isPlaying)
            {
                return;
            }

            var startSec = _planningResumeTimeSec >= 0f ? _planningResumeTimeSec : planningStartSec;
            planningSource.clip = planningClip;
            planningSource.loop = false;
            planningSource.volume = planningVolume;
            planningSource.time = Mathf.Clamp(startSec, 0f, Mathf.Max(0f, planningClip.length - 0.01f));
            planningSource.Play();
            _planningMusicActive = true;
        }

        public void StopPlanningMusic()
        {
            if (_planningMusicActive && planningSource != null)
            {
                _planningResumeTimeSec = planningSource.time;
            }

            _planningMusicActive = false;
            if (planningSource != null && planningSource.isPlaying)
            {
                planningSource.Stop();
            }
        }

        public void PlayPlanningTransitionSound()
        {
            if (planningTransitionClip == null)
            {
                return;
            }

            EnsureTransitionSource();
            transitionSource.Stop();
            transitionSource.clip = planningTransitionClip;
            transitionSource.volume = planningTransitionVolume;
            transitionSource.time = 0f;
            transitionSource.Play();
        }

        private void HandlePlanningLoopRegion()
        {
            if (planningClip == null || planningSource == null)
            {
                return;
            }

            var loopEnd = planningClip.length - 0.02f;
            if (planningSource.time >= loopEnd)
            {
                planningSource.time = Mathf.Clamp(planningStartSec, 0f, loopEnd);
            }
        }

        private void PreloadPlanningTransition()
        {
            if (planningTransitionClip == null)
            {
                return;
            }

            EnsureTransitionSource();
            transitionSource.clip = planningTransitionClip;
            transitionSource.volume = 0f;
            transitionSource.Play();
            transitionSource.Stop();
            transitionSource.volume = planningTransitionVolume;
        }

        private void EnsurePlanningSource()
        {
            if (planningSource != null)
            {
                return;
            }

            var go = new GameObject("PlanningBGM");
            go.transform.SetParent(transform, false);
            planningSource = go.AddComponent<AudioSource>();
            planningSource.playOnAwake = false;
            planningSource.loop = true;
            planningSource.spatialBlend = 0f;
            planningSource.bypassReverbZones = true;
            planningSource.priority = 64;
        }

        private void EnsureTransitionSource()
        {
            if (transitionSource != null)
            {
                return;
            }

            var go = new GameObject("TransitionSFX");
            go.transform.SetParent(transform, false);
            transitionSource = go.AddComponent<AudioSource>();
            transitionSource.playOnAwake = false;
            transitionSource.loop = false;
            transitionSource.spatialBlend = 0f;
            transitionSource.bypassReverbZones = true;
            transitionSource.priority = 0;
        }

        private void EnsureCoverSource()
        {
            if (coverSource != null)
            {
                return;
            }

            var go = new GameObject("RenCoverBGM");
            go.transform.SetParent(transform, false);
            coverSource = go.AddComponent<AudioSource>();
            coverSource.playOnAwake = false;
            coverSource.loop = false;
            coverSource.spatialBlend = 0f;
            coverSource.bypassReverbZones = true;
            coverSource.priority = 16;
        }

        /// <summary>TODO: one-shot transition sting when entering the next 2-phase block (asset TBD).</summary>
        public void PlaySegmentTransitionMusic(int segmentIndex)
        {
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
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix.mp3");
            }

            if (beatMap == null)
            {
                beatMap = UnityEditor.AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix_BeatMap.asset");
            }

            if (planningTransitionClip == null)
            {
                planningTransitionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/Combat_PlanningTransition.wav");
            }

            if (coverClip == null)
            {
                coverClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Music/EternalSpark_RenCover.mp3");
            }
#endif
        }
    }
}
