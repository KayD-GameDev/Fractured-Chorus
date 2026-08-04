using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CharlotteUltSmashSettings
    {
        public Sprite Impact;
        public Sprite[] Rocks;
        public Material AdditiveMaterial;
        public float ImpactWorldSize = 3.6f;
        public float ImpactSeconds = 0.28f;
        public float RockWorldSize = 0.55f;
        public int RockCount = 10;
        public float RockBurstSeconds = 0.55f;
        public float RockSpeedMin = 4.5f;
        public float RockSpeedMax = 9.5f;
        public int SortingOrder = 46;
        public Vector2 BurstDir = Vector2.right;
    }

    public class CharlotteUltSmashView : MonoBehaviour
    {
        private CharlotteUltSmashSettings _settings;
        private SpriteRenderer _impact;
        private readonly List<RockPiece> _rocks = new();

        private struct RockPiece
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Spin;
            public float Gravity;
            public float Life;
        }

        public static CharlotteUltSmashView Spawn(
            Vector3 contact,
            CharlotteUltSmashSettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Impact == null && (settings.Rocks == null || settings.Rocks.Length == 0)))
            {
                return null;
            }

            var go = new GameObject("CharlotteUltSmash");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = contact;
            var view = go.AddComponent<CharlotteUltSmashView>();
            view._settings = settings;
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        public static Sprite[] SliceRockSheet(Texture2D sheet, int cols = 3, int rows = 3)
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
                    var rect = new Rect(x, y, cellW, cellH);
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
                        name = sheet.name + "_rock_" + list.Count,
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
                Fit(_impact, _settings.ImpactWorldSize * 0.55f);
                _impact.color = new Color(1f, 1f, 1f, 0f);
            }

            var rocks = _settings.Rocks;
            if (rocks == null || rocks.Length == 0)
            {
                return;
            }

            var count = Mathf.Clamp(_settings.RockCount, 4, 16);
            var burst = _settings.BurstDir.sqrMagnitude > 0.0001f
                ? _settings.BurstDir.normalized
                : Vector2.right;
            var baseAngle = Mathf.Atan2(burst.y, burst.x);

            for (var i = 0; i < count; i++)
            {
                var sprite = rocks[i % rocks.Length];
                if (sprite == null)
                {
                    continue;
                }

                var sr = CreateRenderer("Rock" + i, sprite, _settings.SortingOrder + 1, false);
                Fit(sr, _settings.RockWorldSize * Random.Range(0.65f, 1.25f));
                sr.color = Color.white;
                sr.transform.localPosition = Vector3.zero;

                var spread = Random.Range(-0.85f, 0.85f);
                var angle = baseAngle + spread;
                var speed = Random.Range(_settings.RockSpeedMin, _settings.RockSpeedMax);
                var vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + Random.Range(0.35f, 1.1f)) * speed;
                _rocks.Add(new RockPiece
                {
                    Renderer = sr,
                    Velocity = vel,
                    Spin = Random.Range(-720f, 720f),
                    Gravity = Random.Range(10f, 16f),
                    Life = _settings.RockBurstSeconds * Random.Range(0.75f, 1.1f)
                });
            }
        }

        private IEnumerator PlayRoutine()
        {
            var impactSeconds = Mathf.Max(0.08f, _settings.ImpactSeconds);
            var burstSeconds = Mathf.Max(impactSeconds, _settings.RockBurstSeconds);
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
                        var rise = t < 0.18f ? t / 0.18f : 1f;
                        var fade = t > 0.55f ? 1f - (t - 0.55f) / 0.45f : 1f;
                        var sizePulse = Mathf.Lerp(0.55f, 1.15f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.6f)));
                        Fit(_impact, _settings.ImpactWorldSize * sizePulse);
                        _impact.color = new Color(1f, 0.95f, 0.75f, Mathf.Clamp01(rise * fade));
                        _impact.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * -25f);
                    }
                    else
                    {
                        _impact.enabled = false;
                    }
                }

                for (var i = 0; i < _rocks.Count; i++)
                {
                    var rock = _rocks[i];
                    if (rock.Renderer == null)
                    {
                        continue;
                    }

                    rock.Life -= dt;
                    rock.Velocity.y -= rock.Gravity * dt;
                    var p = rock.Renderer.transform.localPosition;
                    p.x += rock.Velocity.x * dt;
                    p.y += rock.Velocity.y * dt;
                    rock.Renderer.transform.localPosition = p;
                    rock.Renderer.transform.Rotate(0f, 0f, rock.Spin * dt);
                    var lifeT = Mathf.Clamp01(rock.Life / Mathf.Max(0.05f, _settings.RockBurstSeconds));
                    var c = rock.Renderer.color;
                    rock.Renderer.color = new Color(c.r, c.g, c.b, lifeT);
                    if (lifeT <= 0.001f)
                    {
                        rock.Renderer.enabled = false;
                    }

                    _rocks[i] = rock;
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
