using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public enum BossSwordShotMode
    {
        Hit = 0,
        Deflect = 1,
        Vanish = 2
    }

    public sealed class BossSwordShotSettings
    {
        public Sprite Sword;
        public Sprite Impact;
        public Material AdditiveMaterial;
        public float TravelSeconds = 0.32f;
        public float ImpactSeconds = 0.18f;
        public float DeflectSeconds = 0.22f;
        public float SwordWorldLength = 1.9f;
        public float ImpactWorldSize = 1.7f;
        public float DeflectTravel = 2.4f;
        public float SpriteFacingOffsetDegrees;
        public bool ProjectileAdditive;
        public int SortingOrder = 42;
    }

    public class BossSwordShotView : MonoBehaviour
    {
        private SpriteRenderer _sword;
        private SpriteRenderer _impact;
        private BossSwordShotSettings _settings;
        private BossSwordShotMode _mode;

        public static BossSwordShotView Spawn(
            Vector3 from,
            Vector3 to,
            BossSwordShotSettings settings,
            BossSwordShotMode mode = BossSwordShotMode.Hit,
            Transform parent = null)
        {
            if (settings?.Sword == null)
            {
                return null;
            }

            var go = new GameObject("BossSwordShot");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<BossSwordShotView>();
            view._settings = settings;
            view._mode = mode;
            view.Build();
            view.StartCoroutine(view.PlayRoutine(from, to));
            return view;
        }

        private void Build()
        {
            _sword = CreateChildRenderer("Sword", _settings.Sword, additive: _settings.ProjectileAdditive);
            _impact = CreateChildRenderer("Impact", _settings.Impact, additive: true);
            if (_impact != null)
            {
                _impact.enabled = false;
            }
        }

        private SpriteRenderer CreateChildRenderer(string name, Sprite sprite, bool additive)
        {
            if (sprite == null)
            {
                return null;
            }

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
            var delta = to - from;
            delta.z = 0f;
            var distance = Mathf.Max(0.05f, delta.magnitude);
            var dir = delta / distance;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg
                        + _settings.SpriteFacingOffsetDegrees;
            transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, angle));
            FitSword();

            var travel = Mathf.Max(0.01f, _settings.TravelSeconds);
            if (_mode == BossSwordShotMode.Deflect)
            {
                yield return TravelTo(from, to, travel * 0.55f);
                var contact = transform.position;
                yield return PlayImpactAt(contact);
                yield return DeflectAway(contact, dir);
                Destroy(gameObject);
                yield break;
            }

            if (_mode == BossSwordShotMode.Vanish)
            {
                yield return TravelTo(from, to, travel * 0.55f);
                var contact = transform.position;
                yield return VanishAt(contact);
                Destroy(gameObject);
                yield break;
            }

            yield return TravelTo(from, to, travel);
            if (_sword != null)
            {
                _sword.enabled = false;
            }

            yield return PlayImpactAt(to);
            Destroy(gameObject);
        }

        private IEnumerator TravelTo(Vector3 from, Vector3 to, float seconds)
        {
            var elapsed = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(from, to, eased);
                yield return null;
            }

            transform.position = to;
        }

        private IEnumerator PlayImpactAt(Vector3 world)
        {
            if (_impact == null || _impact.sprite == null)
            {
                yield break;
            }

            _impact.enabled = true;
            _impact.transform.position = world;
            FitImpact();

            var seconds = Mathf.Max(0.01f, _settings.ImpactSeconds);
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                _impact.transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.35f, u);
                var c = _impact.color;
                _impact.color = new Color(c.r, c.g, c.b, 1f - u);
                yield return null;
            }

            _impact.enabled = false;
        }

        private IEnumerator VanishAt(Vector3 contact)
        {
            var baseScale = _sword != null ? _sword.transform.localScale : Vector3.one;
            var seconds = Mathf.Max(0.01f, _settings.ImpactSeconds);
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                if (_sword != null)
                {
                    _sword.transform.localScale = baseScale * Mathf.Lerp(1f, 0.12f, u);
                    var c = _sword.color;
                    _sword.color = new Color(c.r, c.g, c.b, 1f - u);
                }

                yield return null;
            }

            if (_sword != null)
            {
                _sword.enabled = false;
            }

            yield return PlayImpactAt(contact);
        }

        private IEnumerator DeflectAway(Vector3 contact, Vector3 inboundDir)
        {
            if (_sword != null)
            {
                _sword.enabled = true;
            }

            var side = Random.value < 0.5f ? 1f : -1f;
            var deflectDir = Vector3.Normalize(new Vector3(-inboundDir.y, inboundDir.x, 0f) * side + inboundDir * -0.35f);
            if (deflectDir.sqrMagnitude < 0.0001f)
            {
                deflectDir = Vector3.up;
            }

            var end = contact + deflectDir * Mathf.Max(0.5f, _settings.DeflectTravel);
            var angle = Mathf.Atan2(deflectDir.y, deflectDir.x) * Mathf.Rad2Deg
                        + _settings.SpriteFacingOffsetDegrees;
            var seconds = Mathf.Max(0.01f, _settings.DeflectSeconds);
            var elapsed = 0f;
            var spin = side * 420f;

            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = t * t;
                transform.position = Vector3.Lerp(contact, end, eased);
                transform.rotation = Quaternion.Euler(0f, 0f, angle + spin * t);
                if (_sword != null)
                {
                    var c = _sword.color;
                    _sword.color = new Color(c.r, c.g, c.b, 1f - t);
                }

                yield return null;
            }
        }

        private void FitSword()
        {
            if (_sword == null || _sword.sprite == null)
            {
                return;
            }

            var sprite = _sword.sprite;
            var worldLen = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            var scale = worldLen > 0.001f ? _settings.SwordWorldLength / worldLen : 1f;
            _sword.transform.localScale = new Vector3(scale, scale, 1f);
            _sword.transform.localPosition = Vector3.zero;
            _sword.sortingOrder = _settings.SortingOrder;
        }

        private void FitImpact()
        {
            if (_impact == null || _impact.sprite == null)
            {
                return;
            }

            var sprite = _impact.sprite;
            var worldH = sprite.bounds.size.y;
            var scale = worldH > 0.001f ? _settings.ImpactWorldSize / worldH : 1f;
            _impact.transform.localScale = Vector3.one * scale;
            _impact.sortingOrder = _settings.SortingOrder + 2;
        }
    }
}
