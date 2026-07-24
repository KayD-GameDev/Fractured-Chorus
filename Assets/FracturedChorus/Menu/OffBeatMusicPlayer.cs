using System;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class OffBeatMusicPlayer : MonoBehaviour
    {
        public enum RepeatMode
        {
            Off = 0,
            All = 1,
            One = 2
        }

        [SerializeField] [Range(0f, 1f)] private float volume = 0.85f;

        private AudioSource _source;
        private float _baseVolume = 0.85f;
        private float _masterVolume = 1f;
        private readonly List<OffBeatTrackSO> _playlist = new List<OffBeatTrackSO>();
        private readonly List<int> _shuffleOrder = new List<int>();
        private int _index;
        private int _shuffleCursor;
        private bool _shuffle;
        private RepeatMode _repeat = RepeatMode.Off;
        private bool _wasPlayingBeforeSeek;

        public event Action TrackChanged;
        public event Action PlaybackStateChanged;

        public AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    _source = GetComponent<AudioSource>();
                }

                return _source;
            }
        }

        public OffBeatTrackSO CurrentTrack =>
            _playlist.Count == 0 || _index < 0 || _index >= _playlist.Count
                ? null
                : _playlist[_index];

        public int CurrentIndex => _index;
        public int TrackCount => _playlist.Count;
        public bool IsPlaying => _source != null && _source.isPlaying;
        public bool IsPaused { get; private set; }
        public bool ShuffleEnabled => _shuffle;
        public RepeatMode Repeat => _repeat;

        public float Time
        {
            get => _source != null ? _source.time : 0f;
            set
            {
                if (_source == null || _source.clip == null)
                {
                    return;
                }

                _source.time = Mathf.Clamp(value, 0f, Mathf.Max(0f, _source.clip.length - 0.01f));
            }
        }

        public float Duration => _source != null && _source.clip != null ? _source.clip.length : 0f;
        public float NormalizedTime => Duration > 0.01f ? Mathf.Clamp01(Time / Duration) : 0f;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _baseVolume = volume;
            ApplyVolume();
        }

        private void Update()
        {
            if (_source == null || _source.clip == null || IsPaused)
            {
                return;
            }

            if (_source.isPlaying)
            {
                return;
            }

            if (_source.time > 0f && _source.time < _source.clip.length - 0.05f)
            {
                return;
            }

            HandleTrackEnded();
        }

        public void SetPlaylist(IReadOnlyList<OffBeatTrackSO> tracks, int startIndex = 0)
        {
            _playlist.Clear();
            if (tracks != null)
            {
                for (var i = 0; i < tracks.Count; i++)
                {
                    if (tracks[i] != null && tracks[i].clip != null)
                    {
                        _playlist.Add(tracks[i]);
                    }
                }
            }

            _index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, _playlist.Count - 1));
            RebuildShuffleOrder(preserveCurrent: false);
            LoadCurrent(autoPlay: false);
        }

        public void SelectIndex(int index, bool autoPlay)
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            _index = Mathf.Clamp(index, 0, _playlist.Count - 1);
            SyncShuffleCursorToIndex();
            LoadCurrent(autoPlay);
        }

        public void Play()
        {
            if (_source == null || CurrentTrack == null)
            {
                return;
            }

            if (_source.clip != CurrentTrack.clip)
            {
                LoadCurrent(autoPlay: true);
                return;
            }

            IsPaused = false;
            if (!_source.isPlaying)
            {
                _source.Play();
            }

            PlaybackStateChanged?.Invoke();
        }

        public void Pause()
        {
            if (_source == null)
            {
                return;
            }

            if (_source.isPlaying)
            {
                _source.Pause();
                IsPaused = true;
                PlaybackStateChanged?.Invoke();
            }
        }

        public void TogglePlayPause()
        {
            if (IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        public void Stop()
        {
            if (_source == null)
            {
                return;
            }

            _source.Stop();
            IsPaused = false;
            PlaybackStateChanged?.Invoke();
        }

        public void Next()
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            if (_shuffle)
            {
                _shuffleCursor = (_shuffleCursor + 1) % _shuffleOrder.Count;
                _index = _shuffleOrder[_shuffleCursor];
            }
            else
            {
                _index = (_index + 1) % _playlist.Count;
            }

            LoadCurrent(autoPlay: true);
        }

        public void Previous()
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            if (Time > 3f)
            {
                Time = 0f;
                if (!IsPlaying)
                {
                    Play();
                }

                return;
            }

            if (_shuffle)
            {
                _shuffleCursor = (_shuffleCursor - 1 + _shuffleOrder.Count) % _shuffleOrder.Count;
                _index = _shuffleOrder[_shuffleCursor];
            }
            else
            {
                _index = (_index - 1 + _playlist.Count) % _playlist.Count;
            }

            LoadCurrent(autoPlay: true);
        }

        public void SetShuffle(bool enabled)
        {
            _shuffle = enabled;
            if (_shuffle)
            {
                RebuildShuffleOrder(preserveCurrent: true);
            }

            PlaybackStateChanged?.Invoke();
        }

        public void ToggleShuffle()
        {
            SetShuffle(!_shuffle);
        }

        public void SetRepeat(RepeatMode mode)
        {
            _repeat = mode;
            PlaybackStateChanged?.Invoke();
        }

        public void CycleRepeat()
        {
            _repeat = (RepeatMode)(((int)_repeat + 1) % 3);
            PlaybackStateChanged?.Invoke();
        }

        public void ToggleRepeat()
        {
            _repeat = _repeat == RepeatMode.Off ? RepeatMode.All : RepeatMode.Off;
            PlaybackStateChanged?.Invoke();
        }

        public void BeginSeek()
        {
            _wasPlayingBeforeSeek = IsPlaying;
            if (_wasPlayingBeforeSeek)
            {
                _source.Pause();
            }
        }

        public void SetNormalizedTime(float normalized)
        {
            if (Duration <= 0.01f)
            {
                return;
            }

            Time = Mathf.Clamp01(normalized) * Duration;
        }

        public void EndSeek()
        {
            if (_wasPlayingBeforeSeek)
            {
                IsPaused = false;
                _source.UnPause();
                if (!_source.isPlaying)
                {
                    _source.Play();
                }
            }

            PlaybackStateChanged?.Invoke();
        }

        public void ApplyMasterVolume(float masterVolume)
        {
            _masterVolume = Mathf.Clamp01(masterVolume);
            ApplyVolume();
        }

        public void SetVolume(float value)
        {
            _baseVolume = Mathf.Clamp01(value);
            volume = _baseVolume;
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (_source != null)
            {
                _source.volume = _baseVolume * _masterVolume;
            }
        }

        private void LoadCurrent(bool autoPlay)
        {
            if (_source == null)
            {
                return;
            }

            var track = CurrentTrack;
            if (track == null)
            {
                _source.Stop();
                _source.clip = null;
                IsPaused = false;
                TrackChanged?.Invoke();
                PlaybackStateChanged?.Invoke();
                return;
            }

            _source.clip = track.clip;
            _source.time = 0f;
            IsPaused = false;
            if (autoPlay)
            {
                _source.Play();
            }
            else
            {
                _source.Stop();
            }

            TrackChanged?.Invoke();
            PlaybackStateChanged?.Invoke();
        }

        private void HandleTrackEnded()
        {
            if (_repeat == RepeatMode.One)
            {
                Time = 0f;
                Play();
                return;
            }

            if (_repeat == RepeatMode.Off)
            {
                var atEnd = (!_shuffle && _index >= _playlist.Count - 1)
                            || (_shuffle && _shuffleCursor >= _shuffleOrder.Count - 1);
                if (atEnd)
                {
                    IsPaused = false;
                    PlaybackStateChanged?.Invoke();
                    return;
                }
            }

            Next();
        }

        private void RebuildShuffleOrder(bool preserveCurrent)
        {
            _shuffleOrder.Clear();
            for (var i = 0; i < _playlist.Count; i++)
            {
                _shuffleOrder.Add(i);
            }

            for (var i = _shuffleOrder.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
            }

            if (preserveCurrent && _playlist.Count > 0)
            {
                SyncShuffleCursorToIndex();
            }
            else
            {
                _shuffleCursor = 0;
                for (var i = 0; i < _shuffleOrder.Count; i++)
                {
                    if (_shuffleOrder[i] == _index)
                    {
                        _shuffleCursor = i;
                        break;
                    }
                }
            }
        }

        private void SyncShuffleCursorToIndex()
        {
            _shuffleCursor = 0;
            for (var i = 0; i < _shuffleOrder.Count; i++)
            {
                if (_shuffleOrder[i] == _index)
                {
                    _shuffleCursor = i;
                    return;
                }
            }
        }
    }
}
