using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CodaArcVolleySettings
    {
        public Sprite Charge;
        public Sprite Bolt;
        public Sprite Impact;
        public Material AdditiveMaterial;
        public float ChargeWorldSize = 2.1f;
        public float ChargeSeconds = 0.45f;
        public float BoltWorldSize = 1.55f;
        public float ImpactWorldSize = 2.2f;
        public float ImpactSeconds = 0.22f;
        public float FinaleImpactScale = 1.55f;
        public float FinaleImpactSeconds = 0.42f;
        public float AftermathHoldSeconds = 0.2f;
        public float FlightSeconds = 0.32f;
        public float StaggerSeconds = 0.045f;
        public int BoltCount = 5;
        public float ArcSpreadY = 1.55f;
        public float ControlBulge = 1.35f;
        public float BoltFacingOffsetDegrees = 180f;
        public bool InvertArcNormal;
        public int SortingOrder = 47;
        public Action OnImpact;
    }

    public class CodaArcVolleyView : MonoBehaviour
    {
        private CodaArcVolleySettings _settings;
        private SpriteRenderer _charge;
        private Vector3 _from;
        private Vector3 _through;
        private bool _impactFired;
        private int _activeBolts;

        public static CodaArcVolleyView Spawn(
            Vector3 from,
            Vector3 through,
            CodaArcVolleySettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Bolt == null && settings.Charge == null))
            {
                return null;
            }

            var go = new GameObject("CodaArcVolley");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<CodaArcVolleyView>();
            view._settings = settings;
            view._from = from;
            view._through = through;
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        private void Build()
        {
            if (_settings.Charge == null)
            {
                return;
            }

            _charge = CreateRenderer("Charge", _settings.Charge, _settings.SortingOrder + 1);
            _charge.transform.position = _from;
            Fit(_charge, _settings.ChargeWorldSize * 0.4f);
            _charge.color = new Color(1f, 1f, 1f, 0f);
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
                    var pulse = 1f + Mathf.Sin(elapsed * 26f) * 0.16f * t;
                    Fit(_charge, _settings.ChargeWorldSize * Mathf.Lerp(0.35f, 1.28f, eased) * pulse);
                    _charge.color = new Color(0.65f, 0.88f, 1f, Mathf.Lerp(0.15f, 1f, eased));
                    _charge.transform.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 120f);
                }

                yield return null;
            }

            if (_charge != null)
            {
                _charge.enabled = false;
            }

            var count = Mathf.Clamp(_settings.BoltCount, 1, 8);
            var stagger = Mathf.Max(0f, _settings.StaggerSeconds);
            var flight = Mathf.Max(0.08f, _settings.FlightSeconds);
            _activeBolts = count;
            for (var i = 0; i < count; i++)
            {
                StartCoroutine(FlyBolt(i, count, flight));
                if (stagger > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(stagger);
                }
            }

            while (_activeBolts > 0)
            {
                yield return null;
            }

            CombatImpactFeel.PunchUltimateNow();
            yield return PlayFinaleImpact(_through);

            if (_settings.AftermathHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(_settings.AftermathHoldSeconds);
            }

            Destroy(gameObject);
        }

        private IEnumerator FlyBolt(int index, int count, float flightSeconds)
        {
            ResolveArc(index, count, out var start, out var control, out var end);
            SpriteRenderer bolt = null;
            if (_settings.Bolt != null)
            {
                bolt = CreateRenderer("Bolt_" + index, _settings.Bolt, _settings.SortingOrder);
                bolt.transform.position = start;
                Fit(bolt, _settings.BoltWorldSize * 0.7f);
            }

            var elapsed = 0f;
            var prev = start;
            while (elapsed < flightSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / flightSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                var pos = QuadBezier(start, control, end, eased);
                if (bolt != null)
                {
                    var delta = pos - prev;
                    if (delta.sqrMagnitude > 0.00001f)
                    {
                        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
                                    + _settings.BoltFacingOffsetDegrees;
                        bolt.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, angle));
                    }
                    else
                    {
                        bolt.transform.position = pos;
                    }

                    Fit(bolt, _settings.BoltWorldSize * Mathf.Lerp(0.75f, 1.15f, t));
                    bolt.color = Color.white;
                }

                prev = pos;
                yield return null;
            }

            if (bolt != null)
            {
                bolt.enabled = false;
            }

            FireImpactOnce();
            yield return PlayImpact(_through);

            _activeBolts = Mathf.Max(0, _activeBolts - 1);
        }

        private IEnumerator PlayImpact(Vector3 at)
        {
            yield return PlayImpactBurst(
                at,
                _settings.ImpactWorldSize,
                _settings.ImpactSeconds,
                _settings.SortingOrder + 3);
        }

        private IEnumerator PlayFinaleImpact(Vector3 at)
        {
            var scale = Mathf.Max(1f, _settings.FinaleImpactScale);
            yield return PlayImpactBurst(
                at,
                _settings.ImpactWorldSize * scale,
                Mathf.Max(0.12f, _settings.FinaleImpactSeconds),
                _settings.SortingOrder + 5);
        }

        private IEnumerator PlayImpactBurst(Vector3 at, float worldSize, float seconds, int sortingOrder)
        {
            var sprite = _settings.Impact;
            if (sprite == null)
            {
                yield break;
            }

            var impact = CreateRenderer("Impact", sprite, sortingOrder);
            impact.transform.position = at;
            Fit(impact, worldSize * 0.65f);
            impact.enabled = true;
            var duration = Mathf.Max(0.08f, seconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / duration);
                Fit(impact, worldSize * Mathf.Lerp(0.65f, 1.55f, Mathf.Min(1f, u * 1.35f)));
                impact.color = new Color(1f, 0.9f, 1f, 1f - u * u);
                impact.transform.localRotation = Quaternion.Euler(0f, 0f, elapsed * 160f);
                yield return null;
            }

            if (impact != null)
            {
                Destroy(impact.gameObject);
            }
        }

        private void FireImpactOnce()
        {
            if (_impactFired)
            {
                return;
            }

            _impactFired = true;
            CombatImpactFeel.PunchMediumNow();
            _settings.OnImpact?.Invoke();
        }

        private void ResolveArc(int index, int count, out Vector3 start, out Vector3 control, out Vector3 end)
        {
            var dir = _through - _from;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.right;
            }

            dir.Normalize();
            var normal = new Vector3(-dir.y, dir.x, 0f);
            if (_settings.InvertArcNormal)
            {
                normal = -normal;
            }

            var t = count <= 1 ? 0.5f : index / (float)(count - 1);
            var signed = Mathf.Lerp(1f, -1f, t);
            var spread = _settings.ArcSpreadY * signed;
            var bulge = _settings.ControlBulge * (0.75f + 0.5f * (1f - Mathf.Abs(signed)));
            var side = signed >= 0f ? 1f : -1f;

            start = _from + normal * spread * 0.35f;
            end = _through;
            var mid = Vector3.Lerp(start, end, 0.4f);
            control = mid + normal * (bulge * side);
            if (Mathf.Abs(signed) < 0.08f)
            {
                control = mid + Vector3.up * bulge * 0.9f;
            }
        }

        private static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
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
