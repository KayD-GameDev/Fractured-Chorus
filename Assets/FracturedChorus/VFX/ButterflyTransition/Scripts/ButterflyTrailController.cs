using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.VFX
{
    public sealed class ButterflyTrailController : MonoBehaviour
    {
        private enum Kind
        {
            Star,
            Prism,
            Music,
            AmbientFar,
            AmbientNear
        }

        private struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public CanvasGroup Group;
            public Vector2 Drift;
            public float Gravity;
            public float Spin;
            public float Life;
            public float MaxLife;
            public float StartSize;
            public Kind Kind;
            public bool Active;
        }

        [SerializeField] private RectTransform starDustRoot;
        [SerializeField] private RectTransform fragmentRoot;
        [SerializeField] private RectTransform prismRoot;
        [SerializeField] private RectTransform musicRoot;
        [SerializeField] private RectTransform waveformRoot;
        [SerializeField] private RectTransform ambientFarRoot;
        [SerializeField] private RectTransform ambientNearRoot;
        [SerializeField] private Sprite starDustSprite;
        [SerializeField] private Sprite glitterSprite;
        [SerializeField] private Sprite[] prismSprites;
        [SerializeField] private Sprite[] musicSprites;
        [SerializeField] private Sprite waveformSprite;
        [SerializeField] private Sprite ambientFarSprite;
        [SerializeField] private Sprite ambientNearSprite;
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private float starDustEmission = 42f;
        [SerializeField] private float fragmentEmission = 10f;
        [SerializeField] private float musicFragmentEmission = 5f;
        [SerializeField] private float trailLifetime = 2.1f;
        [SerializeField] private float trailBrightness = 1f;

        private Particle[] _stars;
        private Particle[] _prisms;
        private Particle[] _music;
        private Particle[] _ambientFar;
        private Particle[] _ambientNear;
        private ButterflyWaveformTrail[] _waves;
        private float[] _emitAcc;
        private bool _fieldAmbientEnabled = true;
        private bool _emitting;
        private float _globalFade = 1f;
        private float _waveFade = 1f;

        public void Bind(
            RectTransform stars,
            RectTransform fragments,
            RectTransform prisms,
            RectTransform music,
            RectTransform waveforms,
            Sprite star,
            Sprite glitter,
            Sprite[] prism,
            Sprite[] notes,
            Material additive)
        {
            starDustRoot = stars;
            fragmentRoot = fragments;
            prismRoot = prisms;
            musicRoot = music;
            waveformRoot = waveforms;
            starDustSprite = star;
            glitterSprite = glitter;
            prismSprites = prism;
            musicSprites = notes;
            additiveMaterial = additive;
            if (waveformSprite == null)
            {
                waveformSprite = ResolveWaveformSprite();
            }
        }

        public void SetFieldAmbientEnabled(bool enabled)
        {
            _fieldAmbientEnabled = enabled;
            ApplyFieldAmbientVisibility();
        }

        private void ApplyFieldAmbientVisibility()
        {
            SetRootActive(starDustRoot, _fieldAmbientEnabled);
            SetRootActive(fragmentRoot, _fieldAmbientEnabled);
            SetRootActive(ambientFarRoot, _fieldAmbientEnabled);
            SetRootActive(ambientNearRoot, _fieldAmbientEnabled);
        }

        private static void SetRootActive(Component root, bool active)
        {
            if (root != null)
            {
                root.gameObject.SetActive(active);
            }
        }

        public void ResolveFrom(Transform root)
        {
            if (starDustRoot == null)
            {
                starDustRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.StarDustName) as RectTransform;
            }

            if (fragmentRoot == null)
            {
                fragmentRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.SmallFragmentsName) as RectTransform;
            }

            if (prismRoot == null)
            {
                prismRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.PrismFragmentsName) as RectTransform;
            }

            if (musicRoot == null)
            {
                musicRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.MusicFragmentsName) as RectTransform;
            }

            if (waveformRoot == null)
            {
                waveformRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.WaveformTrailsName) as RectTransform;
            }

            if (ambientFarRoot == null)
            {
                ambientFarRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.AmbientFarName) as RectTransform;
            }

            if (ambientNearRoot == null)
            {
                ambientNearRoot = root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.AmbientNearName) as RectTransform;
            }
        }

        public void SetRates(float stars, float fragments, float music, float lifetime, float brightness)
        {
            starDustEmission = stars;
            fragmentEmission = fragments;
            musicFragmentEmission = music;
            trailLifetime = lifetime;
            trailBrightness = brightness;
        }

        public void Prepare()
        {
            _stars = _fieldAmbientEnabled ? BindPool(starDustRoot, Kind.Star) : System.Array.Empty<Particle>();
            EnsurePoolChildren(prismRoot != null ? prismRoot : fragmentRoot, 72, new Vector2(12.6f, 12.6f), "Prism_");
            _prisms = BindPool(prismRoot != null ? prismRoot : fragmentRoot, Kind.Prism);
            EnsurePoolChildren(musicRoot, 12, new Vector2(32f, 32f), "Music_");
            _music = BindPool(musicRoot, Kind.Music);
            if (_fieldAmbientEnabled)
            {
                EnsurePoolChildren(ambientFarRoot, 6, new Vector2(384f, 384f), "AmbientFar_");
                EnsurePoolChildren(ambientNearRoot, 4, new Vector2(288f, 288f), "AmbientNear_");
            }

            _ambientFar = _fieldAmbientEnabled ? BindPool(ambientFarRoot, Kind.AmbientFar) : System.Array.Empty<Particle>();
            _ambientNear = _fieldAmbientEnabled ? BindPool(ambientNearRoot, Kind.AmbientNear) : System.Array.Empty<Particle>();
            _waves = BindWaves();
            _emitAcc = new float[3];
            if (_fieldAmbientEnabled)
            {
                SeedAmbient();
            }

            ApplyFieldAmbientVisibility();
        }

        public void Play()
        {
            if (_stars == null)
            {
                Prepare();
            }

            ClearActive(_stars);
            ClearActive(_prisms);
            ClearActive(_music);
            if (_fieldAmbientEnabled)
            {
                SeedAmbient();
            }

            ApplyFieldAmbientVisibility();
            ClearWaves();
            _emitting = true;
            _globalFade = 1f;
            _waveFade = 1f;
            _emitAcc[0] = 0f;
            _emitAcc[1] = 0f;
            _emitAcc[2] = 0f;
        }

        public void StopEmitting()
        {
            _emitting = false;
        }

        public void StopImmediate()
        {
            _emitting = false;
            ClearActive(_stars);
            ClearActive(_prisms);
            ClearActive(_music);
            ClearActive(_ambientFar);
            ClearActive(_ambientNear);
            ClearWaves();
        }

        public void Tick(float deltaTime, Vector2 origin, float emission, float fade, bool hovering)
        {
            _globalFade = fade;
            if (_emitting && emission > 0.001f)
            {
                if (_fieldAmbientEnabled)
                {
                    Emit(ref _emitAcc[0], starDustEmission * emission, deltaTime, origin, _stars, Kind.Star);
                }

                Emit(ref _emitAcc[1], fragmentEmission * emission, deltaTime, origin, _prisms, Kind.Prism);
                Emit(ref _emitAcc[2], musicFragmentEmission * emission, deltaTime, origin, _music, Kind.Music);
                if (!hovering)
                {
                    AppendWaves(origin);
                }
            }

            if (hovering)
            {
                _waveFade = Mathf.MoveTowards(_waveFade, 0f, deltaTime * 2.4f);
                if (_waveFade <= 0.001f)
                {
                    ClearWaves();
                }
            }
            else
            {
                _waveFade = fade;
            }

            TickPool(_stars, deltaTime, 1f);
            TickPool(_prisms, deltaTime, 1f);
            TickPool(_music, deltaTime, 1.1f);
            TickPool(_ambientFar, deltaTime, 0.55f);
            TickPool(_ambientNear, deltaTime, 0.8f);
            if (_waves != null && _waveFade > 0.001f)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    _waves[i].Tick(deltaTime, _waveFade);
                }
            }
        }

        private void AppendWaves(Vector2 origin)
        {
            if (_waves == null)
            {
                return;
            }

            var canvas = transform as RectTransform;
            var world = canvas != null ? canvas.TransformPoint(origin) : (Vector3)origin;
            for (var i = 0; i < _waves.Length; i++)
            {
                var wave = _waves[i];
                if (wave == null)
                {
                    continue;
                }

                var local = (Vector2)wave.rectTransform.InverseTransformPoint(world);
                wave.AddPoint(local, 256);
            }
        }

        private void Emit(ref float accumulator, float rate, float deltaTime, Vector2 origin, Particle[] pool, Kind kind)
        {
            accumulator += rate * deltaTime;
            while (accumulator >= 1f)
            {
                accumulator -= 1f;
                Spawn(pool, origin, kind);
            }
        }

        private void Spawn(Particle[] pool, Vector2 origin, Kind kind)
        {
            if (pool == null)
            {
                return;
            }

            var slot = -1;
            for (var i = 0; i < pool.Length; i++)
            {
                if (!pool[i].Active)
                {
                    slot = i;
                    break;
                }
            }

            if (slot < 0)
            {
                return;
            }

            var particle = pool[slot];
            if (particle.Rect == null || particle.Image == null)
            {
                return;
            }
            var jitter = kind == Kind.Prism
                ? new Vector2(Random.Range(-18f, 18f), Random.Range(-6f, 10f))
                : new Vector2(Random.Range(-36f, 36f), Random.Range(-28f, 28f));
            particle.Rect.anchoredPosition = origin + jitter;
            particle.Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
            particle.Life = 0f;
            particle.MaxLife = kind == Kind.Star
                ? Random.Range(0.5f, 1.5f)
                : kind == Kind.Prism
                    ? Random.Range(1.7f, 2.6f)
                    : kind == Kind.Music
                        ? Random.Range(1.5f, 2.4f)
                        : Random.Range(1.0f, 1.8f);
            particle.Spin = kind == Kind.Prism ? Random.Range(-90f, 90f) : kind == Kind.Music ? Random.Range(-24f, 24f) : Random.Range(-28f, 28f);
            particle.Drift = kind == Kind.Prism
                ? new Vector2(Random.Range(-28f, 28f), Random.Range(-48f, -18f))
                : kind == Kind.Music
                    ? new Vector2(Random.Range(-16f, 10f), Random.Range(18f, 52f))
                    : new Vector2(Random.Range(-12f, 8f), Random.Range(-6f, 14f));
            particle.Gravity = kind == Kind.Prism ? Random.Range(-320f, -210f) : 0f;
            if (kind == Kind.Prism)
            {
                particle.StartSize = Random.Range(7.7f, 16.8f);
            }
            else if (kind == Kind.Music)
            {
                particle.StartSize = Random.Range(26f, 38f);
            }
            else if (particle.StartSize < 1f)
            {
                particle.StartSize = particle.Rect.sizeDelta.x;
            }
            particle.Active = true;
            particle.Rect.gameObject.SetActive(true);
            var sprite = SpriteFor(kind);
            if (sprite != null)
            {
                particle.Image.sprite = sprite;
            }

            var tint = kind == Kind.Music
                ? new Color(0.9f, 0.97f, 1f, 0.9f)
                : kind == Kind.Prism
                    ? new Color(0.7f, 0.92f, 1f, 0.92f)
                    : Color.white;
            tint.a *= trailBrightness;
            particle.Image.color = tint;
            pool[slot] = particle;
        }

        private Sprite SpriteFor(Kind kind)
        {
            if (kind == Kind.Prism)
            {
                return RandomSprite(prismSprites, starDustSprite);
            }

            if (kind == Kind.Music)
            {
                return musicSprites != null && musicSprites.Length > 0 ? musicSprites[0] : starDustSprite;
            }

            return Random.value > 0.82f && glitterSprite != null ? glitterSprite : starDustSprite;
        }

        private void TickPool(Particle[] pool, float deltaTime, float fadeMul)
        {
            if (pool == null)
            {
                return;
            }

            for (var i = 0; i < pool.Length; i++)
            {
                var particle = pool[i];
                if (!particle.Active || particle.Rect == null)
                {
                    continue;
                }

                particle.Life += deltaTime * fadeMul;
                var t = particle.Life / Mathf.Max(0.01f, particle.MaxLife);
                if (t >= 1f)
                {
                    if (particle.Kind == Kind.AmbientFar || particle.Kind == Kind.AmbientNear)
                    {
                        RecycleAmbient(ref particle);
                        pool[i] = particle;
                        continue;
                    }

                    particle.Active = false;
                    particle.Rect.gameObject.SetActive(false);
                    pool[i] = particle;
                    continue;
                }

                if (particle.Kind == Kind.Prism)
                {
                    particle.Drift.y += particle.Gravity * deltaTime;
                }
                else if (particle.Kind == Kind.AmbientFar || particle.Kind == Kind.AmbientNear)
                {
                    var turn = (particle.Kind == Kind.AmbientFar ? 8f : 14f) * Mathf.Deg2Rad * deltaTime;
                    var dx = particle.Drift.x;
                    var dy = particle.Drift.y;
                    var cos = Mathf.Cos(turn);
                    var sin = Mathf.Sin(turn);
                    particle.Drift = new Vector2(dx * cos - dy * sin, dx * sin + dy * cos);
                }

                particle.Rect.anchoredPosition += particle.Drift * deltaTime;
                if (particle.Kind == Kind.AmbientFar || particle.Kind == Kind.AmbientNear)
                {
                    WrapInParent(particle.Rect);
                }
                particle.Rect.localRotation *= Quaternion.Euler(0f, 0f, particle.Spin * deltaTime);
                var size = particle.Kind == Kind.Prism
                    ? particle.StartSize * Mathf.Lerp(1f, 0.78f, t)
                    : particle.Kind == Kind.AmbientFar || particle.Kind == Kind.AmbientNear
                        ? particle.StartSize
                        : particle.StartSize * Mathf.Lerp(1f, 0.55f, t);
                particle.Rect.sizeDelta = new Vector2(size, size);
                var alpha = particle.Kind == Kind.Prism
                    ? (t < 0.72f ? 1f : 1f - Mathf.InverseLerp(0.72f, 1f, t))
                    : particle.Kind == Kind.AmbientFar || particle.Kind == Kind.AmbientNear
                        ? (particle.Kind == Kind.AmbientFar ? 0.1f : 0.16f)
                          + 0.08f * (0.5f + 0.5f * Mathf.Sin(particle.Life * 0.9f))
                        : 1f - t;
                particle.Group.alpha = Mathf.Clamp01(alpha * _globalFade * trailBrightness);
                pool[i] = particle;
            }
        }

        private void SeedAmbient()
        {
            var halo = AmbientCircleSprite();
            SeedLayer(_ambientFar, 6, 0.18f, new Vector2(352f, 640f), new Vector2(-22f, 18f), halo);
            SeedLayer(_ambientNear, 3, 0.32f, new Vector2(224f, 440f), new Vector2(-28f, 22f), halo);
        }

        private void SeedLayer(Particle[] pool, int count, float speed, Vector2 size, Vector2 drift, Sprite sprite)
        {
            if (pool == null)
            {
                return;
            }

            ClearActive(pool);
            for (var i = 0; i < Mathf.Min(count, pool.Length); i++)
            {
                var particle = pool[i];
                PlaceAmbient(ref particle, speed, size, drift, sprite);
                pool[i] = particle;
            }
        }

        private void RecycleAmbient(ref Particle particle)
        {
            var far = particle.Kind == Kind.AmbientFar;
            var halo = AmbientCircleSprite();
            PlaceAmbient(
                ref particle,
                far ? 0.18f : 0.32f,
                far ? new Vector2(352f, 640f) : new Vector2(224f, 440f),
                far ? new Vector2(-22f, 18f) : new Vector2(-28f, 22f),
                halo);
        }

        private Sprite AmbientCircleSprite()
        {
            if (ambientFarSprite != null)
            {
                return ambientFarSprite;
            }

            if (ambientNearSprite != null)
            {
                return ambientNearSprite;
            }

            return starDustSprite;
        }

        private void PlaceAmbient(ref Particle particle, float speed, Vector2 size, Vector2 drift, Sprite sprite)
        {
            particle.Rect.anchoredPosition = RandomInRect(particle.Rect.parent as RectTransform);
            particle.Life = Random.Range(0f, 0.4f);
            particle.MaxLife = Random.Range(4.5f, 8.5f);
            particle.Spin = Random.Range(-14f, 14f);
            var ang = Random.Range(0f, Mathf.PI * 2f);
            var spd = speed * Random.Range(70f, 130f);
            particle.Drift = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            particle.Gravity = 0f;
            particle.StartSize = Random.Range(size.x, size.y);
            particle.Active = true;
            particle.Rect.gameObject.SetActive(true);
            particle.Rect.sizeDelta = new Vector2(particle.StartSize, particle.StartSize);
            particle.Group.alpha = (particle.Kind == Kind.AmbientFar ? 0.16f : 0.22f) * trailBrightness;
            if (particle.Image != null)
            {
                particle.Image.color = Color.white;
                if (sprite != null)
                {
                    particle.Image.sprite = sprite;
                }
            }
        }

        private Particle[] BindPool(RectTransform parent, Kind kind)
        {
            if (parent == null || parent.childCount == 0)
            {
                return System.Array.Empty<Particle>();
            }

            var list = new System.Collections.Generic.List<Particle>(parent.childCount);
            for (var i = 0; i < parent.childCount; i++)
            {
                var rect = parent.GetChild(i) as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                var image = rect.GetComponent<Image>();
                var group = rect.GetComponent<CanvasGroup>();
                if (image == null || group == null)
                {
                    continue;
                }

                image.raycastTarget = false;
                group.blocksRaycasts = false;
                rect.gameObject.SetActive(false);
                list.Add(new Particle
                {
                    Rect = rect,
                    Image = image,
                    Group = group,
                    Kind = kind,
                    StartSize = rect.sizeDelta.x
                });
            }

            return list.ToArray();
        }

        private ButterflyWaveformTrail[] BindWaves()
        {
            if (waveformRoot == null)
            {
                return System.Array.Empty<ButterflyWaveformTrail>();
            }

            var sprite = ResolveWaveformSprite();
            var widths = new[] { 120f, 78f, 44f };
            var colors = new[]
            {
                new Color(0.55f, 0.9f, 1f, 0.28f),
                new Color(0.72f, 0.95f, 1f, 0.7f),
                new Color(0.88f, 0.97f, 1f, 1f)
            };
            var list = new System.Collections.Generic.List<ButterflyWaveformTrail>(waveformRoot.childCount);
            for (var i = 0; i < waveformRoot.childCount; i++)
            {
                var child = waveformRoot.GetChild(i);
                if (!child.TryGetComponent(out ButterflyWaveformTrail wave))
                {
                    if (child.GetComponent<CanvasRenderer>() == null)
                    {
                        child.gameObject.AddComponent<CanvasRenderer>();
                    }

                    wave = child.gameObject.AddComponent<ButterflyWaveformTrail>();
                }

                var layer = i % colors.Length;
                wave.color = colors[layer];
                wave.raycastTarget = false;
                wave.BindVisual(sprite, widths[layer]);
                list.Add(wave);
            }

            return list.ToArray();
        }

        private Sprite ResolveWaveformSprite()
        {
            if (waveformSprite != null)
            {
                return waveformSprite;
            }

            if (musicSprites == null)
            {
                return null;
            }

            for (var i = 0; i < musicSprites.Length; i++)
            {
                var candidate = musicSprites[i];
                if (candidate != null && candidate.name.IndexOf("waveform", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    waveformSprite = candidate;
                    return candidate;
                }
            }

            if (musicSprites.Length > 2 && musicSprites[2] != null)
            {
                waveformSprite = musicSprites[2];
                return musicSprites[2];
            }

            return null;
        }

        private void ClearWaves()
        {
            if (_waves == null)
            {
                return;
            }

            for (var i = 0; i < _waves.Length; i++)
            {
                if (_waves[i] != null)
                {
                    _waves[i].ClearPoints();
                }
            }
        }

        private static Vector2 RandomInRect(RectTransform parent)
        {
            var rect = parent != null ? parent.rect : new Rect(-960f, -540f, 1920f, 1080f);
            return new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
        }

        private static void WrapInParent(RectTransform rect)
        {
            var parent = rect.parent as RectTransform;
            var bounds = parent != null ? parent.rect : new Rect(-960f, -540f, 1920f, 1080f);
            var pos = rect.anchoredPosition;
            if (pos.x < bounds.xMin) pos.x = bounds.xMax;
            else if (pos.x > bounds.xMax) pos.x = bounds.xMin;
            if (pos.y < bounds.yMin) pos.y = bounds.yMax;
            else if (pos.y > bounds.yMax) pos.y = bounds.yMin;
            rect.anchoredPosition = pos;
        }

        private static void ClearActive(Particle[] pool)
        {
            if (pool == null)
            {
                return;
            }

            for (var i = 0; i < pool.Length; i++)
            {
                var particle = pool[i];
                particle.Active = false;
                if (particle.Rect != null)
                {
                    particle.Rect.gameObject.SetActive(false);
                }

                pool[i] = particle;
            }
        }

        private static Sprite RandomSprite(Sprite[] sprites, Sprite fallback)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return fallback;
            }

            return sprites[Random.Range(0, sprites.Length)];
        }

        private static void EnsurePoolChildren(RectTransform parent, int count, Vector2 size, string prefix)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount; i < count; i++)
            {
                var go = new GameObject(prefix + i.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                go.GetComponent<CanvasGroup>().blocksRaycasts = false;
                go.SetActive(false);
            }
        }
    }
}
