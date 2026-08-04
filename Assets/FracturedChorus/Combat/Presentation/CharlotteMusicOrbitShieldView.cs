using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CharlotteMusicOrbitShieldSettings
    {
        public Sprite[] WaveFrames;
        public Sprite[] NoteSprites;
        public Material AdditiveMaterial;
        public float WorldSize = 1.6f;
        public float OrbitRadius = 1.1f;
        public int WaveCount = 6;
        public int NoteCount = 8;
        public float WaveFps = 12f;
        public float HoldSeconds = 1.18f;
        public float FadeSeconds = 0.2f;
        public int SortingOrder = 42;
        public Color Tint = new Color(1f, 0.85f, 0.3f, 1f);
    }

    public class CharlotteMusicOrbitShieldView : MonoBehaviour
    {
        private static readonly List<CharlotteMusicOrbitShieldView> Active = new();

        private CharlotteMusicOrbitShieldSettings _settings;
        private Transform _follow;
        private Vector3 _bodyOffset;
        private SpriteRenderer _halo;
        private SpriteRenderer[] _waves;
        private SpriteRenderer[] _notes;
        private float[] _waveAngles;
        private float[] _waveRadii;
        private float[] _waveSpin;
        private float[] _noteAngles;
        private float[] _noteRadii;
        private float[] _noteBob;
        private float _pulse;
        private float _animTime;
        private bool _hiddenForEncounter;
        private bool _fadingOut;

        public static CharlotteMusicOrbitShieldView Spawn(
            Transform follow,
            Vector3 bodyCenter,
            CharlotteMusicOrbitShieldSettings settings,
            Transform parent = null)
        {
            if (settings == null || follow == null)
            {
                return null;
            }

            if ((settings.WaveFrames == null || settings.WaveFrames.Length == 0)
                && (settings.NoteSprites == null || settings.NoteSprites.Length == 0))
            {
                return null;
            }

            var go = new GameObject("CharlotteMusicOrbitShield");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<CharlotteMusicOrbitShieldView>();
            view._settings = settings;
            view._follow = follow;
            view._bodyOffset = bodyCenter - follow.position;
            view.transform.position = bodyCenter;
            view.Build();
            Active.Add(view);
            if (EncounterDirector.IsPresenting)
            {
                view.ApplyEncounterHidden(true);
            }

            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        public static void SetEncounterHidden(bool hidden)
        {
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var shield = Active[i];
                if (shield == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }

                shield.ApplyEncounterHidden(hidden);
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        private void Build()
        {
            var haloSprite = PickWave(0) ?? PickNote(0);
            if (haloSprite != null)
            {
                _halo = CreateRenderer("Halo", haloSprite, _settings.SortingOrder);
                Fit(_halo, _settings.WorldSize * 0.85f);
                var t = _settings.Tint;
                _halo.color = new Color(t.r, t.g, t.b, 0f);
            }

            var waveCount = Mathf.Clamp(_settings.WaveCount, 0, 12);
            _waves = new SpriteRenderer[waveCount];
            _waveAngles = new float[waveCount];
            _waveRadii = new float[waveCount];
            _waveSpin = new float[waveCount];
            for (var i = 0; i < waveCount; i++)
            {
                var sprite = PickWave(i);
                _waves[i] = CreateRenderer("Wave" + i, sprite, _settings.SortingOrder + 1);
                _waveAngles[i] = (i / (float)Mathf.Max(1, waveCount)) * Mathf.PI * 2f;
                _waveRadii[i] = _settings.OrbitRadius * (i % 2 == 0 ? 1f : 0.78f);
                _waveSpin[i] = (i % 2 == 0 ? 1f : -1f) * (70f + i * 8f);
                if (_waves[i] != null)
                {
                    Fit(_waves[i], _settings.WorldSize * 0.42f);
                    var t = _settings.Tint;
                    _waves[i].color = new Color(t.r, t.g, t.b, 0f);
                }
            }

            var noteCount = Mathf.Clamp(_settings.NoteCount, 0, 12);
            _notes = new SpriteRenderer[noteCount];
            _noteAngles = new float[noteCount];
            _noteRadii = new float[noteCount];
            _noteBob = new float[noteCount];
            for (var i = 0; i < noteCount; i++)
            {
                var sprite = PickNote(i);
                _notes[i] = CreateRenderer("Note" + i, sprite, _settings.SortingOrder + 2);
                _noteAngles[i] = (i / (float)Mathf.Max(1, noteCount)) * Mathf.PI * 2f + 0.35f;
                _noteRadii[i] = _settings.OrbitRadius * (0.75f + (i % 3) * 0.12f);
                _noteBob[i] = i * 0.7f;
                if (_notes[i] != null)
                {
                    Fit(_notes[i], _settings.WorldSize * 0.18f);
                    var t = _settings.Tint;
                    _notes[i].color = new Color(1f, t.g, t.b * 0.7f, 0f);
                }
            }
        }

        private IEnumerator PlayRoutine()
        {
            const float rise = 0.14f;
            var elapsed = 0f;
            while (elapsed < rise)
            {
                elapsed += Time.deltaTime;
                if (!_hiddenForEncounter)
                {
                    SetAlpha(Mathf.Clamp01(elapsed / rise));
                }

                yield return null;
            }

            SetAlpha(1f);

            elapsed = 0f;
            var hold = Mathf.Max(0.05f, _settings.HoldSeconds);
            while (elapsed < hold)
            {
                if (!_hiddenForEncounter)
                {
                    elapsed += Time.deltaTime;
                }

                yield return null;
            }

            _fadingOut = true;
            elapsed = 0f;
            var fade = Mathf.Max(0.05f, _settings.FadeSeconds);
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                if (!_hiddenForEncounter)
                {
                    SetAlpha(1f - Mathf.Clamp01(elapsed / fade));
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = _follow.position + _bodyOffset;
            }

            if (_hiddenForEncounter || _fadingOut)
            {
                return;
            }

            var dt = Time.deltaTime;
            _pulse += dt * 5.2f;
            _animTime += dt;
            var pulse = 1f + Mathf.Sin(_pulse * 1.4f) * 0.08f;

            if (_halo != null)
            {
                Fit(_halo, _settings.WorldSize * 0.85f * pulse);
                _halo.transform.localRotation = Quaternion.Euler(0f, 0f, -_pulse * 35f);
            }

            AdvanceWaveFrames();

            if (_waves != null)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    var wave = _waves[i];
                    if (wave == null)
                    {
                        continue;
                    }

                    _waveAngles[i] += dt * (_waveSpin[i] * Mathf.Deg2Rad);
                    var r = _waveRadii[i] * pulse;
                    wave.transform.localPosition = new Vector3(
                        Mathf.Cos(_waveAngles[i]) * r,
                        Mathf.Sin(_waveAngles[i]) * r * 0.55f,
                        0f);
                    wave.transform.localRotation = Quaternion.Euler(0f, 0f, _waveAngles[i] * Mathf.Rad2Deg + 90f);
                    Fit(wave, _settings.WorldSize * 0.42f * pulse);
                }
            }

            if (_notes != null)
            {
                for (var i = 0; i < _notes.Length; i++)
                {
                    var note = _notes[i];
                    if (note == null)
                    {
                        continue;
                    }

                    _noteAngles[i] += dt * 1.8f * (i % 2 == 0 ? 1f : -1.15f);
                    _noteBob[i] += dt * 5f;
                    var r = _noteRadii[i] * (0.95f + 0.08f * Mathf.Sin(_noteBob[i]));
                    var bobY = Mathf.Sin(_noteBob[i]) * 0.08f;
                    note.transform.localPosition = new Vector3(
                        Mathf.Cos(_noteAngles[i]) * r,
                        Mathf.Sin(_noteAngles[i]) * r * 0.6f + bobY,
                        0f);
                    note.transform.localRotation = Quaternion.Euler(0f, 0f, _noteAngles[i] * Mathf.Rad2Deg);
                    Fit(note, _settings.WorldSize * 0.18f);
                }
            }
        }

        private void AdvanceWaveFrames()
        {
            if (_settings.WaveFrames == null || _settings.WaveFrames.Length == 0 || _waves == null)
            {
                return;
            }

            var index = Mathf.FloorToInt(_animTime * Mathf.Max(1f, _settings.WaveFps))
                        % _settings.WaveFrames.Length;
            if (index < 0)
            {
                index += _settings.WaveFrames.Length;
            }

            var frame = _settings.WaveFrames[index];
            if (frame == null)
            {
                return;
            }

            for (var i = 0; i < _waves.Length; i++)
            {
                if (_waves[i] != null && _waves[i].sprite != frame)
                {
                    _waves[i].sprite = frame;
                }
            }

            if (_halo != null && _halo.sprite != frame)
            {
                _halo.sprite = frame;
            }
        }

        private void SetAlpha(float a)
        {
            a = Mathf.Clamp01(a);
            if (_halo != null)
            {
                var t = _settings.Tint;
                _halo.color = new Color(t.r, t.g, t.b, 0.55f * a);
            }

            if (_waves != null)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    if (_waves[i] == null)
                    {
                        continue;
                    }

                    var t = _settings.Tint;
                    _waves[i].color = new Color(t.r, t.g, t.b, 0.85f * a);
                }
            }

            if (_notes != null)
            {
                for (var i = 0; i < _notes.Length; i++)
                {
                    if (_notes[i] == null)
                    {
                        continue;
                    }

                    var t = _settings.Tint;
                    _notes[i].color = new Color(1f, t.g, t.b * 0.7f, 0.9f * a);
                }
            }
        }

        private void ApplyEncounterHidden(bool hidden)
        {
            _hiddenForEncounter = hidden;
            SetRenderersEnabled(!hidden);
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_halo != null)
            {
                _halo.enabled = enabled;
            }

            if (_waves != null)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    if (_waves[i] != null)
                    {
                        _waves[i].enabled = enabled;
                    }
                }
            }

            if (_notes != null)
            {
                for (var i = 0; i < _notes.Length; i++)
                {
                    if (_notes[i] != null)
                    {
                        _notes[i].enabled = enabled;
                    }
                }
            }
        }

        private Sprite PickWave(int index)
        {
            if (_settings.WaveFrames == null || _settings.WaveFrames.Length == 0)
            {
                return null;
            }

            return _settings.WaveFrames[index % _settings.WaveFrames.Length];
        }

        private Sprite PickNote(int index)
        {
            if (_settings.NoteSprites == null || _settings.NoteSprites.Length == 0)
            {
                return PickWave(index);
            }

            return _settings.NoteSprites[index % _settings.NoteSprites.Length];
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            if (sprite == null)
            {
                return null;
            }

            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Color.white;
            if (_settings.AdditiveMaterial != null)
            {
                sr.sharedMaterial = _settings.AdditiveMaterial;
            }

            return sr;
        }

        private static void Fit(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? worldSize / native : 1f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
