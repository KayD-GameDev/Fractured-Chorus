using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class RenRedMusicAuraSettings
    {
        public Sprite Glow;
        public Sprite Waveform;
        public Sprite[] WaveVariants;
        public Sprite Notes;
        public Sprite[] NoteVariants;
        public Material AdditiveMaterial;
        public float WorldSize = 3.4f;
        public float OrbitRadius = 1.45f;
        public int WaveCount = 12;
        public int NoteCount = 10;
        public int SortingOrder = 35;
        public float FadeOutSeconds = 0.16f;
        public Color Tint = new Color(1f, 0.16f, 0.24f, 1f);
    }

    public class RenRedMusicAuraView : MonoBehaviour
    {
        private RenRedMusicAuraSettings _settings;
        private SpriteRenderer _glow;
        private SpriteRenderer _innerRing;
        private SpriteRenderer[] _waves;
        private SpriteRenderer[] _notes;
        private float[] _waveAngles;
        private float[] _waveRadii;
        private float[] _waveSpin;
        private float[] _wavePhase;
        private float[] _noteAngles;
        private float[] _noteBob;
        private float[] _noteRadii;
        private Vector3[] _waveStartLocal;
        private Vector3[] _noteStartLocal;
        private Color[] _waveBaseColor;
        private Color[] _noteBaseColor;
        private Color _glowBaseColor;
        private Transform _follow;
        private Vector3 _bodyOffset;
        private int _waveCount;
        private int _noteCount;
        private bool _orbiting = true;
        private bool _stopping;
        private float _pulse;
        private float _convergeT = -1f;
        private Vector3 _muzzleLocal;

        public static RenRedMusicAuraView Spawn(
            Transform follow,
            Vector3 bodyCenter,
            RenRedMusicAuraSettings settings,
            Transform parent = null)
        {
            if (settings?.Glow == null
                && settings?.Waveform == null
                && (settings?.WaveVariants == null || settings.WaveVariants.Length == 0))
            {
                return null;
            }

            var go = new GameObject("RenEerieMusicAura");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<RenRedMusicAuraView>();
            view._settings = settings;
            view._follow = follow;
            view._bodyOffset = follow != null ? bodyCenter - follow.position : Vector3.zero;
            view.transform.position = bodyCenter;
            view.Build();
            return view;
        }

        public IEnumerator PlayOrbitThenConverge(Vector3 muzzleWorld, float orbitSeconds, float convergeSeconds)
        {
            _orbiting = true;
            _convergeT = -1f;
            if (orbitSeconds > 0f)
            {
                yield return new WaitForSeconds(orbitSeconds);
            }

            CaptureStartLocals();
            _muzzleLocal = transform.InverseTransformPoint(muzzleWorld);
            _orbiting = false;
            _convergeT = 0f;
            var converge = Mathf.Max(0.05f, convergeSeconds);
            while (_convergeT < 1f)
            {
                _convergeT += Time.deltaTime / converge;
                yield return null;
            }

            _convergeT = 1f;
        }

        public void StopAndDestroy()
        {
            if (_stopping)
            {
                return;
            }

            _stopping = true;
            StartCoroutine(FadeOutRoutine());
        }

        private void Build()
        {
            if (_settings.Glow != null)
            {
                _glow = CreateRenderer("Glow", _settings.Glow, _settings.SortingOrder);
                FitSprite(_glow, _settings.WorldSize);
                _glowBaseColor = _settings.Tint;
                if (_glow != null)
                {
                    _glow.color = _glowBaseColor;
                }

                var ringSprite = PickWaveSprite(0) ?? _settings.Glow;
                _innerRing = CreateRenderer("InnerRing", ringSprite, _settings.SortingOrder + 1);
                if (_innerRing != null)
                {
                    FitSprite(_innerRing, _settings.WorldSize * 0.72f);
                    var c = _settings.Tint;
                    _innerRing.color = new Color(c.r, c.g * 0.2f, c.b * 0.35f, 0.55f);
                }
            }

            _waveCount = Mathf.Clamp(_settings.WaveCount, 0, 18);
            _waves = new SpriteRenderer[_waveCount];
            _waveAngles = new float[_waveCount];
            _waveRadii = new float[_waveCount];
            _waveSpin = new float[_waveCount];
            _wavePhase = new float[_waveCount];
            _waveStartLocal = new Vector3[_waveCount];
            _waveBaseColor = new Color[_waveCount];
            for (var i = 0; i < _waveCount; i++)
            {
                var sprite = PickWaveSprite(i);
                _waves[i] = CreateRenderer("Wave" + i, sprite, _settings.SortingOrder + 2);
                var ring = i % 2 == 0 ? 1f : 0.72f;
                _waveAngles[i] = (i / (float)Mathf.Max(1, _waveCount)) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                _waveRadii[i] = _settings.OrbitRadius * ring * Random.Range(0.85f, 1.25f);
                _waveSpin[i] = Random.Range(-260f, 260f);
                _wavePhase[i] = Random.Range(0f, Mathf.PI * 2f);
                if (_waves[i] != null)
                {
                    var longWave = sprite != null && sprite.rect.width > sprite.rect.height * 1.35f;
                    FitSprite(_waves[i], _settings.WorldSize * (longWave ? 0.62f : 0.42f));
                    var c = _settings.Tint;
                    _waveBaseColor[i] = new Color(c.r, c.g * 0.35f, c.b * 0.5f, 0.82f);
                    _waves[i].color = _waveBaseColor[i];
                }
            }

            _noteCount = Mathf.Clamp(_settings.NoteCount, 0, 14);
            _notes = new SpriteRenderer[_noteCount];
            _noteAngles = new float[_noteCount];
            _noteBob = new float[_noteCount];
            _noteRadii = new float[_noteCount];
            _noteStartLocal = new Vector3[_noteCount];
            _noteBaseColor = new Color[_noteCount];
            for (var i = 0; i < _noteCount; i++)
            {
                var sprite = PickNoteSprite(i);
                _notes[i] = CreateRenderer("Note" + i, sprite, _settings.SortingOrder + 3);
                _noteAngles[i] = (i / (float)Mathf.Max(1, _noteCount)) * Mathf.PI * 2f + 0.4f;
                _noteBob[i] = Random.Range(0f, Mathf.PI * 2f);
                _noteRadii[i] = _settings.OrbitRadius * Random.Range(0.7f, 1.2f);
                if (_notes[i] != null)
                {
                    FitSprite(_notes[i], _settings.WorldSize * 0.2f);
                    var c = _settings.Tint;
                    _noteBaseColor[i] = new Color(1f, c.g * 0.28f, c.b * 0.38f, 0.9f);
                    _notes[i].color = _noteBaseColor[i];
                }
            }
        }

        private Sprite PickWaveSprite(int index)
        {
            if (_settings.WaveVariants != null && _settings.WaveVariants.Length > 0)
            {
                return _settings.WaveVariants[index % _settings.WaveVariants.Length];
            }

            return _settings.Waveform != null ? _settings.Waveform : _settings.Notes;
        }

        private Sprite PickNoteSprite(int index)
        {
            if (_settings.NoteVariants != null && _settings.NoteVariants.Length > 0)
            {
                return _settings.NoteVariants[index % _settings.NoteVariants.Length];
            }

            return _settings.Notes;
        }

        private void CaptureStartLocals()
        {
            for (var i = 0; i < _waveCount; i++)
            {
                if (_waves[i] != null)
                {
                    _waveStartLocal[i] = _waves[i].transform.localPosition;
                }
            }

            for (var i = 0; i < _noteCount; i++)
            {
                if (_notes[i] != null)
                {
                    _noteStartLocal[i] = _notes[i].transform.localPosition;
                }
            }
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

        private static void FitSprite(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? worldSize / native : 1f;
            sr.transform.localScale = Vector3.one * scale;
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = _follow.position + _bodyOffset;
            }

            _pulse += Time.deltaTime * 4.6f;
            var pulse = 1f + Mathf.Sin(_pulse * 1.35f) * 0.14f + Mathf.Sin(_pulse * 3.4f) * 0.05f;

            if (_glow != null)
            {
                FitSprite(_glow, _settings.WorldSize * pulse * 1.08f);
                var flicker = 0.5f + 0.4f * Mathf.Abs(Mathf.Sin(_pulse * 2.7f));
                var c = _glowBaseColor;
                _glow.color = new Color(c.r, c.g * 0.22f, c.b * 0.28f, flicker);
                _glow.transform.localRotation = Quaternion.Euler(0f, 0f, _pulse * 22f);
            }

            if (_innerRing != null && _orbiting)
            {
                FitSprite(_innerRing, _settings.WorldSize * (0.55f + 0.12f * pulse));
                _innerRing.transform.localRotation = Quaternion.Euler(0f, 0f, -_pulse * 48f);
                var c = _innerRing.color;
                _innerRing.color = new Color(c.r, c.g, c.b, 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(_pulse * 1.8f)));
            }

            if (_orbiting)
            {
                UpdateOrbit(pulse);
            }
            else if (_convergeT >= 0f)
            {
                UpdateConverge(Mathf.Clamp01(_convergeT));
            }
        }

        private void UpdateOrbit(float pulse)
        {
            for (var i = 0; i < _waveCount; i++)
            {
                var wave = _waves[i];
                if (wave == null)
                {
                    continue;
                }

                var dir = (i % 2 == 0) ? 1f : -1f;
                _waveAngles[i] += Time.deltaTime * (1.55f + (i % 4) * 0.28f) * dir;
                _wavePhase[i] += Time.deltaTime * 3.2f;
                var radius = _waveRadii[i] * pulse;
                var x = Mathf.Cos(_waveAngles[i]) * radius;
                var y = Mathf.Sin(_waveAngles[i]) * radius * 0.58f + Mathf.Sin(_wavePhase[i]) * 0.12f;
                wave.transform.localPosition = new Vector3(x, y + 0.18f, 0f);
                wave.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    _waveAngles[i] * Mathf.Rad2Deg + _waveSpin[i] * Time.time * 0.15f);
                var longWave = wave.sprite != null && wave.sprite.rect.width > wave.sprite.rect.height * 1.35f;
                var scalePulse = (longWave ? 0.5f : 0.34f) + 0.1f * Mathf.Abs(Mathf.Sin(_pulse * 2.1f + i));
                FitSprite(wave, _settings.WorldSize * scalePulse);
                var baseCol = _waveBaseColor[i];
                wave.color = new Color(
                    baseCol.r,
                    baseCol.g,
                    baseCol.b,
                    0.4f + 0.5f * Mathf.Abs(Mathf.Sin(_pulse * 1.4f + i * 0.7f)));
            }

            for (var i = 0; i < _noteCount; i++)
            {
                var note = _notes[i];
                if (note == null)
                {
                    continue;
                }

                _noteAngles[i] += Time.deltaTime * (1.25f + i * 0.05f) * ((i % 2 == 0) ? 1f : -1f);
                _noteBob[i] += Time.deltaTime * 4.4f;
                var radius = _noteRadii[i] * pulse;
                var x = Mathf.Cos(_noteAngles[i]) * radius;
                var y = Mathf.Sin(_noteAngles[i]) * radius * 0.55f + Mathf.Sin(_noteBob[i]) * 0.14f;
                note.transform.localPosition = new Vector3(x, y + 0.12f, 0f);
                note.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_noteBob[i]) * 28f);
                FitSprite(note, _settings.WorldSize * (0.16f + 0.07f * Mathf.Abs(Mathf.Sin(_noteBob[i]))));
                var baseCol = _noteBaseColor[i];
                note.color = new Color(
                    baseCol.r,
                    baseCol.g,
                    baseCol.b,
                    0.45f + 0.5f * Mathf.Abs(Mathf.Sin(_noteBob[i])));
            }
        }

        private void UpdateConverge(float t)
        {
            var eased = t * t * (3f - 2f * t);
            if (_innerRing != null)
            {
                _innerRing.transform.localPosition = Vector3.Lerp(Vector3.zero, _muzzleLocal, eased);
                FitSprite(_innerRing, _settings.WorldSize * Mathf.Lerp(0.65f, 0.12f, eased));
                var c = _innerRing.color;
                _innerRing.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0.95f, eased));
            }

            for (var i = 0; i < _waveCount; i++)
            {
                var wave = _waves[i];
                if (wave == null)
                {
                    continue;
                }

                var from = _waveStartLocal[i];
                var delay = (i / (float)Mathf.Max(1, _waveCount)) * 0.18f;
                var localT = Mathf.Clamp01((eased - delay) / Mathf.Max(0.001f, 1f - delay));
                var localEase = localT * localT * (3f - 2f * localT);
                var jitter = new Vector3(
                    Mathf.Sin(_pulse * 9f + i) * 0.06f * (1f - localEase),
                    Mathf.Cos(_pulse * 8f + i) * 0.06f * (1f - localEase),
                    0f);
                wave.transform.localPosition = Vector3.Lerp(from, _muzzleLocal, localEase) + jitter;
                wave.transform.localScale = Vector3.Lerp(wave.transform.localScale, Vector3.one * 0.12f, localEase);
                var baseCol = _waveBaseColor[i];
                wave.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(baseCol.a, 1f, localEase));
                var aim = _muzzleLocal - wave.transform.localPosition;
                if (aim.sqrMagnitude > 0.0001f)
                {
                    var ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    wave.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                }
            }

            for (var i = 0; i < _noteCount; i++)
            {
                var note = _notes[i];
                if (note == null)
                {
                    continue;
                }

                var delay = (i / (float)Mathf.Max(1, _noteCount)) * 0.15f;
                var localT = Mathf.Clamp01((eased - delay) / Mathf.Max(0.001f, 1f - delay));
                note.transform.localPosition = Vector3.Lerp(_noteStartLocal[i], _muzzleLocal, localT);
                var baseCol = _noteBaseColor[i];
                note.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(baseCol.a, 0.15f, localT));
                FitSprite(note, _settings.WorldSize * Mathf.Lerp(0.2f, 0.06f, localT));
            }

            if (_glow != null)
            {
                _glow.transform.localPosition = Vector3.Lerp(Vector3.zero, _muzzleLocal, eased * 0.7f);
                FitSprite(_glow, _settings.WorldSize * Mathf.Lerp(1.08f, 0.28f, eased));
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            var seconds = Mathf.Max(0.05f, _settings.FadeOutSeconds);
            var glow0 = _glow != null ? _glow.color.a : 0f;
            var ring0 = _innerRing != null ? _innerRing.color.a : 0f;
            var wave0 = new float[_waveCount];
            var note0 = new float[_noteCount];
            for (var i = 0; i < _waveCount; i++)
            {
                wave0[i] = _waves[i] != null ? _waves[i].color.a : 0f;
            }

            for (var i = 0; i < _noteCount; i++)
            {
                note0[i] = _notes[i] != null ? _notes[i].color.a : 0f;
            }

            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var u = 1f - Mathf.Clamp01(elapsed / seconds);
                SetAlpha(_glow, glow0 * u);
                SetAlpha(_innerRing, ring0 * u);
                for (var i = 0; i < _waveCount; i++)
                {
                    SetAlpha(_waves[i], wave0[i] * u);
                }

                for (var i = 0; i < _noteCount; i++)
                {
                    SetAlpha(_notes[i], note0[i] * u);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            if (sr == null)
            {
                return;
            }

            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}
