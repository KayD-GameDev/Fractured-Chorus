using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CodaBeamShotSettings
    {
        public Sprite Charge;
        public Sprite Beam;
        public Sprite Impact;
        public Material AdditiveMaterial;
        public float ChargeWorldSize = 1.8f;
        public float ChargeSeconds = 0.42f;
        public float BeamThickness = 1.35f;
        public float BeamHoldSeconds = 0.28f;
        public float BeamFadeSeconds = 0.18f;
        public float PiercePast = 4.5f;
        public bool PierceThroughMap = true;
        public float ImpactWorldSize = 2.6f;
        public float ImpactSeconds = 0.24f;
        public float AimHeight = 0.75f;
        public int SortingOrder = 46;
        public Action OnImpact;
    }

    public class CodaBeamShotView : MonoBehaviour
    {
        private CodaBeamShotSettings _settings;
        private SpriteRenderer _charge;
        private SpriteRenderer _beam;
        private SpriteRenderer _impact;
        private Vector3 _from;
        private Vector3 _through;
        private Vector3 _end;

        public static CodaBeamShotView Spawn(
            Vector3 from,
            Vector3 through,
            CodaBeamShotSettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Beam == null && settings.Charge == null))
            {
                return null;
            }

            var go = new GameObject("CodaBeamShot");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var dir = through - from;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.right;
            }

            dir.Normalize();
            var view = go.AddComponent<CodaBeamShotView>();
            view._settings = settings;
            view._from = from;
            view._through = through;
            view._end = ResolvePierceEnd(from, through, dir, settings);
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        private static Vector3 ResolvePierceEnd(
            Vector3 from,
            Vector3 through,
            Vector3 dir,
            CodaBeamShotSettings settings)
        {
            var minPast = Mathf.Max(1.5f, settings.PiercePast);
            if (!settings.PierceThroughMap)
            {
                return through + dir * minPast;
            }

            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                var halfW = cam.orthographicSize * Mathf.Max(0.01f, cam.aspect);
                var centerX = cam.transform.position.x;
                var edgeX = dir.x >= 0f ? centerX + halfW + 1.5f : centerX - halfW - 1.5f;
                var dx = edgeX - from.x;
                if (Mathf.Abs(dir.x) > 0.001f && Mathf.Sign(dx) == Mathf.Sign(dir.x))
                {
                    var dist = Mathf.Abs(dx / dir.x);
                    dist = Mathf.Max(dist, Vector2.Distance(from, through) + minPast * 0.35f);
                    return from + dir * dist;
                }
            }

            var fallback = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(through.x, through.y))
                           + minPast;
            return from + dir * Mathf.Max(minPast, fallback);
        }

        private void Build()
        {
            if (_settings.Charge != null)
            {
                _charge = CreateRenderer("Charge", _settings.Charge, _settings.SortingOrder + 1);
                _charge.transform.position = _from;
                Fit(_charge, _settings.ChargeWorldSize * 0.35f);
                _charge.color = new Color(1f, 1f, 1f, 0f);
            }

            if (_settings.Beam != null)
            {
                _beam = CreateRenderer("Beam", _settings.Beam, _settings.SortingOrder);
                _beam.enabled = false;
            }

            if (_settings.Impact != null)
            {
                _impact = CreateRenderer("Impact", _settings.Impact, _settings.SortingOrder + 2);
                _impact.transform.position = _through;
                Fit(_impact, _settings.ImpactWorldSize * 0.4f);
                _impact.color = new Color(1f, 1f, 1f, 0f);
                _impact.enabled = false;
            }
        }

        private IEnumerator PlayRoutine()
        {
            var chargeSeconds = Mathf.Max(0.08f, _settings.ChargeSeconds);
            var elapsed = 0f;
            while (elapsed < chargeSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / chargeSeconds);
                var eased = t * t * (3f - 2f * t);
                if (_charge != null)
                {
                    var pulse = 1f + Mathf.Sin(elapsed * 22f) * 0.08f * t;
                    Fit(_charge, _settings.ChargeWorldSize * Mathf.Lerp(0.35f, 1.05f, eased) * pulse);
                    _charge.color = new Color(0.75f, 0.95f, 1f, Mathf.Lerp(0.15f, 1f, eased));
                    _charge.transform.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 90f);
                    _charge.transform.position = Vector3.Lerp(_from - (_through - _from).normalized * 0.35f, _from, eased);
                }

                yield return null;
            }

            if (_charge != null)
            {
                _charge.enabled = false;
            }

            FireBeamLayout();
            if (_beam != null)
            {
                _beam.enabled = true;
                _beam.color = Color.white;
            }

            if (_impact != null)
            {
                _impact.enabled = true;
            }

            _settings.OnImpact?.Invoke();

            var hold = Mathf.Max(0.05f, _settings.BeamHoldSeconds);
            elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / hold);
                var thickness = _settings.BeamThickness * (1f + Mathf.Sin(elapsed * 40f) * 0.08f);
                ApplyBeamScale(thickness);
                if (_impact != null)
                {
                    var size = _settings.ImpactWorldSize * Mathf.Lerp(0.55f, 1.15f, Mathf.Min(1f, t * 2.2f));
                    Fit(_impact, size);
                    _impact.color = new Color(1f, 0.9f, 1f, Mathf.Lerp(1f, 0.55f, t));
                    _impact.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 50f);
                }

                yield return null;
            }

            var fade = Mathf.Max(0.05f, _settings.BeamFadeSeconds);
            elapsed = 0f;
            var beamA0 = _beam != null ? _beam.color.a : 0f;
            var impactA0 = _impact != null ? _impact.color.a : 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                var u = 1f - Mathf.Clamp01(elapsed / fade);
                if (_beam != null)
                {
                    var c = _beam.color;
                    _beam.color = new Color(c.r, c.g, c.b, beamA0 * u);
                    ApplyBeamScale(_settings.BeamThickness * Mathf.Lerp(0.7f, 1f, u));
                }

                if (_impact != null)
                {
                    var c = _impact.color;
                    _impact.color = new Color(c.r, c.g, c.b, impactA0 * u);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void FireBeamLayout()
        {
            if (_beam == null || _beam.sprite == null)
            {
                return;
            }

            var mid = (_from + _end) * 0.5f;
            var delta = _end - _from;
            delta.z = 0f;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            _beam.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, 0f, angle));
            ApplyBeamScale(_settings.BeamThickness);
        }

        private void ApplyBeamScale(float thickness)
        {
            if (_beam == null || _beam.sprite == null)
            {
                return;
            }

            var length = Vector2.Distance(
                new Vector2(_from.x, _from.y),
                new Vector2(_end.x, _end.y));
            var native = _beam.sprite.bounds.size;
            var sx = native.x > 0.001f ? length / native.x : 1f;
            var sy = native.y > 0.001f ? thickness / native.y : 1f;
            _beam.transform.localScale = new Vector3(sx, sy, 1f);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
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
