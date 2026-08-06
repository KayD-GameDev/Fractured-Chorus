using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class RenGlassShatterSettings
    {
        public Sprite Crack;
        public Sprite Shatter;
        public Material AdditiveMaterial;
        public float WorldSize = 2.55f;
        public float CrackInSeconds = 0.04f;
        public float CrackHoldSeconds = 0.06f;
        public float ShatterSeconds = 0.28f;
        public int SortingOrder = 42;
        public Action OnShatter;
    }

    public class RenGlassShatterView : MonoBehaviour
    {
        private SpriteRenderer _crack;
        private SpriteRenderer _shatter;
        private RenGlassShatterSettings _settings;

        public static float EstimateSeconds(RenGlassShatterSettings settings)
        {
            if (settings == null)
            {
                return 0.45f;
            }

            return Mathf.Max(0.01f, settings.CrackInSeconds)
                   + Mathf.Max(0.01f, settings.CrackHoldSeconds)
                   + Mathf.Max(0.01f, settings.ShatterSeconds);
        }

        public static RenGlassShatterView Spawn(
            Vector3 world,
            RenGlassShatterSettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Crack == null && settings.Shatter == null))
            {
                return null;
            }

            var go = new GameObject("RenGlassShatter");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = world;
            var view = go.AddComponent<RenGlassShatterView>();
            view._settings = settings;
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        private void Build()
        {
            _crack = CreateChild("Crack", _settings.Crack, _settings.SortingOrder);
            _shatter = CreateChild("Shatter", _settings.Shatter, _settings.SortingOrder + 1);
            if (_crack != null)
            {
                _crack.enabled = false;
            }

            if (_shatter != null)
            {
                _shatter.enabled = false;
            }
        }

        private SpriteRenderer CreateChild(string name, Sprite sprite, int order)
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

            Fit(sr, _settings.WorldSize * 0.72f);
            return sr;
        }

        private IEnumerator PlayRoutine()
        {
            if (_crack != null)
            {
                _crack.enabled = true;
                var crackIn = Mathf.Max(0.01f, _settings.CrackInSeconds);
                var elapsed = 0f;
                while (elapsed < crackIn)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / crackIn);
                    var eased = t * t * (3f - 2f * t);
                    Fit(_crack, _settings.WorldSize * Mathf.Lerp(0.55f, 1f, eased));
                    var c = _crack.color;
                    _crack.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.35f, 1f, eased));
                    yield return null;
                }

                Fit(_crack, _settings.WorldSize);
                var hold = Mathf.Max(0.01f, _settings.CrackHoldSeconds);
                elapsed = 0f;
                while (elapsed < hold)
                {
                    elapsed += Time.deltaTime;
                    var pulse = 1f + Mathf.Sin(elapsed * 28f) * 0.02f;
                    Fit(_crack, _settings.WorldSize * pulse);
                    yield return null;
                }
            }

            _settings.OnShatter?.Invoke();

            if (_crack != null)
            {
                _crack.enabled = false;
            }

            if (_shatter != null)
            {
                _shatter.enabled = true;
                var shatterSeconds = Mathf.Max(0.01f, _settings.ShatterSeconds);
                var elapsed = 0f;
                while (elapsed < shatterSeconds)
                {
                    elapsed += Time.deltaTime;
                    var u = Mathf.Clamp01(elapsed / shatterSeconds);
                    var eased = 1f - (1f - u) * (1f - u);
                    Fit(_shatter, _settings.WorldSize * Mathf.Lerp(0.7f, 1.55f, eased));
                    var c = _shatter.color;
                    _shatter.color = new Color(c.r, c.g, c.b, 1f - eased);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }

        private static void Fit(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? worldSize / native : 1f;
            sr.transform.localScale = Vector3.one * scale;
        }
    }
}
