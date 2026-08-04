using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CharlotteDomeRingView : MonoBehaviour
    {
        private static readonly List<CharlotteDomeRingView> Active = new();

        private SpriteRenderer _ring;
        private SpriteRenderer _wave;
        private Transform _follow;
        private Vector3 _offset;
        private float _worldSize;
        private float _remainingHold;
        private float _fadeSeconds;
        private Color _tint;
        private Sprite[] _waveFrames;
        private float _waveFps = 24f;
        private float _waveSizeScale = 1.18f;
        private float _animTime;
        private float _pulse;
        private bool _hiddenForEncounter;
        private bool _fadingOut;
        private bool _inHold;
        private Coroutine _lifeRoutine;

        public static CharlotteDomeRingView SpawnOrExtend(
            Transform follow,
            Vector3 worldCenter,
            Sprite ringSprite,
            Material additive,
            float worldSize = 2.8f,
            float holdSeconds = 1.1f,
            float fadeSeconds = 0.22f,
            int sortingOrder = 38,
            Transform parent = null,
            Sprite[] waveFrames = null,
            float waveFps = 24f,
            float waveSizeScale = 1.18f)
        {
            PruneActive();
            var existing = FindPrimary();
            if (existing != null)
            {
                for (var i = Active.Count - 1; i >= 0; i--)
                {
                    var other = Active[i];
                    if (other != null && other != existing)
                    {
                        Destroy(other.gameObject);
                    }
                }

                existing.Retarget(follow, worldCenter, worldSize, waveFrames, waveFps, waveSizeScale);
                existing.ExtendHold(holdSeconds);
                return existing;
            }

            return Spawn(
                follow,
                worldCenter,
                ringSprite,
                additive,
                worldSize,
                holdSeconds,
                fadeSeconds,
                sortingOrder,
                parent,
                waveFrames,
                waveFps,
                waveSizeScale);
        }

        public static CharlotteDomeRingView Spawn(
            Transform follow,
            Vector3 worldCenter,
            Sprite ringSprite,
            Material additive,
            float worldSize = 2.8f,
            float holdSeconds = 1.1f,
            float fadeSeconds = 0.22f,
            int sortingOrder = 38,
            Transform parent = null,
            Sprite[] waveFrames = null,
            float waveFps = 24f,
            float waveSizeScale = 1.18f)
        {
            if (ringSprite == null || follow == null)
            {
                return null;
            }

            var go = new GameObject("CharlotteDomeRing");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<CharlotteDomeRingView>();
            view._follow = follow;
            view._offset = worldCenter - follow.position;
            view.transform.position = worldCenter;
            view._worldSize = Mathf.Max(0.2f, worldSize);
            view._remainingHold = Mathf.Max(0.05f, holdSeconds);
            view._fadeSeconds = fadeSeconds;
            view._tint = new Color(1f, 0.85f, 0.3f, 1f);
            view._waveFrames = SanitizeFrames(waveFrames);
            view._waveFps = Mathf.Max(1f, waveFps);
            view._waveSizeScale = Mathf.Max(0.2f, waveSizeScale);

            var child = new GameObject("Ring");
            child.transform.SetParent(go.transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite;
            sr.sortingOrder = sortingOrder;
            sr.color = new Color(view._tint.r, view._tint.g, view._tint.b, 0f);
            if (additive != null)
            {
                sr.sharedMaterial = additive;
            }

            view._ring = sr;
            Fit(sr, view._worldSize);

            var firstWave = FirstFrame(view._waveFrames);
            if (firstWave != null)
            {
                var waveGo = new GameObject("WaveDome");
                waveGo.transform.SetParent(go.transform, false);
                var wr = waveGo.AddComponent<SpriteRenderer>();
                wr.sprite = firstWave;
                wr.sortingOrder = sortingOrder + 2;
                wr.color = new Color(1f, 0.95f, 0.75f, 0f);
                if (additive != null)
                {
                    wr.sharedMaterial = additive;
                }

                view._wave = wr;
                Fit(wr, view._worldSize * view._waveSizeScale);
            }

            Active.Add(view);
            if (EncounterDirector.IsPresenting)
            {
                view.ApplyEncounterHidden(true);
            }

            view._lifeRoutine = view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        public void ExtendHold(float additionalSeconds)
        {
            var add = Mathf.Max(0.05f, additionalSeconds);
            if (_fadingOut)
            {
                _fadingOut = false;
                _remainingHold = add;
                if (_lifeRoutine != null)
                {
                    StopCoroutine(_lifeRoutine);
                }

                ApplyHoldVisual(1f);
                _lifeRoutine = StartCoroutine(HoldThenFade());
                return;
            }

            _remainingHold += add;
            if (_inHold)
            {
                BurstRefreshPulse();
            }
        }

        public static void SetEncounterHidden(bool hidden)
        {
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var dome = Active[i];
                if (dome == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }

                dome.ApplyEncounterHidden(hidden);
            }
        }

        public static void DismissAll()
        {
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var dome = Active[i];
                if (dome != null)
                {
                    Destroy(dome.gameObject);
                }
            }

            Active.Clear();
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = _follow.position + _offset;
            }

            if (_hiddenForEncounter || _fadingOut || _wave == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            _animTime += dt;
            _pulse += dt * 8f;
            AdvanceWaveFrame();
            PulseWave();
        }

        private void Retarget(
            Transform follow,
            Vector3 worldCenter,
            float worldSize,
            Sprite[] waveFrames,
            float waveFps,
            float waveSizeScale)
        {
            if (follow != null)
            {
                _follow = follow;
                _offset = worldCenter - follow.position;
                transform.position = worldCenter;
            }

            _worldSize = Mathf.Max(0.2f, worldSize);
            _waveFps = Mathf.Max(1f, waveFps);
            _waveSizeScale = Mathf.Max(0.2f, waveSizeScale);
            var cleaned = SanitizeFrames(waveFrames);
            if (cleaned != null)
            {
                _waveFrames = cleaned;
            }

            if (_ring != null)
            {
                Fit(_ring, _worldSize);
            }

            if (_wave != null)
            {
                Fit(_wave, _worldSize * _waveSizeScale);
            }
        }

        private void ApplyEncounterHidden(bool hidden)
        {
            _hiddenForEncounter = hidden;
            if (_ring != null)
            {
                _ring.enabled = !hidden;
            }

            if (_wave != null)
            {
                _wave.enabled = !hidden;
            }
        }

        private IEnumerator PlayRoutine()
        {
            const float rise = 0.16f;
            var elapsed = 0f;
            while (elapsed < rise)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / rise);
                var eased = t * t * (3f - 2f * t);
                if (!_hiddenForEncounter)
                {
                    Fit(_ring, _worldSize * Mathf.Lerp(0.55f, 1f, eased));
                    _ring.color = new Color(_tint.r, _tint.g, _tint.b, Mathf.Lerp(0f, 0.95f, eased));
                    ApplyWaveVisual(eased);
                }

                yield return null;
            }

            ApplyHoldVisual(1f);
            yield return HoldThenFade();
        }

        private IEnumerator HoldThenFade()
        {
            _inHold = true;
            _fadingOut = false;
            while (_remainingHold > 0f)
            {
                if (!_hiddenForEncounter)
                {
                    _remainingHold -= Time.deltaTime;
                    var pulse = 1f + Mathf.Sin(_pulse * 1.5f) * 0.03f;
                    Fit(_ring, _worldSize * pulse);
                    _ring.color = new Color(
                        _tint.r,
                        _tint.g,
                        _tint.b,
                        0.78f + 0.2f * Mathf.Abs(Mathf.Sin(_pulse * 2f)));
                    ApplyWaveVisual(1f);
                }

                yield return null;
            }

            _inHold = false;
            _fadingOut = true;
            var fade = Mathf.Max(0.05f, _fadeSeconds);
            var elapsed = 0f;
            var a0 = _ring != null ? _ring.color.a : 0f;
            var waveA0 = _wave != null ? _wave.color.a : 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                if (_hiddenForEncounter)
                {
                    yield return null;
                    continue;
                }

                if (_remainingHold > 0f)
                {
                    _fadingOut = false;
                    ApplyHoldVisual(1f);
                    yield return HoldThenFade();
                    yield break;
                }

                var u = 1f - Mathf.Clamp01(elapsed / fade);
                if (_ring != null)
                {
                    _ring.color = new Color(_tint.r, _tint.g, _tint.b, a0 * u);
                    Fit(_ring, _worldSize * Mathf.Lerp(0.92f, 1f, u));
                }

                if (_wave != null)
                {
                    var wc = _wave.color;
                    _wave.color = new Color(wc.r, wc.g, wc.b, waveA0 * u);
                    Fit(_wave, _worldSize * _waveSizeScale * Mathf.Lerp(0.92f, 1f, u));
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void ApplyHoldVisual(float alpha)
        {
            if (_hiddenForEncounter || _ring == null)
            {
                return;
            }

            Fit(_ring, _worldSize);
            _ring.color = new Color(_tint.r, _tint.g, _tint.b, 0.95f * alpha);
            ApplyWaveVisual(alpha);
        }

        private void BurstRefreshPulse()
        {
            if (_ring == null || _hiddenForEncounter)
            {
                return;
            }

            Fit(_ring, _worldSize * 1.06f);
            _ring.color = new Color(_tint.r, _tint.g, _tint.b, 1f);
            ApplyWaveVisual(1f);
        }

        private void ApplyWaveVisual(float alphaScale)
        {
            if (_wave == null)
            {
                return;
            }

            var a = Mathf.Clamp01(alphaScale);
            var pulse = 1f + Mathf.Sin(_pulse * 2.8f) * 0.06f;
            Fit(_wave, _worldSize * _waveSizeScale * Mathf.Lerp(0.7f, 1f, a) * pulse);
            _wave.color = new Color(1f, 0.95f, 0.7f, Mathf.Lerp(0f, 0.98f, a));
            _wave.transform.localPosition = Vector3.zero;
            _wave.transform.localRotation = Quaternion.identity;
            AdvanceWaveFrame();
        }

        private void PulseWave()
        {
            if (_wave == null || !_inHold)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(_pulse * 2.8f) * 0.06f;
            var amp = 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(_pulse * 3.6f));
            Fit(_wave, _worldSize * _waveSizeScale * pulse);
            var c = _wave.color;
            _wave.color = new Color(c.r, c.g, c.b, amp);
        }

        private void AdvanceWaveFrame()
        {
            if (_wave == null || _waveFrames == null || _waveFrames.Length == 0)
            {
                return;
            }

            var index = Mathf.FloorToInt(_animTime * _waveFps) % _waveFrames.Length;
            if (index < 0)
            {
                index += _waveFrames.Length;
            }

            var sprite = _waveFrames[index];
            if (sprite == null || _wave.sprite == sprite)
            {
                return;
            }

            _wave.sprite = sprite;
        }

        private static CharlotteDomeRingView FindPrimary()
        {
            for (var i = 0; i < Active.Count; i++)
            {
                if (Active[i] != null)
                {
                    return Active[i];
                }
            }

            return null;
        }

        private static void PruneActive()
        {
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i] == null)
                {
                    Active.RemoveAt(i);
                }
            }
        }

        private static Sprite[] SanitizeFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            var count = 0;
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }

            if (count == frames.Length)
            {
                return frames;
            }

            var cleaned = new Sprite[count];
            var w = 0;
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    cleaned[w++] = frames[i];
                }
            }

            return cleaned;
        }

        private static Sprite FirstFrame(Sprite[] frames)
        {
            if (frames == null)
            {
                return null;
            }

            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    return frames[i];
                }
            }

            return null;
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
