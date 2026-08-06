using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class RenBulletShotSettings
    {
        public Sprite Head;
        public Sprite Trail;
        public Sprite Impact;
        public Sprite[] FlightFrames;
        public Material AdditiveMaterial;
        public float TravelSeconds = 0.16f;
        public float TravelSpeed = 48f;
        public float ImpactSeconds = 0.12f;
        public float HeadWorldSize = 1.275f;
        public float TrailHeight = 0.825f;
        public int SortingOrder = 40;
        public bool PierceThroughScreen;
        public Vector3? ImpactWorld;
        public RenGlassShatterSettings GlassShatter;
        public Action<Vector3> OnImpact;
    }

    public class RenBulletShotView : MonoBehaviour
    {
        private SpriteRenderer _trail;
        private SpriteRenderer _head;
        private SpriteRenderer _impact;
        private RenBulletShotSettings _settings;

        public static RenBulletShotView Spawn(
            Vector3 from,
            Vector3 to,
            RenBulletShotSettings settings,
            Transform parent = null)
        {
            if (settings == null)
            {
                return null;
            }

            var go = new GameObject("RenBulletShot");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<RenBulletShotView>();
            view._settings = settings;
            view.Build();
            view.StartCoroutine(view.PlayRoutine(from, to));
            return view;
        }

        private bool UsesFlightStrip =>
            _settings.FlightFrames != null
            && _settings.FlightFrames.Length > 0
            && _settings.FlightFrames[0] != null;

        private void Build()
        {
            if (UsesFlightStrip)
            {
                _head = CreateChildRenderer("Flight", ResolveHeadSprite(0f), additive: true);
                _trail = null;
            }
            else
            {
                _trail = CreateChildRenderer("Trail", _settings.Trail, additive: true);
                _head = CreateChildRenderer("Head", _settings.Head, additive: false);
            }

            _impact = CreateChildRenderer("Impact", _settings.Impact, additive: true);
            if (_impact != null)
            {
                _impact.enabled = false;
            }
        }

        private SpriteRenderer CreateChildRenderer(string name, Sprite sprite, bool additive)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = _settings.SortingOrder;
            sr.color = Color.white;
            if (additive && _settings.AdditiveMaterial != null)
            {
                sr.sharedMaterial = _settings.AdditiveMaterial;
            }

            return sr;
        }

        private IEnumerator PlayRoutine(Vector3 from, Vector3 to)
        {
            var impactAt = _settings.ImpactWorld ?? to;
            if (_settings.PierceThroughScreen)
            {
                to = ResolveScreenExit(from, to);
            }

            var delta = to - from;
            delta.z = 0f;
            var distance = delta.magnitude;
            if (distance < 0.05f)
            {
                distance = 0.05f;
                delta = Vector3.right * distance;
            }

            var dir = delta / distance;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, angle));

            FitHead();
            if (!UsesFlightStrip)
            {
                FitTrail(0f);
            }

            if (_impact != null)
            {
                _impact.enabled = false;
                FitImpact();
            }

            var impactDist = Vector3.Distance(from, impactAt);
            var impactFired = false;
            var travel = ResolveTravelSeconds(distance, _settings);

            var elapsed = 0f;
            while (elapsed < travel)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / travel);
                var eased = 1f - (1f - t) * (1f - t);
                var pos = Vector3.Lerp(from, to, eased);
                transform.position = pos;
                ApplyFlightFrame(t);
                if (!UsesFlightStrip)
                {
                    FitTrail(Vector3.Distance(from, pos));
                }

                if (!impactFired
                    && _settings.PierceThroughScreen
                    && Vector3.Distance(from, pos) >= impactDist * 0.98f)
                {
                    impactFired = true;
                    StartCoroutine(PlayImpactAt(impactAt));
                }

                yield return null;
            }

            transform.position = to;
            if (!UsesFlightStrip)
            {
                FitTrail(distance);
            }

            if (_head != null)
            {
                _head.enabled = false;
            }

            if (_trail != null)
            {
                _trail.enabled = false;
            }

            if (!impactFired)
            {
                yield return PlayImpactAt(_settings.PierceThroughScreen ? impactAt : to);
            }

            Destroy(gameObject);
        }

        private bool UsesGlassShatter =>
            _settings.GlassShatter != null
            && (_settings.GlassShatter.Crack != null || _settings.GlassShatter.Shatter != null);

        private IEnumerator PlayImpactAt(Vector3 world)
        {
            _settings.OnImpact?.Invoke(world);
            _settings.OnImpact = null;

            if (UsesGlassShatter)
            {
                RenGlassShatterView.Spawn(world, _settings.GlassShatter, transform.parent);
                if (!_settings.PierceThroughScreen)
                {
                    yield return new WaitForSeconds(
                        RenGlassShatterView.EstimateSeconds(_settings.GlassShatter));
                }

                yield break;
            }

            if (_impact != null && _impact.sprite != null)
            {
                yield return PlayImpactFlash(world);
            }
        }

        private IEnumerator PlayImpactFlash(Vector3 world)
        {
            if (_impact == null || _impact.sprite == null)
            {
                yield break;
            }

            var flash = _impact;
            if (_settings.PierceThroughScreen)
            {
                flash.transform.SetParent(null, true);
            }

            flash.enabled = true;
            flash.transform.position = world;
            FitImpactRenderer(flash);
            var impactSeconds = Mathf.Max(0.01f, _settings.ImpactSeconds);
            var impactElapsed = 0f;
            while (impactElapsed < impactSeconds)
            {
                if (flash == null)
                {
                    yield break;
                }

                impactElapsed += Time.deltaTime;
                var u = Mathf.Clamp01(impactElapsed / impactSeconds);
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, u);
                var c = flash.color;
                flash.color = new Color(c.r, c.g, c.b, 1f - u);
                yield return null;
            }

            if (_settings.PierceThroughScreen)
            {
                if (flash != null)
                {
                    Destroy(flash.gameObject);
                }

                _impact = null;
            }
            else if (flash != null)
            {
                flash.enabled = false;
            }
        }

        private void FitImpactRenderer(SpriteRenderer impact)
        {
            if (impact == null || impact.sprite == null)
            {
                return;
            }

            var sprite = impact.sprite;
            var worldH = sprite.bounds.size.y;
            var scale = worldH > 0.001f ? _settings.HeadWorldSize * 1.1f / worldH : 1f;
            impact.transform.localScale = Vector3.one * scale;
            impact.sortingOrder = _settings.SortingOrder + 1;
        }

        public static float ResolveTravelSeconds(float distance, RenBulletShotSettings settings)
        {
            if (settings == null)
            {
                return 0.16f;
            }

            if (settings.PierceThroughScreen && settings.TravelSpeed > 0.01f)
            {
                return Mathf.Clamp(distance / settings.TravelSpeed, 0.08f, 0.45f);
            }

            return Mathf.Max(0.01f, settings.TravelSeconds);
        }

        public static float EstimatePresentationSeconds(
            Vector3 from,
            Vector3 to,
            RenBulletShotSettings settings)
        {
            if (settings == null)
            {
                return 0.2f;
            }

            var end = settings.PierceThroughScreen ? ResolveScreenExit(from, to) : to;
            var distance = Vector3.Distance(from, end);
            var travel = ResolveTravelSeconds(distance, settings);
            var glass = settings.GlassShatter;
            var hasGlass = glass != null && (glass.Crack != null || glass.Shatter != null);
            if (settings.PierceThroughScreen)
            {
                return hasGlass ? travel + RenGlassShatterView.EstimateSeconds(glass) : travel;
            }

            if (hasGlass)
            {
                return travel + RenGlassShatterView.EstimateSeconds(glass);
            }

            return travel + Mathf.Max(0.01f, settings.ImpactSeconds);
        }

        public static Vector3 ResolveScreenExit(Vector3 from, Vector3 through)
        {
            var delta = through - from;
            delta.z = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                delta = Vector3.right;
            }

            var dir = delta.normalized;
            var cam = Camera.main;
            if (cam == null)
            {
                return from + dir * 18f;
            }

            var depth = Mathf.Abs(from.z - cam.transform.position.z);
            if (depth < 0.01f)
            {
                depth = 10f;
            }

            var bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            var tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            var pad = 1.25f;
            var minX = Mathf.Min(bl.x, tr.x) - pad;
            var maxX = Mathf.Max(bl.x, tr.x) + pad;
            var minY = Mathf.Min(bl.y, tr.y) - pad;
            var maxY = Mathf.Max(bl.y, tr.y) + pad;

            var tExit = 40f;
            if (Mathf.Abs(dir.x) > 0.001f)
            {
                var tx = dir.x > 0f ? (maxX - from.x) / dir.x : (minX - from.x) / dir.x;
                if (tx > 0f)
                {
                    tExit = Mathf.Min(tExit, tx);
                }
            }

            if (Mathf.Abs(dir.y) > 0.001f)
            {
                var ty = dir.y > 0f ? (maxY - from.y) / dir.y : (minY - from.y) / dir.y;
                if (ty > 0f)
                {
                    tExit = Mathf.Min(tExit, ty);
                }
            }

            return from + dir * Mathf.Max(tExit, Vector3.Distance(from, through) + 2f);
        }

        private void ApplyFlightFrame(float t)
        {
            if (_head == null)
            {
                return;
            }

            var sprite = ResolveHeadSprite(t);
            if (sprite != null && _head.sprite != sprite)
            {
                _head.sprite = sprite;
                FitHead();
            }
        }

        private Sprite ResolveHeadSprite(float t)
        {
            var frames = _settings.FlightFrames;
            if (frames != null && frames.Length > 0)
            {
                var index = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
                if (frames[index] != null)
                {
                    return frames[index];
                }
            }

            return _settings.Head;
        }

        private void FitHead()
        {
            if (_head == null || _head.sprite == null)
            {
                return;
            }

            var sprite = _head.sprite;
            var worldH = sprite.bounds.size.y;
            var targetH = UsesFlightStrip ? _settings.TrailHeight * 1.35f : _settings.HeadWorldSize;
            var scale = worldH > 0.001f ? targetH / worldH : 1f;
            _head.transform.localScale = new Vector3(scale, scale, 1f);
            if (UsesFlightStrip)
            {
                var worldW = sprite.bounds.size.x * scale;
                _head.transform.localPosition = new Vector3(-worldW * 0.35f, 0f, 0f);
            }
            else
            {
                _head.transform.localPosition = Vector3.zero;
            }
        }

        private void FitTrail(float traveled)
        {
            if (_trail == null || _trail.sprite == null)
            {
                return;
            }

            var length = Mathf.Max(0.15f, traveled);
            var sprite = _trail.sprite;
            var nativeW = Mathf.Max(0.001f, sprite.bounds.size.x);
            var nativeH = Mathf.Max(0.001f, sprite.bounds.size.y);
            var scaleX = length / nativeW;
            var scaleY = _settings.TrailHeight / nativeH;
            _trail.drawMode = SpriteDrawMode.Simple;
            _trail.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            _trail.transform.localPosition = new Vector3(-length * 0.5f, 0f, 0f);
            _trail.sortingOrder = _settings.SortingOrder - 1;
        }

        private void FitImpact()
        {
            if (_impact == null || _impact.sprite == null)
            {
                return;
            }

            var sprite = _impact.sprite;
            var worldH = sprite.bounds.size.y;
            var scale = worldH > 0.001f ? _settings.HeadWorldSize * 1.1f / worldH : 1f;
            _impact.transform.localScale = Vector3.one * scale;
            _impact.sortingOrder = _settings.SortingOrder + 1;
        }
    }
}
