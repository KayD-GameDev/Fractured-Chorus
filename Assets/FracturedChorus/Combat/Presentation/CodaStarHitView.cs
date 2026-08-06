using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CodaStarHitSettings
    {
        public Sprite Impact;
        public Sprite[] Stars;
        public Material AdditiveMaterial;
        public float ImpactWorldSize = 2.8f;
        public float ImpactSeconds = 0.26f;
        public float StarWorldSize = 0.42f;
        public int StarCount = 12;
        public float StarBurstSeconds = 0.62f;
        public float StarSpeedMin = 3.8f;
        public float StarSpeedMax = 8.5f;
        public int SortingOrder = 46;
        public Vector2 BurstDir = Vector2.right;
    }

    public class CodaStarHitView : MonoBehaviour
    {
        private CodaStarHitSettings _settings;
        private SpriteRenderer _impact;
        private readonly List<StarPiece> _stars = new();

        private struct StarPiece
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Spin;
            public float Gravity;
            public float Life;
            public float MaxLife;
        }

        public static CodaStarHitView Spawn(
            Vector3 contact,
            CodaStarHitSettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Impact == null && (settings.Stars == null || settings.Stars.Length == 0)))
            {
                return null;
            }

            var go = new GameObject("CodaStarHit");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = contact;
            var view = go.AddComponent<CodaStarHitView>();
            view._settings = settings;
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        public static Sprite[] SliceStarSheet(Texture2D sheet, int cols = 3, int rows = 3)
        {
            if (sheet == null || cols < 1 || rows < 1)
            {
                return null;
            }

            var cellW = sheet.width / cols;
            var cellH = sheet.height / rows;
            if (cellW < 4 || cellH < 4)
            {
                return null;
            }

            var list = new List<Sprite>(cols * rows);
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var x = col * cellW;
                    var y = (rows - 1 - row) * cellH;
                    Color[] pixels;
                    try
                    {
                        pixels = sheet.GetPixels(x, y, cellW, cellH);
                    }
                    catch
                    {
                        return null;
                    }

                    KeyBlackToAlpha(pixels);
                    var tex = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        name = sheet.name + "_star_" + list.Count,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    tex.SetPixels(pixels);
                    tex.Apply(false, true);
                    list.Add(Sprite.Create(
                        tex,
                        new Rect(0f, 0f, cellW, cellH),
                        new Vector2(0.5f, 0.5f),
                        100f));
                }
            }

            return list.Count > 0 ? list.ToArray() : null;
        }

        private void Build()
        {
            if (_settings.Impact != null)
            {
                _impact = CreateRenderer("Impact", _settings.Impact, _settings.SortingOrder, true);
                Fit(_impact, _settings.ImpactWorldSize * 0.45f);
                _impact.color = new Color(1f, 1f, 1f, 0f);
            }

            var stars = _settings.Stars;
            if (stars == null || stars.Length == 0)
            {
                return;
            }

            var count = Mathf.Clamp(_settings.StarCount, 5, 18);
            var burst = _settings.BurstDir.sqrMagnitude > 0.0001f
                ? _settings.BurstDir.normalized
                : Vector2.right;
            var baseAngle = Mathf.Atan2(burst.y, burst.x);

            for (var i = 0; i < count; i++)
            {
                var sprite = stars[i % stars.Length];
                if (sprite == null)
                {
                    continue;
                }

                var sr = CreateRenderer("Star" + i, sprite, _settings.SortingOrder + 1, true);
                Fit(sr, _settings.StarWorldSize * Random.Range(0.55f, 1.35f));
                sr.color = Color.white;
                sr.transform.localPosition = Random.insideUnitCircle * 0.08f;

                var spread = Random.Range(-1.15f, 1.15f);
                var angle = baseAngle + spread;
                var speed = Random.Range(_settings.StarSpeedMin, _settings.StarSpeedMax);
                var life = _settings.StarBurstSeconds * Random.Range(0.7f, 1.15f);
                var vel = new Vector2(
                    Mathf.Cos(angle) * speed,
                    Mathf.Sin(angle) * speed * 0.75f + Random.Range(1.2f, 3.2f));
                _stars.Add(new StarPiece
                {
                    Renderer = sr,
                    Velocity = vel,
                    Spin = Random.Range(-420f, 420f),
                    Gravity = Random.Range(2.8f, 5.5f),
                    Life = life,
                    MaxLife = life
                });
            }
        }

        private IEnumerator PlayRoutine()
        {
            var impactSeconds = Mathf.Max(0.08f, _settings.ImpactSeconds);
            var burstSeconds = Mathf.Max(impactSeconds, _settings.StarBurstSeconds);
            var elapsed = 0f;

            while (elapsed < burstSeconds)
            {
                elapsed += Time.deltaTime;
                var dt = Time.deltaTime;

                if (_impact != null)
                {
                    var t = Mathf.Clamp01(elapsed / impactSeconds);
                    if (t < 1f)
                    {
                        var rise = t < 0.16f ? t / 0.16f : 1f;
                        var fade = t > 0.5f ? 1f - (t - 0.5f) / 0.5f : 1f;
                        var sizePulse = Mathf.Lerp(0.45f, 1.2f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.7f)));
                        Fit(_impact, _settings.ImpactWorldSize * sizePulse);
                        _impact.color = new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(rise * fade));
                        _impact.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 40f);
                    }
                    else
                    {
                        _impact.enabled = false;
                    }
                }

                for (var i = 0; i < _stars.Count; i++)
                {
                    var star = _stars[i];
                    if (star.Renderer == null)
                    {
                        continue;
                    }

                    star.Life -= dt;
                    star.Velocity.y -= star.Gravity * dt;
                    var p = star.Renderer.transform.localPosition;
                    p.x += star.Velocity.x * dt;
                    p.y += star.Velocity.y * dt;
                    star.Renderer.transform.localPosition = p;
                    star.Renderer.transform.Rotate(0f, 0f, star.Spin * dt);
                    var lifeT = Mathf.Clamp01(star.Life / Mathf.Max(0.05f, star.MaxLife));
                    var twinkle = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(elapsed * 18f + i));
                    var c = star.Renderer.color;
                    star.Renderer.color = new Color(c.r, c.g, c.b, lifeT * twinkle);
                    if (lifeT <= 0.001f)
                    {
                        star.Renderer.enabled = false;
                    }

                    _stars[i] = star;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order, bool additive)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Color.white;
            if (additive && _settings.AdditiveMaterial != null)
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

        private static void KeyBlackToAlpha(Color[] pixels)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                var lum = (c.r + c.g + c.b) / 3f;
                if (lum < 0.07f)
                {
                    c.a = 0f;
                }
                else if (lum < 0.16f)
                {
                    c.a *= (lum - 0.07f) / 0.09f;
                }

                pixels[i] = c;
            }
        }
    }
}
